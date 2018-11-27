package main

import (
	"context"
	"flag"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"sync"
	"time"

	homedir "github.com/mitchellh/go-homedir"
	"github.com/proepkes/speeddate/src/spawnsvc"
	clientset "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/client/informers/externalversions"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/gs"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/signals"

	extclientset "k8s.io/apiextensions-apiserver/pkg/client/clientset/clientset"
	kubeinformers "k8s.io/client-go/informers"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"

	armada "github.com/proepkes/speeddate/src/spawnsvc/gen/armada"
	armadasvr "github.com/proepkes/speeddate/src/spawnsvc/gen/http/armada/server"
	swaggersvr "github.com/proepkes/speeddate/src/spawnsvc/gen/http/swagger/server"
	goahttp "goa.design/goa/http"
	"goa.design/goa/http/middleware"
)

// Runner ...
type Runner interface {
	Run(workers int, stop <-chan struct{}) error
}

func main() {
	// Define command line flags, add any other flag required to configure
	// the service.
	var (
		addr = flag.String("listen", ":8001", "HTTP listen `address`")
		dbg  = flag.Bool("debug", false, "Log request and response bodies")
	)
	flag.Parse()

	// Setup logger and goa log adapter. Replace logger with your own using
	// your log package of choice.
	var (
		adapter middleware.Logger
		logger  *log.Logger
	)
	{
		logger = log.New(os.Stderr, "[spawnsvc] ", log.Ltime)
		adapter = middleware.NewLogger(logger)
	}

	// get the Kubernetes client for connectivity
	k8sClient, extClient, client := createClientSets()

	kubeInformerFactory := kubeinformers.NewSharedInformerFactory(k8sClient, time.Second*30)
	informerFactory := externalversions.NewSharedInformerFactory(client, time.Second*30)

	allocationMutex := &sync.Mutex{}
	gsController := gs.NewController(allocationMutex, k8sClient, kubeInformerFactory, extClient, client, informerFactory)

	// Create the structs that implement the services.
	var (
		armadaSvc armada.Service
	)
	{
		armadaSvc = spawnsvc.NewArmada(logger, gsController)
	}

	// Wrap the services in endpoints that can be invoked from other
	// services potentially running in different processes.
	var (
		armadaEndpoints *armada.Endpoints
	)
	{
		armadaEndpoints = armada.NewEndpoints(armadaSvc)
	}

	// Provide the transport specific request decoder and response encoder.
	// The goa http package has built-in support for JSON, XML and gob.
	// Other encodings can be used by providing the corresponding functions,
	// see goa.design/encoding.
	var (
		dec = goahttp.RequestDecoder
		enc = goahttp.ResponseEncoder
	)
	// Build the service HTTP request multiplexer and configure it to serve
	// HTTP requests to the service endpoints.
	var mux goahttp.Muxer
	{
		mux = goahttp.NewMuxer()
	}
	// Wrap the endpoints with the transport specific layers. The generated
	// server packages contains code generated from the design which maps
	// the service input and output data structures to HTTP requests and
	// responses.
	var (
		armadaServer  *armadasvr.Server
		swaggerServer *swaggersvr.Server
	)
	{
		eh := ErrorHandler(logger)
		armadaServer = armadasvr.New(armadaEndpoints, mux, dec, enc, eh)
		swaggerServer = swaggersvr.New(nil, mux, dec, enc, eh)
	}
	// Configure the mux.
	armadasvr.Mount(mux, armadaServer)
	swaggersvr.Mount(mux, swaggerServer)

	// Wrap the multiplexer with additional middlewares. Middlewares mounted
	// here apply to all the service endpoints.
	var handler http.Handler = mux
	{
		if *dbg {
			handler = middleware.Debug(mux, os.Stdout)(handler)
		}
		handler = middleware.Log(adapter)(handler)
		handler = middleware.RequestID()(handler)
	}

	// Create channel used by both the signal handler and server goroutines
	// to notify the main goroutine when to stop the server.
	errc := make(chan error)
	// Setup interrupt handler. This optional step configures the process so
	// that SIGINT and SIGTERM signals cause the service to stop gracefully.
	go func() {
		c := make(chan os.Signal, 1)
		signal.Notify(c, os.Interrupt)
		errc <- fmt.Errorf("%s", <-c)
	}()

	stopCh := signals.SetupSignalHandler()

	kubeInformerFactory.Start(stopCh)
	informerFactory.Start(stopCh)

	for _, r := range []Runner{gsController} {
		go func(runner Runner) {
			if err := runner.Run(1, stopCh); err != nil {
				log.Fatalf("Error running controller: %s", err.Error())
			}
		}(r)
	}

	// Start HTTP server using default configuration, change the code to
	// configure the server as required by your service.
	srv := &http.Server{Addr: *addr, Handler: handler}
	go func() {
		for _, m := range armadaServer.Mounts {
			logger.Printf("method %q mounted on %s %s", m.Method, m.Verb, m.Pattern)
		}
		for _, m := range swaggerServer.Mounts {
			logger.Printf("file %q mounted on %s %s", m.Method, m.Verb, m.Pattern)
		}
		logger.Printf("listening on %s", *addr)
		errc <- srv.ListenAndServe()
	}()

	// Wait for signal.
	logger.Printf("exiting (%v)", <-errc)
	// Shutdown gracefully with a 30s timeout.
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()
	srv.Shutdown(ctx)
	logger.Println("exited")
}

// ErrorHandler returns a function that writes and logs the given error.
// The function also writes and logs the error unique ID so that it's possible
// to correlate.
func ErrorHandler(logger *log.Logger) func(context.Context, http.ResponseWriter, error) {
	return func(ctx context.Context, w http.ResponseWriter, err error) {
		id := ctx.Value(middleware.RequestIDKey).(string)
		w.Write([]byte("[" + id + "] encoding: " + err.Error()))
		logger.Printf("[%s] ERROR: %s", id, err.Error())
	}
}

func createClientSets() (kubernetes.Interface, *extclientset.Clientset, *clientset.Clientset) {
	_, hasHost := os.LookupEnv("KUBERNETES_SERVICE_HOST")
	_, hasPort := os.LookupEnv("KUBERNETES_SERVICE_PORT")

	var err error
	var config *rest.Config
	if hasHost && hasPort {
		// We are most likely inside a kuberneter cluster
		config, err = rest.InClusterConfig()
		if err != nil {
			log.Fatalf("getClusterConfig (inCluster): %v", err)
		}
	} else {
		home, err := homedir.Dir()
		if err != nil {
			fmt.Println(home)
			os.Exit(1)
		}

		// construct the path to resolve to `~/.kube/config`
		kubeConfigPath := home + "/.kube/config"

		// create the config from the path
		config, err = clientcmd.BuildConfigFromFlags("", kubeConfigPath)
		if err != nil {
			log.Fatalf("getClusterConfig (local): %v", err)
		}
	}
	return createClients(config)
}

func createClients(config *rest.Config) (kubernetes.Interface, *extclientset.Clientset, *clientset.Clientset) {

	// generate the client based off of the config
	k8sClient, err := kubernetes.NewForConfig(config)
	if err != nil {
		log.Fatalf("getClusterConfig: %v", err)
	}

	extClient, err := extclientset.NewForConfig(config)
	if err != nil {
		log.Fatalf("Could not create the api extension clientset")
	}

	client, err := clientset.NewForConfig(config)
	if err != nil {
		log.Fatalf("Error building example clientset: %s", err.Error())
	}

	log.Println("Successfully constructed clients")

	return k8sClient, extClient, client
}

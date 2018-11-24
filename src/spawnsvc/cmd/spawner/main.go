package main

import (
	"context"
	"flag"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"time"

	homedir "github.com/mitchellh/go-homedir"
	"github.com/proepkes/speeddate/src/spawnsvc"
	armada "github.com/proepkes/speeddate/src/spawnsvc/gen/armada"
	armadasvr "github.com/proepkes/speeddate/src/spawnsvc/gen/http/armada/server"
	swaggersvr "github.com/proepkes/speeddate/src/spawnsvc/gen/http/swagger/server"
	clientset "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/gs"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/signals"
	goahttp "goa.design/goa/http"
	"goa.design/goa/http/middleware"

	glog "github.com/golang/glog"
	informers "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/informers/externalversions"
	kubeinformers "k8s.io/client-go/informers"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
	"k8s.io/klog"

	"github.com/go-kit/kit/log"
	"github.com/go-kit/kit/log/level"
)

// retrieve the Kubernetes cluster client from outside of the cluster
func getClientLocal() (kubernetes.Interface, *clientset.Clientset) {
	home, err := homedir.Dir()
	if err != nil {
		fmt.Println(home)
		os.Exit(1)
	}

	// construct the path to resolve to `~/.kube/config`
	kubeConfigPath := home + "/.kube/config"

	// create the config from the path
	config, err := clientcmd.BuildConfigFromFlags("", kubeConfigPath)
	if err != nil {
		log.Fatalf("getClusterConfig: %v", err)
	}

	// generate the client based off of the config
	k8sClient, err := kubernetes.NewForConfig(config)
	if err != nil {
		log.Fatalf("getClusterConfig: %v", err)
	}

	client, err := clientset.NewForConfig(config)
	if err != nil {
		log.Fatalf("Error building example clientset: %s", err.Error())
	}

	log.Println("Successfully constructed k8s client")

	return k8sClient, client
}

func getClientInCluster() (kubernetes.Interface, *clientset.Clientset) {
	config, err := rest.InClusterConfig()
	if err != nil {
		log.Fatalf("getClusterConfig: %v", err)
	}

	// generate the client based off of the config
	k8sClient, err := kubernetes.NewForConfig(config)
	if err != nil {
		log.Fatalf("getClusterConfig: %v", err)
	}

	client, err := clientset.NewForConfig(config)
	if err != nil {
		log.Fatalf("Error building example clientset: %s", err.Error())
	}

	log.Println("Successfully constructed k8s client")

	return k8sClient, client
}

func main() {
	// Define command line flags, add any other flag required to configure
	// the service.
	var (
		addr = flag.String("listen", ":8001", "HTTP listen `address`")
		dbg  = flag.Bool("debug", false, "Log request and response bodies")
	)
	flag.Parse()

	// Create go-kit logger in your main.go
	logger := log.NewLogfmtLogger(log.NewSyncWriter(os.Stdout))
	logger = log.With(logger, "ts", log.DefaultTimestampUTC)
	logger = log.With(logger, "caller", log.DefaultCaller)
	logger = level.NewFilter(logger, level.AllowAll())

	// Overriding the default glog with our go-kit glog implementation.
	// Thus we need to pass it our go-kit logger object.
	glog.SetLogger(logger)

	// Setup logger and goa log adapter. Replace logger with your own using
	// your log package of choice.
	var (
		adapter middleware.Logger
		// logger  *log.Logger
	)
	{
		// logger = log.New(os.Stderr, "[spawnsvc] ", log.Ltime)
		// adapter = middleware.NewLogger(logger)
	}

	// get the Kubernetes client for connectivity
	k8sClient, client := getClientInCluster()

	// set up signals so we handle the first shutdown signal gracefully
	stopCh := signals.SetupSignalHandler()

	kubeInformerFactory := kubeinformers.NewSharedInformerFactory(k8sClient, time.Second*30)
	exampleInformerFactory := informers.NewSharedInformerFactory(client, time.Second*30)

	// Create the structs that implement the services.
	var (
		armadaSvc armada.Service
	)
	{
		armadaSvc = spawnsvc.NewArmada(logger)
	}

	// notice that there is no need to run Start methods in a separate goroutine. (i.e. go kubeInformerFactory.Start(stopCh)
	// Start method is non-blocking and runs all registered informers in a dedicated goroutine.
	kubeInformerFactory.Start(stopCh)
	exampleInformerFactory.Start(stopCh)

	gsController := gs.NewGameServerController(
		k8sClient,
		client,
		kubeInformerFactory.Apps().V1().Deployments(),
		exampleInformerFactory.Dev().V1().GameServers())

	if err := gsController.Run(1, stopCh); err != nil {
		klog.Info("Error running controller: %s", err.Error())
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
	swaggersvr.Mount(mux)

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

	srvHealth := http.NewServeMux()
	srvHealth.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.Write([]byte("ok"))
	})
	go func() {
		http.ListenAndServe(":9000", srvHealth)
	}()

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

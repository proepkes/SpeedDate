package main

import (
	"flag"
	"fmt"
	"os"
	"time"

	homedir "github.com/mitchellh/go-homedir"
	clientset "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/gs"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/signals"

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
		glog.Fatalf("getClusterConfig: %v", err)
	}

	// generate the client based off of the config
	k8sClient, err := kubernetes.NewForConfig(config)
	if err != nil {
		glog.Fatalf("getClusterConfig: %v", err)
	}

	client, err := clientset.NewForConfig(config)
	if err != nil {
		glog.Fatalf("Error building example clientset: %s", err.Error())
	}

	glog.Infoln("Successfully constructed k8s client")

	return k8sClient, client
}

func getClientInCluster() (kubernetes.Interface, *clientset.Clientset) {
	config, err := rest.InClusterConfig()
	if err != nil {
		glog.Fatalf("getClusterConfig: %v", err)
	}

	// generate the client based off of the config
	k8sClient, err := kubernetes.NewForConfig(config)
	if err != nil {
		glog.Fatalf("getClusterConfig: %v", err)
	}

	client, err := clientset.NewForConfig(config)
	if err != nil {
		glog.Fatalf("Error building example clientset: %s", err.Error())
	}

	glog.Infoln("Successfully constructed k8s client")

	return k8sClient, client
}

func main() {
	flag.Parse()

	// Create go-kit logger in your main.go
	logger := log.NewLogfmtLogger(log.NewSyncWriter(os.Stdout))
	logger = log.With(logger, "ts", log.DefaultTimestampUTC)
	logger = log.With(logger, "caller", log.DefaultCaller)
	logger = level.NewFilter(logger, level.AllowAll())

	// Overriding the default glog with our go-kit glog implementation.
	// Thus we need to pass it our go-kit logger object.
	glog.SetLogger(logger)

	// get the Kubernetes client for connectivity
	k8sClient, client := getClientInCluster()

	// set up signals so we handle the first shutdown signal gracefully
	stopCh := signals.SetupSignalHandler()

	kubeInformerFactory := kubeinformers.NewSharedInformerFactory(k8sClient, time.Second*30)
	exampleInformerFactory := informers.NewSharedInformerFactory(client, time.Second*30)

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
		klog.Infof("Error running controller: %s", err.Error())
	}
}

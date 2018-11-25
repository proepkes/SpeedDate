package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"sync"
	"time"

	homedir "github.com/mitchellh/go-homedir"
	clientset "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/client/informers/externalversions"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/gs"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/signals"

	extclientset "k8s.io/apiextensions-apiserver/pkg/client/clientset/clientset"
	kubeinformers "k8s.io/client-go/informers"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
)

// retrieve the Kubernetes cluster client from outside of the cluster
func getClientLocal() (kubernetes.Interface, *extclientset.Clientset, *clientset.Clientset) {
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

	return createClients(config)
}

func getClientInCluster() (kubernetes.Interface, *extclientset.Clientset, *clientset.Clientset) {
	config, err := rest.InClusterConfig()
	if err != nil {
		log.Fatalf("getClusterConfig: %v", err)
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

func main() {
	flag.Parse()

	// get the Kubernetes client for connectivity
	k8sClient, extClient, client := getClientLocal()

	// set up signals so we handle the first shutdown signal gracefully
	stopCh := signals.SetupSignalHandler()

	kubeInformerFactory := kubeinformers.NewSharedInformerFactory(k8sClient, time.Second*30)
	informerFactory := externalversions.NewSharedInformerFactory(client, time.Second*30)

	allocationMutex := &sync.Mutex{}
	gsController := gs.NewController(allocationMutex, k8sClient, kubeInformerFactory, extClient, client, informerFactory)

	// notice that there is no need to run Start methods in a separate goroutine. (i.e. go kubeInformerFactory.Start(stopCh)
	// Start method is non-blocking and runs all registered informers in a dedicated goroutine.
	kubeInformerFactory.Start(stopCh)
	informerFactory.Start(stopCh)

	if err := gsController.Run(1, stopCh); err != nil {
		log.Fatalf("Error running controller: %s", err.Error())
	}
}

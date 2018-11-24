package main

import (
	"flag"
	"fmt"
	"log"
	"os"
	"time"

	homedir "github.com/mitchellh/go-homedir"
	clientset "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/gs"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/signals"

	informers "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/informers/externalversions"
	kubeinformers "k8s.io/client-go/informers"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
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
	flag.Parse()

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
		log.Fatalf("Error running controller: %s", err.Error())
	}
}

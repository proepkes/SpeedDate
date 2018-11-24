package spawnsvc

import (
	"context"
	"fmt"
	"log"
	"time"

	armada "github.com/proepkes/speeddate/src/spawnsvc/gen/armada"
	utilruntime "k8s.io/apimachinery/pkg/util/runtime"
	"k8s.io/apimachinery/pkg/util/wait"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/tools/cache"
	"k8s.io/client-go/util/workqueue"
)

// armada service example implementation.
// The example methods log the requests and return zero values.
type armadaSvc struct {
	logger    *log.Logger
	clientset kubernetes.Interface
	informer  cache.SharedIndexInformer
	queue     workqueue.RateLimitingInterface
	handler   Handler
}

// NewArmada returns the armada service implementation.
func NewArmada(logger *log.Logger, clientset kubernetes.Interface, queue workqueue.RateLimitingInterface, informer cache.SharedIndexInformer, handler Handler) armada.Service {
	return &armadaSvc{logger, clientset, informer, queue, handler}
}

// Add a new gameserver to the armada.
func (s *armadaSvc) Add(ctx context.Context) (res string, err error) {
	s.logger.Print("armada.add")
	return
}

// Run is the main path of execution for the controller loop
func (s *armadaSvc) Run(stopCh <-chan struct{}) {
	// handle a panic with logging and exiting
	defer utilruntime.HandleCrash()
	// ignore new items in the queue but when all goroutines
	// have completed existing items then shutdown
	defer s.queue.ShutDown()

	s.logger.Println("Controller.Run: initiating")

	// run the informer to start listing and watching resources
	go s.informer.Run(stopCh)

	// do the initial synchronization (one time) to populate resources
	if !cache.WaitForCacheSync(stopCh, s.HasSynced) {
		utilruntime.HandleError(fmt.Errorf("Error syncing cache"))
		return
	}
	s.logger.Println("Controller.Run: cache sync complete")

	// run the runWorker method every second with a stop channel
	wait.Until(s.runWorker, time.Second, stopCh)
}

// HasSynced allows us to satisfy the Controller interface
// by wiring up the informer's HasSynced method to it
func (s *armadaSvc) HasSynced() bool {
	return s.informer.HasSynced()
}

// runWorker executes the loop to process new items added to the queue
func (s *armadaSvc) runWorker() {
	log.Println("Controller.runWorker: starting")

	// invoke processNextItem to fetch and consume the next change
	// to a watched or listed resource
	for s.processNextItem() {
		log.Println("Controller.runWorker: processing next item")
	}

	log.Println("Controller.runWorker: completed")
}

// processNextItem retrieves each queued item and takes the
// necessary handler action based off of if the item was
// created or deleted
func (s *armadaSvc) processNextItem() bool {
	log.Println("Controller.processNextItem: start")

	// fetch the next item (blocking) from the queue to process or
	// if a shutdown is requested then return out of this to stop
	// processing
	key, quit := s.queue.Get()

	log.Println("Processing...")
	// stop the worker loop from running as this indicates we
	// have sent a shutdown message that the queue has indicated
	// from the Get method
	if quit {
		return false
	}

	defer s.queue.Done(key)

	// assert the string out of the key (format `namespace/name`)
	keyRaw := key.(string)

	// take the string key and get the object out of the indexer
	//
	// item will contain the complex object for the resource and
	// exists is a bool that'll indicate whether or not the
	// resource was created (true) or deleted (false)
	//
	// if there is an error in getting the key from the index
	// then we want to retry this particular queue key a certain
	// number of times (5 here) before we forget the queue key
	// and throw an error
	item, exists, err := s.informer.GetIndexer().GetByKey(keyRaw)
	if err != nil {
		if s.queue.NumRequeues(key) < 5 {
			fmt.Errorf("Controller.processNextItem: Failed processing item with key %s with error %v, retrying", key, err)
			s.queue.AddRateLimited(key)
		} else {
			fmt.Errorf("Controller.processNextItem: Failed processing item with key %s with error %v, no more retries", key, err)
			s.queue.Forget(key)
			utilruntime.HandleError(err)
		}
	}

	// if the item doesn't exist then it was deleted and we need to fire off the handler's
	// ObjectDeleted method. but if the object does exist that indicates that the object
	// was created (or updated) so run the ObjectCreated method
	//
	// after both instances, we want to forget the key from the queue, as this indicates
	// a code path of successful queue key processing
	if !exists {
		s.logger.Printf("Controller.processNextItem: object deleted detected: %s", keyRaw)
		s.handler.ObjectDeleted(item)
		s.queue.Forget(key)
	} else {
		s.logger.Printf("Controller.processNextItem: object created detected: %s", keyRaw)
		s.handler.ObjectCreated(item)
		s.queue.Forget(key)
	}

	// keep the worker loop running by returning true
	return true
}

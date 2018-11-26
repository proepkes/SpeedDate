package gs

import (
	"fmt"
	"log"
	"sync"
	"time"

	errorsPkg "github.com/pkg/errors"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/apis/dev/v1alpha1"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/client/informers/externalversions"
	corev1 "k8s.io/api/core/v1"
	apiv1beta1 "k8s.io/apiextensions-apiserver/pkg/apis/apiextensions/v1beta1"
	extclientset "k8s.io/apiextensions-apiserver/pkg/client/clientset/clientset"
	"k8s.io/apiextensions-apiserver/pkg/client/clientset/clientset/typed/apiextensions/v1beta1"
	extv1beta1 "k8s.io/apiextensions-apiserver/pkg/client/clientset/clientset/typed/apiextensions/v1beta1"
	"k8s.io/apimachinery/pkg/api/errors"
	"k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/labels"
	"k8s.io/apimachinery/pkg/util/runtime"
	"k8s.io/apimachinery/pkg/util/wait"
	"k8s.io/client-go/informers"
	"k8s.io/client-go/kubernetes"
	typedcorev1 "k8s.io/client-go/kubernetes/typed/core/v1"
	"k8s.io/client-go/tools/cache"
	"k8s.io/client-go/tools/record"
	"k8s.io/client-go/util/workqueue"

	"github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned/scheme"
	getterv1alpha1 "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/clientset/versioned/typed/dev/v1alpha1"
	listerv1alpha1 "github.com/proepkes/speeddate/src/spawnsvc/pkg/client/listers/dev/v1alpha1"

	corelisterv1 "k8s.io/client-go/listers/core/v1"
)

type GameServerController struct {
	logger           *log.Logger
	kubeclientset    kubernetes.Interface
	clientset        versioned.Interface
	crdGetter        v1beta1.CustomResourceDefinitionInterface
	podGetter        typedcorev1.PodsGetter
	podLister        corelisterv1.PodLister
	podSynced        cache.InformerSynced
	gameServerGetter getterv1alpha1.GameServersGetter
	gameServerLister listerv1alpha1.GameServerLister
	gameServerSynced cache.InformerSynced
	nodeLister       corelisterv1.NodeLister
	allocationMutex  *sync.Mutex
	stop             <-chan struct{}
	recorder         record.EventRecorder
	workerqueue      workqueue.RateLimitingInterface
}

const (
	// SuccessSynced is used as part of the Event 'reason' when a Gameserver is synced
	SuccessSynced = "Synced"
	// ErrResourceExists is used as part of the Event 'reason' when a Gameserver fails
	// to sync due to a Deployment of the same name already existing.
	ErrResourceExists = "ErrResourceExists"

	// MessageResourceExists is the message used for Events when a resource
	// fails to sync due to a Deployment already existing
	MessageResourceExists = "Resource %q already exists and is not managed by Gameserver"
	// MessageResourceSynced is the message used for an Event fired when a Gameserver
	// is synced successfully
	MessageResourceSynced = "Gameserver synced successfully"
)

func NewController(
	allocationMutex *sync.Mutex,
	kubeClient kubernetes.Interface,
	kubeInformerFactory informers.SharedInformerFactory,
	extClient extclientset.Interface,
	client versioned.Interface, informerFactory externalversions.SharedInformerFactory) *GameServerController {

	pods := kubeInformerFactory.Core().V1().Pods()
	gameServers := informerFactory.Speeddate().V1alpha1().GameServers()
	gsInformer := gameServers.Informer()

	c := &GameServerController{
		allocationMutex:  allocationMutex,
		kubeclientset:    kubeClient,
		clientset:        client,
		crdGetter:        extClient.ApiextensionsV1beta1().CustomResourceDefinitions(),
		podGetter:        kubeClient.CoreV1(),
		podLister:        pods.Lister(),
		podSynced:        pods.Informer().HasSynced,
		gameServerGetter: client.SpeeddateV1alpha1(),
		gameServerLister: gameServers.Lister(),
		gameServerSynced: gsInformer.HasSynced,
		nodeLister:       kubeInformerFactory.Core().V1().Nodes().Lister(),
		workerqueue:      workqueue.NewNamedRateLimitingQueue(workqueue.DefaultControllerRateLimiter(), "Gameservers"),
	}

	eventBroadcaster := record.NewBroadcaster()
	// eventBroadcaster.StartLogging(c.logger.Printf)
	eventBroadcaster.StartRecordingToSink(&typedcorev1.EventSinkImpl{Interface: kubeClient.CoreV1().Events("")})
	c.recorder = eventBroadcaster.NewRecorder(scheme.Scheme, corev1.EventSource{Component: "gameserver-controller"})

	gsInformer.AddEventHandler(cache.ResourceEventHandlerFuncs{
		AddFunc: c.enqueueFoo,
		UpdateFunc: func(oldObj, newObj interface{}) {
			c.enqueueFoo(newObj)
		},
	})

	return c
}

func (c *GameServerController) CreateGameserver() {
	c.gameServerGetter.GameServers("default").Create(v1alpha1.NewGameserver())
}

func (c *GameServerController) ClearAll() {
	err := c.gameServerGetter.GameServers("default").DeleteCollection(&v1.DeleteOptions{}, v1.ListOptions{})
	if err != nil {
		log.Fatalf("%s", err)
	}
}

// Run the GameServer controller. Will block until stop is closed.
// Runs threadiness number workers to process the rate limited queue
func (c *GameServerController) Run(workers int, stopCh <-chan struct{}) error {
	defer runtime.HandleCrash()
	defer c.workerqueue.ShutDown()

	err := WaitForEstablishedCRD(c.crdGetter, "gameservers.speeddate.dev")
	if err != nil {
		return err
	}

	log.Println("Wait for cache sync")
	if !cache.WaitForCacheSync(stopCh, c.gameServerSynced) {
		return errorsPkg.New("failed to wait for caches to sync")
	}

	log.Println("Starting workers")
	// Launch two workers to process Foo resources
	for i := 0; i < workers; i++ {
		go wait.Until(c.runWorker, time.Second, stopCh)
	}

	log.Println("Started workers")
	<-stopCh
	log.Println("Shutting down workers")
	return nil
}

// runWorker is a long-running function that will continually call the
// processNextWorkItem function in order to read and process a message on the
// workqueue.
func (c *GameServerController) runWorker() {
	for c.processNextWorkItem() {
	}
}

// processNextWorkItem will read a single work item off the workqueue and
// attempt to process it, by calling the syncHandler.
func (c *GameServerController) processNextWorkItem() bool {
	obj, shutdown := c.workerqueue.Get()

	if shutdown {
		return false
	}

	// We wrap this block in a func so we can defer c.workqueue.Done.
	err := func(obj interface{}) error {
		// We call Done here so the workqueue knows we have finished
		// processing this item. We also must remember to call Forget if we
		// do not want this work item being re-queued. For example, we do
		// not call Forget if a transient error occurs, instead the item is
		// put back on the workqueue and attempted again after a back-off
		// period.
		defer c.workerqueue.Done(obj)
		var key string
		var ok bool
		// We expect strings to come off the workqueue. These are of the
		// form namespace/name. We do this as the delayed nature of the
		// workqueue means the items in the informer cache may actually be
		// more up to date that when the item was initially put onto the
		// workqueue.
		if key, ok = obj.(string); !ok {
			// As the item in the workqueue is actually invalid, we call
			// Forget here else we'd go into a loop of attempting to
			// process a work item that is invalid.
			c.workerqueue.Forget(obj)
			runtime.HandleError(fmt.Errorf("expected string in workqueue but got %#v", obj))
			return nil
		}
		// Run the syncHandler, passing it the namespace/name string of the
		// Foo resource to be synced.
		if err := c.syncHandler(key); err != nil {
			// Put the item back on the workqueue to handle any transient errors.
			c.workerqueue.AddRateLimited(key)
			return fmt.Errorf("error syncing '%s': %s, requeuing", key, err.Error())
		}
		// Finally, if no error occurs we Forget this item so it does not
		// get queued again until another change happens.
		c.workerqueue.Forget(obj)
		log.Printf("Successfully synced '%s'", key)
		return nil
	}(obj)

	if err != nil {
		runtime.HandleError(err)
		return true
	}

	return true
}

// syncHandler compares the actual state with the desired, and attempts to
// converge the two. It then updates the Status block of the Foo resource
// with the current status of the resource.
func (c *GameServerController) syncHandler(key string) error {
	namespace, name, err := cache.SplitMetaNamespaceKey(key)
	if err != nil {
		runtime.HandleError(fmt.Errorf("invalid resource key: %s", key))
		return nil
	}

	// Get the Foo resource with this namespace/name
	gs, err := c.gameServerLister.GameServers(namespace).Get(name)
	if err != nil {
		// The Foo resource may no longer exist, in which case we stop
		// processing.
		if errors.IsNotFound(err) {
			runtime.HandleError(fmt.Errorf("foo '%s' in work queue no longer exists", key))
			return nil
		}

		return err
	}

	if gs, err = c.handleDeleted(gs); err != nil {
		return err
	}

	if gs, err = c.handleRequested(gs); err != nil {
		return err
	}

	// deploymentName := foo.Spec.DeploymentName
	// if deploymentName == "" {
	// 	// We choose to absorb the error here as the worker would requeue the
	// 	// resource otherwise. Instead, the next time the resource is updated
	// 	// the resource will be queued again.
	// 	runtime.HandleError(fmt.Errorf("%s: deployment name must be specified", key))
	// 	return nil
	// }

	// // Get the deployment with the name specified in Foo.spec
	// deployment, err := c.deploymentsLister.Deployments(foo.Namespace).Get(deploymentName)
	// // If the resource doesn't exist, we'll create it
	// if errors.IsNotFound(err) {
	// 	deployment, err = c.kubeclientset.AppsV1().Deployments(foo.Namespace).Create(newDeployment(foo))
	// }

	// If an error occurs during Get/Create, we'll requeue the item so we can
	// attempt processing again later. This could have been caused by a
	// temporary network failure, or any other transient reason.
	if err != nil {
		return err
	}

	// If the Deployment is not controlled by this Foo resource, we should log
	// a warning to the event recorder and ret
	// if !metav1.IsControlledBy(deployment, foo) {
	// 	msg := fmt.Sprintf(MessageResourceExists, deployment.Name)
	// 	c.recorder.Event(foo, corev1.EventTypeWarning, ErrResourceExists, msg)
	// 	return fmt.Errorf(msg)
	// }

	// If this number of the replicas on the Foo resource is specified, and the
	// number does not equal the current desired replicas on the Deployment, we
	// should update the Deployment resource.
	// if foo.Spec.Replicas != nil && *foo.Spec.Replicas != *deployment.Spec.Replicas {
	// 	log.Printf("Foo %s replicas: %d, deployment replicas: %d", name, *foo.Spec.Replicas, *deployment.Spec.Replicas)
	// 	deployment, err = c.kubeclientset.AppsV1().Deployments(foo.Namespace).Update(newDeployment(foo))
	// }

	// If an error occurs during Update, we'll requeue the item so we can
	// attempt processing again later. THis could have been caused by a
	// temporary network failure, or any other transient reason.
	if err != nil {
		return err
	}

	// Finally, we update the status block of the Foo resource to reflect the
	// current state of the world
	// err = c.updateFooStatus(foo, deployment)
	// if err != nil {
	// 	return err
	// }

	c.recorder.Event(gs, corev1.EventTypeNormal, SuccessSynced, MessageResourceSynced)
	return nil
}

// syncGameServerCreatingState checks if the GameServer is in the Creating state, and if so
// creates a Pod for the GameServer and moves the state to Starting
func (c *GameServerController) handleRequested(gs *v1alpha1.GameServer) (*v1alpha1.GameServer, error) {
	if gs.State != v1alpha1.Requested {
		return gs, nil
	}

	if !(cache.WaitForCacheSync(c.stop, c.podSynced)) {
		return nil, fmt.Errorf("could not sync pod cache state")
	}

	gs, err := c.createGameServerPod(gs)
	if err != nil {
		return gs, err
	}

	gsCopy := gs.DeepCopy()
	gsCopy.State = v1alpha1.Ready
	gs, err = c.gameServerGetter.GameServers(gs.ObjectMeta.Namespace).Update(gsCopy)
	if err != nil {
		return gs, fmt.Errorf("error updating GameServer %s to Ready state", gs.Name)
	}

	return gs, nil
}

// syncGameServerCreatingState checks if the GameServer is in the Creating state, and if so
// creates a Pod for the GameServer and moves the state to Starting
func (c *GameServerController) handleDeleted(gs *v1alpha1.GameServer) (*v1alpha1.GameServer, error) {
	if gs.ObjectMeta.DeletionTimestamp.IsZero() {
		return gs, nil
	}

	pod, err := c.getPod(gs)
	if err != nil {
		return gs, err
	}

	err = c.podGetter.Pods(pod.ObjectMeta.Namespace).Delete(pod.ObjectMeta.Name, nil)
	if err != nil {
		return gs, err
	}
	c.recorder.Event(gs, corev1.EventTypeNormal, string(gs.State), fmt.Sprintf("Deleting Pod %s", pod.ObjectMeta.Name))

	return gs, nil
}

// enqueueFoo takes a Foo resource and converts it into a namespace/name
// string which is then put onto the work queue. This method should *not* be
// passed resources of any type other than Foo.
func (c *GameServerController) enqueueFoo(obj interface{}) {
	var key string
	var err error
	if key, err = cache.MetaNamespaceKeyFunc(obj); err != nil {
		runtime.HandleError(err)
		return
	}
	c.workerqueue.AddRateLimited(key)
}

// WaitForEstablishedCRD blocks until CRD comes to an Established state.
// Has a deadline of 60 seconds for this to occur.
func WaitForEstablishedCRD(crdGetter extv1beta1.CustomResourceDefinitionInterface, name string) error {
	return wait.PollImmediate(time.Second, 60*time.Second, func() (done bool, err error) {
		crd, err := crdGetter.Get(name, v1.GetOptions{})
		if err != nil {
			return false, err
		}

		for _, cond := range crd.Status.Conditions {
			switch cond.Type {
			case apiv1beta1.Established:
				if cond.Status == apiv1beta1.ConditionTrue {
					log.Println("custom resource definition established")
					return true, err
				}
			}
		}

		return false, nil
	})
}

func (c *GameServerController) createGameServerPod(gs *v1alpha1.GameServer) (*v1alpha1.GameServer, error) {
	pod, err := c.podGetter.Pods(gs.ObjectMeta.Namespace).Create(gs.Pod())
	if err != nil {
		return gs, err
	}

	c.recorder.Event(gs, corev1.EventTypeNormal, string(gs.State),
		fmt.Sprintf("Pod %s created", pod.ObjectMeta.Name))

	return gs, nil
}

func (c *GameServerController) getPod(gs *v1alpha1.GameServer) (*corev1.Pod, error) {
	pods, err := c.podLister.List(labels.SelectorFromSet(labels.Set{v1alpha1.GameServerPodLabel: gs.ObjectMeta.Name}))
	if err != nil {
		return nil, fmt.Errorf("error checking if pod exists for GameServer %s", gs.Name)
	}
	for _, p := range pods {
		if v1.IsControlledBy(p, gs) {
			return p, nil
		}
	}

	return nil, fmt.Errorf("no pod exists for GameServer %s", gs.Name)
}

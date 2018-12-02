package spawnsvc

import (
	"context"
	"fmt"
	"log"

	fleet "github.com/proepkes/speeddate/src/spawnsvc/gen/fleet"

	"agones.dev/agones/pkg/apis/stable/v1alpha1"
	"agones.dev/agones/pkg/client/clientset/versioned"
	corev1 "k8s.io/api/core/v1"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/rest"
)

// fleet service example implementation.
// The example methods log the requests and return zero values.
type fleetSvc struct {
	logger              *log.Logger
	gameserverNamespace string
	client              *versioned.Clientset
}

// NewFleet returns the fleet service implementation.
func NewFleet(logger *log.Logger, gameserverNamespace string, clusterConfig *rest.Config) fleet.Service {
	// Access to the Agones resources through the Agones Clientset
	// Note that we reuse the same config as we used for the Kubernetes Clientset
	client, err := versioned.NewForConfig(clusterConfig)
	if err != nil {
		logger.Fatal("Could not create the agones api clientset")
	}

	return &fleetSvc{logger, gameserverNamespace, client}
}

// Add a new gameserver.
func (s *fleetSvc) Add(ctx context.Context) (res string, err error) {
	s.logger.Print("fleet.add")

	// Create a GameServer
	gs := &v1alpha1.GameServer{ObjectMeta: metav1.ObjectMeta{GenerateName: "udp-server", Namespace: "default"},
		Spec: v1alpha1.GameServerSpec{
			Ports: []v1alpha1.GameServerPort{{ContainerPort: 7777, PortPolicy: v1alpha1.Dynamic}},
			Template: corev1.PodTemplateSpec{
				Spec: corev1.PodSpec{
					Containers: []corev1.Container{{Name: "udp-server", Image: "gcr.io/agones-images/udp-server:0.4"}},
				},
			},
		},
	}
	newGS, err := s.client.StableV1alpha1().GameServers(s.gameserverNamespace).Create(gs)
	if err != nil {
		panic(err)
	}

	fmt.Printf("New game servers' name is: %s", newGS.ObjectMeta.Name)

	return
}

// Removes all gameserver pods.
func (s *fleetSvc) Clear(ctx context.Context) (res string, err error) {
	s.logger.Print("fleet.clear")
	s.client.StableV1alpha1().GameServers(s.gameserverNamespace).DeleteCollection(&metav1.DeleteOptions{}, metav1.ListOptions{})
	return
}

package spawnsvc

import (
	"context"
	"log"

	gameserver "github.com/proepkes/speeddate/src/spawnsvc/gen/gameserver"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/kubernetes"
)

// gameserver service example implementation.
// The example methods log the requests and return zero values.
type gameserverSvc struct {
	logger             *log.Logger
	speeddateNamespace string
	k8sClient          kubernetes.Interface
}

// NewGameserver returns the gameserver service implementation.
func NewGameserver(logger *log.Logger, gameserverNamespace string, k8sClient kubernetes.Interface) gameserver.Service {
	return &gameserverSvc{logger, gameserverNamespace, k8sClient}
}

// Configure gameserver-properties.
func (s *gameserverSvc) Configure(ctx context.Context) (res string, err error) {
	s.logger.Print("gameserver.configure")
	configMaps, err := s.k8sClient.CoreV1().ConfigMaps(s.speeddateNamespace).List(metav1.ListOptions{})
	if err != nil {
		return "", err
	}
	for _, cm := range configMaps.Items {
		s.logger.Print(cm)
	}
	return
}

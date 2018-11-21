package spawnsvc

import (
	"context"
	"log"

	spawn "github.com/proepkes/speeddate/src/spawnsvc/gen/spawn"
	"k8s.io/client-go/informers"
)

// spawn service example implementation.
// The example methods log the requests and return zero values.
type spawnSvc struct {
	logger *log.Logger
}

// NewSpawn returns the spawn service implementation.
func NewSpawn(logger *log.Logger, k8sInformerFactory informers.SharedInformerFactory) spawn.Service {
	return &spawnSvc{logger}
}

// Spawn a new gameserver.
func (s *spawnSvc) New(ctx context.Context) (res string, err error) {
	s.logger.Print("spawn.new")
	return
}

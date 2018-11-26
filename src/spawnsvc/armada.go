package spawnsvc

import (
	"context"
	"log"

	armada "github.com/proepkes/speeddate/src/spawnsvc/gen/armada"
	"github.com/proepkes/speeddate/src/spawnsvc/pkg/gs"
)

// armada service example implementation.
// The example methods log the requests and return zero values.
type armadaSvc struct {
	logger       *log.Logger
	gsController *gs.GameServerController
}

// NewArmada returns the armada service implementation.
func NewArmada(logger *log.Logger, gsController *gs.GameServerController) armada.Service {
	return &armadaSvc{logger, gsController}
}

// Add a new gameserver to the armada.
func (s *armadaSvc) Add(ctx context.Context) (res string, err error) {
	s.logger.Print("armada.add")
	s.gsController.CreateGameserver()
	return
}

// Removes all gameserver pods.
func (s *armadaSvc) Clear(ctx context.Context) (res string, err error) {
	s.logger.Print("armada.clear")
	s.gsController.ClearAll()
	return
}

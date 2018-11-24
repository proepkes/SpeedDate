package spawnsvc

import (
	"context"
	"log"

	armada "github.com/proepkes/speeddate/src/spawnsvc/gen/armada"
)

// armada service example implementation.
// The example methods log the requests and return zero values.
type armadaSvc struct {
	logger *log.Logger
}

// NewArmada returns the armada service implementation.
func NewArmada(logger *log.Logger) armada.Service {
	return &armadaSvc{logger}
}

// Add a new gameserver to the armada.
func (s *armadaSvc) Add(ctx context.Context) (res string, err error) {
	s.logger.Print("armada.add")
	return
}

package spawnsvc

import (
	"context"

	armada "github.com/proepkes/speeddate/src/spawnsvc/gen/armada"
)

// armada service example implementation.
// The example methods log the requests and return zero values.
type armadaSvc struct {
}

// NewArmada returns the armada service implementation.
func NewArmada() armada.Service {
	return &armadaSvc{}
}

// Add a new gameserver to the armada.
func (s *armadaSvc) Add(ctx context.Context) (res string, err error) {

	return
}

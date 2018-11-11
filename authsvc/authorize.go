package authsvc

import (
	"context"
	"log"

	authorize "github.com/proepkes/speeddate/authsvc/gen/authorize"
)

// authorize service example implementation.
// The example methods log the requests and return zero values.
type authorizeSvc struct {
	logger *log.Logger
}

// NewAuthorize returns the authorize service implementation.
func NewAuthorize(logger *log.Logger) authorize.Service {
	return &authorizeSvc{logger}
}

// Creates a valid JWT
func (s *authorizeSvc) Login(ctx context.Context, p *authorize.LoginPayload) (err error) {
	s.logger.Print("authorize.login")
	return
}

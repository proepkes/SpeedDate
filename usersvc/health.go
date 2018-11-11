package usersvc

import (
	"context"
	"log"

	health "github.com/proepkes/speeddate/usersvc/gen/health"
)

// health service example implementation.
// The example methods log the requests and return zero values.
type healthSvc struct {
	logger *log.Logger
}

// NewHealth returns the health service implementation.
func NewHealth(logger *log.Logger) health.Service {
	return &healthSvc{logger}
}

// Health check endpoint
func (s *healthSvc) CheckHealth(ctx context.Context) (res string, err error) {
	s.logger.Print("health.checkHealth")
	return
}

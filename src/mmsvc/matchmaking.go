package mmsvc

import (
	"context"
	"log"

	matchmaking "github.com/proepkes/speeddate/src/mmsvc/gen/matchmaking"
)

// matchmaking service example implementation.
// The example methods log the requests and return zero values.
type matchmakingSvc struct {
	logger *log.Logger
}

// NewMatchmaking returns the matchmaking service implementation.
func NewMatchmaking(logger *log.Logger) matchmaking.Service {
	return &matchmakingSvc{logger}
}

// .
func (s *matchmakingSvc) Insert(ctx context.Context) (res string, err error) {
	s.logger.Print("matchmaking.insert")
	return
}

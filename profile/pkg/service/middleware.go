package service

import (
	"context"
	log "github.com/go-kit/kit/log"
	gouuid "github.com/satori/go.uuid"
)

// Middleware describes a service middleware.
type Middleware func(ProfileService) ProfileService

type loggingMiddleware struct {
	logger log.Logger
	next   ProfileService
}

// LoggingMiddleware takes a logger as a dependency
// and returns a ProfileService Middleware.
func LoggingMiddleware(logger log.Logger) Middleware {
	return func(next ProfileService) ProfileService {
		return &loggingMiddleware{logger, next}
	}

}

func (l loggingMiddleware) Get(ctx context.Context, id gouuid.UUID) (p Profile, err error) {
	defer func() {
		l.logger.Log("method", "Get", "id", id, "p", p, "err", err)
	}()
	return l.next.Get(ctx, id)
}
func (l loggingMiddleware) Add(ctx context.Context, user Profile) (e0 error) {
	defer func() {
		l.logger.Log("method", "Add", "user", user, "e0", e0)
	}()
	return l.next.Add(ctx, user)
}

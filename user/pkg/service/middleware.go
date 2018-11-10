package service

import (
	"context"

	log "github.com/go-kit/kit/log"
	gouuid "github.com/satori/go.uuid"
)

// Middleware describes a service middleware.
type Middleware func(UserService) UserService

type loggingMiddleware struct {
	logger log.Logger
	next   UserService
}

// LoggingMiddleware takes a logger as a dependency
// and returns a UserService Middleware.
func LoggingMiddleware(logger log.Logger) Middleware {
	return func(next UserService) UserService {
		return &loggingMiddleware{logger, next}
	}

}

func (l loggingMiddleware) Get(ctx context.Context, id gouuid.UUID) (u User, err error) {
	defer func() {
		l.logger.Log("method", "Get", "id", id, "u", u, "err", err)
	}()
	return l.next.Get(ctx, id)
}
func (l loggingMiddleware) Add(ctx context.Context, user User) (u User, err error) {
	defer func() {
		l.logger.Log("method", "Add", "user", user, "u", u, "err", err)
	}()
	return l.next.Add(ctx, user)
}

package service

import (
	"context"

	u "github.com/satori/go.uuid"
)

// ProfileService describes the service.
type ProfileService interface {
	Get(ctx context.Context, id u.UUID) (p Profile, err error)
	Add(ctx context.Context, user Profile) error
}

type basicProfileService struct{}

func (b *basicProfileService) Get(ctx context.Context, id u.UUID) (p Profile, err error) {
	// TODO implement the business logic of Get
	return p, err
}
func (b *basicProfileService) Add(ctx context.Context, user Profile) (e0 error) {
	// TODO implement the business logic of Add
	return e0
}

// NewBasicProfileService returns a naive, stateless implementation of ProfileService.
func NewBasicProfileService() ProfileService {
	return &basicProfileService{}
}

// New returns a ProfileService with all of the expected middleware wired in.
func New(middleware []Middleware) ProfileService {
	var svc ProfileService = NewBasicProfileService()
	for _, m := range middleware {
		svc = m(svc)
	}
	return svc
}

package service

import (
	"context"
	"encoding/json"

	u "github.com/satori/go.uuid"
)

type User struct {
	ID       u.UUID `json:"id"`
	Title    string `json:"title"`
	Complete bool   `json:"complete"`
}

func (t User) String() string {
	b, err := json.Marshal(t)
	if err != nil {
		return "unsupported value type"
	}
	return string(b)
}

// UserService describes the service.
type UserService interface {
	// Add your methods here
	Get(ctx context.Context, id u.UUID) (u User, err error)
	Add(ctx context.Context, user User) (u User, err error)
}

type basicUserService struct{}

func (b *basicUserService) Get(ctx context.Context, id u.UUID) (u User, err error) {
	// TODO implement the business logic of Get
	return u, err
}
func (b *basicUserService) Add(ctx context.Context, user User) (u User, err error) {
	// TODO implement the business logic of Add
	return u, err
}

// NewBasicUserService returns a naive, stateless implementation of UserService.
func NewBasicUserService() UserService {
	return &basicUserService{}
}

// New returns a UserService with all of the expected middleware wired in.
func New(middleware []Middleware) UserService {
	var svc UserService = NewBasicUserService()
	for _, m := range middleware {
		svc = m(svc)
	}
	return svc
}

package gamehostsvc

import (
	"context"
	"log"

	repository "github.com/proepkes/speeddate/gamehostsvc/gen/repository"
)

// repository service example implementation.
// The example methods log the requests and return zero values.
type repositorySvc struct {
	logger *log.Logger
}

// NewRepository returns the repository service implementation.
func NewRepository(logger *log.Logger) repository.Service {
	return &repositorySvc{logger}
}

// Add new user and return its ID.
func (s *repositorySvc) Insert(ctx context.Context, p *repository.User) (res string, err error) {
	s.logger.Print("repository.insert")
	return
}

// Remove user from storage
func (s *repositorySvc) Delete(ctx context.Context, p *repository.DeletePayload) (err error) {
	s.logger.Print("repository.delete")
	return
}

// Get implements get.
func (s *repositorySvc) Get(ctx context.Context, p *repository.GetPayload) (res *repository.StoredUser, view string, err error) {
	res = &repository.StoredUser{}
	view = "default"
	s.logger.Print("repository.get")
	return
}

package user

import (
	"context"
	"log"
	usersvc "speeddate/usersvc/gen/user"
)

// user service example implementation.
// The example methods log the requests and return zero values.
type userSvc struct {
	logger *log.Logger
}

// NewUser returns the user service implementation.
func NewUser(logger *log.Logger) usersvc.Service {
	return &userSvc{logger}
}

// Add new bottle and return its ID.
func (s *userSvc) Insert(ctx context.Context, p *usersvc.User) (res string, err error) {
	s.logger.Print("user.insert")
	return
}

// Remove bottle from storage
func (s *userSvc) Delete(ctx context.Context, p *usersvc.DeletePayload) (err error) {
	s.logger.Print("user.delete")
	return
}

// Get implements get.
func (s *userSvc) Get(ctx context.Context, p *usersvc.GetPayload) (res *usersvc.StoredUser, err error) {
	res = &usersvc.StoredUser{}
	s.logger.Print("user.get")
	return
}

package storagesvc

import (
	"context"
	"crypto/ecdsa"
	"fmt"
	"io/ioutil"
	"log"
	"path/filepath"

	jwt "github.com/dgrijalva/jwt-go"
	"github.com/jinzhu/gorm"
	"github.com/proepkes/speeddate/storagesvc/gen/repository"
)

// repository service example implementation.
// The example methods log the requests and return zero values.
type repositorySvc struct {
	db        *Cockroach
	publicKey *ecdsa.PublicKey
	logger    *log.Logger
}

// NewRepository returns the repository service implementation.
func NewRepository(db *gorm.DB, logger *log.Logger) (repository.Service, error) {
	// Setup database
	cockroach, err := NewCockroachDB(db)
	if err != nil {
		logger.Fatalln("1. " + err.Error())
		return nil, err
	}

	//TODO: configurable path or secret
	abs, _ := filepath.Abs("../../keys/auth.key.pub")
	b, err := ioutil.ReadFile(abs)
	if err != nil {
		logger.Fatalln("2. " + err.Error())
		return nil, err
	}

	pubKey, err := jwt.ParseECPublicKeyFromPEM(b)
	if err != nil {
		logger.Fatalln("3. " + err.Error())
		return nil, err
	}

	// Build and return service implementation.
	return &repositorySvc{cockroach, pubKey, logger}, nil
}

// Add new user and return its ID.
func (s *repositorySvc) Insert(ctx context.Context, p *repository.User) (res string, err error) {
	su := repository.StoredUser{
		Name: p.Name,
	}
	if err = s.db.CreateUser(&su); err != nil {
		return "", err // internal error
	}
	return su.ID, nil
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
	if p.View != nil {
		view = *p.View
	} else {
		view = "default"
	}

	if err = s.db.GetUser(p.ID, &res); err != nil {
		if err == ErrNotFound {
			return nil, view, repository.MakeNotFound(fmt.Errorf("User not found"))
		}
		return nil, view, err // internal error
	}
	return res, view, nil
}

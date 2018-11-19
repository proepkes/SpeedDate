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
	"github.com/proepkes/speeddate/src/storagesvc/gen/authstorage"
)

// authstorage service example implementation.
// The example methods log the requests and return zero values.
type authstorageSvc struct {
	db        *Cockroach
	publicKey *ecdsa.PublicKey
	logger    *log.Logger
}

// NewAuthstorage returns the authstorage service implementation.
func NewAuthstorage(db *gorm.DB, logger *log.Logger) (authstorage.Service, error) {
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
	return &authstorageSvc{cockroach, pubKey, logger}, nil
}

// Add new user and return its ID.
func (s *authstorageSvc) Insert(ctx context.Context, p *authstorage.AuthUser) (res string, err error) {
	s.logger.Print("authstorage.insert")
	su := authstorage.StoredUser{
		Name: p.Name,
	}
	if err = s.db.CreateUser(&su); err != nil {
		return "", err // internal error
	}
	return su.ID, nil
}

// Remove user from storage
func (s *authstorageSvc) Delete(ctx context.Context, p *authstorage.DeletePayload) (err error) {
	s.logger.Print("authstorage.delete")
	return
}

// Get implements get.
func (s *authstorageSvc) Get(ctx context.Context, p *authstorage.GetPayload) (res *authstorage.StoredUser, view string, err error) {
	res = &authstorage.StoredUser{}
	view = "default"
	s.logger.Print("authstorage.get")
	if p.View != nil {
		view = *p.View
	} else {
		view = "default"
	}

	if err = s.db.GetUser(p.ID, &res); err != nil {
		if err == ErrNotFound {
			return nil, view, authstorage.MakeNotFound(fmt.Errorf("User not found"))
		}
		return nil, view, err // internal error
	}
	return res, view, nil
}

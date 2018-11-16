package authsvc

import (
	"context"
	"crypto/ecdsa"
	"fmt"
	"io/ioutil"
	"log"
	"path/filepath"
	"time"

	authorize "github.com/proepkes/speeddate/authsvc/gen/authorize"

	jwt "github.com/dgrijalva/jwt-go"
	jwtgo "github.com/dgrijalva/jwt-go"
)

// authorize service example implementation.
// The example methods log the requests and return zero values.
type authorizeSvc struct {
	logger     *log.Logger
	privateKey *ecdsa.PrivateKey
}

type defaultClaims struct {
	Scopes string `json:"scopes"`
	jwt.StandardClaims
}

// NewAuthorize returns the authorize service implementation.
func NewAuthorize(logger *log.Logger) authorize.Service {
	//TODO: configurable path or secret
	abs, _ := filepath.Abs("../../keys/secret.key")
	b, err := ioutil.ReadFile(abs)
	if err != nil {
		logger.Fatalln(err)
		return nil
	}

	privKey, err := jwtgo.ParseECPrivateKeyFromPEM(b)
	if err != nil {
		logger.Fatalln(err)
		return nil
	}

	return &authorizeSvc{
		logger:     logger,
		privateKey: privKey,
	}
}

// Creates a valid JWT
func (s *authorizeSvc) Login(ctx context.Context, p *authorize.LoginPayload) (res *authorize.LoginResult, err error) {
	res = &authorize.LoginResult{}

	s.logger.Print("authorize.login")

	// Create the Claims
	claims := defaultClaims{
		"api:read",
		jwt.StandardClaims{
			ExpiresAt: time.Now().Add(time.Duration(24) * time.Hour).Unix(),
			Issuer:    "SpeedDate",
		},
	}
	// Create a new token object, specifying signing method and the claims
	// you would like it to contain.
	token := jwt.NewWithClaims(jwt.SigningMethodES512, claims)
	signedToken, err := token.SignedString(s.privateKey)
	if err != nil {
		return nil, fmt.Errorf("failed to sign token: %s", err) // internal error
	}

	res.Token = signedToken

	return
}

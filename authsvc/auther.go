package authsvc

import (
	"context"
	"crypto/ecdsa"
	"fmt"
	"io/ioutil"
	"log"
	"time"

	authorize "github.com/proepkes/speeddate/authsvc/gen/authorize"

	"github.com/satori/go.uuid"

	jwt "github.com/dgrijalva/jwt-go"
	jwtgo "github.com/dgrijalva/jwt-go"
)

// authorize service example implementation.
// The example methods log the requests and return zero values.
type authorizeSvc struct {
	logger     *log.Logger
	privateKey *ecdsa.PrivateKey
}

// NewAuthorize returns the authorize service implementation.
func NewAuthorize(logger *log.Logger) authorize.Service {
	b, err := ioutil.ReadFile("./secret/secret.key")
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

	// Create a new token object, specifying signing method and the claims
	// you would like it to contain.
	in10m := time.Now().Add(time.Duration(10) * time.Minute).Unix()
	token := jwt.NewWithClaims(jwt.SigningMethodES512, jwt.MapClaims{
		"iss":    "Issuer",                         // who creates the token and signs it
		"aud":    "Audience",                       // to whom the token is intended to be sent
		"exp":    in10m,                            // time when the token will expire (10 minutes from now)
		"jti":    uuid.Must(uuid.NewV4()).String(), // a unique identifier for the token
		"iat":    time.Now().Unix(),                // when the token was issued/created (now)
		"nbf":    2,                                // time before which the token is not yet valid (2 minutes ago)
		"sub":    "subject",                        // the subject/principal is whom the token is about
		"scopes": "api:access",                     // token scope - not a standard claim
	})
	signedToken, err := token.SignedString(s.privateKey)
	if err != nil {
		return nil, fmt.Errorf("failed to sign token: %s", err) // internal error
	}

	res.Auth = signedToken

	return
}

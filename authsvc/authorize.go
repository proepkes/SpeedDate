package authsvc

import (
	"context"
	"log"
	"time"

	authorize "github.com/proepkes/speeddate/authsvc/gen/authorize"
	
	"github.com/satori/go.uuid"

	jwtgo "github.com/dgrijalva/jwt-go"
)

// authorize service example implementation.
// The example methods log the requests and return zero values.
type authorizeSvc struct {
	logger *log.Logger
}

// NewAuthorize returns the authorize service implementation.
func NewAuthorize(logger *log.Logger) authorize.Service {
	return &authorizeSvc{logger}
}

// Creates a valid JWT
func (s *authorizeSvc) Login(ctx context.Context, p *authorize.LoginPayload) (err error) {
	s.logger.Print("authorize.login")
	token := jwtgo.New(jwtgo.SigningMethodRS512)
	in10m := time.Now().Add(time.Duration(10) * time.Minute).Unix()
	token.Claims = jwtgo.MapClaims{
		"iss":    "Issuer",                         // who creates the token and signs it
		"aud":    "Audience",                       // to whom the token is intended to be sent
		"exp":    in10m,                            // time when the token will expire (10 minutes from now)
		"jti":    uuid.Must(uuid.NewV4()).String(), // a unique identifier for the token
		"iat":    time.Now().Unix(),                // when the token was issued/created (now)
		"nbf":    2,                                // time before which the token is not yet valid (2 minutes ago)
		"sub":    "subject",                        // the subject/principal is whom the token is about
		"scopes": "api:access",                     // token scope - not a standard claim
	}
	signedToken, err := token.SignedString(c.privateKey)
	if err != nil {
		return fmt.Errorf("failed to sign token: %s", err) // internal error
	}

	// Set auth header for client retrieval
	ctx.ResponseData.Header().Set("Authorization", "Bearer "+signedToken)

	return
}

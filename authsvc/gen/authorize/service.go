// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// authorize service
//
// Command:
// $ goa gen github.com/proepkes/speeddate/authsvc/design

package authorize

import (
	"context"

	"goa.design/goa/security"
)

// The service makes it possible to ...
type Service interface {
	// Creates a valid JWT
	Login(context.Context, *LoginPayload) (res *LoginResult, err error)
}

// Auther defines the authorization functions to be implemented by the service.
type Auther interface {
	// BasicAuth implements the authorization logic for the Basic security scheme.
	BasicAuth(ctx context.Context, user, pass string, schema *security.BasicScheme) (context.Context, error)
}

// ServiceName is the name of the service as defined in the design. This is the
// same value that is set in the endpoint request contexts under the ServiceKey
// key.
const ServiceName = "authorize"

// MethodNames lists the service method names as defined in the design. These
// are the same values that are set in the endpoint request contexts under the
// MethodKey key.
var MethodNames = [1]string{"login"}

// Credentials used to authenticate to retrieve JWT token
type LoginPayload struct {
	// Username used to perform signin
	Username string
	// Password used to perform signin
	Password string
}

// Result defines a JWT Token.
type LoginResult struct {
	// Resulting token
	Token string
}

package authsvc

import (
	"context"

	"goa.design/goa/security"
)

// BasicAuth implements the authorization logic for service "authorize" for the
// "basic" security scheme.
func (s *authorizeSvc) BasicAuth(ctx context.Context, user, pass string, scheme *security.BasicScheme) (context.Context, error) {
	//TODO: check user and pass against database
	return ctx, nil
}

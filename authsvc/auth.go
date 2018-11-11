package authsvc

import (
	"context"

	"goa.design/goa/security"
)

// AuthorizeBasicAuth implements the authorization logic for service
// "authorize" for the "basic" security scheme.
func AuthorizeBasicAuth(ctx context.Context, user, pass string, s *security.BasicScheme) (context.Context, error) {
	//
	// TBD: add authorization logic.
	//
	// In case of authorization failure this function should return
	// one of the generated error structs, e.g.:
	//
	//    return ctx, myservice.MakeUnauthorizedError("invalid token")
	//
	// Alternatively this function may return an instance of
	// goa.ServiceError with a Name field value that matches one of
	// the design error names, e.g:
	//
	//    return ctx, goa.PermanentError("unauthorized", "invalid token")
	//
	return ctx, nil // fmt.Errorf("not implemented")
}

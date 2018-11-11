package usersvc

import (
	"context"
	"fmt"

	"goa.design/goa/security"
)

// RepositoryJWTAuth implements the authorization logic for service
// "repository" for the "jwt" security scheme.
func RepositoryJWTAuth(ctx context.Context, token string, s *security.JWTScheme) (context.Context, error) {
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
	return ctx, fmt.Errorf("not implemented")
}

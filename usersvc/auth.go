package usersvc

import (
	"context"
	"fmt"
	"io/ioutil"
	"path/filepath"
	"sort"
	"strings"

	jwt "github.com/dgrijalva/jwt-go"
	"goa.design/goa/security"
)

// validScopeClaimKeys are the claims under which scopes may be found in a token
var validScopeClaimKeys = []string{"scope", "scopes"}

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

	//TODO: configurable path to secret
	abs, _ := filepath.Abs("../../../secret/secret.key.pub")
	b, err := ioutil.ReadFile(abs)
	privKey, err := jwt.ParseECPublicKeyFromPEM(b)
	if err != nil {
		return ctx, fmt.Errorf("not implemented")
	}

	parsedToken, err := jwt.Parse(token, func(t *jwt.Token) (interface{}, error) {
		if _, ok := t.Method.(*jwt.SigningMethodECDSA); !ok {
			return nil, fmt.Errorf("Unexpected signing method: %v", t.Header["alg"])
		}

		return privKey, nil
	})
	if parsedToken == nil {
		return ctx, fmt.Errorf("not implemented")
	}
	if !parsedToken.Valid {
		return ctx, fmt.Errorf("not implemented")
	}

	parseClaimScopes(parsedToken)

	return ctx, fmt.Errorf("not implemented")
}

// parseClaimScopes parses the "scope" or "scopes" parameter in the Claims. It
// supports two formats:
//
// * a list of strings
//
// * a single string with space-separated scopes (akin to OAuth2's "scope").
//
// An empty string is an explicit claim of no scopes.
func parseClaimScopes(token *jwt.Token) (map[string]bool, []string, error) {
	scopesInClaim := make(map[string]bool)
	var scopesInClaimList []string
	claims, ok := token.Claims.(jwt.MapClaims)
	if !ok {
		return nil, nil, fmt.Errorf("unsupported claims shape")
	}
	for _, k := range validScopeClaimKeys {
		if rawscopes, ok := claims[k]; ok && rawscopes != nil {
			switch scopes := rawscopes.(type) {
			case string:
				for _, scope := range strings.Split(scopes, " ") {
					scopesInClaim[scope] = true
					scopesInClaimList = append(scopesInClaimList, scope)
				}
			case []interface{}:
				for _, scope := range scopes {
					if val, ok := scope.(string); ok {
						scopesInClaim[val] = true
						scopesInClaimList = append(scopesInClaimList, val)
					}
				}
			default:
				return nil, nil, fmt.Errorf("unsupported scope format in incoming JWT claim, was type %T", scopes)
			}
			break
		}
	}
	sort.Strings(scopesInClaimList)
	return scopesInClaim, scopesInClaimList, nil
}

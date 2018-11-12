package usersvc

import (
	"context"
	"fmt"
	"io/ioutil"
	"path/filepath"
	"sort"
	"strings"

	jwt "github.com/dgrijalva/jwt-go"
	"github.com/proepkes/speeddate/usersvc/gen/repository"
	"goa.design/goa/security"
)

// JWTAuth implements the authorization logic for service "repository" for the
// "jwt" security scheme.
func (s *repositorySvc) JWTAuth(ctx context.Context, token string, scheme *security.JWTScheme) (context.Context, error) {
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

	if err != nil {
		return ctx, repository.MakeUnauthorized(err)
	}

	if !parsedToken.Valid {
		return ctx, repository.MakeUnauthorized(fmt.Errorf("Token invalid"))
	}

	scopesMap, _, err := parseClaimScopes(parsedToken)
	if err != nil {
		return ctx, repository.MakeUnauthorized(err)
	}

	if !scopesMap["api:read"] {
		return ctx, repository.MakeUnauthorized(fmt.Errorf("Scope invalid"))
	}

	return ctx, nil
}

// parseClaimScopes parses the "scope" or "scopes" parameter in the Claims. It
// supports two formats:
//
// * a list of strings
//
// * a single string with space-separated scopes
//
// An empty string is an explicit claim of no scopes.
func parseClaimScopes(token *jwt.Token) (map[string]bool, []string, error) {
	var validScopeClaimKeys = []string{"scope", "scopes"}

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

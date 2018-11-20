// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// matchmaking service
//
// Command:
// $ goa gen github.com/proepkes/speeddate/src/mmsvc/design

package matchmaking

import (
	"context"
)

// matchmaking.
type Service interface {
	// .
	Insert(context.Context) (res string, err error)
}

// ServiceName is the name of the service as defined in the design. This is the
// same value that is set in the endpoint request contexts under the ServiceKey
// key.
const ServiceName = "matchmaking"

// MethodNames lists the service method names as defined in the design. These
// are the same values that are set in the endpoint request contexts under the
// MethodKey key.
var MethodNames = [1]string{"insert"}

// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// fleet endpoints
//
// Command:
// $ goa gen github.com/proepkes/speeddate/src/spawnsvc/design

package fleet

import (
	"context"

	goa "goa.design/goa"
)

// Endpoints wraps the "fleet" service endpoints.
type Endpoints struct {
	Create        goa.Endpoint
	Delete        goa.Endpoint
	Patch         goa.Endpoint
	List          goa.Endpoint
	Allocate      goa.Endpoint
	Configuration goa.Endpoint
	Configure     goa.Endpoint
}

// NewEndpoints wraps the methods of the "fleet" service with endpoints.
func NewEndpoints(s Service) *Endpoints {
	return &Endpoints{
		Create:        NewCreateEndpoint(s),
		Delete:        NewDeleteEndpoint(s),
		Patch:         NewPatchEndpoint(s),
		List:          NewListEndpoint(s),
		Allocate:      NewAllocateEndpoint(s),
		Configuration: NewConfigurationEndpoint(s),
		Configure:     NewConfigureEndpoint(s),
	}
}

// Use applies the given middleware to all the "fleet" service endpoints.
func (e *Endpoints) Use(m func(goa.Endpoint) goa.Endpoint) {
	e.Create = m(e.Create)
	e.Delete = m(e.Delete)
	e.Patch = m(e.Patch)
	e.List = m(e.List)
	e.Allocate = m(e.Allocate)
	e.Configuration = m(e.Configuration)
	e.Configure = m(e.Configure)
}

// NewCreateEndpoint returns an endpoint function that calls the method
// "create" of service "fleet".
func NewCreateEndpoint(s Service) goa.Endpoint {
	return func(ctx context.Context, req interface{}) (interface{}, error) {
		p := req.(*Fleet)
		return s.Create(ctx, p)
	}
}

// NewDeleteEndpoint returns an endpoint function that calls the method
// "delete" of service "fleet".
func NewDeleteEndpoint(s Service) goa.Endpoint {
	return func(ctx context.Context, req interface{}) (interface{}, error) {
		p := req.(*DeletePayload)
		return nil, s.Delete(ctx, p)
	}
}

// NewPatchEndpoint returns an endpoint function that calls the method "patch"
// of service "fleet".
func NewPatchEndpoint(s Service) goa.Endpoint {
	return func(ctx context.Context, req interface{}) (interface{}, error) {
		p := req.(*PatchPayload)
		return s.Patch(ctx, p)
	}
}

// NewListEndpoint returns an endpoint function that calls the method "list" of
// service "fleet".
func NewListEndpoint(s Service) goa.Endpoint {
	return func(ctx context.Context, req interface{}) (interface{}, error) {
		p := req.(*ListPayload)
		res, err := s.List(ctx, p)
		if err != nil {
			return nil, err
		}
		vres := NewViewedStoredFleetCollection(res, "default")
		return vres, nil
	}
}

// NewAllocateEndpoint returns an endpoint function that calls the method
// "allocate" of service "fleet".
func NewAllocateEndpoint(s Service) goa.Endpoint {
	return func(ctx context.Context, req interface{}) (interface{}, error) {
		p := req.(*AllocatePayload)
		return s.Allocate(ctx, p)
	}
}

// NewConfigurationEndpoint returns an endpoint function that calls the method
// "configuration" of service "fleet".
func NewConfigurationEndpoint(s Service) goa.Endpoint {
	return func(ctx context.Context, req interface{}) (interface{}, error) {
		return s.Configuration(ctx)
	}
}

// NewConfigureEndpoint returns an endpoint function that calls the method
// "configure" of service "fleet".
func NewConfigureEndpoint(s Service) goa.Endpoint {
	return func(ctx context.Context, req interface{}) (interface{}, error) {
		p := req.(*ConfigurePayload)
		return s.Configure(ctx, p)
	}
}

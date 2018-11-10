package endpoint

import (
	"context"
	endpoint "github.com/go-kit/kit/endpoint"
	service "github.com/proepkes/SpeedDate/profile/pkg/service"
	gouuid "github.com/satori/go.uuid"
)

// GetRequest collects the request parameters for the Get method.
type GetRequest struct {
	Id gouuid.UUID `json:"id"`
}

// GetResponse collects the response parameters for the Get method.
type GetResponse struct {
	P   service.Profile `json:"p"`
	Err error           `json:"err"`
}

// MakeGetEndpoint returns an endpoint that invokes Get on the service.
func MakeGetEndpoint(s service.ProfileService) endpoint.Endpoint {
	return func(ctx context.Context, request interface{}) (interface{}, error) {
		req := request.(GetRequest)
		p, err := s.Get(ctx, req.Id)
		return GetResponse{
			Err: err,
			P:   p,
		}, nil
	}
}

// Failed implements Failer.
func (r GetResponse) Failed() error {
	return r.Err
}

// AddRequest collects the request parameters for the Add method.
type AddRequest struct {
	User service.Profile `json:"user"`
}

// AddResponse collects the response parameters for the Add method.
type AddResponse struct {
	E0 error `json:"e0"`
}

// MakeAddEndpoint returns an endpoint that invokes Add on the service.
func MakeAddEndpoint(s service.ProfileService) endpoint.Endpoint {
	return func(ctx context.Context, request interface{}) (interface{}, error) {
		req := request.(AddRequest)
		e0 := s.Add(ctx, req.User)
		return AddResponse{E0: e0}, nil
	}
}

// Failed implements Failer.
func (r AddResponse) Failed() error {
	return r.E0
}

// Failer is an interface that should be implemented by response types.
// Response encoders can check if responses are Failer, and if so they've
// failed, and if so encode them using a separate write path based on the error.
type Failure interface {
	Failed() error
}

// Get implements Service. Primarily useful in a client.
func (e Endpoints) Get(ctx context.Context, id gouuid.UUID) (p service.Profile, err error) {
	request := GetRequest{Id: id}
	response, err := e.GetEndpoint(ctx, request)
	if err != nil {
		return
	}
	return response.(GetResponse).P, response.(GetResponse).Err
}

// Add implements Service. Primarily useful in a client.
func (e Endpoints) Add(ctx context.Context, user service.Profile) (e0 error) {
	request := AddRequest{User: user}
	response, err := e.AddEndpoint(ctx, request)
	if err != nil {
		return
	}
	return response.(AddResponse).E0
}

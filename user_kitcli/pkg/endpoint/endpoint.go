package endpoint

import (
	"context"

	endpoint "github.com/go-kit/kit/endpoint"
	service "github.com/proepkes/SpeedDate/user/pkg/service"
	gouuid "github.com/satori/go.uuid"
)

// GetRequest collects the request parameters for the Get method.
type GetRequest struct {
	Id gouuid.UUID `json:"id"`
}

// GetResponse collects the response parameters for the Get method.
type GetResponse struct {
	User service.User `json:"user"`
	Err  error        `json:"err"`
}

// MakeGetEndpoint returns an endpoint that invokes Get on the service.
func MakeGetEndpoint(s service.UserService) endpoint.Endpoint {
	return func(ctx context.Context, request interface{}) (interface{}, error) {
		req := request.(GetRequest)
		user, err := s.Get(ctx, req.Id)
		return GetResponse{
			Err:  err,
			User: user,
		}, nil
	}
}

// Failed implements Failer.
func (r GetResponse) Failed() error {
	return r.Err
}

// AddRequest collects the request parameters for the Add method.
type AddRequest struct {
	User service.User `json:"user"`
}

// AddResponse collects the response parameters for the Add method.
type AddResponse struct {
	U   service.User `json:"u"`
	Err error        `json:"err"`
}

// MakeAddEndpoint returns an endpoint that invokes Add on the service.
func MakeAddEndpoint(s service.UserService) endpoint.Endpoint {
	return func(ctx context.Context, request interface{}) (interface{}, error) {
		req := request.(AddRequest)
		u, err := s.Add(ctx, req.User)
		return AddResponse{
			Err: err,
			U:   u,
		}, nil
	}
}

// Failed implements Failer.
func (r AddResponse) Failed() error {
	return r.Err
}

// Failer is an interface that should be implemented by response types.
// Response encoders can check if responses are Failer, and if so they've
// failed, and if so encode them using a separate write path based on the error.
type Failure interface {
	Failed() error
}

// Get implements Service. Primarily useful in a client.
func (e Endpoints) Get(ctx context.Context, id gouuid.UUID) (u service.User, err error) {
	request := GetRequest{Id: id}
	response, err := e.GetEndpoint(ctx, request)
	if err != nil {
		return
	}
	return response.(GetResponse).User, response.(GetResponse).Err
}

// Add implements Service. Primarily useful in a client.
func (e Endpoints) Add(ctx context.Context, user service.User) (u service.User, err error) {
	request := AddRequest{User: user}
	response, err := e.AddEndpoint(ctx, request)
	if err != nil {
		return
	}
	return response.(AddResponse).U, response.(AddResponse).Err
}

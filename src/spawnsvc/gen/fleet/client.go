// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// fleet client
//
// Command:
// $ goa gen github.com/proepkes/speeddate/src/spawnsvc/design

package fleet

import (
	"context"

	goa "goa.design/goa"
)

// Client is the "fleet" service client.
type Client struct {
	AddEndpoint           goa.Endpoint
	CreateEndpoint        goa.Endpoint
	DeleteEndpoint        goa.Endpoint
	ListEndpoint          goa.Endpoint
	ClearEndpoint         goa.Endpoint
	ConfigurationEndpoint goa.Endpoint
	ConfigureEndpoint     goa.Endpoint
}

// NewClient initializes a "fleet" service client given the endpoints.
func NewClient(add, create, delete_, list, clear, configuration, configure goa.Endpoint) *Client {
	return &Client{
		AddEndpoint:           add,
		CreateEndpoint:        create,
		DeleteEndpoint:        delete_,
		ListEndpoint:          list,
		ClearEndpoint:         clear,
		ConfigurationEndpoint: configuration,
		ConfigureEndpoint:     configure,
	}
}

// Add calls the "add" endpoint of the "fleet" service.
func (c *Client) Add(ctx context.Context) (res string, err error) {
	var ires interface{}
	ires, err = c.AddEndpoint(ctx, nil)
	if err != nil {
		return
	}
	return ires.(string), nil
}

// Create calls the "create" endpoint of the "fleet" service.
func (c *Client) Create(ctx context.Context, p *Fleet) (res string, err error) {
	var ires interface{}
	ires, err = c.CreateEndpoint(ctx, p)
	if err != nil {
		return
	}
	return ires.(string), nil
}

// Delete calls the "delete" endpoint of the "fleet" service.
// Delete may return the following errors:
//	- "not_found" (type *goa.ServiceError)
//	- error: internal error
func (c *Client) Delete(ctx context.Context, p *DeletePayload) (err error) {
	_, err = c.DeleteEndpoint(ctx, p)
	return
}

// List calls the "list" endpoint of the "fleet" service.
func (c *Client) List(ctx context.Context, p *ListPayload) (res StoredFleetCollection, err error) {
	var ires interface{}
	ires, err = c.ListEndpoint(ctx, p)
	if err != nil {
		return
	}
	return ires.(StoredFleetCollection), nil
}

// Clear calls the "clear" endpoint of the "fleet" service.
func (c *Client) Clear(ctx context.Context) (res string, err error) {
	var ires interface{}
	ires, err = c.ClearEndpoint(ctx, nil)
	if err != nil {
		return
	}
	return ires.(string), nil
}

// Configuration calls the "configuration" endpoint of the "fleet" service.
func (c *Client) Configuration(ctx context.Context) (res *GameserverTemplate, err error) {
	var ires interface{}
	ires, err = c.ConfigurationEndpoint(ctx, nil)
	if err != nil {
		return
	}
	return ires.(*GameserverTemplate), nil
}

// Configure calls the "configure" endpoint of the "fleet" service.
func (c *Client) Configure(ctx context.Context, p *GameserverTemplate) (res string, err error) {
	var ires interface{}
	ires, err = c.ConfigureEndpoint(ctx, p)
	if err != nil {
		return
	}
	return ires.(string), nil
}

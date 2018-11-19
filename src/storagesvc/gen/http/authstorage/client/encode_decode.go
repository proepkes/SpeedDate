// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// authstorage HTTP client encoders and decoders
//
// Command:
// $ goa gen github.com/proepkes/speeddate/src/storagesvc/design

package client

import (
	"bytes"
	"context"
	"io/ioutil"
	"net/http"
	"net/url"
	"strings"

	authstorage "github.com/proepkes/speeddate/src/storagesvc/gen/authstorage"
	authstorageviews "github.com/proepkes/speeddate/src/storagesvc/gen/authstorage/views"
	goahttp "goa.design/goa/http"
)

// BuildInsertRequest instantiates a HTTP request object with method and path
// set to call the "authstorage" service "insert" endpoint
func (c *Client) BuildInsertRequest(ctx context.Context, v interface{}) (*http.Request, error) {
	u := &url.URL{Scheme: c.scheme, Host: c.host, Path: InsertAuthstoragePath()}
	req, err := http.NewRequest("POST", u.String(), nil)
	if err != nil {
		return nil, goahttp.ErrInvalidURL("authstorage", "insert", u.String(), err)
	}
	if ctx != nil {
		req = req.WithContext(ctx)
	}

	return req, nil
}

// EncodeInsertRequest returns an encoder for requests sent to the authstorage
// insert server.
func EncodeInsertRequest(encoder func(*http.Request) goahttp.Encoder) func(*http.Request, interface{}) error {
	return func(req *http.Request, v interface{}) error {
		p, ok := v.(*authstorage.AuthUser)
		if !ok {
			return goahttp.ErrInvalidType("authstorage", "insert", "*authstorage.AuthUser", v)
		}
		body := NewInsertRequestBody(p)
		if err := encoder(req).Encode(&body); err != nil {
			return goahttp.ErrEncodingError("authstorage", "insert", err)
		}
		return nil
	}
}

// DecodeInsertResponse returns a decoder for responses returned by the
// authstorage insert endpoint. restoreBody controls whether the response body
// should be restored after having been read.
func DecodeInsertResponse(decoder func(*http.Response) goahttp.Decoder, restoreBody bool) func(*http.Response) (interface{}, error) {
	return func(resp *http.Response) (interface{}, error) {
		if restoreBody {
			b, err := ioutil.ReadAll(resp.Body)
			if err != nil {
				return nil, err
			}
			resp.Body = ioutil.NopCloser(bytes.NewBuffer(b))
			defer func() {
				resp.Body = ioutil.NopCloser(bytes.NewBuffer(b))
			}()
		} else {
			defer resp.Body.Close()
		}
		switch resp.StatusCode {
		case http.StatusCreated:
			var (
				body string
				err  error
			)
			err = decoder(resp).Decode(&body)
			if err != nil {
				return nil, goahttp.ErrDecodingError("authstorage", "insert", err)
			}
			return body, nil
		default:
			body, _ := ioutil.ReadAll(resp.Body)
			return nil, goahttp.ErrInvalidResponse("authstorage", "insert", resp.StatusCode, string(body))
		}
	}
}

// BuildDeleteRequest instantiates a HTTP request object with method and path
// set to call the "authstorage" service "delete" endpoint
func (c *Client) BuildDeleteRequest(ctx context.Context, v interface{}) (*http.Request, error) {
	var (
		id string
	)
	{
		p, ok := v.(*authstorage.DeletePayload)
		if !ok {
			return nil, goahttp.ErrInvalidType("authstorage", "delete", "*authstorage.DeletePayload", v)
		}
		id = p.ID
	}
	u := &url.URL{Scheme: c.scheme, Host: c.host, Path: DeleteAuthstoragePath(id)}
	req, err := http.NewRequest("DELETE", u.String(), nil)
	if err != nil {
		return nil, goahttp.ErrInvalidURL("authstorage", "delete", u.String(), err)
	}
	if ctx != nil {
		req = req.WithContext(ctx)
	}

	return req, nil
}

// DecodeDeleteResponse returns a decoder for responses returned by the
// authstorage delete endpoint. restoreBody controls whether the response body
// should be restored after having been read.
func DecodeDeleteResponse(decoder func(*http.Response) goahttp.Decoder, restoreBody bool) func(*http.Response) (interface{}, error) {
	return func(resp *http.Response) (interface{}, error) {
		if restoreBody {
			b, err := ioutil.ReadAll(resp.Body)
			if err != nil {
				return nil, err
			}
			resp.Body = ioutil.NopCloser(bytes.NewBuffer(b))
			defer func() {
				resp.Body = ioutil.NopCloser(bytes.NewBuffer(b))
			}()
		} else {
			defer resp.Body.Close()
		}
		switch resp.StatusCode {
		case http.StatusNoContent:
			return nil, nil
		default:
			body, _ := ioutil.ReadAll(resp.Body)
			return nil, goahttp.ErrInvalidResponse("authstorage", "delete", resp.StatusCode, string(body))
		}
	}
}

// BuildGetRequest instantiates a HTTP request object with method and path set
// to call the "authstorage" service "get" endpoint
func (c *Client) BuildGetRequest(ctx context.Context, v interface{}) (*http.Request, error) {
	var (
		id string
	)
	{
		p, ok := v.(*authstorage.GetPayload)
		if !ok {
			return nil, goahttp.ErrInvalidType("authstorage", "get", "*authstorage.GetPayload", v)
		}
		id = p.ID
	}
	u := &url.URL{Scheme: c.scheme, Host: c.host, Path: GetAuthstoragePath(id)}
	req, err := http.NewRequest("GET", u.String(), nil)
	if err != nil {
		return nil, goahttp.ErrInvalidURL("authstorage", "get", u.String(), err)
	}
	if ctx != nil {
		req = req.WithContext(ctx)
	}

	return req, nil
}

// EncodeGetRequest returns an encoder for requests sent to the authstorage get
// server.
func EncodeGetRequest(encoder func(*http.Request) goahttp.Encoder) func(*http.Request, interface{}) error {
	return func(req *http.Request, v interface{}) error {
		p, ok := v.(*authstorage.GetPayload)
		if !ok {
			return goahttp.ErrInvalidType("authstorage", "get", "*authstorage.GetPayload", v)
		}
		if p.Token != nil {
			if !strings.Contains(*p.Token, " ") {
				req.Header.Set("Authorization", "Bearer "+*p.Token)
			} else {
				req.Header.Set("Authorization", *p.Token)
			}
		}
		values := req.URL.Query()
		if p.View != nil {
			values.Add("view", *p.View)
		}
		req.URL.RawQuery = values.Encode()
		return nil
	}
}

// DecodeGetResponse returns a decoder for responses returned by the
// authstorage get endpoint. restoreBody controls whether the response body
// should be restored after having been read.
// DecodeGetResponse may return the following errors:
//	- "not_found" (type *goa.ServiceError): http.StatusNotFound
//	- "unauthorized" (type *goa.ServiceError): http.StatusUnauthorized
//	- error: internal error
func DecodeGetResponse(decoder func(*http.Response) goahttp.Decoder, restoreBody bool) func(*http.Response) (interface{}, error) {
	return func(resp *http.Response) (interface{}, error) {
		if restoreBody {
			b, err := ioutil.ReadAll(resp.Body)
			if err != nil {
				return nil, err
			}
			resp.Body = ioutil.NopCloser(bytes.NewBuffer(b))
			defer func() {
				resp.Body = ioutil.NopCloser(bytes.NewBuffer(b))
			}()
		} else {
			defer resp.Body.Close()
		}
		switch resp.StatusCode {
		case http.StatusOK:
			var (
				body GetResponseBody
				err  error
			)
			err = decoder(resp).Decode(&body)
			if err != nil {
				return nil, goahttp.ErrDecodingError("authstorage", "get", err)
			}
			p := NewGetStoredUserOK(&body)
			view := resp.Header.Get("goa-view")
			vres := &authstorageviews.StoredUser{p, view}
			if err = vres.Validate(); err != nil {
				return nil, goahttp.ErrValidationError("authstorage", "get", err)
			}
			res := authstorage.NewStoredUser(vres)
			return res, nil
		case http.StatusNotFound:
			var (
				body GetNotFoundResponseBody
				err  error
			)
			err = decoder(resp).Decode(&body)
			if err != nil {
				return nil, goahttp.ErrDecodingError("authstorage", "get", err)
			}
			err = body.Validate()
			if err != nil {
				return nil, goahttp.ErrValidationError("authstorage", "get", err)
			}
			return nil, NewGetNotFound(&body)
		case http.StatusUnauthorized:
			var (
				body GetUnauthorizedResponseBody
				err  error
			)
			err = decoder(resp).Decode(&body)
			if err != nil {
				return nil, goahttp.ErrDecodingError("authstorage", "get", err)
			}
			err = body.Validate()
			if err != nil {
				return nil, goahttp.ErrValidationError("authstorage", "get", err)
			}
			return nil, NewGetUnauthorized(&body)
		default:
			body, _ := ioutil.ReadAll(resp.Body)
			return nil, goahttp.ErrInvalidResponse("authstorage", "get", resp.StatusCode, string(body))
		}
	}
}
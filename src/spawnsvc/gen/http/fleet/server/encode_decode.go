// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// fleet HTTP server encoders and decoders
//
// Command:
// $ goa gen github.com/proepkes/speeddate/src/spawnsvc/design

package server

import (
	"context"
	"io"
	"net/http"

	fleet "github.com/proepkes/speeddate/src/spawnsvc/gen/fleet"
	fleetviews "github.com/proepkes/speeddate/src/spawnsvc/gen/fleet/views"
	goa "goa.design/goa"
	goahttp "goa.design/goa/http"
)

// EncodeAddResponse returns an encoder for responses returned by the fleet add
// endpoint.
func EncodeAddResponse(encoder func(context.Context, http.ResponseWriter) goahttp.Encoder) func(context.Context, http.ResponseWriter, interface{}) error {
	return func(ctx context.Context, w http.ResponseWriter, v interface{}) error {
		res := v.(string)
		enc := encoder(ctx, w)
		body := res
		w.WriteHeader(http.StatusCreated)
		return enc.Encode(body)
	}
}

// EncodeCreateResponse returns an encoder for responses returned by the fleet
// create endpoint.
func EncodeCreateResponse(encoder func(context.Context, http.ResponseWriter) goahttp.Encoder) func(context.Context, http.ResponseWriter, interface{}) error {
	return func(ctx context.Context, w http.ResponseWriter, v interface{}) error {
		res := v.(string)
		enc := encoder(ctx, w)
		body := res
		w.WriteHeader(http.StatusCreated)
		return enc.Encode(body)
	}
}

// DecodeCreateRequest returns a decoder for requests sent to the fleet create
// endpoint.
func DecodeCreateRequest(mux goahttp.Muxer, decoder func(*http.Request) goahttp.Decoder) func(*http.Request) (interface{}, error) {
	return func(r *http.Request) (interface{}, error) {
		var (
			body CreateRequestBody
			err  error
		)
		err = decoder(r).Decode(&body)
		if err != nil {
			if err == io.EOF {
				return nil, goa.MissingPayloadError()
			}
			return nil, goa.DecodePayloadError(err.Error())
		}
		err = body.Validate()
		if err != nil {
			return nil, err
		}
		payload := NewCreateFleet(&body)

		return payload, nil
	}
}

// EncodeDeleteResponse returns an encoder for responses returned by the fleet
// delete endpoint.
func EncodeDeleteResponse(encoder func(context.Context, http.ResponseWriter) goahttp.Encoder) func(context.Context, http.ResponseWriter, interface{}) error {
	return func(ctx context.Context, w http.ResponseWriter, v interface{}) error {
		w.WriteHeader(http.StatusNoContent)
		return nil
	}
}

// DecodeDeleteRequest returns a decoder for requests sent to the fleet delete
// endpoint.
func DecodeDeleteRequest(mux goahttp.Muxer, decoder func(*http.Request) goahttp.Decoder) func(*http.Request) (interface{}, error) {
	return func(r *http.Request) (interface{}, error) {
		var (
			name      string
			namespace string

			params = mux.Vars(r)
		)
		name = params["name"]
		namespaceRaw := r.URL.Query().Get("namespace")
		if namespaceRaw != "" {
			namespace = namespaceRaw
		} else {
			namespace = "default"
		}
		payload := NewDeletePayload(name, namespace)

		return payload, nil
	}
}

// EncodeListResponse returns an encoder for responses returned by the fleet
// list endpoint.
func EncodeListResponse(encoder func(context.Context, http.ResponseWriter) goahttp.Encoder) func(context.Context, http.ResponseWriter, interface{}) error {
	return func(ctx context.Context, w http.ResponseWriter, v interface{}) error {
		res := v.(fleetviews.StoredFleetCollection)
		enc := encoder(ctx, w)
		body := NewStoredFleetResponseCollection(res.Projected)
		w.WriteHeader(http.StatusOK)
		return enc.Encode(body)
	}
}

// DecodeListRequest returns a decoder for requests sent to the fleet list
// endpoint.
func DecodeListRequest(mux goahttp.Muxer, decoder func(*http.Request) goahttp.Decoder) func(*http.Request) (interface{}, error) {
	return func(r *http.Request) (interface{}, error) {
		var (
			namespace string
			view      *string
			err       error
		)
		namespaceRaw := r.URL.Query().Get("namespace")
		if namespaceRaw != "" {
			namespace = namespaceRaw
		} else {
			namespace = "default"
		}
		viewRaw := r.URL.Query().Get("view")
		if viewRaw != "" {
			view = &viewRaw
		}
		if view != nil {
			if !(*view == "default") {
				err = goa.MergeErrors(err, goa.InvalidEnumValueError("view", *view, []interface{}{"default"}))
			}
		}
		if err != nil {
			return nil, err
		}
		payload := NewListPayload(namespace, view)

		return payload, nil
	}
}

// EncodeClearResponse returns an encoder for responses returned by the fleet
// clear endpoint.
func EncodeClearResponse(encoder func(context.Context, http.ResponseWriter) goahttp.Encoder) func(context.Context, http.ResponseWriter, interface{}) error {
	return func(ctx context.Context, w http.ResponseWriter, v interface{}) error {
		res := v.(string)
		enc := encoder(ctx, w)
		body := res
		w.WriteHeader(http.StatusOK)
		return enc.Encode(body)
	}
}

// EncodeConfigurationResponse returns an encoder for responses returned by the
// fleet configuration endpoint.
func EncodeConfigurationResponse(encoder func(context.Context, http.ResponseWriter) goahttp.Encoder) func(context.Context, http.ResponseWriter, interface{}) error {
	return func(ctx context.Context, w http.ResponseWriter, v interface{}) error {
		res := v.(*fleet.GameserverTemplate)
		enc := encoder(ctx, w)
		body := NewConfigurationResponseBody(res)
		w.WriteHeader(http.StatusOK)
		return enc.Encode(body)
	}
}

// EncodeConfigureResponse returns an encoder for responses returned by the
// fleet configure endpoint.
func EncodeConfigureResponse(encoder func(context.Context, http.ResponseWriter) goahttp.Encoder) func(context.Context, http.ResponseWriter, interface{}) error {
	return func(ctx context.Context, w http.ResponseWriter, v interface{}) error {
		res := v.(string)
		enc := encoder(ctx, w)
		body := res
		w.WriteHeader(http.StatusOK)
		return enc.Encode(body)
	}
}

// DecodeConfigureRequest returns a decoder for requests sent to the fleet
// configure endpoint.
func DecodeConfigureRequest(mux goahttp.Muxer, decoder func(*http.Request) goahttp.Decoder) func(*http.Request) (interface{}, error) {
	return func(r *http.Request) (interface{}, error) {
		var (
			body ConfigureRequestBody
			err  error
		)
		err = decoder(r).Decode(&body)
		if err != nil {
			if err == io.EOF {
				return nil, goa.MissingPayloadError()
			}
			return nil, goa.DecodePayloadError(err.Error())
		}
		err = body.Validate()
		if err != nil {
			return nil, err
		}
		payload := NewConfigureGameserverTemplate(&body)

		return payload, nil
	}
}

// unmarshalObjectMetaRequestBodyToObjectMeta builds a value of type
// *fleet.ObjectMeta from a value of type *ObjectMetaRequestBody.
func unmarshalObjectMetaRequestBodyToObjectMeta(v *ObjectMetaRequestBody) *fleet.ObjectMeta {
	if v == nil {
		return nil
	}
	res := &fleet.ObjectMeta{
		GenerateName: *v.GenerateName,
		Namespace:    *v.Namespace,
	}

	return res
}

// unmarshalFleetSpecRequestBodyToFleetSpec builds a value of type
// *fleet.FleetSpec from a value of type *FleetSpecRequestBody.
func unmarshalFleetSpecRequestBodyToFleetSpec(v *FleetSpecRequestBody) *fleet.FleetSpec {
	res := &fleet.FleetSpec{
		Replicas: *v.Replicas,
	}
	res.Template = unmarshalGameserverTemplateRequestBodyToGameserverTemplate(v.Template)

	return res
}

// unmarshalGameserverTemplateRequestBodyToGameserverTemplate builds a value of
// type *fleet.GameserverTemplate from a value of type
// *GameserverTemplateRequestBody.
func unmarshalGameserverTemplateRequestBodyToGameserverTemplate(v *GameserverTemplateRequestBody) *fleet.GameserverTemplate {
	res := &fleet.GameserverTemplate{}
	if v.ObjectMeta != nil {
		res.ObjectMeta = unmarshalObjectMetaRequestBodyToObjectMeta(v.ObjectMeta)
	}
	res.GameServerSpec = unmarshalGameServerSpecRequestBodyToGameServerSpec(v.GameServerSpec)

	return res
}

// unmarshalGameServerSpecRequestBodyToGameServerSpec builds a value of type
// *fleet.GameServerSpec from a value of type *GameServerSpecRequestBody.
func unmarshalGameServerSpecRequestBodyToGameServerSpec(v *GameServerSpecRequestBody) *fleet.GameServerSpec {
	res := &fleet.GameServerSpec{
		ContainerName:  *v.ContainerName,
		ContainerImage: *v.ContainerImage,
		ContainerPort:  *v.ContainerPort,
	}
	if v.PortPolicy != nil {
		res.PortPolicy = *v.PortPolicy
	}
	if v.PortPolicy == nil {
		res.PortPolicy = "dynamic"
	}

	return res
}

// marshalObjectMetaViewToObjectMetaResponse builds a value of type
// *ObjectMetaResponse from a value of type *fleetviews.ObjectMetaView.
func marshalObjectMetaViewToObjectMetaResponse(v *fleetviews.ObjectMetaView) *ObjectMetaResponse {
	res := &ObjectMetaResponse{
		GenerateName: *v.GenerateName,
		Namespace:    *v.Namespace,
	}

	return res
}

// marshalFleetSpecViewToFleetSpecResponse builds a value of type
// *FleetSpecResponse from a value of type *fleetviews.FleetSpecView.
func marshalFleetSpecViewToFleetSpecResponse(v *fleetviews.FleetSpecView) *FleetSpecResponse {
	res := &FleetSpecResponse{
		Replicas: *v.Replicas,
	}
	if v.Template != nil {
		res.Template = marshalGameserverTemplateViewToGameserverTemplateResponse(v.Template)
	}

	return res
}

// marshalGameserverTemplateViewToGameserverTemplateResponse builds a value of
// type *GameserverTemplateResponse from a value of type
// *fleetviews.GameserverTemplateView.
func marshalGameserverTemplateViewToGameserverTemplateResponse(v *fleetviews.GameserverTemplateView) *GameserverTemplateResponse {
	res := &GameserverTemplateResponse{}
	if v.ObjectMeta != nil {
		res.ObjectMeta = marshalObjectMetaViewToObjectMetaResponse(v.ObjectMeta)
	}
	if v.GameServerSpec != nil {
		res.GameServerSpec = marshalGameServerSpecViewToGameServerSpecResponse(v.GameServerSpec)
	}

	return res
}

// marshalGameServerSpecViewToGameServerSpecResponse builds a value of type
// *GameServerSpecResponse from a value of type *fleetviews.GameServerSpecView.
func marshalGameServerSpecViewToGameServerSpecResponse(v *fleetviews.GameServerSpecView) *GameServerSpecResponse {
	res := &GameServerSpecResponse{
		ContainerName:  *v.ContainerName,
		ContainerImage: *v.ContainerImage,
		ContainerPort:  *v.ContainerPort,
	}
	if v.PortPolicy != nil {
		res.PortPolicy = *v.PortPolicy
	}
	if v.PortPolicy == nil {
		res.PortPolicy = "dynamic"
	}

	return res
}

// marshalFleetStatusViewToFleetStatusResponse builds a value of type
// *FleetStatusResponse from a value of type *fleetviews.FleetStatusView.
func marshalFleetStatusViewToFleetStatusResponse(v *fleetviews.FleetStatusView) *FleetStatusResponse {
	if v == nil {
		return nil
	}
	res := &FleetStatusResponse{
		Replicas:          *v.Replicas,
		ReadyReplicas:     *v.ReadyReplicas,
		AllocatedReplicas: *v.AllocatedReplicas,
	}

	return res
}

// marshalObjectMetaToObjectMetaResponseBody builds a value of type
// *ObjectMetaResponseBody from a value of type *fleet.ObjectMeta.
func marshalObjectMetaToObjectMetaResponseBody(v *fleet.ObjectMeta) *ObjectMetaResponseBody {
	if v == nil {
		return nil
	}
	res := &ObjectMetaResponseBody{
		GenerateName: v.GenerateName,
		Namespace:    v.Namespace,
	}

	return res
}

// marshalGameServerSpecToGameServerSpecResponseBody builds a value of type
// *GameServerSpecResponseBody from a value of type *fleet.GameServerSpec.
func marshalGameServerSpecToGameServerSpecResponseBody(v *fleet.GameServerSpec) *GameServerSpecResponseBody {
	res := &GameServerSpecResponseBody{
		PortPolicy:     v.PortPolicy,
		ContainerName:  v.ContainerName,
		ContainerImage: v.ContainerImage,
		ContainerPort:  v.ContainerPort,
	}

	return res
}

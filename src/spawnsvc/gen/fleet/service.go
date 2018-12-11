// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// fleet service
//
// Command:
// $ goa gen github.com/proepkes/speeddate/src/spawnsvc/design

package fleet

import (
	"context"

	fleetviews "github.com/proepkes/speeddate/src/spawnsvc/gen/fleet/views"
	"goa.design/goa"
)

// The service makes it possible to manage gameservers
type Service interface {
	// Create a new fleet.
	Create(context.Context, *Fleet) (res string, err error)
	// Delete a fleet
	Delete(context.Context, *DeletePayload) (err error)
	// Patch a fleet.
	Patch(context.Context, *PatchPayload) (res string, err error)
	// List all fleets.
	List(context.Context, *ListPayload) (res StoredFleetCollection, err error)
	// Create a fleetallocation.
	Allocate(context.Context, *AllocatePayload) (res string, err error)
	// Get default fleet configuration.
	Configuration(context.Context) (res *Fleet, err error)
	// Configure default fleet options.
	Configure(context.Context, *ConfigurePayload) (res string, err error)
}

// ServiceName is the name of the service as defined in the design. This is the
// same value that is set in the endpoint request contexts under the ServiceKey
// key.
const ServiceName = "fleet"

// MethodNames lists the service method names as defined in the design. These
// are the same values that are set in the endpoint request contexts under the
// MethodKey key.
var MethodNames = [7]string{"create", "delete", "patch", "list", "allocate", "configuration", "configure"}

// Fleet is the payload type of the fleet service create method.
type Fleet struct {
	// Fleets ObjectMeta
	ObjectMeta *ObjectMeta
	// FleetSpec
	FleetSpec *FleetSpec
}

// DeletePayload is the payload type of the fleet service delete method.
type DeletePayload struct {
	// The namespace
	Namespace string
	// Name of the fleet
	Name string
}

// PatchPayload is the payload type of the fleet service patch method.
type PatchPayload struct {
	// The namespace
	Namespace string
	// Name of the fleet
	Name string
	// Set replicas value
	Replicas *uint32
}

// ListPayload is the payload type of the fleet service list method.
type ListPayload struct {
	// The namespace
	Namespace string
	// View to render
	View *string
}

// StoredFleetCollection is the result type of the fleet service list method.
type StoredFleetCollection []*StoredFleet

// AllocatePayload is the payload type of the fleet service allocate method.
type AllocatePayload struct {
	// Name of the fleet to allocate from
	Fleet string
	// Must match the namespace of the fleet
	Namespace string
	// Nameprefix for the allocation
	Name string
}

// ConfigurePayload is the payload type of the fleet service configure method.
type ConfigurePayload struct {
	// The NamePrefix
	NamePrefix string
	// The ContainerImage
	ContainerImage string
	// The ContainerName
	ContainerName string
	// The ContainerPort
	ContainerPort int32
	// The GameserverNamePrefix
	GameserverNamePrefix string
	// The Namespace
	Namespace string
	// The Replicas
	Replicas uint32
}

// Spec for ObjectMeta
type ObjectMeta struct {
	// Prefix for the generated fleetname
	GenerateName string
	// Namespace where the fleet will run in
	Namespace string
}

// Spec for Fleet
type FleetSpec struct {
	// Replicas
	Replicas int32
	// Template of the gameserver
	Template *GameserverTemplate
}

// GameserverTemplate describes gameserver
type GameserverTemplate struct {
	// GameserverTemplates ObjectMeta
	ObjectMeta *ObjectMeta
	// GameServerSpec
	GameServerSpec *GameServerSpec
}

// GameserverTemplate describes gameserver
type GameServerSpec struct {
	// Portpolicy either dynamic or static
	PortPolicy string
	// Name of the gameserver-container
	ContainerName string
	// Image of the gameserver
	ContainerImage string
	// Exposed port of the gameserver
	ContainerPort int32
}

// Stored Fleet
type StoredFleet struct {
	// The Fleets Name
	Name string
	// The Fleets ObjectMeta
	ObjectMeta *ObjectMeta
	// The FleetSpec
	FleetSpec *FleetSpec
	// The FleetStatus
	FleetStatus *FleetStatus
}

type FleetStatus struct {
	// Replicas
	Replicas int32
	// ReadyReplicas
	ReadyReplicas int32
	// AllocatedReplicas
	AllocatedReplicas int32
}

// MakeNotFound builds a goa.ServiceError from an error.
func MakeNotFound(err error) *goa.ServiceError {
	return &goa.ServiceError{
		Name:    "not_found",
		ID:      goa.NewErrorID(),
		Message: err.Error(),
	}
}

// NewStoredFleetCollection initializes result type StoredFleetCollection from
// viewed result type StoredFleetCollection.
func NewStoredFleetCollection(vres fleetviews.StoredFleetCollection) StoredFleetCollection {
	var res StoredFleetCollection
	switch vres.View {
	case "default", "":
		res = newStoredFleetCollection(vres.Projected)
	}
	return res
}

// NewViewedStoredFleetCollection initializes viewed result type
// StoredFleetCollection from result type StoredFleetCollection using the given
// view.
func NewViewedStoredFleetCollection(res StoredFleetCollection, view string) fleetviews.StoredFleetCollection {
	var vres fleetviews.StoredFleetCollection
	switch view {
	case "default", "":
		p := newStoredFleetCollectionView(res)
		vres = fleetviews.StoredFleetCollection{p, "default"}
	}
	return vres
}

// newStoredFleetCollection converts projected type StoredFleetCollection to
// service type StoredFleetCollection.
func newStoredFleetCollection(vres fleetviews.StoredFleetCollectionView) StoredFleetCollection {
	res := make(StoredFleetCollection, len(vres))
	for i, n := range vres {
		res[i] = newStoredFleet(n)
	}
	return res
}

// newStoredFleetCollectionView projects result type StoredFleetCollection into
// projected type StoredFleetCollectionView using the "default" view.
func newStoredFleetCollectionView(res StoredFleetCollection) fleetviews.StoredFleetCollectionView {
	vres := make(fleetviews.StoredFleetCollectionView, len(res))
	for i, n := range res {
		vres[i] = newStoredFleetView(n)
	}
	return vres
}

// newStoredFleet converts projected type StoredFleet to service type
// StoredFleet.
func newStoredFleet(vres *fleetviews.StoredFleetView) *StoredFleet {
	res := &StoredFleet{}
	if vres.Name != nil {
		res.Name = *vres.Name
	}
	if vres.ObjectMeta != nil {
		res.ObjectMeta = unmarshalObjectMetaViewToObjectMeta(vres.ObjectMeta)
	}
	if vres.FleetSpec != nil {
		res.FleetSpec = unmarshalFleetSpecViewToFleetSpec(vres.FleetSpec)
	}
	if vres.FleetStatus != nil {
		res.FleetStatus = unmarshalFleetStatusViewToFleetStatus(vres.FleetStatus)
	}
	return res
}

// newStoredFleetView projects result type StoredFleet into projected type
// StoredFleetView using the "default" view.
func newStoredFleetView(res *StoredFleet) *fleetviews.StoredFleetView {
	vres := &fleetviews.StoredFleetView{
		Name: &res.Name,
	}
	if res.ObjectMeta != nil {
		vres.ObjectMeta = marshalObjectMetaToObjectMetaView(res.ObjectMeta)
	}
	if res.FleetSpec != nil {
		vres.FleetSpec = marshalFleetSpecToFleetSpecView(res.FleetSpec)
	}
	if res.FleetStatus != nil {
		vres.FleetStatus = marshalFleetStatusToFleetStatusView(res.FleetStatus)
	}
	return vres
}

// unmarshalObjectMetaViewToObjectMeta builds a value of type *ObjectMeta from
// a value of type *fleetviews.ObjectMetaView.
func unmarshalObjectMetaViewToObjectMeta(v *fleetviews.ObjectMetaView) *ObjectMeta {
	if v == nil {
		return nil
	}
	res := &ObjectMeta{
		GenerateName: *v.GenerateName,
		Namespace:    *v.Namespace,
	}

	return res
}

// unmarshalFleetSpecViewToFleetSpec builds a value of type *FleetSpec from a
// value of type *fleetviews.FleetSpecView.
func unmarshalFleetSpecViewToFleetSpec(v *fleetviews.FleetSpecView) *FleetSpec {
	if v == nil {
		return nil
	}
	res := &FleetSpec{
		Replicas: *v.Replicas,
	}
	res.Template = unmarshalGameserverTemplateViewToGameserverTemplate(v.Template)

	return res
}

// unmarshalGameserverTemplateViewToGameserverTemplate builds a value of type
// *GameserverTemplate from a value of type *fleetviews.GameserverTemplateView.
func unmarshalGameserverTemplateViewToGameserverTemplate(v *fleetviews.GameserverTemplateView) *GameserverTemplate {
	res := &GameserverTemplate{}
	if v.ObjectMeta != nil {
		res.ObjectMeta = unmarshalObjectMetaViewToObjectMeta(v.ObjectMeta)
	}
	res.GameServerSpec = unmarshalGameServerSpecViewToGameServerSpec(v.GameServerSpec)

	return res
}

// unmarshalGameServerSpecViewToGameServerSpec builds a value of type
// *GameServerSpec from a value of type *fleetviews.GameServerSpecView.
func unmarshalGameServerSpecViewToGameServerSpec(v *fleetviews.GameServerSpecView) *GameServerSpec {
	res := &GameServerSpec{
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

// unmarshalFleetStatusViewToFleetStatus builds a value of type *FleetStatus
// from a value of type *fleetviews.FleetStatusView.
func unmarshalFleetStatusViewToFleetStatus(v *fleetviews.FleetStatusView) *FleetStatus {
	if v == nil {
		return nil
	}
	res := &FleetStatus{
		Replicas:          *v.Replicas,
		ReadyReplicas:     *v.ReadyReplicas,
		AllocatedReplicas: *v.AllocatedReplicas,
	}

	return res
}

// marshalObjectMetaToObjectMetaView builds a value of type
// *fleetviews.ObjectMetaView from a value of type *ObjectMeta.
func marshalObjectMetaToObjectMetaView(v *ObjectMeta) *fleetviews.ObjectMetaView {
	res := &fleetviews.ObjectMetaView{
		GenerateName: &v.GenerateName,
		Namespace:    &v.Namespace,
	}

	return res
}

// marshalFleetSpecToFleetSpecView builds a value of type
// *fleetviews.FleetSpecView from a value of type *FleetSpec.
func marshalFleetSpecToFleetSpecView(v *FleetSpec) *fleetviews.FleetSpecView {
	res := &fleetviews.FleetSpecView{
		Replicas: &v.Replicas,
	}
	if v.Template != nil {
		res.Template = marshalGameserverTemplateToGameserverTemplateView(v.Template)
	}

	return res
}

// marshalGameserverTemplateToGameserverTemplateView builds a value of type
// *fleetviews.GameserverTemplateView from a value of type *GameserverTemplate.
func marshalGameserverTemplateToGameserverTemplateView(v *GameserverTemplate) *fleetviews.GameserverTemplateView {
	res := &fleetviews.GameserverTemplateView{}
	if v.ObjectMeta != nil {
		res.ObjectMeta = marshalObjectMetaToObjectMetaView(v.ObjectMeta)
	}
	if v.GameServerSpec != nil {
		res.GameServerSpec = marshalGameServerSpecToGameServerSpecView(v.GameServerSpec)
	}

	return res
}

// marshalGameServerSpecToGameServerSpecView builds a value of type
// *fleetviews.GameServerSpecView from a value of type *GameServerSpec.
func marshalGameServerSpecToGameServerSpecView(v *GameServerSpec) *fleetviews.GameServerSpecView {
	res := &fleetviews.GameServerSpecView{
		PortPolicy:     &v.PortPolicy,
		ContainerName:  &v.ContainerName,
		ContainerImage: &v.ContainerImage,
		ContainerPort:  &v.ContainerPort,
	}

	return res
}

// marshalFleetStatusToFleetStatusView builds a value of type
// *fleetviews.FleetStatusView from a value of type *FleetStatus.
func marshalFleetStatusToFleetStatusView(v *FleetStatus) *fleetviews.FleetStatusView {
	if v == nil {
		return nil
	}
	res := &fleetviews.FleetStatusView{
		Replicas:          &v.Replicas,
		ReadyReplicas:     &v.ReadyReplicas,
		AllocatedReplicas: &v.AllocatedReplicas,
	}

	return res
}

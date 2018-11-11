// Code generated by goa v2.0.0-wip, DO NOT EDIT.
//
// user service
//
// Command:
// $ goa gen speeddate/usersvc/design

package usersvc

import (
	"context"
	usersvcviews "speeddate/usersvc/gen/user/views"
)

// The storage service makes it possible to view, add or remove wine bottles.
type Service interface {
	// Add new bottle and return its ID.
	Insert(context.Context, *User) (res string, err error)
	// Remove bottle from storage
	Delete(context.Context, *DeletePayload) (err error)
	// Get implements get.
	Get(context.Context, *GetPayload) (res *StoredUser, err error)
}

// ServiceName is the name of the service as defined in the design. This is the
// same value that is set in the endpoint request contexts under the ServiceKey
// key.
const ServiceName = "user"

// MethodNames lists the service method names as defined in the design. These
// are the same values that are set in the endpoint request contexts under the
// MethodKey key.
var MethodNames = [3]string{"insert", "delete", "get"}

// User is the payload type of the user service insert method.
type User struct {
	// The username
	Name string
	// Indicates whether the user is currently online.
	Online bool
}

// DeletePayload is the payload type of the user service delete method.
type DeletePayload struct {
	// ID of bottle to remove
	ID string
}

// GetPayload is the payload type of the user service get method.
type GetPayload struct {
	// ID of bottle to remove
	ID string
}

// StoredUser is the result type of the user service get method.
type StoredUser struct {
	// UUID is the unique id of the user.
	ID string
	// The username
	Name *string
	// Indicates whether the user is currently online.
	Online bool
}

// NotFound is the type returned when attempting to show or delete a bottle
// that does not exist.
type NotFound struct {
	// Message of error
	Message string
	// ID of missing bottle
	ID string
}

// Missing criteria
type NoCriteria string

// No bottle matched given criteria
type NoMatch string

// Error returns an error description.
func (e *NotFound) Error() string {
	return "NotFound is the type returned when attempting to show or delete a bottle that does not exist."
}

// ErrorName returns "NotFound".
func (e *NotFound) ErrorName() string {
	return e.Message
}

// Error returns an error description.
func (e NoCriteria) Error() string {
	return "Missing criteria"
}

// ErrorName returns "no_criteria".
func (e NoCriteria) ErrorName() string {
	return "no_criteria"
}

// Error returns an error description.
func (e NoMatch) Error() string {
	return "No bottle matched given criteria"
}

// ErrorName returns "no_match".
func (e NoMatch) ErrorName() string {
	return "no_match"
}

// NewStoredUser initializes result type StoredUser from viewed result type
// StoredUser.
func NewStoredUser(vres *usersvcviews.StoredUser) *StoredUser {
	var res *StoredUser
	switch vres.View {
	case "default", "":
		res = newStoredUser(vres.Projected)
	}
	return res
}

// NewViewedStoredUser initializes viewed result type StoredUser from result
// type StoredUser using the given view.
func NewViewedStoredUser(res *StoredUser, view string) *usersvcviews.StoredUser {
	var vres *usersvcviews.StoredUser
	switch view {
	case "default", "":
		p := newStoredUserView(res)
		vres = &usersvcviews.StoredUser{p, "default"}
	}
	return vres
}

// newStoredUser converts projected type StoredUser to service type StoredUser.
func newStoredUser(vres *usersvcviews.StoredUserView) *StoredUser {
	res := &StoredUser{
		Name: vres.Name,
	}
	if vres.ID != nil {
		res.ID = *vres.ID
	}
	if vres.Online != nil {
		res.Online = *vres.Online
	}
	if vres.Online == nil {
		res.Online = false
	}
	return res
}

// newStoredUserView projects result type StoredUser into projected type
// StoredUserView using the "default" view.
func newStoredUserView(res *StoredUser) *usersvcviews.StoredUserView {
	vres := &usersvcviews.StoredUserView{
		ID:     &res.ID,
		Name:   res.Name,
		Online: &res.Online,
	}
	return vres
}
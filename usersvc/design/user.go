package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = Service("user", func() {
	Description("The storage service makes it possible to view, add or remove wine bottles.")

	HTTP(func() {
		Path("/")
	})

	Method("insert", func() {
		Description("Add new bottle and return its ID.")
		Payload(User)
		Result(String)
		HTTP(func() {
			POST("/insert")
			Response(StatusCreated)
		})
	})

	Method("delete", func() {
		Description("Remove bottle from storage")
		Payload(func() {
			Attribute("id", String, "ID of bottle to remove")
			Required("id")
		})
		Error("not_found", NotFound, "Bottle not found")
		HTTP(func() {
			DELETE("/delete/{id}")
			Response(StatusNoContent)
		})
	})

	Method("get", func() {
		Payload(func() {
			Attribute("id", String, "ID of bottle to remove")
			Required("id")
		})
		Result(StoredUser, func() {
			View("default")
		})
		Error("no_criteria", String, "Missing criteria")
		Error("no_match", String, "No bottle matched given criteria")
		HTTP(func() {
			GET("/get/{id}")
			Response(StatusOK)
			Response("no_match", StatusNotFound)
		})
	})
})

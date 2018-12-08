package design

import (
	. "goa.design/goa/dsl"
)

var _ = Service("authstorage", func() {
	Description("The service makes it possible to persist data used by the authservice.")

	HTTP(func() {
		Path("/storage/auth")
	})

	Method("insert", func() {
		Description("Add new user and return its ID.")
		Payload(AuthUser)
		Result(String)
		HTTP(func() {
			POST("/insert")
			Response(StatusCreated)
		})
	})

	Method("delete", func() {
		Description("Remove user from storage")
		Payload(func() {
			Attribute("id", String, "ID of user to remove")
			Required("id")
		})
		Error("not_found")
		HTTP(func() {
			DELETE("/delete/{id}")
			Response(StatusNoContent)
		})
	})

	Method("get", func() {
		Security(JWTAuth, func() { // Use JWT to auth requests to this endpoint.
			Scope("api:read") // Enforce presence of "api:read" scope in JWT claims.
		})
		Result(AuthStoredUser)
		Error("not_found")
		Error("unauthorized")
		Payload(func() {
			Token("token", String, func() {
				Description("JWT used for authentication")
			})
			Attribute("id", String, "Get user by ID")
			Attribute("view", String, "View to render", func() {
				Enum("default", "tiny")
			})
			Required("id")
		})
		HTTP(func() {
			GET("/get/{id}")
			Param("view")
			Response(StatusOK)
			Response("not_found", StatusNotFound)
			Response("unauthorized", StatusUnauthorized)
		})
	})
})

var AuthUser = Type("AuthUser", func() {
	Description("User in the system.")
	Attribute("name", String, "The username", func() {
		MaxLength(50)
	})
	Required("name")
})

var AuthStoredUser = ResultType("application/sd.data.stored-authuser", func() {
	Description("A StoredUser describes a user retrieved by the auth service.")
	Reference(AuthUser)
	TypeName("StoredUser")

	Attributes(func() {
		Attribute("id", String, "UUID is the unique id of the user.", func() {
			Example("f923e008-e511-11e8-9f32-f2801f1b9fd1")
			Meta("struct:tag:gorm", "TYPE:uuid; COLUMN:id; PRIMARY_KEY; DEFAULT: gen_random_uuid()")
			Meta("struct:tag:json", "id")
		})
		Attribute("name")
		Attribute("online", Boolean, "Indicates whether the user is currently online.")
	})

	View("default", func() {
		Attribute("id")
		Attribute("name")
		Attribute("online")
	})

	View("tiny", func() {
		Attribute("id")
		Attribute("name")
	})

	Required("id")
})

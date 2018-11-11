package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = API("user", func() {
	Title("Speeddate Masterserver")
	Description("HTTP service for managing your users")

	Server("user", func() {
		Description("Serves users and swagger service")
		Services("user", "swagger")
		Host("localhost", func() {
			Description("Host on local machine")
			URI("http://localhost:8000")
		})
	})
})

var User = Type("User", func() {
	Description("User in the system.")
	Attribute("name", String, "The username", func() {
		MaxLength(50)
	})
	Attribute("online", Boolean, "Indicates whether the user is currently online.", func() {
		Default(false)
	})
	Required("name")
})

var StoredUser = ResultType("application/sd.data.stored-user", func() {
	Description("A StoredUser describes a user retrieved by the storage service.")
	Reference(User)
	TypeName("StoredUser")

	Attributes(func() {
		Attribute("id", String, "UUID is the unique id of the user.", func() {
			Example("f923e008-e511-11e8-9f32-f2801f1b9fd1")
		})
		Attribute("name")
		Attribute("online", Boolean, "Indicates whether the user is currently online.")
	})

	View("default", func() {
		Attribute("id")
		Attribute("name")
		Attribute("online")
	})

	Required("id")
})

var NotFound = Type("NotFound", func() {
	Description("NotFound is the type returned when attempting to show or delete a bottle that does not exist.")
	Attribute("message", String, "Message of error", func() {
		Metadata("struct:error:name")
		Example("bottle 1 not found")
	})
	Attribute("id", String, "ID of missing bottle")
	Required("message", "id")
})

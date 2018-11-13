package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = API("usersvc", func() {
	Title("Speeddate Userservice")
	Description("HTTP service for managing users in the SpeedDate-system")

	Server("user", func() {
		Description("Serves repository and swagger service")
		Services("health", "repository", "swagger")
		Host("localhost", func() {
			Description("Host on local machine")
			URI("http://localhost:8001/user")
		})
	})
})

// JWTAuth defines a security scheme that uses JWT tokens.
var JWTAuth = JWTSecurity("jwt", func() {
	Description(`Secures endpoint by requiring a valid JWT token retrieved via the signin endpoint. Supports scopes "api:read" and "api:write".`)
	Scope("api:read", "Read-only access")
	Scope("api:write", "Read and write access")
})

var User = Type("User", func() {
	Description("User in the system.")
	Attribute("name", String, "The username", func() {
		MaxLength(50)
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
			Metadata("struct:tag:gorm", "TYPE:uuid; COLUMN:id; PRIMARY_KEY; DEFAULT: gen_random_uuid()")
			Metadata("struct:tag:json", "id")
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

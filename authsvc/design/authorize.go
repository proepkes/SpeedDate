package design

import (
	. "goa.design/goa/http/design"
	. "goa.design/goa/http/dsl"
	_ "goa.design/plugins/cors"
)

var _ = Service("authorize", func() {
	Description("The service makes it possible to ...")

	HTTP(func() {
		Path("/auth")
	})
	// Sets CORS response headers for requests with any Origin header
	Origin("*")
	Method("login", func() {
		Description("Creates a valid JWT")

		// The signin endpoint is secured via basic auth
		Security(BasicAuth)

		Payload(func() {
			Description("Credentials used to authenticate to retrieve JWT token")
			Username("username", String, "Username used to perform signin", func() {
				Example("user")
			})
			Password("password", String, "Password used to perform signin", func() {
				Example("password")
			})
			Required("username", "password")
		})
		Result(func() {
			Description("Result defines a JWT Token.")
			Attribute("token", String, "Resulting token")
			Required("token")
		})
		HTTP(func() {
			POST("/login")
			// Use Authorization header to provide basic auth value.
			Response(StatusNoContent, func() {
				Header("token:Authorization", String, "Generated JWT")
			})
		})
	})
})

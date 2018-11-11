package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = Service("auth", func() {
	Description("The service makes it possible to ...")

	HTTP(func() {
		Path("/")
	})

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

		HTTP(func() {
			POST("/signin")
			// Use Authorization header to provide basic auth value.
			Response(StatusNoContent)
		})
	})
})

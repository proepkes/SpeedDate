package design

import . "goa.design/goa/http/dsl"

var _ = API("usersvc", func() {
	Title("Speeddate Userservice")
	Description("HTTP service for managing users in the SpeedDate-system")

	Server("auth", func() {
		Description("Serves repository and swagger service")
		Services("auth", "swagger")
		Host("localhost", func() {
			Description("Host on local machine")
			URI("http://localhost:8001")
		})
	})
})

// BasicAuth defines a security scheme using basic authentication. The scheme
// protects the "signin" action used to create JWTs.
var BasicAuth = BasicAuthSecurity("basic", func() {
	Description("Basic authentication used to authenticate security principal during signin")
})

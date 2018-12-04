package design

import . "goa.design/goa/http/design"
import . "goa.design/plugins/cors/dsl"

var _ = API("spawnsvc", func() {
	Title("Speeddate Spawnerservice")
	Description("HTTP service for managing game-instances")

	Server("spawner", func() {
		Description("Serves fleet and swagger service")
		Services("fleet", "swagger")
		Host("localhost", func() {
			Description("Host on local machine")
			URI("http://:8001")
		})
	})
})

// JWTAuth defines a security scheme that uses JWT tokens.
var JWTAuth = JWTSecurity("jwt", func() {
	Description(`Secures endpoint by requiring a valid JWT token retrieved via the signin endpoint. Supports scopes "api:read" and "api:write".`)
	Scope("api:read", "Read-only access")
	Scope("api:write", "Read and write access")
})

var GameserverTemplate = Type("GameserverTemplate", func() {
	Description("GameserverTemplate describes gameserver")
	Attribute("Namespace", String, "Namespace where the gameserver will run in", func() {
		MaxLength(100)
		Example("speeddate-system")
	})
	Attribute("NamePrefix", String, "Prefix for the generated pod-name", func() {
		MaxLength(100)
		Example("my-server")
	})
	Attribute("PortPolicy", String, "Portpolicy either dynamic or static", func() {
		Example("dynamic")
		Example("static")
	})
	Attribute("ContainerName", String, "Name of the gameserver-container", func() {
		Example("my-server")
	})
	Attribute("ContainerImage", String, "Image of the gameserver", func() {
		Example("gcr.io/agones-images/udp-server:0.4")
	})
	Attribute("ContainerPort", String, "Exposed port of the gameserver", func() {
		Example("7777")
	})
	Required("Namespace", "NamePrefix", "PortPolicy", "ContainerName", "ContainerImage", "ContainerPort")
})

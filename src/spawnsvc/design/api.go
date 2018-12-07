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

var ObjectMeta = Type("ObjectMeta", func() {
	Description("Spec for ObjectMeta")
	Attribute("GenerateName", String, "Prefix for the generated fleetname", func() {
		MaxLength(100)
		Example("my-server")
	})
	Attribute("Namespace", String, "Namespace where the fleet will run in", func() {
		MaxLength(100)
		Example("speeddate-system")
	})
	Required("GenerateName", "Namespace")
})

var GameServerSpec = Type("GameServerSpec", func() {
	Description("GameserverTemplate describes gameserver")
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
	Attribute("ContainerPort", Int32, "Exposed port of the gameserver", func() {
		Example(7777)
	})
	Required("PortPolicy", "ContainerImage", "ContainerName", "ContainerPort")
})

var GameserverTemplate = Type("GameserverTemplate", func() {
	Description("GameserverTemplate describes gameserver")
	Attribute("ObjectMeta", ObjectMeta, "GameserverTemplates ObjectMeta")
	Attribute("GameServerSpec", GameServerSpec, "GameServerSpec")

	Required("GameServerSpec")
})

var FleetSpec = Type("FleetSpec", func() {
	Description("Spec for Fleet")
	Attribute("Replicas", Int32, "Replicas")
	Attribute("Template", GameserverTemplate, "Template of the gameserver")

	Required("Replicas", "Template")
})

var Fleet = Type("Fleet", func() {
	Description("Fleet")
	Attribute("ObjectMeta", ObjectMeta, "Fleets ObjectMeta")
	Attribute("FleetSpec", FleetSpec, "FleetSpec")

	Required("FleetSpec")
})

var FleetStatus = Type("FleetStatus", func() {
	// Replicas the total number of current GameServer replicas
	Attribute("Replicas", Int32, "Replicas")
	// ReadyReplicas are the number of Ready GameServer replicas
	Attribute("ReadyReplicas", Int32, "ReadyReplicas")
	// AllocatedReplicas are the number of Allocated GameServer replicas
	Attribute("AllocatedReplicas", Int32, "AllocatedReplicas")

	Required("Replicas", "ReadyReplicas", "AllocatedReplicas")
})

var StoredFleet = Type("StoredFleet", func() {
	Description("Fleet")
	Attribute("ObjectMeta", ObjectMeta, "The Fleets ObjectMeta")
	Attribute("FleetSpec", FleetSpec, "The FleetSpec")
	Attribute("FleetStatus", FleetStatus, "The FleetStatus")

	Required("ObjectMeta", "FleetSpec")
})

var NamespacePayload = Type("NamespacePayload", func() {
	Attribute("namespace", String)
})

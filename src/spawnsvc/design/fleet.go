package design

import (
	. "goa.design/goa/dsl"
	cors "goa.design/plugins/cors/dsl" // Use CORS plugin
)

var _ = Service("fleet", func() {
	Description("The service makes it possible to manage gameservers")

	HTTP(func() {
		Path("/fleet")
	})

	// Sets CORS response headers for requests with any Origin header
	cors.Origin("*", func() {
		cors.Headers("Origin, X-Requested-With, Content-Type, Accept")
		cors.Methods("OPTIONS", "PUT", "GET", "POST", "DELETE", "PATCH")
		cors.MaxAge(600)
	})

	Method("create", func() {
		Description("Create a new fleet.")
		Result(String)
		Payload(Fleet)
		HTTP(func() {
			PUT("/")
			Response(StatusCreated)
		})
	})

	Method("delete", func() {
		Description("Delete a fleet")
		Payload(func() {
			Attribute("namespace", String, "The namespace", func() {
				Default("default")
			})
			Attribute("name", String, "Name of the fleet")
			Required("name")
		})
		Error("not_found")
		HTTP(func() {
			DELETE("/{name}")
			Param("namespace")
			Response(StatusNoContent)
		})
	})

	Method("list", func() {
		Description("List all fleets.")
		Payload(func() {
			Attribute("namespace", String, "The namespace", func() {
				Default("default")
			})
			Attribute("view", String, "View to render", func() {
				Enum("default")
			})
		})
		Result(CollectionOf(StoredFleet), func() {
			View("default")
		})
		HTTP(func() {
			GET("/list")
			Param("namespace")
			Param("view")
			Response(StatusOK)
		})
	})

	Method("allocate", func() {
		Description("Create a fleetallocation.")
		Result(String)
		Payload(func() {
			Attribute("fleet", String, "Name of the fleet to allocate from")
			Attribute("namespace", String, "Must match the namespace of the fleet", func() {
				Default("default")
			})
			Attribute("name", String, "Nameprefix for the allocation")
			Required("fleet", "name")
		})
		HTTP(func() {
			POST("/allocate")
			Param("namespace")
			Param("fleet")
			Param("name")
			Response(StatusOK)
		})
	})

	Method("configuration", func() {
		Description("Get default fleet configuration.")
		Result(Fleet)
		HTTP(func() {
			GET("/configuration")
			Response(StatusOK)
		})
	})

	Method("configure", func() {
		Description("Configure default fleet options.")
		Result(String)
		Payload(func() {
			Attribute("NamePrefix", String, "The NamePrefix")
			Attribute("ContainerImage", String, "The ContainerImage")
			Attribute("ContainerName", String, "The ContainerName")
			Attribute("ContainerPort", Int32, "The ContainerPort")
			Attribute("GameserverNamePrefix", String, "The GameserverNamePrefix")
			Attribute("Namespace", String, "The Namespace")
			Attribute("Replicas", UInt32, "The Replicas")

			Required("ContainerImage", "ContainerName", "ContainerPort", "NamePrefix", "GameserverNamePrefix", "Namespace", "Replicas")
		})
		HTTP(func() {
			PATCH("/configuration")
			Response(StatusOK)
		})
	})
})

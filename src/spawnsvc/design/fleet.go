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
		cors.Methods("OPTIONS", "POST", "GET")
		cors.MaxAge(600)
	})

	Method("add", func() {
		Description("Add a new gameserver.")
		Result(String)
		HTTP(func() {
			POST("/add")
			Response(StatusCreated)
		})
	})

	Method("create", func() {
		Description("Create a new fleet.")
		Result(String)
		Payload(Fleet)
		HTTP(func() {
			POST("/create")
			Response(StatusCreated)
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

	Method("clear", func() {
		Description("Removes all gameserver pods.")
		Result(String)
		HTTP(func() {
			POST("/clear")
			Response(StatusOK)
		})
	})

	Method("configuration", func() {
		Description("Get gameserver deployment configuration.")
		Result(GameserverTemplate)
		HTTP(func() {
			GET("/configuration")
			Response(StatusOK)
		})
	})

	Method("configure", func() {
		Description("Configure gameserver deployment.")
		Result(String)
		Payload(GameserverTemplate)
		HTTP(func() {
			POST("/configure")
			Response(StatusOK)
		})
	})
})

package design

import (
	. "goa.design/goa/http/design"
	_ "goa.design/plugins/cors"
	. "goa.design/plugins/cors/dsl"
)

var _ = Service("fleet", func() {
	Description("The service makes it possible to manage gameservers")

	HTTP(func() {
		Path("/fleet")
	})

	// Sets CORS response headers for requests with any Origin header
	Origin("*", func() {
		Headers("Origin, X-Requested-With, Content-Type, Accept")
		Methods("OPTIONS", "POST")
		MaxAge(600)
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

package design

import (
	. "goa.design/goa/http/design"
	_ "goa.design/plugins/cors"
	. "goa.design/plugins/cors/dsl"
)

var _ = Service("armada", func() {
	Description("The service makes it possible to manage gameservers")

	HTTP(func() {
		Path("/armada")
	})

	// Sets CORS response headers for requests with any Origin header
	Origin("*")

	Method("add", func() {
		Description("Add a new gameserver to the armada.")
		Result(String)
		HTTP(func() {
			POST("/add")
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
})

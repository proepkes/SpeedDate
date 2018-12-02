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
	Origin("*")

	Method("add", func() {
		Description("Add a new gameserver.")
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

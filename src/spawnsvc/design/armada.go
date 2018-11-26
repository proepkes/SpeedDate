package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = Service("armada", func() {
	Description("The service makes it possible to manage gameservers")

	HTTP(func() {
		Path("/armada")
	})

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

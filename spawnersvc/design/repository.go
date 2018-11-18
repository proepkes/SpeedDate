package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = Service("spawn", func() {
	Description("The service makes it possible to spawn gameservers")

	HTTP(func() {
		Path("/spawn")
	})

	Method("insert", func() {
		Description("Add new user and return its ID.")
		Result(String)
		HTTP(func() {
			POST("/insert")
			Response(StatusCreated)
		})
	})
})

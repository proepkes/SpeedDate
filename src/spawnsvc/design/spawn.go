package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = Service("spawn", func() {
	Description("The service makes it possible to spawn gameservers")

	HTTP(func() {
		Path("/spawn")
	})

	Method("allocate", func() {
		Description("Spawn a new gameserver.")
		Result(String)
		HTTP(func() {
			POST("/")
			Response(StatusCreated)
		})
	})
})

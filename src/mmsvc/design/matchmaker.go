package design

import (
	. "goa.design/goa/dsl"
)

var _ = Service("matchmaking", func() {
	Description("matchmaking.")

	HTTP(func() {
		Path("/mm")
	})

	Method("insert", func() {
		Description(".")
		Result(String)
		HTTP(func() {
			POST("/insert")
			Response(StatusCreated)
		})
	})
})

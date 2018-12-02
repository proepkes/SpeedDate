package design

import (
	. "goa.design/goa/http/design"
	_ "goa.design/plugins/cors"
	. "goa.design/plugins/cors/dsl"
)

var _ = Service("gameserver", func() {
	Description("The service makes it possible to manage gameservers")

	HTTP(func() {
		Path("/gameserver")
	})

	// Sets CORS response headers for requests with any Origin header
	Origin("*")

	Method("configure", func() {
		Description("Configure gameserver-properties.")
		Result(String)
		HTTP(func() {
			POST("/configure")
			Response(StatusOK)
		})
	})
})

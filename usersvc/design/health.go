package design

import . "goa.design/goa/http/design"
import . "goa.design/goa/http/dsl"

var _ = Service("health", func() {
	HTTP(func() {
		Path("/health")
	})
	Method("checkHealth", func() {
		Description("Health check endpoint")
		Result(Bytes)
		HTTP(func() {
			GET("/")
			Response(StatusOK, func() {
				ContentType("application/octet-stream")
			})
		})
	})
})

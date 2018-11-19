package design

import (
	_ "goa.design/plugins/cors"
	. "goa.design/plugins/cors/dsl"
)

var _ = Service("swagger", func() {
	Description("The swagger service serves the API swagger definition.")

	HTTP(func() {
		Path("/swagger")
	})
	Files("/swagger.json", "../../gen/http/openapi.json", func() {
		Description("JSON document containing the API swagger definition")
	})
})

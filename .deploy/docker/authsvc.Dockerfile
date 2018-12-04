# Golang application builder steps
FROM golang:1.11.2-alpine3.7 as builder

RUN apk add --no-cache git
RUN go get github.com/golang/dep/cmd/dep

COPY Gopkg.lock Gopkg.toml /go/src/github.com/proepkes/speeddate/src/
WORKDIR /go/src/github.com/proepkes/speeddate/src/

COPY src/authsvc authsvc/

# Install library dependencies
RUN dep ensure -vendor-only

WORKDIR /go/src/github.com/proepkes/speeddate/src/authsvc/cmd/auther
RUN CGO_ENABLED=0 GOOS=linux go build

FROM scratch 
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/authsvc/keys ./keys
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/authsvc/gen/http/openapi.json .
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/authsvc/cmd/auther/auther . 
ENTRYPOINT ["./auther"]

# Golang application builder steps
FROM golang:latest as builder
WORKDIR /go/src/github.com/proepkes/speeddate/src/authsvc
COPY . .
RUN go get -v ./...
WORKDIR /go/src/github.com/proepkes/speeddate/src/authsvc/cmd/auther
RUN CGO_ENABLED=0 GOOS=linux go build

FROM scratch 
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/authsvc/keys ./keys
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/authsvc/gen/http/openapi.json .
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/authsvc/cmd/auther/auther . 
ENTRYPOINT ["./auther"]

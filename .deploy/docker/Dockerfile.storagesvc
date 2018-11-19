# Golang application builder steps
FROM golang:latest as builder
WORKDIR /go/src/github.com/proepkes/speeddate/src/storagesvc
COPY . .
RUN go get -v ./...
WORKDIR /go/src/github.com/proepkes/speeddate/src/storagesvc/cmd/storager
RUN CGO_ENABLED=0 GOOS=linux go build

FROM scratch 
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/storagesvc/keys ./keys
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/storagesvc/gen/http/openapi.json .
COPY --from=builder /go/src/github.com/proepkes/speeddate/src/storagesvc/cmd/storager/storager . 
ENTRYPOINT ["./storager"]

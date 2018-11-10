# Golang application builder steps
FROM golang:latest as builder
WORKDIR /go/src/github.com/proepkes/SpeedDate/user
ADD . .
RUN go get -v ./...
WORKDIR /go/src/github.com/proepkes/SpeedDate/user/cmd
RUN CGO_ENABLED=0 GOOS=linux go build

FROM scratch
COPY --from=builder /go/src/github.com/proepkes/SpeedDate/user/cmd/cmd . 
ENTRYPOINT ["./cmd"]

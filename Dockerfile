# Golang application builder steps
FROM golang:latest as builder
WORKDIR /go/src/github.com/proepkes/SpeedDate/profile
ADD . .
RUN go get -v ./...
WORKDIR /go/src/github.com/proepkes/SpeedDate/profile/cmd
RUN CGO_ENABLED=0 GOOS=linux go build

FROM scratch
COPY --from=builder /go/src/github.com/proepkes/SpeedDate/profile/cmd/cmd . 
ENTRYPOINT ["./cmd"]

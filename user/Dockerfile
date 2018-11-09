# Golang application builder steps
FROM golang:latest as builder
WORKDIR /go/src/github.com/proepkes/SpeedDate/user
ADD . .
RUN go get -v ./...
RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo ./...

FROM scratch
COPY --from=builder /go/src/github.com/proepkes/SpeedDate/user/user . 
ENTRYPOINT ["./user"]

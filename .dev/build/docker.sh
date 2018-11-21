#!/bin/bash

echo "Building docker: $1"

# Build & Tag & push svc
docker build --tag=proepkes/$1:dev -f $GOPATH/src/github.com/proepkes/speeddate/.deploy/docker/$1.Dockerfile $GOPATH/src/github.com/proepkes/speeddate


echo "Pushing to docker: $1"
docker push proepkes/$1:dev

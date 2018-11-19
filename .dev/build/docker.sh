#!/bin/bash

echo "Building docker: $1"

# Build & Tag & push svc
docker build --tag=proepkes/$1:dev -f $GOPATH/src/github.com/proepkes/speeddate/.deploy/docker/Dockerfile.$1 $GOPATH/src/github.com/proepkes/speeddate/src/$1


echo "Pushing to docker: $1"
docker push proepkes/$1:dev

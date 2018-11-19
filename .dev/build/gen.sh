#!/bin/bash

# Generate CRD-API
cd $GOPATH/src/github.com/proepkes/speeddate
vendor/k8s.io/code-generator/generate-groups.sh all github.com/proepkes/speeddate/src/pkg/client github.com/proepkes/speeddate/src/pkg/apis dev:v1


# Generate services
cd $GOPATH/src/github.com/proepkes/speeddate/.dev/build

./gensvc.sh storagesvc
./gensvc.sh authsvc
./gensvc.sh spawnsvc
./gensvc.sh mmsvc

# Build docker images
./docker.sh storagesvc
./docker.sh authsvc
./docker.sh spawnsvc
./docker.sh mmsvc

# Generate k8s-yaml
cd $GOPATH/src/github.com/proepkes/speeddate
helm.exe template .deploy/charts/ -f .deploy/charts/values.yaml > .deploy/k8s/speeddate.yaml
kubectl apply -f .deploy/k8s/speeddate.yaml

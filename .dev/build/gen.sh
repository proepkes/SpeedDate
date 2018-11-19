#!/bin/bash

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

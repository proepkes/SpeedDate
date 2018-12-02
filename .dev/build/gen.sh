#!/bin/bash
  
# CALL FROM SPEEDDATE-ROOT-DIRECTORY

# Generate services
.dev/build/gensvc.sh storagesvc
.dev/build/gensvc.sh authsvc
.dev/build/gensvc.sh spawnsvc
.dev/build/gensvc.sh mmsvc

# Build docker images
.dev/build/docker.sh storagesvc
.dev/build/docker.sh authsvc
.dev/build/docker.sh spawnsvc
.dev/build/docker.sh mmsvc

# Generate k8s-yaml
helm template .deploy/charts/ -f .deploy/charts/values.yaml --namespace speeddate-system > .deploy/k8s/speeddate.yaml
kubectl apply -f .deploy/k8s/speeddate.yaml --namespace=speeddate-system

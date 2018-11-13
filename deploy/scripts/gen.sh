# Generate user-service
cd $GOPATH/src/github.com/proepkes/speeddate/usersvc
goa gen github.com/proepkes/speeddate/usersvc/design
goa example github.com/proepkes/speeddate/usersvc/design

# Generate auth-service
cd $GOPATH/src/github.com/proepkes/speeddate/authsvc
goa gen github.com/proepkes/speeddate/authsvc/design
goa example github.com/proepkes/speeddate/authsvc/design

# Build & Tag userservice
cd $GOPATH/src/github.com/proepkes/speeddate
docker build --tag=proepkes/usersvc:dev -f $GOPATH/src/github.com/proepkes/speeddate/deploy/docker/Dockerfile.usersvc .

# Generate k8s-yaml
helm template deploy/charts/ -f deploy/charts/values.yaml > deploy/k8s/speeddate.yaml
kubectl apply -f deploy/k8s/speeddate.yaml
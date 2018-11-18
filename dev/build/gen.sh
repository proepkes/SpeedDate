# Generate user-service


cd $GOPATH/src/github.com/proepkes/speeddate/build/scripts

./gensvc.sh usersvc
./gensvc.sh authsvc
./gensvc.sh gamehostsvc
./gensvc.sh mmsvc

./docker.sh usersvc
./docker.sh authsvc
./docker.sh gamehostsvc
./docker.sh mmsvc

# Generate k8s-yaml
cd $GOPATH/src/github.com/proepkes/speeddate
helm.exe template deploy/charts/ -f deploy/charts/values.yaml > deploy/k8s/speeddate.yaml
kubectl apply -f deploy/k8s/speeddate.yaml

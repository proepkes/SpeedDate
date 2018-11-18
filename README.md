Under construction

## Requirements:
Kubernetes-cluster
Istio
Cockroachdb


## Development:

Minikube
- minikube start --memory=4096 --cpus=4 
- kubectl apply -f dev/setup/minikube/metallb.yaml
- kubectl apply -f dev/setup/minikube/metallb-config.yaml



https://www.cockroachlabs.com/docs/stable/install-cockroachdb.html


cockroach sql --insecure -p 8888
 
 CREATE USER IF NOT EXISTS speeddateuser;
 CREATE DATABASE speeddate;
 GRANT ALL ON DATABASE speeddate TO speeddateuser;
Under construction

## Requirements:
- Kubernetes-cluster
- Istio
- Cockroachdb

# Development:
## Minikube
- https://kubernetes.io/docs/tasks/tools/install-minikube/
- minikube start --kubernetes-version v1.11.3 --memory=4096 --cpus=4 --extra-config=apiserver.authorization-mode=RBAC --extra-config=apiserver.admission-control=NamespaceLifecycle,LimitRanger,ServiceAccount,DefaultStorageClass,DefaultTolerationSeconds,MutatingAdmissionWebhook,ValidatingAdmissionWebhook,ResourceQuota

- kubectl apply -f .dev/setup/minikube/metallb.yaml
- kubectl apply -f .dev/setup/minikube/metallb-config.yaml

## Cockroachdb

https://www.cockroachlabs.com/docs/stable/install-cockroachdb.html


- cockroach start --insecure --listen-addr=0.0.0.0:8888
- cockroach sql --insecure -p 8888

 ````
 CREATE USER IF NOT EXISTS speeddateuser; 
 CREATE DATABASE speeddate; 
 GRANT ALL ON DATABASE speeddate TO speeddateuser;
 ````

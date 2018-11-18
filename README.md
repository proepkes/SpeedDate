Under construction

## Requirements:
Kubernetes-cluster
Istio
Cockroachdb


## Development:

<!-- https://kubernetes.io/docs/reference/access-authn-authz/admission-controllers/ -->
Minikube
- minikube start --memory=4096 --cpus=4 --extra-config=apiserver.authorization-mode=RBAC --enable-admission-plugins=NamespaceLifecycle,LimitRanger,ServiceAccount,DefaultStorageClass,DefaultTolerationSeconds,MutatingAdmissionWebhook,ValidatingAdmissionWebhook,ResourceQuota

- kubectl apply -f dev/setup/minikube/metallb.yaml
- kubectl apply -f dev/setup/minikube/metallb-config.yaml



https://www.cockroachlabs.com/docs/stable/install-cockroachdb.html


cockroach sql --insecure -p 8888
 
 CREATE USER IF NOT EXISTS speeddateuser;
 CREATE DATABASE speeddate;
 GRANT ALL ON DATABASE speeddate TO speeddateuser;
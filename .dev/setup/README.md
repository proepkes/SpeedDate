# Development setup:
## Minikube
- https://kubernetes.io/docs/tasks/tools/install-minikube/

- minikube start --kubernetes-version v1.11.3 --memory=4096 --cpus=4 --extra-config=apiserver.authorization-mode=RBAC --extra-config=apiserver.enable-admission-plugins="LimitRanger,NamespaceExists,NamespaceLifecycle,ResourceQuota,ServiceAccount,DefaultStorageClass,MutatingAdmissionWebhook,ValidatingAdmissionWebhook,DefaultTolerationSeconds"

- kubectl apply -f .dev/setup/metallb.yaml
- kubectl apply -f .dev/setup/metallb-config.yaml

## Istio

From: https://istio.io/docs/setup/kubernetes/quick-start/

curl -L https://git.io/getLatestIstio | sh -
cd istio-1.0.4


kubectl apply -f install/kubernetes/helm/istio/templates/crds.yaml
kubectl apply -f install/kubernetes/istio-demo-auth.yaml
kubectl label namespace default istio-injection=enabled
kubectl get namespace -L istio-injection
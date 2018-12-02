# Development setup:

### Minikube

https://kubernetes.io/docs/tasks/tools/install-minikube/

```
- minikube start --kubernetes-version v1.11.3 --memory=4096 --cpus=4 --extra-config=apiserver.authorization-mode=RBAC --extra-config=apiserver.enable-admission-plugins="LimitRanger,NamespaceExists,NamespaceLifecycle,ResourceQuota,ServiceAccount,DefaultStorageClass,MutatingAdmissionWebhook,ValidatingAdmissionWebhook,DefaultTolerationSeconds"
```

### LoadBalancer

```
kubectl apply -f .dev/setup/metallb.yaml
kubectl apply -f .dev/setup/metallb-config.yaml
```

### Helm/Tiller

```
helm init
```

### Istio

https://istio.io/docs/setup/kubernetes/quick-start/

```
curl -L https://git.io/getLatestIstio | sh -
cd istio-1.0.4

kubectl apply -f install/kubernetes/helm/istio/templates/crds.yaml
kubectl apply -f install/kubernetes/istio-demo-auth.yaml
kubectl create namespace speeddate-system
kubectl label namespace speeddate-system istio-injection=enabled
kubectl get namespace -L istio-injection
```

### Agones

https://github.com/GoogleCloudPlatform/agones/blob/master/install/helm/README.md

```
helm repo add agones https://agones.dev/chart/stable
helm install --name agones --namespace speeddate-system agones/agones
```
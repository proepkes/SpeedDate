// From: https://istio.io/docs/setup/kubernetes/quick-start/

curl -L https://git.io/getLatestIstio | sh -
cd istio-1.0.3


kubectl apply -f install/kubernetes/helm/istio/templates/crds.yaml
kubectl apply -f install/kubernetes/istio-demo-auth.yaml
kubectl label namespace default istio-injection=enabled
kubectl get namespace -L istio-injection

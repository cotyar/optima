#!/bin/bash

helm uninstall dapr -n optima
helm uninstall keyclock -n optima

if [[ $redis == true ]]; then
    helm uninstall redis -n optima
else
    helm uninstall postgresql -n optima
fi

helm uninstall dapr-prom -n dapr-monitoring
helm uninstall grafana -n dapr-monitoring

helm repo remove stable
helm repo remove dapr
helm repo remove bitnami

# kubectl delete ns optima
kubectl delete ns optima
kubectl delete ns dapr-monitoring
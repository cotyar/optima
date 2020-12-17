#!/bin/bash

# Updating Help repos...
helm repo add stable https://charts.helm.sh/stable
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

# Installing Dapr...
kubectl create ns optima
helm install dapr dapr/dapr --namespace optima --set global.logAsJson=true

# Installing Keycloak...
helm install keyclock -f ./dapr/components/keyclock.yaml bitnami/keycloak -n optima

if [[ $redis == true ]]; then
    echo "Configuring state management with Redis..."
    helm install redis bitnami/redis -n optima
    kubectl apply -f  ./dapr/components/state.redis.yaml
else
    echo "Configuring state management with PostgreSQL..."
    kubectl apply -f ./dapr/components/postgres.state.secret.yaml
    helm install postgresql bitnami/postgresql --set existingSecret=postgres-state-secret -n optima
    kubectl apply -f  ./dapr/components/state.postgres.yaml
fi

# Installing deployments...
kubectl apply -f  ./dapr/components/zipkin.yaml

# Installing monitoring...
kubectl create namespace dapr-monitoring
helm install dapr-prom stable/prometheus -n dapr-monitoring
helm install grafana stable/grafana -n dapr-monitoring
SHELL := /bin/bash

all: help

install: ## Install Dapr into current kubeconfig selected context
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

	# Configuring the state store...
	kubectl apply -f ./dapr/components/postgres.state.secret.yaml
	helm install postgresql bitnami/postgresql --set existingSecret=postgres-state-secret -n optima
	kubectl apply -f  ./dapr/components/state.postgres.yaml

	# helm install redis bitnami/redis -n optima
	# kubectl apply -f  ./dapr/components/state.redis.yaml

	# Installing deployments...
	kubectl apply -f  ./dapr/components/zipkin.yaml
	
	# Installing monitoring...
	kubectl create namespace dapr-monitoring
	helm install dapr-prom stable/prometheus -n dapr-monitoring
	helm install grafana stable/grafana -n dapr-monitoring

	# Installing Grafana dashbaords...
	source install_grafana.sh

	# Deploying the services...
	source deploy.sh

grafana_password: ## Retrieve grafana admin password
	$(eval GPASS=$(shell kubectl get secret -n dapr-monitoring grafana -o jsonpath="{.data.admin-password}" | base64 --decode))
	@echo Grafana admin password is: $(GPASS)

grafana_dash: ## Create Prometheus data source and import Dapr dashboards
	source install_grafana.sh

forward: ## Forward observability ports
	# Forwarding ports: dapr=8081, grafana=8082, zipkin=9412, keyclock=9080
	kubectl port-forward svc/dapr-dashboard 8081:8080 -n optima  &
	kubectl port-forward svc/zipkin 9412:9411 -n optima  &
	kubectl port-forward svc/grafana 8082:80 -n dapr-monitoring  &
	kubectl port-forward deployment/optimaserver 3500:3500 -n optima &
	kubectl port-forward svc/keyclock-keycloak 9080:80 -n optima &

unforward: ## Stop previously forwarded ports 
	pkill kubectl -9

uninstall: ## Removes all the deployments and the helm repository.
	helm uninstall dapr -n optima
	helm uninstall keyclock -n optima
	helm uninstall postgresql -n optima
	# helm uninstall redis -n optima
	helm uninstall dapr-prom -n dapr-monitoring
	helm uninstall grafana -n dapr-monitoring

	helm repo remove stable
	helm repo remove dapr
	helm repo remove bitnami

	# kubectl delete ns optima
	kubectl delete ns optima
	kubectl delete ns dapr-monitoring

help: ## Display available commands
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk \
		'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

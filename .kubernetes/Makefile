SHELL := /bin/bash
redis = false

all: help

install: ## Install Dapr into current kubeconfig selected context (default: redis=false for the state management)
	# Installing Dapr runtime...
	source ./scripts/install_dapr.sh

	# Installing Grafana dashbaords...
	source ./scripts/install_grafana.sh

	# Deploying the services...
	source ./scripts/deploy.sh

presto: ## Install Presto
	kubectl apply -f ./presto/namespace.yaml
	kubectl apply -f ./presto/internal/ --namespace presto
	kubectl apply -f ./presto/example_presto_v1_cr.yaml --namespace presto

grafana_password: ## Retrieve grafana admin password
	$(eval GPASS=$(shell kubectl get secret -n dapr-monitoring grafana -o jsonpath="{.data.admin-password}" | base64 --decode))
	@echo Grafana admin password is: $(GPASS)

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
	source ./scripts/uninstall.sh

help: ## Display available commands
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk \
		'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

all: help

dapr: ## Install Dapr into current kubeconfig selected context
	# Updating Help repos...
	helm repo add stable https://charts.helm.sh/stable
	helm repo add dapr https://dapr.github.io/helm-charts/
	helm repo update

	# Installing Dapr...
	kubectl create ns optima
	helm install dapr dapr/dapr --namespace optima --set global.logAsJson=true	
	
	# Installing deployments...
	kubectl apply -f  ./dapr/components/secret.yaml
	kubectl apply -f  ./dapr/components/redis.yaml
	kubectl apply -f  ./dapr/components/state.redis.yaml
	kubectl apply -f  ./dapr/components/zipkin.yaml
	kubectl apply -f  ./deployment.yaml
	
	# Installing monitoring...
	kubectl create namespace dapr-monitoring
	helm install dapr-prom stable/prometheus -n dapr-monitoring
	helm install grafana stable/grafana -n dapr-monitoring

show_grafana_password: ## Retrieve grafana admin password
	$(eval GPASS=$(shell kubectl get secret -n dapr-monitoring grafana -o jsonpath="{.data.admin-password}" | base64 --decode))
	@echo Grafana admin password is: $(GPASS)

install_grafana_dash: ## Create Prometheus data source and import Dapr dashboards
	$(eval GPASS=$(shell kubectl get secret -n dapr-monitoring grafana -o jsonpath="{.data.admin-password}" | base64 --decode))
	curl -X POST -s -k -u "admin:$(GPASS)" \
		 -H "Content-Type: application/json" \
		 -d '{ "name":"Dapr", "type":"prometheus", "url":"http://dapr-prom-prometheus-server.dapr-monitoring", "access":"proxy", "basicAuth":false }' \
	     http://localhost:8082/api/datasources
	curl -X POST -s -k -u "admin:$(GPASS)" \
		 -H "Content-Type: application/json" \
		 -d @dapr/grafana/system-services-dashboard.json \
	     http://localhost:8082/api/dashboards/db
	curl -X POST -s -k -u "admin:$(GPASS)" \
		 -H "Content-Type: application/json" \
		 -d @dapr/grafana/sidecar-dashboard.json \
	     http://localhost:8082/api/dashboards/db
	curl -X POST -s -k -u "admin:$(GPASS)" \
		 -H "Content-Type: application/json" \
		 -d @dapr/grafana/actor-dashboard.json \
	     http://localhost:8082/api/dashboards/db

port_forwards: ## Forward observability ports
	# Forwarding ports: dapr=8081, grafana=8082, zipkin=9412
	kubectl port-forward svc/dapr-dashboard 8081:8080 -n optima  &
	kubectl port-forward svc/zipkin 9412:9411 -n optima  &
	kubectl port-forward svc/grafana 8082:80 -n dapr-monitoring  &

unforward: ## Stop previously forwarded ports 
	pkill kubectl -9

uninstall: ## Removes all the deployments and the helm repository.
	helm repo remove stable

	# kubectl delete ns optima
	kubectl delete ns dapr-monitoring

help: ## Display available commands
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk \
		'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

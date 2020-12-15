#!/bin/bash

# This script opens 4 terminal windows.

while [[ $(kubectl get pods -l app.kubernetes.io/name=grafana -n dapr-monitoring -o 'jsonpath={..status.conditions[?(@.type=="Ready")].status}') != "True" ]]; 
    do echo "Waiting for Grafana..." && sleep 1; 
done

kubectl port-forward svc/grafana 8082:80 -n dapr-monitoring &

GPASS=$(kubectl get secret -n dapr-monitoring grafana -o jsonpath="{.data.admin-password}" | base64 --decode)

while [[ "$(curl -s -o /dev/null -w ''%{http_code}'' http://localhost:8082/api/health)" != "200" ]]; do echo "Waiting for port forwarding..." && sleep 1; done

curl -X POST -s -k -u "admin:$GPASS" \
	 -H "Content-Type: application/json" \
	 -d '{ "name":"Dapr", "type":"prometheus", "url":"http://dapr-prom-prometheus-server.dapr-monitoring", "access":"proxy", "basicAuth":false }' \
     http://localhost:8082/api/datasources
curl -X POST -s -k -u "admin:$GPASS" \
	 -H "Content-Type: application/json" \
	 -d @dapr/grafana/system-services-dashboard.json \
     http://localhost:8082/api/dashboards/db
curl -X POST -s -k -u "admin:$GPASS" \
	 -H "Content-Type: application/json" \
	 -d @dapr/grafana/sidecar-dashboard.json \
     http://localhost:8082/api/dashboards/db
curl -X POST -s -k -u "admin:$GPASS" \
	 -H "Content-Type: application/json" \
	 -d @dapr/grafana/actor-dashboard.json \
     http://localhost:8082/api/dashboards/db

pkill kubectl -9

printf "\nDone installing Grafana!"
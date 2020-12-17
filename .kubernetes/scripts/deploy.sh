#!/bin/bash

while [[ $(kubectl get pods -l app=dapr-operator -n optima -o 'jsonpath={..status.conditions[?(@.type=="Ready")].status}') != "True" ]]; 
    do echo "Waiting for dapr operator..." && sleep 1; 
done

kubectl apply -f  ./deployment.yaml
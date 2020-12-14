kubectl apply `
    -f ./dapr/namespace.yaml `
    -f ./dapr/components/secret.yaml `
    -f ./dapr/components/redis.yaml `
    -f ./dapr/components/state.redis.yaml `
    -f ./dapr/components/zipkin.yaml `
    -f ./deployment.yaml
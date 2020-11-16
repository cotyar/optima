# Postgres
docker run -p 5432:5432 -e POSTGRES_PASSWORD=example postgres


# Debezium
docker run -it --rm --name zookeeper -p 2181:2181 -p 2888:2888 -p 3888:3888 debezium/zookeeper:1.3

docker run -it --rm --name kafka -p 9092:9092 --link zookeeper:zookeeper debezium/kafka:1.3

docker run --name keycloak jboss/keycloak -e DB_USER=keycloak -e -p 9080:8080 DB_PASSWORD=password DB_USER=password -e DB_VENDOR=postgres 
#docker run -e DB_VENDOR="h2" --name docker-keycloak-h2 -p 9990:9990 -p 8080:8080 jboss/keycloak
# Postgres
docker run -p 5432:5432 -e POSTGRES_PASSWORD=example postgres


# Debezium
docker run -it --rm --name zookeeper -p 2181:2181 -p 2888:2888 -p 3888:3888 debezium/zookeeper:1.3

docker run -it --rm --name kafka -p 9092:9092 --link zookeeper:zookeeper debezium/kafka:1.3


# Optima
Distributed Dataset management and Calculation service

***Dealing with datasets naturally***

---

## Entities

- Datasets
- Transformations

## Dataset

- Is identifiable
- Can be versioned
- Can be *Chunked*
- Of type
  - Immutable
    - Id is *Hash* (a checksum based on a cryptographic hash) digest
      - SipHash-2-4 (64bit, fast and "good enough")
      - MD5
      - SHA256 (strongest and longest)
    - Is idemportent
  - Streamed
    - Id is globaly unique (UUID)
    - Can support autobatching into chunks (do we need this?)
      - Chunks are organized in a MerkleTree
      - Not necessary idemportent
        - head chunk(s) can be dropped
        - tail chunk is naturally mutable
    - Can simply wrap Kafka/Debezium
- Is named
- Is owned
- Can be groupped
- Has *RBAC*
- Is searchable
- Is audited
- Has a strongly verifiable format (*Schema*)
- Has heritage tracked (lineage)
- Can be represented by:
  - File
    - text
    - binary
    - csv
    - json
    - parquet
  - Database table
    - SQL
      - Oracle
      - PostgreSql
      - Sqlite
    - KV
      - RocksDB
        - Protobufs
- Has a streaming endpoint
  - gRPC (mandatory)
  - Thrift (optional)
- Governance
  - in Git following *GitOps* principles
  - rows as .*proto* messages

## Transformation

- Can be one-off
- Can be executed
- Can be reoccuring
- Can be re-executed
  - creating a new Dataset
  - creating a new version of existing Dataset
- Has *RBAC*
  - Run
  - RerunFailed
- Can be monitored
- Governance
  - in Git following *GitOps* principles
  - rows as .*proto* messages
- Data masking (optional)
  - role sensitive masking

## Infrastructure

- Sensible defaults
- Implemented:
  - with Dapr
    - MQ agnostic
    - Storage agnostic
    - Cloud agnostic
    - Database agnostic
    - does Virtual Actors
    - is simply brilliant
  - as Service Mesh
    - Linkerd or Istio
  - in K8s
  - in Docker containers
- Governance
  - in Git following *GitOps* principles
    - Fluxcd
  - usage quotas (optional)
- Logging
  - Logz
  - Zipkin
  - AppInsights
  - Jaeger
  - etc
- Location awarness (optional)


## Monitoring

- gRPC liveness probe
- K8s monitoring tools
  - Prometeus
  - Grafana
  - Linkerd, Flux, etc. admin pages

## Principles

- Data masking out-of-the-box
- "The cheapest way of doing something is not doing it at all"
- "Staying on the shoulders of giants"
- "nocode" - "Don't solve in code what can be solved in the infrastructure"
- Automate, automate, automate
- k8s are capable to doing a lot
  
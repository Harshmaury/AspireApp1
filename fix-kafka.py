kafka_deployment = """apiVersion: apps/v1
kind: Deployment
metadata:
  name: kafka
  namespace: ums
spec:
  replicas: 1
  selector:
    matchLabels:
      app: kafka
  template:
    metadata:
      labels:
        app: kafka
    spec:
      enableServiceLinks: false
      containers:
      - name: kafka
        image: confluentinc/cp-kafka:7.5.0
        ports:
        - containerPort: 9092
        env:
        - name: KAFKA_BROKER_ID
          value: "1"
        - name: KAFKA_ZOOKEEPER_CONNECT
          value: "zookeeper:2181"
        - name: KAFKA_LISTENERS
          value: "PLAINTEXT://0.0.0.0:9092"
        - name: KAFKA_ADVERTISED_LISTENERS
          value: "PLAINTEXT://kafka:9092"
        - name: KAFKA_LISTENER_SECURITY_PROTOCOL_MAP
          value: "PLAINTEXT:PLAINTEXT"
        - name: KAFKA_INTER_BROKER_LISTENER_NAME
          value: "PLAINTEXT"
        - name: KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR
          value: "1"
        - name: KAFKA_HEAP_OPTS
          value: "-Xmx512m -Xms256m"
        resources:
          requests:
            memory: "512Mi"
            cpu: "300m"
          limits:
            memory: "1Gi"
            cpu: "1"
"""

kafka_service = """apiVersion: v1
kind: Service
metadata:
  name: kafka
  namespace: ums
spec:
  selector:
    app: kafka
  ports:
  - port: 9092
    targetPort: 9092
"""

# Read and patch configmap - change kafka:29092 to kafka:9092
cm = open("k8s/configmap.yaml").read()
cm = cm.replace("kafka:29092", "kafka:9092")

open("k8s/infra/kafka-deployment.yaml", "w").write(kafka_deployment)
open("k8s/infra/kafka-service.yaml", "w").write(kafka_service)
open("k8s/configmap.yaml", "w").write(cm)

print("wrote kafka-deployment.yaml")
print("wrote kafka-service.yaml")
print("patched configmap.yaml kafka:29092 -> kafka:9092")
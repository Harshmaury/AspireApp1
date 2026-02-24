import os

def write(p, c):
    os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, 'w').write(c)
    print('  wrote ' + p)

write('k8s/namespace.yaml', """apiVersion: v1
kind: Namespace
metadata:
  name: ums
""")

write('k8s/configmap.yaml', """apiVersion: v1
kind: ConfigMap
metadata:
  name: ums-config
  namespace: ums
data:
  Seq__ServerUrl: "http://seq:5341"
  OTEL_EXPORTER_OTLP_ENDPOINT: "http://jaeger:4317"
  ConnectionStrings__kafka: "kafka:29092"
  Auth__Authority: "http://identity-api:8080"
  Auth__Audience: "api-gateway"
  Auth__RequireHttpsMetadata: "false"
  UMS__SeedTenantSlug: "ums"
  ASPNETCORE_ENVIRONMENT: "Production"
""")

write('k8s/secret.yaml', """apiVersion: v1
kind: Secret
metadata:
  name: ums-secrets
  namespace: ums
type: Opaque
stringData:
  POSTGRES_USER: "ums_user"
  POSTGRES_PASSWORD: "ums_pass_dev"
  UMS__SeedEmail: "superadmin@ums.com"
  UMS__SeedPassword: "Admin@1234"
  OpenIddict__ClientSecret: "api-gateway-secret"
  ConnectionStrings__IdentityDb: "Host=postgres;Port=5432;Database=IdentityDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__AcademicDb: "Host=postgres;Port=5432;Database=AcademicDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__StudentDb: "Host=postgres;Port=5432;Database=StudentDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__AttendanceDb: "Host=postgres;Port=5432;Database=AttendanceDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__ExaminationDb: "Host=postgres;Port=5432;Database=ExaminationDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__FeeDb: "Host=postgres;Port=5432;Database=FeeDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__NotificationDb: "Host=postgres;Port=5432;Database=NotificationDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__FacultyDb: "Host=postgres;Port=5432;Database=FacultyDb;Username=ums_user;Password=ums_pass_dev"
  ConnectionStrings__HostelDb: "Host=postgres;Port=5432;Database=HostelDb;Username=ums_user;Password=ums_pass_dev"
""")

write('k8s/infra/postgres-pvc.yaml', """apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
  namespace: ums
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 5Gi
""")

write('k8s/infra/postgres-statefulset.yaml', """apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: ums
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:16-alpine
        ports:
        - containerPort: 5432
        env:
        - name: POSTGRES_USER
          valueFrom:
            secretKeyRef:
              name: ums-secrets
              key: POSTGRES_USER
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: ums-secrets
              key: POSTGRES_PASSWORD
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
        resources:
          requests:
            memory: 256Mi
            cpu: 250m
          limits:
            memory: 512Mi
            cpu: 500m
        readinessProbe:
          exec:
            command: [pg_isready, -U, ums_user]
          initialDelaySeconds: 10
          periodSeconds: 5
      volumes:
      - name: postgres-storage
        persistentVolumeClaim:
          claimName: postgres-pvc
""")

write('k8s/infra/postgres-service.yaml', """apiVersion: v1
kind: Service
metadata:
  name: postgres
  namespace: ums
spec:
  selector:
    app: postgres
  clusterIP: None
  ports:
  - port: 5432
    targetPort: 5432
""")

write('k8s/infra/zookeeper-deployment.yaml', """apiVersion: apps/v1
kind: Deployment
metadata:
  name: zookeeper
  namespace: ums
spec:
  replicas: 1
  selector:
    matchLabels:
      app: zookeeper
  template:
    metadata:
      labels:
        app: zookeeper
    spec:
      containers:
      - name: zookeeper
        image: confluentinc/cp-zookeeper:7.5.0
        ports:
        - containerPort: 2181
        env:
        - name: ZOOKEEPER_CLIENT_PORT
          value: "2181"
        - name: ZOOKEEPER_TICK_TIME
          value: "2000"
        resources:
          requests:
            memory: 256Mi
            cpu: 250m
          limits:
            memory: 512Mi
            cpu: 500m
""")

write('k8s/infra/zookeeper-service.yaml', """apiVersion: v1
kind: Service
metadata:
  name: zookeeper
  namespace: ums
spec:
  selector:
    app: zookeeper
  ports:
  - port: 2181
    targetPort: 2181
""")

write('k8s/infra/kafka-deployment.yaml', """apiVersion: apps/v1
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
      containers:
      - name: kafka
        image: confluentinc/cp-kafka:7.5.0
        ports:
        - containerPort: 29092
        env:
        - name: KAFKA_BROKER_ID
          value: "1"
        - name: KAFKA_ZOOKEEPER_CONNECT
          value: zookeeper:2181
        - name: KAFKA_ADVERTISED_LISTENERS
          value: PLAINTEXT://kafka:29092
        - name: KAFKA_LISTENER_SECURITY_PROTOCOL_MAP
          value: PLAINTEXT:PLAINTEXT
        - name: KAFKA_INTER_BROKER_LISTENER_NAME
          value: PLAINTEXT
        - name: KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR
          value: "1"
        resources:
          requests:
            memory: 512Mi
            cpu: 500m
          limits:
            memory: 1Gi
            cpu: "1"
""")

write('k8s/infra/kafka-service.yaml', """apiVersion: v1
kind: Service
metadata:
  name: kafka
  namespace: ums
spec:
  selector:
    app: kafka
  ports:
  - port: 29092
    targetPort: 29092
""")

write('k8s/infra/seq-deployment.yaml', """apiVersion: apps/v1
kind: Deployment
metadata:
  name: seq
  namespace: ums
spec:
  replicas: 1
  selector:
    matchLabels:
      app: seq
  template:
    metadata:
      labels:
        app: seq
    spec:
      containers:
      - name: seq
        image: datalust/seq:latest
        ports:
        - containerPort: 5341
        - containerPort: 80
        env:
        - name: ACCEPT_EULA
          value: "Y"
        resources:
          requests:
            memory: 256Mi
            cpu: 250m
          limits:
            memory: 512Mi
            cpu: 500m
""")

write('k8s/infra/seq-service.yaml', """apiVersion: v1
kind: Service
metadata:
  name: seq
  namespace: ums
spec:
  selector:
    app: seq
  ports:
  - name: ingest
    port: 5341
    targetPort: 5341
  - name: ui
    port: 80
    targetPort: 80
""")

write('k8s/infra/jaeger-deployment.yaml', """apiVersion: apps/v1
kind: Deployment
metadata:
  name: jaeger
  namespace: ums
spec:
  replicas: 1
  selector:
    matchLabels:
      app: jaeger
  template:
    metadata:
      labels:
        app: jaeger
    spec:
      containers:
      - name: jaeger
        image: jaegertracing/all-in-one:1.57
        ports:
        - containerPort: 4317
        - containerPort: 16686
        resources:
          requests:
            memory: 256Mi
            cpu: 250m
          limits:
            memory: 512Mi
            cpu: 500m
""")

write('k8s/infra/jaeger-service.yaml', """apiVersion: v1
kind: Service
metadata:
  name: jaeger
  namespace: ums
spec:
  selector:
    app: jaeger
  ports:
  - name: otlp
    port: 4317
    targetPort: 4317
  - name: ui
    port: 16686
    targetPort: 16686
""")

services = [
    ('identity',     'identity-api',     'aspireapp1-identity-api'),
    ('academic',     'academic-api',     'aspireapp1-academic-api'),
    ('student',      'student-api',      'aspireapp1-student-api'),
    ('attendance',   'attendance-api',   'aspireapp1-attendance-api'),
    ('examination',  'examination-api',  'aspireapp1-examination-api'),
    ('fee',          'fee-api',          'aspireapp1-fee-api'),
    ('notification', 'notification-api', 'aspireapp1-notification-api'),
    ('faculty',      'faculty-api',      'aspireapp1-faculty-api'),
    ('hostel',       'hostel-api',       'aspireapp1-hostel-api'),
]

for svc, name, image in services:
    write(f'k8s/services/{name}-deployment.yaml', f"""apiVersion: apps/v1
kind: Deployment
metadata:
  name: {name}
  namespace: ums
spec:
  replicas: 2
  selector:
    matchLabels:
      app: {name}
  template:
    metadata:
      labels:
        app: {name}
    spec:
      containers:
      - name: {name}
        image: {image}:latest
        imagePullPolicy: Never
        ports:
        - containerPort: 8080
        envFrom:
        - configMapRef:
            name: ums-config
        - secretRef:
            name: ums-secrets
        env:
        - name: OTEL_SERVICE_NAME
          value: {name}
        resources:
          requests:
            memory: 256Mi
            cpu: 250m
          limits:
            memory: 512Mi
            cpu: 500m
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 40
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 20
          periodSeconds: 10
""")
    write(f'k8s/services/{name}-service.yaml', f"""apiVersion: v1
kind: Service
metadata:
  name: {name}
  namespace: ums
spec:
  selector:
    app: {name}
  ports:
  - port: 8080
    targetPort: 8080
""")

write('k8s/services/identity-hpa.yaml', """apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: identity-api-hpa
  namespace: ums
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: identity-api
  minReplicas: 2
  maxReplicas: 6
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
""")

write('k8s/gateway/deployment.yaml', """apiVersion: apps/v1
kind: Deployment
metadata:
  name: api-gateway
  namespace: ums
spec:
  replicas: 2
  selector:
    matchLabels:
      app: api-gateway
  template:
    metadata:
      labels:
        app: api-gateway
    spec:
      containers:
      - name: api-gateway
        image: aspireapp1-api-gateway:latest
        imagePullPolicy: Never
        ports:
        - name: http
          containerPort: 8080
        - name: https
          containerPort: 8443
        envFrom:
        - configMapRef:
            name: ums-config
        - secretRef:
            name: ums-secrets
        env:
        - name: OTEL_SERVICE_NAME
          value: api-gateway
        volumeMounts:
        - name: tls-cert
          mountPath: /certs
          readOnly: true
        resources:
          requests:
            memory: 256Mi
            cpu: 250m
          limits:
            memory: 512Mi
            cpu: 500m
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 15
          periodSeconds: 10
      volumes:
      - name: tls-cert
        hostPath:
          path: /certs
          type: DirectoryOrCreate
""")

write('k8s/gateway/service.yaml', """apiVersion: v1
kind: Service
metadata:
  name: api-gateway
  namespace: ums
spec:
  type: NodePort
  selector:
    app: api-gateway
  ports:
  - name: http
    port: 8080
    targetPort: 8080
    nodePort: 30080
  - name: https
    port: 8443
    targetPort: 8443
    nodePort: 30443
""")

write('k8s/gateway/hpa.yaml', """apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: api-gateway-hpa
  namespace: ums
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: api-gateway
  minReplicas: 2
  maxReplicas: 8
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 60
""")

write('k8s/bff/deployment.yaml', """apiVersion: apps/v1
kind: Deployment
metadata:
  name: bff
  namespace: ums
spec:
  replicas: 2
  selector:
    matchLabels:
      app: bff
  template:
    metadata:
      labels:
        app: bff
    spec:
      containers:
      - name: bff
        image: aspireapp1-bff:latest
        imagePullPolicy: Never
        ports:
        - containerPort: 8080
        envFrom:
        - configMapRef:
            name: ums-config
        - secretRef:
            name: ums-secrets
        env:
        - name: OTEL_SERVICE_NAME
          value: bff
        resources:
          requests:
            memory: 128Mi
            cpu: 125m
          limits:
            memory: 256Mi
            cpu: 250m
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 15
          periodSeconds: 10
""")

write('k8s/bff/service.yaml', """apiVersion: v1
kind: Service
metadata:
  name: bff
  namespace: ums
spec:
  selector:
    app: bff
  ports:
  - port: 8080
    targetPort: 8080
""")

write('k8s/kustomization.yaml', """apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
namespace: ums
resources:
  - namespace.yaml
  - configmap.yaml
  - secret.yaml
  - infra/postgres-pvc.yaml
  - infra/postgres-statefulset.yaml
  - infra/postgres-service.yaml
  - infra/zookeeper-deployment.yaml
  - infra/zookeeper-service.yaml
  - infra/kafka-deployment.yaml
  - infra/kafka-service.yaml
  - infra/seq-deployment.yaml
  - infra/seq-service.yaml
  - infra/jaeger-deployment.yaml
  - infra/jaeger-service.yaml
  - services/identity-api-deployment.yaml
  - services/identity-api-service.yaml
  - services/identity-hpa.yaml
  - services/academic-api-deployment.yaml
  - services/academic-api-service.yaml
  - services/student-api-deployment.yaml
  - services/student-api-service.yaml
  - services/attendance-api-deployment.yaml
  - services/attendance-api-service.yaml
  - services/examination-api-deployment.yaml
  - services/examination-api-service.yaml
  - services/fee-api-deployment.yaml
  - services/fee-api-service.yaml
  - services/notification-api-deployment.yaml
  - services/notification-api-service.yaml
  - services/faculty-api-deployment.yaml
  - services/faculty-api-service.yaml
  - services/hostel-api-deployment.yaml
  - services/hostel-api-service.yaml
  - gateway/deployment.yaml
  - gateway/service.yaml
  - gateway/hpa.yaml
  - bff/deployment.yaml
  - bff/service.yaml
""")

print('Done — all K8s manifests generated in k8s/ (38 files)')
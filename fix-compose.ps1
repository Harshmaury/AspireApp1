$c = Get-Content "docker-compose.yml" -Raw
$old = '  api-gateway:
    build:
      context: .
      dockerfile: src/ApiGateway/Dockerfile
    container_name: ums-gateway
    env_file: .env
    environment:
      Auth__Authority: ${Auth__Authority}
      Auth__Audience: ${Auth__Audience}
      Seq__ServerUrl: ${Seq__ServerUrl}
      OTEL_EXPORTER_OTLP_ENDPOINT: ${OTEL_EXPORTER_OTLP_ENDPOINT}
      OTEL_SERVICE_NAME: api-gateway
    ports:
        - "8080:8080"
        - "8443:8443"
      volumes:
        - ./certs:/certs:ro
    depends_on:
      identity-api:
        condition: service_healthy
    networks:
      - ums-network'
$new = '  api-gateway:
    build:
      context: .
      dockerfile: src/ApiGateway/Dockerfile
    container_name: ums-gateway
    env_file: .env
    environment:
      Auth__Authority: ${Auth__Authority}
      Auth__Audience: ${Auth__Audience}
      Seq__ServerUrl: ${Seq__ServerUrl}
      OTEL_EXPORTER_OTLP_ENDPOINT: ${OTEL_EXPORTER_OTLP_ENDPOINT}
      OTEL_SERVICE_NAME: api-gateway
    ports:
      - "8080:8080"
      - "8443:8443"
    volumes:
      - ./certs:/certs:ro
    depends_on:
      identity-api:
        condition: service_healthy
    networks:
      - ums-network'
$c = $c.Replace($old, $new)
Set-Content "docker-compose.yml" $c
Write-Host "Done"

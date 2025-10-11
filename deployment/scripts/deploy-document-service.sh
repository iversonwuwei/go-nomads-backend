#!/bin/bash

# Build DocumentService Docker Image
echo "Building DocumentService Docker image..."
podman build -f src/Services/DocumentService/Dockerfile -t go-nomads-document-service:latest . --quiet

# Stop and remove existing container if exists
echo "Stopping existing DocumentService container..."
podman stop go-nomads-document-service 2>/dev/null || true
podman rm go-nomads-document-service 2>/dev/null || true

# Run DocumentService with Dapr sidecar
echo "Starting DocumentService with Dapr..."
podman run -d \
  --name go-nomads-document-service \
  --network go-nomads-network \
  -p 5003:8080 \
  -p 50004:50004 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  go-nomads-document-service:latest

# Wait for container to be ready
sleep 3

# Start Dapr sidecar for DocumentService
dapr run \
  --app-id document-service \
  --app-port 8080 \
  --dapr-http-port 3503 \
  --dapr-grpc-port 50004 \
  --components-path ./deployment/dapr/components \
  --config ./deployment/dapr/config.yaml \
  --log-level debug \
  -- echo "Dapr sidecar for DocumentService started"

echo "DocumentService deployed successfully!"
echo "Access the service at: http://localhost:5003"
echo "Access Scalar documentation at: http://localhost:5003/scalar/v1"

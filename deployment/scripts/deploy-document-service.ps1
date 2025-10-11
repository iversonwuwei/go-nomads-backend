# Build DocumentService Docker Image
Write-Host "Building DocumentService Docker image..." -ForegroundColor Cyan
podman build -f src/Services/DocumentService/Dockerfile -t go-nomads-document-service:latest . --quiet

# Stop and remove existing container if exists
Write-Host "Stopping existing DocumentService container..." -ForegroundColor Yellow
podman stop go-nomads-document-service 2>$null
podman rm go-nomads-document-service 2>$null

# Run DocumentService
Write-Host "Starting DocumentService..." -ForegroundColor Green
podman run -d `
  --name go-nomads-document-service `
  --network go-nomads-network `
  -p 5003:8080 `
  -p 50004:50004 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  go-nomads-document-service:latest

# Wait for container to be ready
Start-Sleep -Seconds 3

Write-Host "`n✅ DocumentService deployed successfully!" -ForegroundColor Green
Write-Host "📝 Access the service at: http://localhost:5003" -ForegroundColor Cyan
Write-Host "📚 Access Scalar documentation at: http://localhost:5003/scalar/v1" -ForegroundColor Cyan
Write-Host "🔍 API Specs aggregation at: http://localhost:5003/api/specs" -ForegroundColor Cyan
Write-Host "📋 Services list at: http://localhost:5003/api/services" -ForegroundColor Cyan

# Go Nomads 可观测性基础设施启动脚本 (PowerShell)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Go Nomads Observability Stack" -ForegroundColor Cyan
Write-Host "  OpenTelemetry + Jaeger + Prometheus" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 检查 Docker 网络
$networks = docker network ls --format "{{.Name}}"
if ($networks -notcontains "go-nomads-network") {
    Write-Host "`n创建 Docker 网络: go-nomads-network" -ForegroundColor Yellow
    docker network create go-nomads-network
}

# 启动可观测性基础设施
Write-Host "`n启动可观测性服务..." -ForegroundColor Yellow
docker-compose -f docker-compose-observability.yml up -d

Write-Host "`n等待服务启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# 检查服务状态
Write-Host "`n检查服务状态..." -ForegroundColor Yellow
Write-Host ""

# Jaeger
try {
    $response = Invoke-WebRequest -Uri "http://localhost:16686" -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    Write-Host "✅ Jaeger UI: http://localhost:16686" -ForegroundColor Green
} catch {
    Write-Host "⏳ Jaeger UI 正在启动..." -ForegroundColor Yellow
}

# Prometheus
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9090/-/healthy" -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    Write-Host "✅ Prometheus: http://localhost:9090" -ForegroundColor Green
} catch {
    Write-Host "⏳ Prometheus 正在启动..." -ForegroundColor Yellow
}

# Grafana
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    Write-Host "✅ Grafana: http://localhost:3000 (admin/admin)" -ForegroundColor Green
} catch {
    Write-Host "⏳ Grafana 正在启动..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  可观测性服务已启动!" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "访问地址:" -ForegroundColor White
Write-Host "  - Jaeger UI:    http://localhost:16686" -ForegroundColor White
Write-Host "  - Prometheus:   http://localhost:9090" -ForegroundColor White
Write-Host "  - Grafana:      http://localhost:3000" -ForegroundColor White
Write-Host ""
Write-Host "Grafana 默认登录: admin / admin" -ForegroundColor Yellow
Write-Host ""
Write-Host "如需启动 OpenTelemetry Collector (可选):" -ForegroundColor Gray
Write-Host "  docker-compose -f docker-compose-observability.yml --profile otel-collector up -d otel-collector" -ForegroundColor Gray
Write-Host ""

# ============================================================
# Go-Nomads 可观测性测试脚本
# 用于快速验证 OpenTelemetry + Jaeger + Prometheus 配置
# ============================================================
param(
    [string]$Action = "test"
)

$ErrorActionPreference = "Stop"

# Colors
function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "[i] $Message" -ForegroundColor Yellow
}

# Test Jaeger availability
function Test-Jaeger {
    Write-Header "Testing Jaeger"
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:16686" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Success "Jaeger UI is accessible at http://localhost:16686"
        }
    }
    catch {
        Write-Fail "Jaeger UI is not accessible"
        return $false
    }
    
    # Test OTLP endpoint
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:4318/v1/traces" -Method POST -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    }
    catch {
        # 405 is expected for GET, but we're testing connectivity
        if ($_.Exception.Response.StatusCode -ne 405 -and $_.Exception.Response.StatusCode -ne 415) {
            Write-Info "Jaeger OTLP HTTP endpoint (4318) - may need POST request with data"
        } else {
            Write-Success "Jaeger OTLP HTTP endpoint is responding at http://localhost:4318"
        }
    }
    
    return $true
}

# Test Prometheus availability
function Test-Prometheus {
    Write-Header "Testing Prometheus"
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:9090/-/healthy" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Success "Prometheus is healthy at http://localhost:9090"
        }
    }
    catch {
        Write-Fail "Prometheus is not accessible"
        return $false
    }
    
    # Check if targets are configured
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec 5
        $activeTargets = $response.data.activeTargets.Count
        $upTargets = ($response.data.activeTargets | Where-Object { $_.health -eq "up" }).Count
        
        Write-Info "Active targets: $activeTargets, Up: $upTargets"
        
        foreach ($target in $response.data.activeTargets) {
            $status = if ($target.health -eq "up") { "[UP]" } else { "[DOWN]" }
            $color = if ($target.health -eq "up") { "Green" } else { "Red" }
            Write-Host "  $status $($target.labels.job) - $($target.scrapeUrl)" -ForegroundColor $color
        }
    }
    catch {
        Write-Info "Could not fetch targets info"
    }
    
    return $true
}

# Test Grafana availability
function Test-Grafana {
    Write-Header "Testing Grafana"
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Success "Grafana is healthy at http://localhost:3000"
        }
    }
    catch {
        Write-Fail "Grafana is not accessible"
        return $false
    }
    
    # Check datasources
    try {
        $headers = @{
            Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
        }
        $response = Invoke-RestMethod -Uri "http://localhost:3000/api/datasources" -Headers $headers -TimeoutSec 5
        
        Write-Info "Configured datasources:"
        foreach ($ds in $response) {
            Write-Host "  - $($ds.name) ($($ds.type))" -ForegroundColor Cyan
        }
    }
    catch {
        Write-Info "Could not fetch datasources (may need authentication)"
    }
    
    return $true
}

# Test service metrics endpoint
function Test-ServiceMetrics {
    param([string]$ServiceName, [int]$Port)
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/metrics" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Success "$ServiceName metrics available at http://localhost:$Port/metrics"
            
            # Count metrics
            $lines = $response.Content -split "`n" | Where-Object { $_ -notmatch "^#" -and $_.Trim() -ne "" }
            Write-Info "  $($lines.Count) metric samples"
            
            return $true
        }
    }
    catch {
        Write-Fail "$ServiceName metrics not available at http://localhost:$Port/metrics"
        return $false
    }
    return $false
}

# Test Gateway health
function Test-Gateway {
    Write-Header "Testing Gateway"
    
    # Test health endpoint
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Success "Gateway health check passed"
        }
    }
    catch {
        Write-Info "Gateway health endpoint not responding (service may not be running)"
    }
    
    # Test metrics
    Test-ServiceMetrics -ServiceName "Gateway" -Port 5000
}

# Test all services
function Test-AllServices {
    Write-Header "Testing Service Metrics"
    
    $services = @(
        @{ Name = "Gateway"; Port = 5000 },
        @{ Name = "UserService"; Port = 5001 },
        @{ Name = "CityService"; Port = 8002 },
        @{ Name = "AccommodationService"; Port = 8003 },
        @{ Name = "CoworkingService"; Port = 8004 },
        @{ Name = "EventService"; Port = 8005 },
        @{ Name = "AIService"; Port = 8006 },
        @{ Name = "MessageService"; Port = 8007 },
        @{ Name = "SearchService"; Port = 8008 }
    )
    
    $runningServices = 0
    foreach ($service in $services) {
        if (Test-ServiceMetrics -ServiceName $service.Name -Port $service.Port) {
            $runningServices++
        }
    }
    
    Write-Info "$runningServices of $($services.Count) services are exposing metrics"
}

# Generate test traffic
function Generate-TestTraffic {
    Write-Header "Generating Test Traffic"
    
    Write-Info "Sending requests to Gateway..."
    
    $endpoints = @(
        "http://localhost:5000/health",
        "http://localhost:5000/api/v1/cities",
        "http://localhost:5000/api/v1/users/profile"
    )
    
    for ($i = 1; $i -le 10; $i++) {
        foreach ($endpoint in $endpoints) {
            try {
                $response = Invoke-WebRequest -Uri $endpoint -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
                Write-Host "." -NoNewline -ForegroundColor Green
            }
            catch {
                Write-Host "x" -NoNewline -ForegroundColor Red
            }
        }
    }
    Write-Host ""
    Write-Success "Test traffic generated. Check Jaeger for traces."
}

# Show summary
function Show-Summary {
    Write-Header "Observability Stack Summary"
    
    Write-Host ""
    Write-Host "URLs:" -ForegroundColor Cyan
    Write-Host "  Jaeger UI:        http://localhost:16686" -ForegroundColor Green
    Write-Host "  Prometheus:       http://localhost:9090"
    Write-Host "  Grafana:          http://localhost:3000 (admin/admin)"
    Write-Host ""
    Write-Host "Endpoints:" -ForegroundColor Cyan
    Write-Host "  OTLP gRPC:        localhost:4317"
    Write-Host "  OTLP HTTP:        localhost:4318"
    Write-Host "  Zipkin compat:    localhost:9411"
    Write-Host ""
    Write-Host "Quick Actions:" -ForegroundColor Cyan
    Write-Host "  1. Open Jaeger UI to view traces"
    Write-Host "  2. Open Prometheus to query metrics"
    Write-Host "  3. Open Grafana to view dashboards"
    Write-Host ""
}

# Help
function Show-Help {
    Write-Host ""
    Write-Host "Go-Nomads Observability Test Script" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\test-observability.ps1 [command]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Cyan
    Write-Host "  test       Run all observability tests (default)" -ForegroundColor Cyan
    Write-Host "  jaeger     Test Jaeger only" -ForegroundColor Cyan
    Write-Host "  prometheus Test Prometheus only" -ForegroundColor Cyan
    Write-Host "  grafana    Test Grafana only" -ForegroundColor Cyan
    Write-Host "  services   Test all service metrics endpoints" -ForegroundColor Cyan
    Write-Host "  traffic    Generate test traffic for tracing" -ForegroundColor Cyan
    Write-Host "  summary    Show stack URLs and info" -ForegroundColor Cyan
    Write-Host "  help       Show this help message" -ForegroundColor Cyan
    Write-Host ""
}

# Main
switch ($Action) {
    'test' {
        Test-Jaeger
        Test-Prometheus
        Test-Grafana
        Test-AllServices
        Show-Summary
    }
    'jaeger' { Test-Jaeger }
    'prometheus' { Test-Prometheus }
    'grafana' { Test-Grafana }
    'services' { Test-AllServices }
    'traffic' { Generate-TestTraffic }
    'summary' { Show-Summary }
    'help' { Show-Help }
    default { Show-Help }
}

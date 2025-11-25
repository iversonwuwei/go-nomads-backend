# Test Event Types API Routing

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Testing Event Types API Routing" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$GatewayUrl = "http://localhost:5000"
$EventServiceUrl = "http://localhost:8005"

# Test 1: Direct access to EventService (bypass Gateway)
Write-Host "Test 1: Direct EventService Access" -ForegroundColor Yellow
Write-Host "URL: $EventServiceUrl/api/v1/event-types" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$EventServiceUrl/api/v1/event-types" -Method Get -ContentType "application/json"
    
    if ($response.StatusCode -eq 200) {
        Write-Host "[OK] EventService responded successfully" -ForegroundColor Green
        $data = $response.Content | ConvertFrom-Json
        Write-Host "  Data type: $($data.data.GetType().Name)" -ForegroundColor Gray
        Write-Host "  Type count: $($data.data.Count)" -ForegroundColor Gray
        
        if ($data.data.Count -gt 0) {
            Write-Host "  Sample: $($data.data[0].name) - $($data.data[0].enName)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "[ERROR] EventService access failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Access through Gateway
Write-Host "Test 2: Gateway Access" -ForegroundColor Yellow
Write-Host "URL: $GatewayUrl/api/v1/event-types" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$GatewayUrl/api/v1/event-types" -Method Get -ContentType "application/json"
    
    if ($response.StatusCode -eq 200) {
        Write-Host "[OK] Gateway routing successful" -ForegroundColor Green
        $data = $response.Content | ConvertFrom-Json
        Write-Host "  Data type: $($data.data.GetType().Name)" -ForegroundColor Gray
        Write-Host "  Type count: $($data.data.Count)" -ForegroundColor Gray
        
        if ($data.data.Count -gt 0) {
            Write-Host "  Sample: $($data.data[0].name) - $($data.data[0].enName)" -ForegroundColor Gray
        }
        
        Write-Host ""
        Write-Host "[SUCCESS] Test passed! Gateway routing is correct!" -ForegroundColor Green
    }
} catch {
    $errorMessage = $_.Exception.Message
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    Write-Host "[ERROR] Gateway routing failed" -ForegroundColor Red
    Write-Host "  Status code: $statusCode" -ForegroundColor Gray
    Write-Host "  Error: $errorMessage" -ForegroundColor Gray
    
    if ($statusCode -eq 404) {
        Write-Host ""
        Write-Host "Possible causes:" -ForegroundColor Yellow
        Write-Host "  1. Gateway service not started" -ForegroundColor Gray
        Write-Host "  2. Gateway routing config not updated" -ForegroundColor Gray
        Write-Host "  3. Gateway needs restart to load new config" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Solutions:" -ForegroundColor Yellow
        Write-Host "  1. Check Gateway health: curl http://localhost:5000/health" -ForegroundColor Gray
        Write-Host "  2. Restart Gateway service" -ForegroundColor Gray
        Write-Host "  3. Wait 30 seconds for Consul service discovery" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

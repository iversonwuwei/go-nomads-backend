# Simple Async Task Test
# Test async travel plan generation API

$ErrorActionPreference = "Stop"

Write-Host "Testing Async Travel Plan API" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

# AI Service URL
$aiServiceUrl = "http://localhost:8009"

# Request body
$requestBody = @{
    cityId = "2"
    cityName = "Shanghai"
    duration = 3
    interests = @("Food", "Culture", "Shopping")
    budget = "medium"
    travelStyle = "culture"
} | ConvertTo-Json

Write-Host "`nStep 1: Create async task" -ForegroundColor Yellow
Write-Host "Request: $requestBody" -ForegroundColor Gray

try {
    $createResponse = Invoke-RestMethod -Uri "$aiServiceUrl/api/v1/ai/travel-plan/async" `
        -Method Post `
        -ContentType "application/json" `
        -Body $requestBody
    
    Write-Host "Task created successfully!" -ForegroundColor Green
    Write-Host "Task ID: $($createResponse.data.taskId)" -ForegroundColor Cyan
    Write-Host "Status: $($createResponse.data.status)" -ForegroundColor Cyan
    
    $taskId = $createResponse.data.taskId
    
    Write-Host "`nStep 2: Poll task status" -ForegroundColor Yellow
    
    $maxAttempts = 40
    $attempt = 0
    $completed = $false
    
    while (-not $completed -and $attempt -lt $maxAttempts) {
        $attempt++
        Write-Host "`nPolling attempt $attempt..." -ForegroundColor Gray
        
        $statusResponse = Invoke-RestMethod -Uri "$aiServiceUrl/api/v1/ai/travel-plan/tasks/$taskId" `
            -Method Get
        
        $status = $statusResponse.data.status
        $progress = $statusResponse.data.progress
        $message = $statusResponse.data.progressMessage
        
        Write-Host "   Status: $status" -ForegroundColor Cyan
        Write-Host "   Progress: $progress%" -ForegroundColor Cyan
        if ($message) {
            Write-Host "   Message: $message" -ForegroundColor Cyan
        }
        
        if ($status -eq "completed") {
            Write-Host "`nTask completed successfully!" -ForegroundColor Green
            Write-Host "Plan ID: $($statusResponse.data.planId)" -ForegroundColor Green
            $completed = $true
        }
        elseif ($status -eq "failed") {
            Write-Host "`nTask failed!" -ForegroundColor Red
            Write-Host "Error: $($statusResponse.data.error)" -ForegroundColor Red
            exit 1
        }
        else {
            Start-Sleep -Seconds 3
        }
    }
    
    if (-not $completed) {
        Write-Host "`nTask timeout (waited more than 2 minutes)" -ForegroundColor Yellow
    }
    
}
catch {
    Write-Host "`nTest failed!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`nTest completed!" -ForegroundColor Green

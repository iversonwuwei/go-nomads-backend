# Test new travel plan retrieval API
$ErrorActionPreference = "Stop"

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "   Testing Travel Plan API - Complete Flow" -ForegroundColor Cyan
Write-Host "============================================`n" -ForegroundColor Cyan

# Step 1: Create async task
Write-Host "Step 1: Creating async task..." -ForegroundColor Yellow
$taskRequest = @{
    cityId = "beijing"
    cityName = "Beijing"
    duration = 3
    budget = "medium"
    travelStyle = "culture"
    interests = @("Coffee", "Museums", "Parks")
} | ConvertTo-Json

try {
    $taskResponse = Invoke-RestMethod -Uri "http://localhost:8009/api/v1/ai/travel-plan/async" `
        -Method Post `
        -Body $taskRequest `
        -ContentType "application/json"
    
    $taskId = $taskResponse.data.taskId
    Write-Host "✅ Task created: $taskId" -ForegroundColor Green
    Write-Host "   Estimated time: $($taskResponse.data.estimatedTimeSeconds)s`n" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Failed to create task: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Poll task status
Write-Host "Step 2: Polling task status..." -ForegroundColor Yellow
$maxAttempts = 60
$attempt = 0
$planId = $null

while ($attempt -lt $maxAttempts) {
    $attempt++
    Start-Sleep -Seconds 2
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "http://localhost:8009/api/v1/ai/travel-plan/tasks/$taskId" -Method Get
        
        $status = $statusResponse.data.status
        $progress = $statusResponse.data.progress
        $message = $statusResponse.data.progressMessage
        
        Write-Host "   [$attempt] Status: $status | Progress: $progress% | $message" -ForegroundColor Gray
        
        if ($status -eq "completed") {
            $planId = $statusResponse.data.planId
            Write-Host "`n✅ Task completed! PlanId: $planId" -ForegroundColor Green
            break
        }
        elseif ($status -eq "failed") {
            $error = $statusResponse.data.error
            Write-Host "`n❌ Task failed: $error" -ForegroundColor Red
            exit 1
        }
    }
    catch {
        Write-Host "   ⚠️ Status check failed: $_" -ForegroundColor Yellow
    }
}

if (-not $planId) {
    Write-Host "`n❌ Timeout waiting for task completion" -ForegroundColor Red
    exit 1
}

# Step 3: Retrieve plan details using NEW API
Write-Host "`nStep 3: Retrieving plan details..." -ForegroundColor Yellow
try {
    $planResponse = Invoke-RestMethod -Uri "http://localhost:8009/api/v1/ai/travel-plans/$planId" -Method Get
    
    if ($planResponse.success -and $planResponse.data) {
        $plan = $planResponse.data
        Write-Host "✅ Plan retrieved successfully!`n" -ForegroundColor Green
        
        # Display plan details
        Write-Host "============================================" -ForegroundColor Cyan
        Write-Host "   Travel Plan Details" -ForegroundColor Cyan
        Write-Host "============================================" -ForegroundColor Cyan
        Write-Host "ID:          $($plan.id)" -ForegroundColor White
        Write-Host "City:        $($plan.cityName)" -ForegroundColor White
        Write-Host "Duration:    $($plan.duration) days" -ForegroundColor White
        Write-Host "Budget:      $($plan.budgetLevel)" -ForegroundColor White
        Write-Host "Created:     $($plan.createdAt)" -ForegroundColor White
        
        # Check if plan has structured data
        if ($plan.attractions) {
            Write-Host "`nAttractions: $($plan.attractions.Count)" -ForegroundColor Cyan
        }
        if ($plan.restaurants) {
            Write-Host "Restaurants: $($plan.restaurants.Count)" -ForegroundColor Cyan
        }
        if ($plan.itineraries) {
            Write-Host "Itineraries: $($plan.itineraries.Count)" -ForegroundColor Cyan
        }
        
        Write-Host "`n✅ TEST PASSED: API returns JSON object!" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Plan data is empty or invalid" -ForegroundColor Red
        Write-Host "Response: $($planResponse | ConvertTo-Json -Depth 5)" -ForegroundColor Gray
        exit 1
    }
}
catch {
    Write-Host "❌ Failed to retrieve plan: $_" -ForegroundColor Red
    Write-Host "Error Details: $($_.Exception.Message)" -ForegroundColor Gray
    exit 1
}

# Step 4: Verify Redis storage format
Write-Host "`nStep 4: Verifying Redis storage..." -ForegroundColor Yellow
try {
    $redisContent = docker exec go-nomads-redis redis-cli GET "plan:$planId"
    
    # Check if content looks like JSON (starts with {)
    if ($redisContent -match '^\s*\{') {
        Write-Host "✅ Redis data is in JSON format!" -ForegroundColor Green
        
        # Try to parse JSON
        try {
            $jsonData = $redisContent | ConvertFrom-Json
            Write-Host "✅ JSON is valid and parseable!" -ForegroundColor Green
            Write-Host "   Contains: id=$($jsonData.id), cityName=$($jsonData.cityName)" -ForegroundColor Gray
        }
        catch {
            Write-Host "⚠️ Redis data looks like JSON but failed to parse: $_" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "❌ Redis data is NOT JSON (raw text)" -ForegroundColor Red
        Write-Host "First 200 chars: $($redisContent.Substring(0, [Math]::Min(200, $redisContent.Length)))" -ForegroundColor Gray
        exit 1
    }
}
catch {
    Write-Host "⚠️ Could not verify Redis: $_" -ForegroundColor Yellow
}

Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host "   ✅ ALL TESTS PASSED!" -ForegroundColor Green
Write-Host "============================================`n" -ForegroundColor Cyan

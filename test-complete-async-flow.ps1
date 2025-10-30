#!/usr/bin/env pwsh
# 测试完整的异步任务流程 (包括获取计划详情)

$baseUrl = "http://localhost:8009"
$headers = @{
    "Content-Type" = "application/json"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "异步任务队列 - 完整流程测试" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# 1. 创建异步任务
Write-Host "📤 1. 创建异步任务..." -ForegroundColor Yellow

$requestBody = @{
    cityId = "test-city-001"
    cityName = "成都"
    duration = 3
    budget = "medium"
    travelStyle = "culture"
    interests = @("美食", "历史", "夜生活")
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/ai/travel-plan/async" -Method Post -Body $requestBody -Headers $headers
    $taskId = $response.data.taskId
    
    Write-Host "✅ 任务已创建" -ForegroundColor Green
    Write-Host "   TaskId: $taskId" -ForegroundColor Gray
    Write-Host "   Status: $($response.data.status)" -ForegroundColor Gray
    Write-Host "   Message: $($response.data.message)`n" -ForegroundColor Gray
} catch {
    Write-Host "❌ 创建任务失败: $_" -ForegroundColor Red
    exit 1
}

# 2. 轮询任务状态
Write-Host "⏳ 2. 等待任务完成..." -ForegroundColor Yellow

$maxRetries = 60
$retryCount = 0
$planId = $null

while ($retryCount -lt $maxRetries) {
    Start-Sleep -Seconds 2
    $retryCount++
    
    try {
        $statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/ai/travel-plan/tasks/$taskId" -Method Get
        $status = $statusResponse.data
        
        Write-Host "   [$retryCount/$maxRetries] 进度: $($status.progress)% - $($status.progressMessage)" -ForegroundColor Gray
        
        if ($status.status -eq "completed") {
            $planId = $status.planId
            Write-Host "`n✅ 任务完成!" -ForegroundColor Green
            Write-Host "   PlanId: $planId`n" -ForegroundColor Gray
            break
        } elseif ($status.status -eq "failed") {
            Write-Host "`n❌ 任务失败: $($status.error)" -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Host "   ⚠️ 查询状态失败: $_" -ForegroundColor Yellow
    }
}

if ($null -eq $planId) {
    Write-Host "`n❌ 任务超时 (120秒)" -ForegroundColor Red
    exit 1
}

# 3. 获取旅行计划详情
Write-Host "📥 3. 获取旅行计划详情..." -ForegroundColor Yellow

try {
    $planResponse = Invoke-RestMethod -Uri "$baseUrl/api/v1/ai/travel-plans/$planId" -Method Get
    $plan = $planResponse.data
    
    Write-Host "✅ 旅行计划获取成功`n" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "旅行计划详情" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "📍 城市: $($plan.cityName)" -ForegroundColor White
    Write-Host "⏱️  天数: $($plan.duration) 天" -ForegroundColor White
    Write-Host "💰 预算: $($plan.budget)" -ForegroundColor White
    Write-Host "🎨 风格: $($plan.travelStyle)" -ForegroundColor White
    Write-Host "🎯 兴趣: $($plan.interests -join ', ')" -ForegroundColor White
    Write-Host ""
    Write-Host "📅 每日行程: $($plan.dailyItineraries.Count) 天" -ForegroundColor White
    Write-Host "🏛️  景点数: $($plan.attractions.Count)" -ForegroundColor White
    Write-Host "🍽️  餐厅数: $($plan.restaurants.Count)" -ForegroundColor White
    Write-Host ""
    
    # 显示第一天的行程
    if ($plan.dailyItineraries.Count -gt 0) {
        $day1 = $plan.dailyItineraries[0]
        Write-Host "📌 Day 1 行程:" -ForegroundColor Yellow
        Write-Host "   主题: $($day1.theme)" -ForegroundColor Gray
        foreach ($activity in $day1.activities) {
            Write-Host "   - $($activity.time): $($activity.name)" -ForegroundColor Gray
        }
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "✅ 完整流程测试通过!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ 获取计划详情失败: $_" -ForegroundColor Red
    Write-Host "   响应: $($_.Exception.Response)" -ForegroundColor Gray
    exit 1
}

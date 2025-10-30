# 测试异步旅行计划生成 API
# 使用方法: .\test-async-travel-plan.ps1

$ErrorActionPreference = "Stop"

Write-Host "🚀 测试异步旅行计划生成 API" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# AI Service URL
$aiServiceUrl = "http://localhost:8009"

# 测试请求数据
$requestBody = @{
    cityId = 2
    cityName = "上海"
    days = 3
    interests = @("美食", "文化", "购物")
    budget = 3000
} | ConvertTo-Json

Write-Host "`n📤 步骤 1: 创建异步任务" -ForegroundColor Yellow
Write-Host "请求数据: $requestBody" -ForegroundColor Gray

try {
    $createResponse = Invoke-RestMethod -Uri "$aiServiceUrl/api/v1/ai/travel-plan/async" `
        -Method Post `
        -ContentType "application/json" `
        -Body $requestBody
    
    Write-Host "✅ 任务创建成功!" -ForegroundColor Green
    Write-Host "任务ID: $($createResponse.data.taskId)" -ForegroundColor Cyan
    Write-Host "状态: $($createResponse.data.status)" -ForegroundColor Cyan
    Write-Host "消息: $($createResponse.data.message)" -ForegroundColor Cyan
    
    $taskId = $createResponse.data.taskId
    
    Write-Host "`n📊 步骤 2: 轮询任务状态" -ForegroundColor Yellow
    
    $maxAttempts = 40  # 最多等待 2 分钟 (40 * 3秒)
    $attempt = 0
    $completed = $false
    
    while (-not $completed -and $attempt -lt $maxAttempts) {
        $attempt++
        Write-Host "`n⏳ 查询任务状态 (第 $attempt 次)..." -ForegroundColor Gray
        
        $statusResponse = Invoke-RestMethod -Uri "$aiServiceUrl/api/v1/ai/travel-plan/tasks/$taskId" `
            -Method Get
        
        $status = $statusResponse.data.status
        $progress = $statusResponse.data.progress
        $message = $statusResponse.data.progressMessage
        
        Write-Host "   状态: $status" -ForegroundColor Cyan
        Write-Host "   进度: $progress%" -ForegroundColor Cyan
        if ($message) {
            Write-Host "   消息: $message" -ForegroundColor Cyan
        }
        
        if ($status -eq "completed") {
            Write-Host "`n🎉 任务完成!" -ForegroundColor Green
            Write-Host "旅行计划 ID: $($statusResponse.data.planId)" -ForegroundColor Green
            $completed = $true
        }
        elseif ($status -eq "failed") {
            Write-Host "`n❌ 任务失败!" -ForegroundColor Red
            Write-Host "错误: $($statusResponse.data.error)" -ForegroundColor Red
            exit 1
        }
        else {
            # 等待 3 秒后再次查询
            Start-Sleep -Seconds 3
        }
    }
    
    if (-not $completed) {
        Write-Host "`n⚠️ 任务超时 (等待超过 2 分钟)" -ForegroundColor Yellow
        Write-Host "任务可能仍在处理中,请稍后手动查询" -ForegroundColor Yellow
    }
    
}
catch {
    Write-Host "`n❌ 测试失败!" -ForegroundColor Red
    Write-Host "错误: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "响应内容: $responseBody" -ForegroundColor Red
    }
    exit 1
}

Write-Host "`n✅ 测试完成!" -ForegroundColor Green

#!/usr/bin/env pwsh
# AI 旅游计划生成接口测试脚本

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "AI 旅游计划生成接口测试" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan

# 1. 先登录获取 token
Write-Host "1️⃣  登录获取 token..." -ForegroundColor Yellow
$loginBody = @{
    email = "test@example.com"
    password = "Test@123456"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody
    
    $token = $loginResponse.data.token
    $userId = $loginResponse.data.userId
    
    Write-Host "✅ 登录成功" -ForegroundColor Green
    Write-Host "Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
    Write-Host "UserId: $userId`n" -ForegroundColor Gray
} catch {
    Write-Host "❌ 登录失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. 调用 AI 旅游计划生成接口
Write-Host "2️⃣  生成旅游计划..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $token"
    "X-User-Id" = $userId
    "Content-Type" = "application/json"
}

$travelPlanBody = @{
    cityId = "北京市"
    cityName = "北京市"
    duration = 7
    budget = "medium"
    travelStyle = "culture"
    interests = @("Art", "Markets", "attraction:historic", "attraction:shopping_mall")
    departureLocation = "北京市东城区东华门街道天安门"
} | ConvertTo-Json

Write-Host "请求体:" -ForegroundColor Gray
Write-Host $travelPlanBody -ForegroundColor Gray
Write-Host ""

try {
    $planResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/ai/travel-plan" `
        -Method Post `
        -Headers $headers `
        -Body $travelPlanBody
    
    Write-Host "✅ 生成成功" -ForegroundColor Green
    Write-Host "`n📋 响应数据:" -ForegroundColor Cyan
    Write-Host ($planResponse | ConvertTo-Json -Depth 10) -ForegroundColor White
} catch {
    Write-Host "❌ 生成失败" -ForegroundColor Red
    Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "详细错误:" -ForegroundColor Red
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    
    exit 1
}

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "测试完成！" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

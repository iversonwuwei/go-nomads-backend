# 快速测试 Coworking Review API
# 用法: .\quick-test-review.ps1 <coworking-id>

param(
    [Parameter(Mandatory=$false)]
    [string]$CoworkingId = "00000000-0000-0000-0000-000000000000"
)

$baseUrl = "http://localhost:8004"

Write-Host "🧪 快速测试 Coworking Review API" -ForegroundColor Cyan
Write-Host "Coworking ID: $CoworkingId" -ForegroundColor Gray
Write-Host ""

# 1. 获取评论列表
Write-Host "1️⃣  获取评论列表..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Method GET -Uri "$baseUrl/api/v1/coworking/$CoworkingId/reviews?page=1&pageSize=10"
    Write-Host "✅ 成功 | 总数: $($response.data.totalCount) | 当前页: $($response.data.items.Count) 条" -ForegroundColor Green
} catch {
    Write-Host "❌ 失败: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

# 2. 添加评论
Write-Host "2️⃣  添加评论..." -ForegroundColor Yellow
$newReview = @{
    rating = 4.5
    title = "测试评论 - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    content = "这是一个自动化测试评论，用于验证 API 功能。包含足够的字符以满足最小长度要求。"
    photoUrls = @("https://example.com/test.jpg")
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/v1/coworking/$CoworkingId/reviews" -Body $newReview -ContentType "application/json"
    $reviewId = $response.data.id
    Write-Host "✅ 成功 | ID: $reviewId | 评分: $($response.data.rating)" -ForegroundColor Green
    
    # 3. 更新评论
    Write-Host "3️⃣  更新评论..." -ForegroundColor Yellow
    $updateReview = @{
        rating = 5.0
        title = "更新后的测试评论"
        content = "这是更新后的评论内容，评分从 4.5 提升到 5.0。测试更新功能是否正常工作。"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Method PUT -Uri "$baseUrl/api/v1/coworking/reviews/$reviewId" -Body $updateReview -ContentType "application/json"
    Write-Host "✅ 成功 | 新评分: $($response.data.rating)" -ForegroundColor Green
    
    # 4. 获取评论详情
    Write-Host "4️⃣  获取评论详情..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Method GET -Uri "$baseUrl/api/v1/coworking/reviews/$reviewId"
    Write-Host "✅ 成功 | 标题: $($response.data.title)" -ForegroundColor Green
    
    # 5. 删除评论
    Write-Host "5️⃣  删除评论..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Method DELETE -Uri "$baseUrl/api/v1/coworking/reviews/$reviewId"
    Write-Host "✅ 成功 | 评论已删除" -ForegroundColor Green
    
} catch {
    Write-Host "❌ 失败: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   详情: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "✨ 测试完成！" -ForegroundColor Cyan

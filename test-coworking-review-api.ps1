# Coworking Review API 测试脚本
# 使用方法: .\test-coworking-review-api.ps1

$baseUrl = "http://localhost:8004"  # CoworkingService 端口
$coworkingId = "your-coworking-id-here"  # 替换为实际的 Coworking ID
$reviewId = ""  # 将在创建后自动填充

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Coworking Review API 测试" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# 测试 1: 获取评论列表（分页）
Write-Host "测试 1: 获取评论列表 (GET /api/v1/coworking/{id}/reviews)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Method GET -Uri "$baseUrl/api/v1/coworking/$coworkingId/reviews?page=1&pageSize=10" -ContentType "application/json"
    Write-Host "✅ 成功获取评论列表" -ForegroundColor Green
    Write-Host "   总数: $($response.data.totalCount)" -ForegroundColor Gray
    Write-Host "   当前页: $($response.data.currentPage)" -ForegroundColor Gray
    Write-Host "   评论数: $($response.data.items.Count)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "❌ 失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# 测试 2: 添加评论
Write-Host "测试 2: 添加评论 (POST /api/v1/coworking/{id}/reviews)" -ForegroundColor Yellow
$newReview = @{
    rating = 4.5
    title = "非常不错的共享办公空间"
    content = "环境很好，网络速度快，工作氛围很棒。咖啡免费，会议室设施齐全。唯一的小问题是停车位有点紧张，但总体来说非常推荐！"
    visitDate = "2025-01-15"
    photoUrls = @(
        "https://example.com/photo1.jpg",
        "https://example.com/photo2.jpg"
    )
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/v1/coworking/$coworkingId/reviews" -Body $newReview -ContentType "application/json"
    Write-Host "✅ 成功添加评论" -ForegroundColor Green
    $reviewId = $response.data.id
    Write-Host "   评论ID: $reviewId" -ForegroundColor Gray
    Write-Host "   评分: $($response.data.rating)" -ForegroundColor Gray
    Write-Host "   标题: $($response.data.title)" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "❌ 失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# 等待一下，确保数据已保存
Start-Sleep -Seconds 1

# 测试 3: 获取评论详情
if ($reviewId) {
    Write-Host "测试 3: 获取评论详情 (GET /api/v1/coworking/reviews/{id})" -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Method GET -Uri "$baseUrl/api/v1/coworking/reviews/$reviewId" -ContentType "application/json"
        Write-Host "✅ 成功获取评论详情" -ForegroundColor Green
        Write-Host "   标题: $($response.data.title)" -ForegroundColor Gray
        Write-Host "   评分: $($response.data.rating)" -ForegroundColor Gray
        Write-Host "   已验证: $($response.data.isVerified)" -ForegroundColor Gray
        Write-Host ""
    } catch {
        Write-Host "❌ 失败: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

# 测试 4: 获取当前用户的评论
Write-Host "测试 4: 获取当前用户的评论 (GET /api/v1/coworking/{id}/reviews/my-review)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Method GET -Uri "$baseUrl/api/v1/coworking/$coworkingId/reviews/my-review" -ContentType "application/json"
    Write-Host "✅ 成功获取用户评论" -ForegroundColor Green
    if ($response.data) {
        Write-Host "   评论ID: $($response.data.id)" -ForegroundColor Gray
        Write-Host "   标题: $($response.data.title)" -ForegroundColor Gray
    } else {
        Write-Host "   该用户暂无评论" -ForegroundColor Gray
    }
    Write-Host ""
} catch {
    Write-Host "❌ 失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# 测试 5: 更新评论
if ($reviewId) {
    Write-Host "测试 5: 更新评论 (PUT /api/v1/coworking/reviews/{id})" -ForegroundColor Yellow
    $updateReview = @{
        rating = 5.0
        title = "非常棒的共享办公空间（更新）"
        content = "更新评论：经过一段时间的使用，我觉得这里真的很棒！环境优美，设施完善，工作效率大大提升。强烈推荐给所有数字游民！"
        visitDate = "2025-01-15"
        photoUrls = @(
            "https://example.com/photo1.jpg",
            "https://example.com/photo2.jpg",
            "https://example.com/photo3.jpg"
        )
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Method PUT -Uri "$baseUrl/api/v1/coworking/reviews/$reviewId" -Body $updateReview -ContentType "application/json"
        Write-Host "✅ 成功更新评论" -ForegroundColor Green
        Write-Host "   新评分: $($response.data.rating)" -ForegroundColor Gray
        Write-Host "   新标题: $($response.data.title)" -ForegroundColor Gray
        Write-Host ""
    } catch {
        Write-Host "❌ 失败: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

# 测试 6: 删除评论
if ($reviewId) {
    Write-Host "测试 6: 删除评论 (DELETE /api/v1/coworking/reviews/{id})" -ForegroundColor Yellow
    Write-Host "   ⚠️  这将删除刚创建的测试评论" -ForegroundColor Yellow
    $confirm = Read-Host "   是否继续? (y/n)"
    
    if ($confirm -eq "y") {
        try {
            $response = Invoke-RestMethod -Method DELETE -Uri "$baseUrl/api/v1/coworking/reviews/$reviewId" -ContentType "application/json"
            Write-Host "✅ 成功删除评论" -ForegroundColor Green
            Write-Host ""
        } catch {
            Write-Host "❌ 失败: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host ""
        }
    } else {
        Write-Host "   已跳过删除测试" -ForegroundColor Gray
        Write-Host ""
    }
}

# 测试 7: 验证评分验证（应该失败）
Write-Host "测试 7: 验证评分验证 - 无效评分 (应该返回 400)" -ForegroundColor Yellow
$invalidReview = @{
    rating = 6.0  # 超出范围
    title = "测试标题"
    content = "这是一个测试内容，用于验证评分验证功能是否正常工作。"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/v1/coworking/$coworkingId/reviews" -Body $invalidReview -ContentType "application/json"
    Write-Host "❌ 验证失败：应该拒绝无效评分" -ForegroundColor Red
    Write-Host ""
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✅ 验证成功：正确拒绝了无效评分" -ForegroundColor Green
    } else {
        Write-Host "❌ 意外错误: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# 测试 8: 验证标题长度验证（应该失败）
Write-Host "测试 8: 验证标题长度 - 标题过短 (应该返回 400)" -ForegroundColor Yellow
$shortTitleReview = @{
    rating = 4.0
    title = "短"  # 少于5个字符
    content = "这是一个测试内容，用于验证标题长度验证功能是否正常工作。"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/v1/coworking/$coworkingId/reviews" -Body $shortTitleReview -ContentType "application/json"
    Write-Host "❌ 验证失败：应该拒绝过短的标题" -ForegroundColor Red
    Write-Host ""
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✅ 验证成功：正确拒绝了过短的标题" -ForegroundColor Green
    } else {
        Write-Host "❌ 意外错误: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  测试完成！" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "提示: 如果需要测试需要认证的端点，请先配置 JWT Token" -ForegroundColor Yellow
Write-Host "可以在请求中添加: -Headers @{Authorization='Bearer YOUR_TOKEN'}" -ForegroundColor Yellow

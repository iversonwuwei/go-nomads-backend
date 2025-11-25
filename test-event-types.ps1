# 测试聚会类型 API
# 端口：EventService - 8005
# 网关端口：Gateway - 8001

$baseUrl = "http://localhost:8005"
$gatewayUrl = "http://localhost:8001"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "测试聚会类型 API" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 1. 获取所有启用的聚会类型
Write-Host "1️⃣ 获取所有启用的聚会类型" -ForegroundColor Yellow
Write-Host "GET $baseUrl/api/v1/event-types" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/v1/event-types" -Method Get -ContentType "application/json"
    
    Write-Host "✅ 成功获取聚会类型列表" -ForegroundColor Green
    Write-Host "总数: $($response.data.Count)" -ForegroundColor Green
    Write-Host ""
    
    # 显示前 5 个类型
    Write-Host "前 5 个聚会类型:" -ForegroundColor Cyan
    $response.data | Select-Object -First 5 | ForEach-Object {
        Write-Host "  ID: $($_.id)" -ForegroundColor White
        Write-Host "  中文名: $($_.name)" -ForegroundColor White
        Write-Host "  英文名: $($_.enName)" -ForegroundColor White
        Write-Host "  描述: $($_.description)" -ForegroundColor Gray
        Write-Host "  排序: $($_.sortOrder)" -ForegroundColor Gray
        Write-Host "  系统预设: $($_.isSystem)" -ForegroundColor Gray
        Write-Host "  ---" -ForegroundColor DarkGray
    }
    Write-Host ""
    
    # 保存所有类型到文件
    $response.data | ConvertTo-Json -Depth 10 | Out-File "event-types-list.json" -Encoding UTF8
    Write-Host "📄 完整列表已保存到: event-types-list.json" -ForegroundColor Cyan
    Write-Host ""
} catch {
    Write-Host "❌ 请求失败" -ForegroundColor Red
    Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# 2. 通过网关获取聚会类型（测试网关路由）
Write-Host "2️⃣ 通过网关获取聚会类型" -ForegroundColor Yellow
Write-Host "GET $gatewayUrl/api/events/types" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "$gatewayUrl/api/events/types" -Method Get -ContentType "application/json"
    
    Write-Host "✅ 通过网关成功获取" -ForegroundColor Green
    Write-Host "总数: $($response.data.Count)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "⚠️ 网关路由可能未配置" -ForegroundColor Yellow
    Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "请在 Gateway 中添加路由配置" -ForegroundColor Yellow
    Write-Host ""
}

# 3. 获取特定聚会类型（使用第一个类型的 ID）
Write-Host "3️⃣ 获取特定聚会类型详情" -ForegroundColor Yellow

try {
    $allTypes = Invoke-RestMethod -Uri "$baseUrl/api/v1/event-types" -Method Get -ContentType "application/json"
    $firstTypeId = $allTypes.data[0].id
    
    Write-Host "GET $baseUrl/api/v1/event-types/$firstTypeId" -ForegroundColor Gray
    Write-Host ""
    
    $typeDetail = Invoke-RestMethod -Uri "$baseUrl/api/v1/event-types/$firstTypeId" -Method Get -ContentType "application/json"
    
    Write-Host "✅ 成功获取类型详情" -ForegroundColor Green
    Write-Host "ID: $($typeDetail.data.id)" -ForegroundColor White
    Write-Host "中文名: $($typeDetail.data.name)" -ForegroundColor White
    Write-Host "英文名: $($typeDetail.data.enName)" -ForegroundColor White
    Write-Host "描述: $($typeDetail.data.description)" -ForegroundColor White
    Write-Host ""
} catch {
    Write-Host "❌ 获取详情失败" -ForegroundColor Red
    Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "测试完成！" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 API 端点:" -ForegroundColor Green
Write-Host "  GET    /api/v1/event-types           - 获取所有启用的类型" -ForegroundColor Gray
Write-Host "  GET    /api/v1/event-types/all       - 获取所有类型（包括禁用）" -ForegroundColor Gray
Write-Host "  GET    /api/v1/event-types/{id}      - 获取特定类型" -ForegroundColor Gray
Write-Host "  POST   /api/v1/event-types           - 创建新类型（需认证）" -ForegroundColor Gray
Write-Host "  PUT    /api/v1/event-types/{id}      - 更新类型（需认证）" -ForegroundColor Gray
Write-Host "  DELETE /api/v1/event-types/{id}      - 删除类型（需认证）" -ForegroundColor Gray
Write-Host ""

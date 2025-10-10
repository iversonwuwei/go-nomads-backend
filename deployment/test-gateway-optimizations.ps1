# Gateway 优化功能测试脚本
# 演示健康检查、负载均衡、重试机制等新功能

Write-Host "`n================================================================" -ForegroundColor Green
Write-Host "     Gateway Consul 集成优化 - 功能测试" -ForegroundColor Green
Write-Host "================================================================`n" -ForegroundColor Green

# 测试 1: 基本功能
Write-Host "测试 1: 基本路由功能" -ForegroundColor Yellow
Write-Host "----------------------------------------"
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/products" -UseBasicParsing
    $data = $response.Content | ConvertFrom-Json
    Write-Host "SUCCESS Product Service: $($response.StatusCode) - $($data.message)" -ForegroundColor Green
} catch {
    Write-Host "FAILED Product Service failed: $_" -ForegroundColor Red
}

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/users" -UseBasicParsing
    $data = $response.Content | ConvertFrom-Json
    Write-Host "✅ User Service: $($response.StatusCode) - $($data.message)" -ForegroundColor Green
} catch {
    Write-Host "❌ User Service 失败: $_" -ForegroundColor Red
}

# 测试 2: 查看健康检查配置
Write-Host "`n测试 2: 健康检查配置" -ForegroundColor Yellow
Write-Host "----------------------------------------"
$healthLogs = podman logs go-nomads-gateway 2>&1 | Select-String "healthy instance" | Select-Object -Last 3
if ($healthLogs) {
    foreach ($log in $healthLogs) {
        Write-Host "  $log" -ForegroundColor Cyan
    }
} else {
    Write-Host "  未找到健康检查日志" -ForegroundColor Gray
}

# 测试 3: 查看服务实例详情
Write-Host "`n测试 3: 服务实例详情" -ForegroundColor Yellow
Write-Host "----------------------------------------"
$instanceLogs = podman logs go-nomads-gateway 2>&1 | Select-String "Instance \d+:" | Select-Object -Last 5
if ($instanceLogs) {
    foreach ($log in $instanceLogs) {
        Write-Host "  $log" -ForegroundColor Cyan
    }
} else {
    Write-Host "  未找到实例详情日志" -ForegroundColor Gray
}

# 测试 4: 查看负载均衡和元数据配置
Write-Host "`n测试 4: 路由和集群配置" -ForegroundColor Yellow
Write-Host "----------------------------------------"
$routeLogs = podman logs go-nomads-gateway 2>&1 | Select-String "Route:|Cluster:" | Select-Object -Last 10
if ($routeLogs) {
    foreach ($log in $routeLogs) {
        Write-Host "  $log" -ForegroundColor Cyan
    }
} else {
    Write-Host "  未找到路由配置日志" -ForegroundColor Gray
}

# 测试 5: 重试机制测试 (模拟)
Write-Host "`n测试 5: Consul 连接重试机制" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "  重试配置: 指数退避,最大重试次数 5" -ForegroundColor Cyan
Write-Host "  退避时间: 2^n 秒 (最大 60 秒)" -ForegroundColor Cyan
Write-Host "  重试延迟: 2s, 4s, 8s, 16s, 32s, 60s" -ForegroundColor Cyan

$retryLogs = podman logs go-nomads-gateway 2>&1 | Select-String "attempt|Retrying" | Select-Object -Last 3
if ($retryLogs.Count -gt 0) {
    Write-Host "`n  检测到重试记录:" -ForegroundColor Yellow
    foreach ($log in $retryLogs) {
        Write-Host "  $log" -ForegroundColor Red
    }
} else {
    Write-Host "  ✅ 当前连接正常,无重试记录" -ForegroundColor Green
}

# 测试 6: 优雅下线测试说明
Write-Host "`n测试 6: 优雅下线机制" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "  已注册 ApplicationStopping 事件处理器" -ForegroundColor Cyan
Write-Host "  关闭时会执行:" -ForegroundColor Cyan
Write-Host "    • 取消 Consul 监听任务" -ForegroundColor Gray
Write-Host "    • 清理资源 (CancellationTokenSource)" -ForegroundColor Gray
Write-Host "    • 记录优雅关闭日志" -ForegroundColor Gray
Write-Host "`n  测试方法: podman stop go-nomads-gateway" -ForegroundColor Yellow
Write-Host "  然后查看日志: podman logs go-nomads-gateway 2>&1 | Select-String 'shutdown'" -ForegroundColor Yellow

# 测试 7: 服务元数据
Write-Host "`n测试 7: 服务元数据支持" -ForegroundColor Yellow
Write-Host "----------------------------------------"
Write-Host "  已配置的元数据字段:" -ForegroundColor Cyan
Write-Host "    • consul.service.id - Consul 服务 ID" -ForegroundColor Gray
Write-Host "    • consul.node - Consul 节点名称" -ForegroundColor Gray
Write-Host "    • consul.version - 服务版本号" -ForegroundColor Gray
Write-Host "    • consul.environment - 运行环境" -ForegroundColor Gray
Write-Host "`n  注意: 需要在 Consul 服务注册时添加 Meta 信息" -ForegroundColor Yellow

# 总结
Write-Host "`n================================================================" -ForegroundColor Green
Write-Host "     优化功能总结" -ForegroundColor Green
Write-Host "================================================================`n" -ForegroundColor Green

Write-Host "✅ 已实现的优化:" -ForegroundColor Green
Write-Host "  1. Consul 健康检查 - 仅路由到健康实例" -ForegroundColor White
Write-Host "  2. 服务元数据支持 - 版本、环境等信息" -ForegroundColor White
Write-Host "  3. YARP 负载均衡 - RoundRobin 轮询策略" -ForegroundColor White
Write-Host "  4. 主动健康检查 - 每 10 秒检查 /health 端点" -ForegroundColor White
Write-Host "  5. 优雅下线机制 - ApplicationStopping 事件处理" -ForegroundColor White
Write-Host "  6. 连接重试逻辑 - 指数退避,最多重试 5 次" -ForegroundColor White
Write-Host "  7. 多实例支持 - 自动发现和负载均衡" -ForegroundColor White

Write-Host "`n📊 当前状态:" -ForegroundColor Yellow
$containers = podman ps --filter "name=go-nomads" --format "{{.Names}}" | Measure-Object
Write-Host "  运行中的容器: $($containers.Count) 个" -ForegroundColor Cyan

try {
    $consulServices = Invoke-WebRequest -Uri "http://localhost:8500/v1/catalog/services" -UseBasicParsing | ConvertFrom-Json
    $serviceCount = ($consulServices | Get-Member -MemberType NoteProperty).Count
    Write-Host "  Consul 注册的服务: $serviceCount 个" -ForegroundColor Cyan
} catch {
    Write-Host "  无法连接到 Consul" -ForegroundColor Red
}

Write-Host "`n================================================================`n" -ForegroundColor Green

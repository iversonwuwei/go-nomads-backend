# Scalar 文档系统验证脚本

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   Scalar 文档系统验证" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$results = @()

# 测试函数
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ExpectedText = ""
    )
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $status = "✅ 通过"
            $color = "Green"
            
            if ($ExpectedText -and $response.Content -notmatch $ExpectedText) {
                $status = "⚠️  警告 (状态码正确但内容不匹配)"
                $color = "Yellow"
            }
        } else {
            $status = "❌ 失败 (状态码: $($response.StatusCode))"
            $color = "Red"
        }
    } catch {
        $status = "❌ 失败 ($($_.Exception.Message))"
        $color = "Red"
    }
    
    Write-Host "$Name`: " -NoNewline
    Write-Host $status -ForegroundColor $color
    
    $script:results += [PSCustomObject]@{
        Service = $Name
        Status = $status
        Url = $Url
    }
}

Write-Host "📚 测试 Scalar UI 界面..." -ForegroundColor Yellow
Write-Host "----------------------------------------`n" -ForegroundColor Gray

Test-Endpoint "DocumentService Scalar UI" "http://localhost:5003/scalar/v1" "Go-Nomads API Documentation"
Test-Endpoint "Gateway Scalar UI" "http://localhost:5000/scalar/v1" "Go-Nomads Gateway API"
Test-Endpoint "ProductService Scalar UI" "http://localhost:5001/scalar/v1" "Product Service API"
Test-Endpoint "UserService Scalar UI" "http://localhost:5002/scalar/v1" "User Service API"

Write-Host "`n🔧 测试 API 端点..." -ForegroundColor Yellow
Write-Host "----------------------------------------`n" -ForegroundColor Gray

Test-Endpoint "DocumentService 健康检查" "http://localhost:5003/health" "healthy"
Test-Endpoint "DocumentService 服务列表" "http://localhost:5003/api/services" "Gateway"
Test-Endpoint "Gateway 健康检查" "http://localhost:5000/health" "healthy"
Test-Endpoint "ProductService 健康检查" "http://localhost:5001/health" "healthy"
Test-Endpoint "UserService 健康检查" "http://localhost:5002/health" "healthy"

Write-Host "`n📊 测试 OpenAPI 规范..." -ForegroundColor Yellow
Write-Host "----------------------------------------`n" -ForegroundColor Gray

Test-Endpoint "Gateway OpenAPI" "http://localhost:5000/openapi/v1.json" "openapi"
Test-Endpoint "ProductService OpenAPI" "http://localhost:5001/openapi/v1.json" "openapi"
Test-Endpoint "UserService OpenAPI" "http://localhost:5002/openapi/v1.json" "openapi"
Test-Endpoint "DocumentService OpenAPI" "http://localhost:5003/openapi/v1.json" "openapi"

Write-Host "`n🔍 检查 Consul 服务注册..." -ForegroundColor Yellow
Write-Host "----------------------------------------`n" -ForegroundColor Gray

try {
    $consulServices = Invoke-RestMethod -Uri "http://localhost:8500/v1/catalog/services" -Method Get
    
    $expectedServices = @("consul", "gateway", "product-service", "user-service", "document-service")
    $registeredServices = $consulServices.PSObject.Properties.Name
    
    foreach ($service in $expectedServices) {
        if ($registeredServices -contains $service) {
            Write-Host "$service`: " -NoNewline
            Write-Host "✅ 已注册" -ForegroundColor Green
        } else {
            Write-Host "$service`: " -NoNewline
            Write-Host "❌ 未注册" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ 无法连接到 Consul" -ForegroundColor Red
}

Write-Host "`n🐳 检查容器状态..." -ForegroundColor Yellow
Write-Host "----------------------------------------`n" -ForegroundColor Gray

try {
    $containers = podman ps --format "{{.Names}}" 2>$null
    
    $expectedContainers = @(
        "go-nomads-consul",
        "go-nomads-gateway",
        "go-nomads-product-service",
        "go-nomads-user-service",
        "go-nomads-document-service"
    )
    
    foreach ($container in $expectedContainers) {
        if ($containers -contains $container) {
            Write-Host "$container`: " -NoNewline
            Write-Host "✅ 运行中" -ForegroundColor Green
        } else {
            Write-Host "$container`: " -NoNewline
            Write-Host "❌ 未运行" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ 无法检查容器状态" -ForegroundColor Red
}

# 生成总结
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "   验证总结" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$passedTests = ($results | Where-Object { $_.Status -like "*通过*" }).Count
$totalTests = $results.Count

Write-Host "总测试数: $totalTests" -ForegroundColor White
Write-Host "通过: $passedTests" -ForegroundColor Green
Write-Host "失败: $($totalTests - $passedTests)" -ForegroundColor Red

if ($passedTests -eq $totalTests) {
    Write-Host "`n🎉 所有测试通过!文档系统运行正常!" -ForegroundColor Green
    Write-Host "`n🚀 快速访问:" -ForegroundColor Cyan
    Write-Host "   主文档: http://localhost:5003/scalar/v1" -ForegroundColor White
} else {
    Write-Host "`n⚠️  部分测试失败,请检查日志" -ForegroundColor Yellow
}

Write-Host "`n========================================`n" -ForegroundColor Cyan

# Go-Nomads Backend API 测试脚本
# 提供快速的API测试功能

param(
    [Parameter(Position=0)]
    [ValidateSet("users", "products", "health", "all", "help")]
    [string]$Test = "help",
    
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$Verbose = $false
)

# 设置颜色输出
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" "Green"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ $Message" "Cyan"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠ $Message" "Yellow"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" "Red"
}

function Test-ApiEndpoint {
    param(
        [string]$Method,
        [string]$Url,
        [string]$Body = $null,
        [string]$Description
    )
    
    Write-Info "测试: $Description"
    Write-Info "请求: $Method $Url"
    
    try {
        $headers = @{
            "Content-Type" = "application/json"
            "Accept" = "application/json"
        }
        
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $headers
            TimeoutSec = 30
        }
        
        if ($Body) {
            $params.Body = $Body
            if ($Verbose) {
                Write-Info "请求体: $Body"
            }
        }
        
        $response = Invoke-RestMethod @params
        
        Write-Success "请求成功"
        if ($Verbose -or $response -is [string]) {
            Write-Host "响应:" -ForegroundColor Gray
            if ($response -is [string]) {
                Write-Host $response -ForegroundColor White
            } else {
                Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor White
            }
        } else {
            Write-Host "响应数据类型: $($response.GetType().Name)" -ForegroundColor Gray
            if ($response.data) {
                Write-Host "数据项数量: $($response.data.Count)" -ForegroundColor Gray
            }
        }
        
        return $true
    } catch {
        Write-Error "请求失败: $($_.Exception.Message)"
        if ($Verbose) {
            Write-Host "详细错误:" -ForegroundColor Red
            Write-Host $_.Exception.ToString() -ForegroundColor Red
        }
        return $false
    }
    
    Write-Host ""
}

function Test-HealthEndpoints {
    Write-Info "健康检查测试"
    Write-Info "========================================"
    
    $endpoints = @(
        @{ Url = "$BaseUrl/health"; Description = "Gateway健康检查" },
        @{ Url = "http://localhost:5001/health"; Description = "UserService健康检查" },
        @{ Url = "http://localhost:5002/health"; Description = "ProductService健康检查" }
    )
    
    $passed = 0
    foreach ($endpoint in $endpoints) {
        if (Test-ApiEndpoint -Method "GET" -Url $endpoint.Url -Description $endpoint.Description) {
            $passed++
        }
    }
    
    Write-Info "健康检查结果: $passed/$($endpoints.Count) 通过"
    Write-Host ""
}

function Test-UserEndpoints {
    Write-Info "用户API测试"
    Write-Info "========================================"
    
    $passed = 0
    $total = 0
    
    # 获取用户列表
    $total++
    if (Test-ApiEndpoint -Method "GET" -Url "$BaseUrl/api/users" -Description "获取用户列表") {
        $passed++
    }
    
    # 创建用户
    $total++
    $newUser = @{
        name = "测试用户$(Get-Random -Minimum 1000 -Maximum 9999)"
        email = "test$(Get-Random -Minimum 1000 -Maximum 9999)@example.com"
        phone = "138$(Get-Random -Minimum 10000000 -Maximum 99999999)"
    } | ConvertTo-Json
    
    if (Test-ApiEndpoint -Method "POST" -Url "$BaseUrl/api/users" -Body $newUser -Description "创建用户") {
        $passed++
    }
    
    # 获取特定用户
    $total++
    if (Test-ApiEndpoint -Method "GET" -Url "$BaseUrl/api/users/1" -Description "获取用户详情") {
        $passed++
    }
    
    Write-Info "用户API测试结果: $passed/$total 通过"
    Write-Host ""
}

function Test-ProductEndpoints {
    Write-Info "产品API测试"
    Write-Info "========================================"
    
    $passed = 0
    $total = 0
    
    # 获取产品列表
    $total++
    if (Test-ApiEndpoint -Method "GET" -Url "$BaseUrl/api/products" -Description "获取产品列表") {
        $passed++
    }
    
    # 创建产品
    $total++
    $newProduct = @{
        name = "测试产品$(Get-Random -Minimum 1000 -Maximum 9999)"
        description = "这是一个测试产品"
        price = [double](Get-Random -Minimum 10 -Maximum 1000)
        userId = "1"
        category = "测试分类"
    } | ConvertTo-Json
    
    if (Test-ApiEndpoint -Method "POST" -Url "$BaseUrl/api/products" -Body $newProduct -Description "创建产品") {
        $passed++
    }
    
    # 获取特定产品
    $total++
    if (Test-ApiEndpoint -Method "GET" -Url "$BaseUrl/api/products/1" -Description "获取产品详情") {
        $passed++
    }
    
    # 获取用户的产品
    $total++
    if (Test-ApiEndpoint -Method "GET" -Url "$BaseUrl/api/products/user/1" -Description "获取用户的产品") {
        $passed++
    }
    
    Write-Info "产品API测试结果: $passed/$total 通过"
    Write-Host ""
}

function Test-AllEndpoints {
    Write-Info "完整API测试套件"
    Write-Info "========================================"
    Write-Info "基础URL: $BaseUrl"
    Write-Info ""
    
    Test-HealthEndpoints
    Test-UserEndpoints
    Test-ProductEndpoints
    
    Write-Success "所有API测试完成"
}

function Show-Help {
    Write-Info "Go-Nomads Backend API 测试工具"
    Write-Info "========================================"
    Write-Info "用法: .\test-api.ps1 <test> [-BaseUrl <url>] [-Verbose]"
    Write-Info ""
    Write-Info "可用测试:"
    Write-Info "  health     - 测试健康检查端点"
    Write-Info "  users      - 测试用户API"
    Write-Info "  products   - 测试产品API"
    Write-Info "  all        - 运行所有测试"
    Write-Info "  help       - 显示此帮助信息"
    Write-Info ""
    Write-Info "参数:"
    Write-Info "  -BaseUrl   - API基础URL (默认: http://localhost:5000)"
    Write-Info "  -Verbose   - 显示详细输出"
    Write-Info ""
    Write-Info "示例:"
    Write-Info "  .\test-api.ps1 all"
    Write-Info "  .\test-api.ps1 users -Verbose"
    Write-Info "  .\test-api.ps1 health -BaseUrl http://localhost:5000"
    Write-Info ""
    Write-Info "注意事项:"
    Write-Info "  - 确保所有服务已启动"
    Write-Info "  - 某些测试可能需要数据库中有数据"
    Write-Info "  - POST请求会创建测试数据"
}

function Test-ServiceAvailability {
    Write-Info "检查服务可用性..."
    
    $services = @(
        @{ Name = "Gateway"; Url = "$BaseUrl/health" },
        @{ Name = "UserService"; Url = "http://localhost:5001/health" },
        @{ Name = "ProductService"; Url = "http://localhost:5002/health" }
    )
    
    $available = $true
    foreach ($service in $services) {
        try {
            Invoke-RestMethod -Uri $service.Url -Method GET -TimeoutSec 5 | Out-Null
            Write-Success "$($service.Name) 可用"
        } catch {
            Write-Error "$($service.Name) 不可用"
            $available = $false
        }
    }
    
    if (-not $available) {
        Write-Error "某些服务不可用，请确保所有服务已启动"
        Write-Info "运行 .\scripts\start-all.ps1 启动服务"
        return $false
    }
    
    return $true
}

# 主逻辑
Write-Info "Go-Nomads Backend API 测试工具"
Write-Info "基础URL: $BaseUrl"
Write-Info ""

# 检查服务可用性（除了help命令）
if ($Test -ne "help") {
    if (-not (Test-ServiceAvailability)) {
        exit 1
    }
    Write-Info ""
}

switch ($Test.ToLower()) {
    "health" {
        Test-HealthEndpoints
    }
    "users" {
        Test-UserEndpoints
    }
    "products" {
        Test-ProductEndpoints
    }
    "all" {
        Test-AllEndpoints
    }
    "help" {
        Show-Help
    }
    default {
        Write-Error "未知测试: $Test"
        Show-Help
    }
}

Write-Info "测试完成"
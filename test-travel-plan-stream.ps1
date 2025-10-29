# 测试 AI 旅行计划流式生成 API
# 使用 PowerShell 测试 Server-Sent Events

$baseUrl = "http://localhost:8009"
$endpoint = "$baseUrl/api/ai/travel-plan/stream"

# 测试参数
$requestBody = @{
    cityId = "1"
    cityName = "北京"
    cityImage = "https://images.unsplash.com/photo-1508804185872-d7badad00f7d"
    duration = 3
    budget = "medium"
    travelStyle = "culture"
    interests = @("历史文化", "美食", "博物馆")
    departureLocation = "上海"
} | ConvertTo-Json -Depth 10

Write-Host "🧪 测试 AI 旅行计划流式生成" -ForegroundColor Cyan
Write-Host "📡 端点: $endpoint" -ForegroundColor Gray
Write-Host "📦 请求体:" -ForegroundColor Gray
Write-Host $requestBody -ForegroundColor DarkGray
Write-Host ""

try {
    Write-Host "⏳ 正在连接到流式 API..." -ForegroundColor Yellow
    
    # 创建 HttpWebRequest 来处理 SSE
    $request = [System.Net.HttpWebRequest]::Create($endpoint)
    $request.Method = "POST"
    $request.ContentType = "application/json"
    $request.Accept = "text/event-stream"
    $request.Headers.Add("Cache-Control", "no-cache")
    $request.Timeout = 300000 # 5 分钟
    
    # 写入请求体
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($requestBody)
    $request.ContentLength = $bytes.Length
    $requestStream = $request.GetRequestStream()
    $requestStream.Write($bytes, 0, $bytes.Length)
    $requestStream.Close()
    
    # 获取响应
    $response = $request.GetResponse()
    $stream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    
    Write-Host "✅ 连接成功,开始接收流式数据..." -ForegroundColor Green
    Write-Host ""
    
    # 读取 SSE 流
    $eventCount = 0
    while (-not $reader.EndOfStream) {
        $line = $reader.ReadLine()
        
        if ($line.StartsWith("data: ")) {
            $eventCount++
            $jsonData = $line.Substring(6)
            
            try {
                $event = $jsonData | ConvertFrom-Json
                
                $timestamp = Get-Date -Format "HH:mm:ss.fff"
                $type = $event.type
                $payload = $event.payload
                
                switch ($type) {
                    "start" {
                        Write-Host "[$timestamp] 🚀 START: $($payload.message) (进度: $($payload.progress)%)" -ForegroundColor Cyan
                    }
                    "analyzing" {
                        Write-Host "[$timestamp] 🔍 ANALYZING: $($payload.message) (进度: $($payload.progress)%)" -ForegroundColor Yellow
                    }
                    "generating" {
                        Write-Host "[$timestamp] ⚙️  GENERATING: $($payload.message) (进度: $($payload.progress)%)" -ForegroundColor Magenta
                    }
                    "success" {
                        Write-Host "[$timestamp] ✅ SUCCESS: $($payload.message) (进度: $($payload.progress)%)" -ForegroundColor Green
                        
                        if ($payload.data) {
                            Write-Host ""
                            Write-Host "📊 旅行计划数据:" -ForegroundColor Green
                            Write-Host "   ID: $($payload.data.id)" -ForegroundColor Gray
                            Write-Host "   城市: $($payload.data.cityName)" -ForegroundColor Gray
                            Write-Host "   天数: $($payload.data.duration)" -ForegroundColor Gray
                            Write-Host "   每日行程数: $($payload.data.dailyItineraries.Count)" -ForegroundColor Gray
                            Write-Host "   景点数: $($payload.data.attractions.Count)" -ForegroundColor Gray
                            Write-Host "   餐厅数: $($payload.data.restaurants.Count)" -ForegroundColor Gray
                            Write-Host ""
                        }
                    }
                    "error" {
                        Write-Host "[$timestamp] ❌ ERROR: $($payload.message)" -ForegroundColor Red
                    }
                    default {
                        Write-Host "[$timestamp] ⚠️  UNKNOWN: $type" -ForegroundColor DarkYellow
                    }
                }
            }
            catch {
                Write-Host "⚠️  无法解析事件: $jsonData" -ForegroundColor DarkYellow
            }
        }
    }
    
    $reader.Close()
    $stream.Close()
    $response.Close()
    
    Write-Host ""
    Write-Host "✅ 流式数据接收完成!" -ForegroundColor Green
    Write-Host "📊 总共接收 $eventCount 个事件" -ForegroundColor Cyan
}
catch {
    Write-Host ""
    Write-Host "❌ 请求失败: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $errorReader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $errorReader.ReadToEnd()
        
        Write-Host "📄 错误详情:" -ForegroundColor Red
        Write-Host $errorBody -ForegroundColor DarkRed
        
        $errorReader.Close()
        $errorStream.Close()
    }
    
    Write-Host ""
    Write-Host "💡 提示:" -ForegroundColor Yellow
    Write-Host "   1. 确认 AIService 正在运行 (端口 8009)" -ForegroundColor Gray
    Write-Host "   2. 检查流式端点是否已实现" -ForegroundColor Gray
    Write-Host "   3. 查看后端日志获取更多信息" -ForegroundColor Gray
}

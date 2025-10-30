# 测试 AI 旅行计划流式文本输出 API
# 像打字机一样逐字显示内容

$baseUrl = "http://localhost:8009"
$endpoint = "$baseUrl/api/v1/ai/travel-plan/stream-text"

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

Write-Host "测试 AI 旅行计划流式文本输出" -ForegroundColor Cyan
Write-Host "端点: $endpoint" -ForegroundColor Gray
Write-Host ""
Write-Host "像流水一样逐步输出内容,模拟 ChatGPT 效果..." -ForegroundColor Yellow
Write-Host ""
Write-Host ("=" * 80) -ForegroundColor DarkGray
Write-Host ""

try {
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
    
    # 读取 SSE 流
    $eventCount = 0
    $completeData = $null
    
    while (-not $reader.EndOfStream) {
        $line = $reader.ReadLine()
        
        # SSE 格式: data: {...}
        if ($line.StartsWith("data: ")) {
            $eventCount++
            $jsonData = $line.Substring(6) # 去掉 "data: " 前缀
            
            try {
                $eventData = $jsonData | ConvertFrom-Json
                
                # 处理不同类型的事件
                switch ($eventData.type) {
                    "text" {
                        # 逐字输出,不换行
                        Write-Host $eventData.content -NoNewline -ForegroundColor White
                    }
                    "complete" {
                        # 保存完整数据
                        $completeData = $eventData.data
                        Write-Host ""
                        Write-Host ""
                        Write-Host ("=" * 80) -ForegroundColor DarkGray
                        Write-Host ""
                        Write-Host "流式输出完成!" -ForegroundColor Green
                        Write-Host "总事件数: $eventCount" -ForegroundColor Gray
                    }
                    default {
                        Write-Host "未知事件类型: $($eventData.type)" -ForegroundColor Yellow
                    }
                }
            }
            catch {
                Write-Host "解析 JSON 失败: $jsonData" -ForegroundColor Red
            }
        }
        elseif ($line -eq "") {
            # 空行分隔事件,SSE 标准格式
            continue
        }
        else {
            # 其他元数据行
            # Write-Host "元数据: $line" -ForegroundColor DarkGray
        }
    }
    
    # 关闭资源
    $reader.Close()
    $stream.Close()
    $response.Close()
    
    # 显示完整数据摘要
    if ($completeData) {
        Write-Host ""
        Write-Host "完整数据摘要:" -ForegroundColor Cyan
        Write-Host "   - 城市: $($completeData.cityName)" -ForegroundColor Gray
        Write-Host "   - 时长: $($completeData.duration) 天" -ForegroundColor Gray
        Write-Host "   - 预算: $($completeData.budget) 元" -ForegroundColor Gray
        Write-Host "   - 每日行程数: $($completeData.dailyItineraries.Count)" -ForegroundColor Gray
        Write-Host "   - 景点推荐数: $($completeData.attractions.Count)" -ForegroundColor Gray
        Write-Host "   - 餐厅推荐数: $($completeData.restaurants.Count)" -ForegroundColor Gray
    }
}
catch {
    Write-Host ""
    Write-Host "错误: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.InnerException) {
        Write-Host "内部错误: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
    
    exit 1
}

Write-Host ""
Write-Host "测试完成!" -ForegroundColor Green
Write-Host ""
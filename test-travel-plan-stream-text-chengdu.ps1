# 测试 AI 旅行计划流式文本输出 API（成都示例）
# 使用方法: .\test-travel-plan-stream-text-chengdu.ps1

$baseUrl = "http://localhost:8009"
$endpoint = "$baseUrl/api/v1/ai/travel-plan/stream-text"

# 指定测试请求体（使用用户提供的数据）
$requestBody = @{
    cityId = "chengdu-001"
    cityName = "成都"
    cityImage = "https://example.com/chengdu.jpg"
    duration = 7
    budget = "medium"
    travelStyle = "culture"
    interests = @("美食", "历史", "熊猫")
    departureLocation = "北京"
    customBudget = "5000-8000元"
} | ConvertTo-Json -Depth 10

Write-Host "测试 AI 旅行计划流式文本输出（成都负载）" -ForegroundColor Cyan
Write-Host "端点: $endpoint" -ForegroundColor Gray
Write-Host ""
Write-Host ("=" * 80) -ForegroundColor DarkGray
Write-Host ""

try {
    $request = [System.Net.HttpWebRequest]::Create($endpoint)
    $request.Method = "POST"
    $request.ContentType = "application/json"
    $request.Accept = "text/event-stream"
    $request.Headers.Add("Cache-Control", "no-cache")
    $request.Timeout = 300000 # 5 分钟

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($requestBody)
    $request.ContentLength = $bytes.Length
    $requestStream = $request.GetRequestStream()
    $requestStream.Write($bytes, 0, $bytes.Length)
    $requestStream.Close()

    $response = $request.GetResponse()
    $stream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)

    $eventCount = 0
    $completeData = $null

    while (-not $reader.EndOfStream) {
        $line = $reader.ReadLine()
        if ($null -eq $line) { continue }

        if ($line.StartsWith("data: ")) {
            $eventCount++
            $jsonData = $line.Substring(6)
            try {
                $eventData = $jsonData | ConvertFrom-Json
                switch ($eventData.type) {
                    "text" {
                        Write-Host $eventData.content -NoNewline -ForegroundColor White
                    }
                    "complete" {
                        $completeData = $eventData.data
                        Write-Host ""
                        Write-Host ""
                        Write-Host ("=" * 80) -ForegroundColor DarkGray
                        Write-Host ""
                        Write-Host " 流式输出完成!" -ForegroundColor Green
                        Write-Host " 总事件数: $eventCount" -ForegroundColor Gray
                    }
                    default {
                        Write-Host "`n[事件:$($eventData.type)] $($eventData | ConvertTo-Json -Depth 5)" -ForegroundColor Yellow
                    }
                }
            }
            catch {
                Write-Host "解析 JSON 失败: $jsonData" -ForegroundColor Red
            }
        }
        elseif ($line -eq "") { continue }
    }

    $reader.Close()
    $stream.Close()
    $response.Close()

    if ($completeData) {
        Write-Host ""
        Write-Host " 完整数据摘要:" -ForegroundColor Cyan
        try { Write-Host "   - 城市: $($completeData.cityName)" -ForegroundColor Gray } catch {}
        try { Write-Host "   - 时长: $($completeData.duration) 天" -ForegroundColor Gray } catch {}
        try { Write-Host "   - 预算: $($completeData.budget)" -ForegroundColor Gray } catch {}
        try { Write-Host "   - 每日行程数: $($completeData.dailyItineraries.Count)" -ForegroundColor Gray } catch {}
    }
}
catch {
    Write-Host "`n 错误: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.InnerException) { Write-Host "内部错误: $($_.Exception.InnerException.Message)" -ForegroundColor Red }
    exit 1
}

Write-Host "`n测试脚本结束" -ForegroundColor Cyan
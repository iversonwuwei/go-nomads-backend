# 测试SSE流式输出 - 使用.NET HttpClient
# PowerShell的Invoke-WebRequest不支持SSE,这个脚本用C#实现真正的流式读取

Add-Type -AssemblyName System.Net.Http

$handler = New-Object System.Net.Http.HttpClientHandler
$client = New-Object System.Net.Http.HttpClient($handler)
$client.Timeout = [TimeSpan]::FromMinutes(5)

$url = "http://localhost:8009/api/v1/ai/travel-plan/stream-text"

$jsonBody = @{
    cityId = "chengdu-001"
    cityName = "成都"
    cityImage = "https://example.com/chengdu.jpg"
    duration = 7
    budget = "medium"
    travelStyle = "culture"
    interests = @("food", "history", "culture")
} | ConvertTo-Json -Compress

$content = New-Object System.Net.Http.StringContent($jsonBody, [System.Text.Encoding]::UTF8, "application/json")

Write-Host "🌐 发送请求到: $url" -ForegroundColor Cyan
Write-Host "📦 请求体: $jsonBody" -ForegroundColor Gray
Write-Host ""

try {
    $response = $client.PostAsync($url, $content).Result
    
    Write-Host "✅ 收到响应: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "📋 Content-Type: $($response.Content.Headers.ContentType)" -ForegroundColor Gray
    Write-Host ""
    
    if ($response.StatusCode -ne 200) {
        $errorBody = $response.Content.ReadAsStringAsync().Result
        Write-Host "❌ 错误: $errorBody" -ForegroundColor Red
        exit 1
    }
    
    # 读取流式响应
    $stream = $response.Content.ReadAsStreamAsync().Result
    $reader = New-Object System.IO.StreamReader($stream)
    
    $buffer = ""
    $charBuffer = New-Object char[] 1024
    $lineCount = 0
    
    Write-Host "📡 开始接收SSE流..." -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor DarkGray
    
    while (-not $reader.EndOfStream) {
        $bytesRead = $reader.Read($charBuffer, 0, $charBuffer.Length)
        if ($bytesRead -gt 0) {
            $chunk = New-Object string($charBuffer, 0, $bytesRead)
            $buffer += $chunk
            
            # 处理SSE消息 (以 \n\n 分隔)
            while ($buffer -match '(.+?)\n\n') {
                $message = $matches[1]
                $buffer = $buffer.Substring($matches[0].Length)
                
                if ($message.StartsWith("data: ")) {
                    $lineCount++
                    $jsonData = $message.Substring(6)
                    
                    try {
                        $event = $jsonData | ConvertFrom-Json
                        $type = $event.type
                        
                        switch ($type) {
                            "init" {
                                Write-Host "🔗 [SSE] 连接已建立" -ForegroundColor Green
                            }
                            "text" {
                                $text = $event.payload.text
                                Write-Host $text -NoNewline -ForegroundColor White
                            }
                            "complete" {
                                Write-Host ""
                                Write-Host "✅ [SSE] 接收complete事件" -ForegroundColor Green
                                Write-Host ("=" * 80) -ForegroundColor DarkGray
                                Write-Host "📊 统计: 共接收 $lineCount 条消息" -ForegroundColor Cyan
                                break
                            }
                            "error" {
                                Write-Host ""
                                Write-Host "❌ [SSE] 错误: $($event.payload.message)" -ForegroundColor Red
                                break
                            }
                        }
                    }
                    catch {
                        Write-Host "⚠️ 解析失败: $jsonData" -ForegroundColor Yellow
                    }
                }
            }
        }
    }
    
    Write-Host ""
    Write-Host "📡 流结束" -ForegroundColor Cyan
}
catch {
    Write-Host "❌ 异常: $_" -ForegroundColor Red
    Write-Host $_.Exception.ToString() -ForegroundColor DarkRed
}
finally {
    if ($reader) { $reader.Close() }
    if ($stream) { $stream.Close() }
    if ($client) { $client.Dispose() }
}

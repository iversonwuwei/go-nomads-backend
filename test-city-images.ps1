# 测试城市图片批量生成 API
# 生成 1 张竖屏封面图 + 4 张横屏图片

$baseUrl = "http://localhost:8009"

Write-Host "=== 测试城市图片批量生成 API ===" -ForegroundColor Cyan
Write-Host "将生成：1张竖屏封面图(720x1280) + 4张横屏图片(1280x720)" -ForegroundColor Yellow
Write-Host ""

$body = @{
    cityId = "chengdu"
    cityName = "Chengdu"
    country = "China"
    style = "<photography>"
    bucket = "city-photos"
    negativePrompt = "blurry, low quality, distorted, watermark, text, logo, ugly, deformed, cartoon, anime"
} | ConvertTo-Json -Depth 10

Write-Host "请求体:" -ForegroundColor Yellow
Write-Host $body
Write-Host ""
Write-Host "开始生成（预计需要 2-3 分钟）..." -ForegroundColor Green

try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/ai/images/city" `
        -Method POST `
        -Headers @{
            "Content-Type" = "application/json"
        } `
        -Body $body `
        -TimeoutSec 300

    $stopwatch.Stop()
    
    Write-Host "`n✅ 生成完成! 耗时: $($stopwatch.Elapsed.TotalSeconds.ToString('F1')) 秒" -ForegroundColor Green
    Write-Host ""
    Write-Host "响应:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10

    # 输出图片 URL
    if ($response.data.portraitImage) {
        Write-Host "`n📷 竖屏封面图 (720x1280):" -ForegroundColor Magenta
        Write-Host $response.data.portraitImage.url
    }

    if ($response.data.landscapeImages -and $response.data.landscapeImages.Count -gt 0) {
        Write-Host "`n🖼️ 横屏图片 (1280x720):" -ForegroundColor Magenta
        for ($i = 0; $i -lt $response.data.landscapeImages.Count; $i++) {
            Write-Host "  [$($i + 1)] $($response.data.landscapeImages[$i].url)"
        }
    }
}
catch {
    Write-Host "`n❌ 错误:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host $reader.ReadToEnd()
    }
}

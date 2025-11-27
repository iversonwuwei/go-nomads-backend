# 测试图片生成 API
# 使用通义万象生成图片并上传到 Supabase Storage

$baseUrl = "http://localhost:5003"

# 首先获取一个有效的 JWT token（需要登录）
# 这里假设你已经有一个有效的 token，请替换为实际值
$token = "YOUR_JWT_TOKEN_HERE"

# 测试图片生成
Write-Host "=== 测试图片生成 API ===" -ForegroundColor Cyan

$body = @{
    prompt = "一座现代化的城市天际线，蓝天白云，高楼大厦，专业摄影风格"
    negativePrompt = "模糊，低质量，变形"
    style = "<photography>"
    size = "1024*1024"
    count = 1
    bucket = "city-photos"
    pathPrefix = "city-covers"
} | ConvertTo-Json

Write-Host "请求体:" -ForegroundColor Yellow
Write-Host $body

try {
    $response = Invoke-RestMethod `
        -Uri "$baseUrl/api/v1/ai/images/generate" `
        -Method POST `
        -Headers @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        } `
        -Body $body `
        -TimeoutSec 180

    Write-Host "`n响应:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10
}
catch {
    Write-Host "`n错误:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host $reader.ReadToEnd()
    }
}

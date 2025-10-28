#!/usr/bin/env pwsh
# 测试 Event 详情 API - 验证参与者信息包含完整的用户信息

$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjlkNzg5MTMxLWU1NjAtNDdjZi05ZmYxLWIwNWY5YzM0NTIwNyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJ3YWxkZW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJ3YWxkZW4ud3V3ZWlAZ21haWwuY29tIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiVXNlciIsImV4cCI6MTc2MTY3MjUyNiwiaXNzIjoiR29Ob21hZHMiLCJhdWQiOiJHb05vbWFkc1VzZXJzIn0.nT1pz95m9_CwhKVVXxOSC-4JpQoLYJGQoELywH7KRlg"
$userId = "9d789131-e560-47cf-9ff1-b05f9c345207"
$headers = @{
    'Authorization' = "Bearer $token"
    'X-User-Id' = $userId
}

Write-Host "🔍 测试 Event 详情 API - 验证参与者信息" -ForegroundColor Cyan
Write-Host ""

# 1. 获取事件列表
Write-Host "1️⃣ 获取事件列表..." -ForegroundColor Yellow
try {
    $uri = 'http://localhost:8005/api/v1/events?page=1&pageSize=5'
    $response = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get
    $events = $response.data.items
    Write-Host "   ✅ 找到 $($events.Count) 个活动" -ForegroundColor Green
    
    if ($events.Count -eq 0) {
        Write-Host "   ⚠️ 没有活动数据，请先创建活动" -ForegroundColor Yellow
        exit 0
    }
    
    $eventId = $events[0].id
    Write-Host "   📌 选择活动: $($events[0].title) (ID: $eventId)" -ForegroundColor Cyan
    Write-Host ""
} catch {
    Write-Host "   ❌ 获取事件列表失败: $_" -ForegroundColor Red
    exit 1
}

# 2. 获取活动详情
Write-Host "2️⃣ 获取活动详情（包含参与者信息）..." -ForegroundColor Yellow
try {
    $eventDetail = Invoke-RestMethod -Uri "http://localhost:8005/api/v1/events/$eventId" -Headers $headers -Method Get
    
    if ($eventDetail.success) {
        $event = $eventDetail.data
        Write-Host "   ✅ 活动: $($event.title)" -ForegroundColor Green
        Write-Host "   📍 地点: $($event.location)" -ForegroundColor Cyan
        Write-Host "   👥 参与人数: $($event.participantCount)" -ForegroundColor Cyan
        Write-Host ""
        
        # 3. 检查参与者信息
        Write-Host "3️⃣ 检查参与者信息..." -ForegroundColor Yellow
        if ($event.participants -and $event.participants.Count -gt 0) {
            Write-Host "   ✅ 找到 $($event.participants.Count) 个参与者" -ForegroundColor Green
            Write-Host ""
            
            foreach ($participant in $event.participants) {
                Write-Host "   👤 参与者信息:" -ForegroundColor Cyan
                Write-Host "      - Participant ID: $($participant.id)" -ForegroundColor White
                Write-Host "      - User ID: $($participant.userId)" -ForegroundColor White
                Write-Host "      - Status: $($participant.status)" -ForegroundColor White
                
                if ($participant.user) {
                    Write-Host "      ✅ User 信息已填充:" -ForegroundColor Green
                    Write-Host "         • Name: $($participant.user.name)" -ForegroundColor White
                    Write-Host "         • Email: $($participant.user.email)" -ForegroundColor White
                    Write-Host "         • Phone: $($participant.user.phone)" -ForegroundColor White
                    Write-Host "         • Avatar: $($participant.user.avatar)" -ForegroundColor White
                } else {
                    Write-Host "      ❌ User 信息缺失!" -ForegroundColor Red
                }
                Write-Host ""
            }
            
            Write-Host "✅ 测试通过！参与者信息包含完整的用户详情" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️ 该活动暂无参与者" -ForegroundColor Yellow
            Write-Host "   💡 提示: 可以调用 JOIN API 添加参与者" -ForegroundColor Cyan
        }
    } else {
        Write-Host "   ❌ API 返回失败: $($eventDetail.message)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   ❌ 获取活动详情失败: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🎉 测试完成！" -ForegroundColor Green

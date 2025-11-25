# Event 状态自动更新后台服务

## 📋 功能说明

自动扫描并更新过期活动的状态,将 `status=upcoming` 且已过期的活动更新为 `status=completed`。

## 🔧 实现细节

### 1. 后台服务类
**文件**: `BackgroundServices/EventStatusUpdateService.cs`

```csharp
public class EventStatusUpdateService : BackgroundService
{
    // 每 10 分钟执行一次扫描
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateExpiredEventsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
```

**工作流程**:
1. 启动后等待 10 秒(确保应用完全启动)
2. 调用 `GetExpiredEventsAsync()` 获取过期活动列表
3. 遍历每个活动,更新 `status = "completed"`
4. 记录成功/失败日志
5. 等待 10 分钟后重复

### 2. Repository 新增方法
**文件**: `Domain/Repositories/IEventRepository.cs`

```csharp
/// <summary>
///     获取已过期的活动（状态为 upcoming 且结束时间已过）
/// </summary>
Task<List<Event>> GetExpiredEventsAsync(DateTime currentTime);
```

**实现逻辑** (`Infrastructure/Repositories/EventRepository.cs`):
1. 查询所有 `status = "upcoming"` 的活动
2. 在内存中过滤 `EndTime < currentTime` 或 `StartTime < currentTime`(如果没有 EndTime)
3. 返回过期活动列表

### 3. 服务注册
**文件**: `Program.cs`

```csharp
// 注册后台服务
builder.Services.AddHostedService<EventStatusUpdateService>();
```

## ⏰ 执行时间配置

### 当前配置
- **扫描频率**: 每 10 分钟
- **启动延迟**: 10 秒

### 修改方法
在 `EventStatusUpdateService.cs` 中修改:

```csharp
// 修改扫描频率(例如改为 5 分钟)
await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

// 修改启动延迟(例如改为 30 秒)
await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
```

## 📊 判断逻辑

### 活动过期判断

```csharp
// 优先使用 EndTime，如果没有则使用 StartTime
var endTime = event.EndTime ?? event.StartTime;
var isExpired = endTime < DateTime.UtcNow;
```

**示例**:
- Event A: `startTime=2025-11-20 10:00`, `endTime=2025-11-20 12:00`, 当前时间 `2025-11-25`
  - ✅ **过期** (endTime < now)
  
- Event B: `startTime=2025-11-28 10:00`, `endTime=null`, 当前时间 `2025-11-25`
  - ❌ **未过期** (startTime > now)

- Event C: `startTime=2025-11-20 10:00`, `endTime=null`, 当前时间 `2025-11-25`
  - ✅ **过期** (startTime < now, 因为没有 endTime)

## 📝 日志输出

### 启动日志
```
🕒 EventStatusUpdateService 已启动
```

### 扫描日志
```
🔄 开始扫描并更新过期活动状态...
🔍 查询过期活动，当前时间: 2025-11-25T10:30:00Z
✅ 找到 3 个过期活动
```

### 更新日志
```
✅ 活动 66d093e1-de75-4ba0-80db-9cfc06e8a67e (北京数字游民周末聚会) 状态已更新为 completed
🎉 活动状态更新完成: 成功 3 个, 失败 0 个
```

### 停止日志
```
🛑 EventStatusUpdateService 已停止
```

## 🔍 监控建议

### Prometheus 指标(可选扩展)
可以添加以下指标监控:
- `event_status_update_total`: 总更新次数
- `event_status_update_success`: 成功更新次数
- `event_status_update_failed`: 失败更新次数
- `event_status_update_duration_seconds`: 扫描耗时

### 日志查询
```bash
# 查看后台服务日志
docker logs go-nomads-event-service | grep EventStatusUpdateService

# 查看更新成功的活动
docker logs go-nomads-event-service | grep "状态已更新为 completed"
```

## ⚠️ 注意事项

1. **时区处理**: 使用 `DateTime.UtcNow` 确保时区一致性
2. **性能优化**: 当前实现会加载所有 `upcoming` 活动到内存,如果数据量大(>10000),建议优化为数据库层过滤
3. **并发安全**: Repository 使用 Scoped 生命周期,每次扫描创建新的 Scope
4. **错误处理**: 单个活动更新失败不影响其他活动,会记录错误日志并继续

## 🚀 部署后验证

### 1. 检查服务启动
```bash
docker logs go-nomads-event-service --tail 50 | grep EventStatusUpdateService
```

应该看到: `🕒 EventStatusUpdateService 已启动`

### 2. 等待首次扫描
等待 10 秒后,应该看到扫描日志

### 3. 验证数据库
```sql
-- 查看最近更新为 completed 的活动
SELECT id, title, status, start_time, end_time, updated_at
FROM events
WHERE status = 'completed'
ORDER BY updated_at DESC
LIMIT 10;
```

## 📅 完成时间
2025-11-25

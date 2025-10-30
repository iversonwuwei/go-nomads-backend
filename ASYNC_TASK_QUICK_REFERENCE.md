# 异步任务队列 - 快速参考

## 🎯 核心概念

用户点击"生成计划" → 创建异步任务 → 后台处理 → 实时通知 → 显示结果

## 📡 API 端点

### 创建任务
```http
POST /api/v1/ai/travel-plan/async
Body: { cityId, days, interests, budget }
Response: { taskId, status: "queued" }
```

### 查询状态
```http
GET /api/v1/ai/travel-plan/tasks/{taskId}
Response: { taskId, status, progress, planId }
```

### SignalR Hub
```
ws://localhost:8009/hubs/notifications
Methods: SubscribeToTask(taskId)
Events: TaskProgress, TaskCompleted, TaskFailed
```

## 🔄 任务状态

- `queued` - 已入队,等待处理
- `processing` - 正在处理 (progress: 0-100)
- `completed` - 完成 (返回 planId)
- `failed` - 失败 (返回 error)

## 🚀 快速测试

```powershell
# 1. 启动服务
docker-compose up -d redis rabbitmq ai-service

# 2. 运行测试
.\test-async-travel-plan.ps1

# 3. 查看 RabbitMQ
http://localhost:15672 (guest/guest)
```

## 📦 关键文件

```
Infrastructure/
  ├── MessageBus/
  │   ├── IMessageBus.cs
  │   └── RabbitMQMessageBus.cs (169行)
  └── Cache/
      ├── IRedisCache.cs
      └── RedisCache.cs (107行)

API/
  ├── Models/TravelPlanTaskModels.cs
  ├── Hubs/NotificationHub.cs (115行)
  ├── Services/AIWorkerService.cs (198行)
  └── Controllers/ChatController.cs (新增2个端点)

配置:
  ├── Program.cs (注册服务)
  ├── appsettings.json (RabbitMQ + Redis)
  └── docker-compose.yml (添加 RabbitMQ)
```

## 🎨 Flutter 集成示例

```dart
// 1. 创建任务
final response = await apiService.createTravelPlanTask(request);
final taskId = response.taskId;

// 2. 连接 SignalR
signalRService.subscribeToTask(taskId);
signalRService.onProgress((progress, message) {
  setState(() { _progress = progress; });
});
signalRService.onCompleted((planId) {
  Navigator.push(...); // 导航到结果页
});

// 3. 备用轮询
Timer.periodic(Duration(seconds: 3), (timer) async {
  final status = await apiService.getTaskStatus(taskId);
  if (status.status == 'completed') {
    timer.cancel();
    // 显示结果
  }
});
```

## ⚙️ 配置项

```json
{
  "RabbitMQ": {
    "HostName": "go-nomads-rabbitmq",
    "Port": 5672
  },
  "Redis": {
    "ConnectionString": "go-nomads-redis:6379"
  }
}
```

## 🔍 监控命令

```bash
# 查看 RabbitMQ 队列
docker exec -it go-nomads-rabbitmq rabbitmqctl list_queues

# 查看 Redis 任务
docker exec -it go-nomads-redis redis-cli KEYS "task:*"

# AI Service 日志
docker logs -f go-nomads-ai-service | grep "任务"
```

## ✅ 优势

- ✅ 可靠: 消息持久化 + 重试机制
- ✅ 实时: SignalR 推送进度
- ✅ 快速: Redis 缓存状态
- ✅ 可扩展: Worker 可水平扩展
- ✅ 容错: 轮询作为备用方案

# 异步任务队列架构实现完成

## 概述

成功实现了基于 RabbitMQ + Redis + SignalR 的异步任务队列架构,替代之前失败的 SSE 流式输出方案。

## 架构设计

```
┌─────────────┐      ┌──────────────┐      ┌─────────────┐
│   Flutter   │─────▶│  AI Service  │─────▶│  RabbitMQ   │
│    App      │      │  (API)       │      │   Queue     │
└─────────────┘      └──────────────┘      └─────────────┘
       │                    │                      │
       │                    │                      ▼
       │                    │              ┌─────────────┐
       │                    │              │ AI Worker   │
       │                    │              │  Service    │
       │                    │              └─────────────┘
       │                    │                      │
       │                    ▼                      │
       │             ┌──────────────┐              │
       │             │    Redis     │◀─────────────┘
       │             │   (Cache)    │
       │             └──────────────┘
       │                    │
       │                    │
       ▼                    ▼
┌─────────────┐      ┌──────────────┐
│  SignalR    │◀─────│  Supabase    │
│   Hub       │      │  PostgreSQL  │
└─────────────┘      └──────────────┘
```

## 已创建的文件

### 1. Infrastructure Layer (基础设施层)

#### **消息队列接口和实现**
- `Infrastructure/MessageBus/IMessageBus.cs` - 消息总线接口
- `Infrastructure/MessageBus/RabbitMQMessageBus.cs` - RabbitMQ 实现 (169 行)
  - 自动重连机制
  - 消息持久化
  - 手动确认 + 重试逻辑 (最多 3 次)
  - 完整的错误处理和日志

#### **缓存接口和实现**
- `Infrastructure/Cache/IRedisCache.cs` - 缓存接口
- `Infrastructure/Cache/RedisCache.cs` - Redis 实现 (107 行)
  - JSON 序列化支持
  - 过期时间配置
  - 键管理操作

### 2. API Layer (接口层)

#### **数据模型**
- `API/Models/TravelPlanTaskModels.cs`
  - `TravelPlanTaskMessage` - 任务消息体
  - `TaskStatus` - 任务状态信息
  - `CreateTaskResponse` - 创建任务响应

#### **SignalR 实时通知**
- `API/Hubs/NotificationHub.cs` (115 行)
  - `NotificationHub` - SignalR Hub 实现
  - `INotificationService` - 通知服务接口
  - `NotificationService` - 通知服务实现
  - 支持事件:
    - `TaskProgress` - 进度更新
    - `TaskCompleted` - 任务完成
    - `TaskFailed` - 任务失败

#### **后台任务处理**
- `API/Services/AIWorkerService.cs` (198 行)
  - 订阅 RabbitMQ 队列 `travel-plan-tasks`
  - 调用 AI 生成旅行计划
  - 更新 Redis 缓存状态
  - 发送 SignalR 实时通知
  - 保存结果到 PostgreSQL

#### **API 控制器**
- `API/Controllers/ChatController.cs` - 已更新
  - 新增端点:
    - `POST /api/v1/ai/travel-plan/async` - 创建异步任务
    - `GET /api/v1/ai/travel-plan/tasks/{taskId}` - 查询任务状态

### 3. Configuration (配置)

#### **项目依赖**
- `AIService.csproj` - 已更新
  - 添加: `RabbitMQ.Client` 6.8.1
  - 添加: `StackExchange.Redis` 2.7.33

#### **应用配置**
- `appsettings.json` - 已更新
  ```json
  {
    "RabbitMQ": {
      "HostName": "go-nomads-rabbitmq",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/"
    },
    "Redis": {
      "ConnectionString": "go-nomads-redis:6379,abortConnect=false"
    }
  }
  ```

#### **服务注册**
- `Program.cs` - 已更新
  - 注册服务:
    - `IMessageBus` → `RabbitMQMessageBus` (Singleton)
    - `IRedisCache` → `RedisCache` (Singleton)
    - `INotificationService` → `NotificationService` (Scoped)
    - `AIWorkerService` (HostedService)
  - 添加 SignalR 支持
  - 映射 Hub 端点: `/hubs/notifications`
  - 配置 SignalR CORS 策略

### 4. Docker Configuration

#### **docker-compose.yml** - 已更新
- 添加 RabbitMQ 服务:
  ```yaml
  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"    # AMQP
      - "15672:15672"  # Management UI
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
  ```
- 更新 ai-service 依赖和环境变量
- 添加 volume: `rabbitmq_data`

### 5. Testing Script

- `test-async-travel-plan.ps1` - PowerShell 测试脚本
  - 创建异步任务
  - 轮询任务状态
  - 显示进度和结果

## API 使用流程

### 1. 创建异步任务

**请求:**
```http
POST /api/v1/ai/travel-plan/async
Content-Type: application/json

{
  "cityId": 2,
  "cityName": "上海",
  "days": 3,
  "interests": ["美食", "文化", "购物"],
  "budget": 3000
}
```

**响应:**
```json
{
  "success": true,
  "message": "任务已创建",
  "data": {
    "taskId": "abc123def456",
    "status": "queued",
    "estimatedTimeSeconds": 120,
    "message": "任务已创建,正在队列中等待处理。预计2分钟内完成。"
  }
}
```

### 2. 查询任务状态

**请求:**
```http
GET /api/v1/ai/travel-plan/tasks/{taskId}
```

**响应 (处理中):**
```json
{
  "success": true,
  "data": {
    "taskId": "abc123def456",
    "status": "processing",
    "progress": 60,
    "progressMessage": "正在解析结果...",
    "createdAt": "2025-10-30T10:00:00Z",
    "updatedAt": "2025-10-30T10:01:30Z"
  }
}
```

**响应 (完成):**
```json
{
  "success": true,
  "data": {
    "taskId": "abc123def456",
    "status": "completed",
    "progress": 100,
    "progressMessage": "生成完成!",
    "planId": "uuid-of-travel-plan",
    "createdAt": "2025-10-30T10:00:00Z",
    "updatedAt": "2025-10-30T10:02:00Z",
    "completedAt": "2025-10-30T10:02:00Z"
  }
}
```

### 3. SignalR 实时通知 (可选)

**连接:**
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:8009/hubs/notifications")
  .build();

await connection.start();
await connection.invoke("SubscribeToTask", taskId);
```

**监听事件:**
```javascript
connection.on("TaskProgress", (data) => {
  console.log(`进度: ${data.progress}% - ${data.message}`);
});

connection.on("TaskCompleted", (data) => {
  console.log(`任务完成! PlanId: ${data.planId}`);
});

connection.on("TaskFailed", (data) => {
  console.error(`任务失败: ${data.error}`);
});
```

## 任务状态流转

```
queued (入队)
   ↓
processing (处理中)
   ├─ progress: 10% → "正在生成旅行计划..."
   ├─ progress: 30% → "正在调用 AI 模型..."
   ├─ progress: 60% → "正在解析结果..."
   └─ progress: 80% → "正在保存到数据库..."
   ↓
completed (完成) / failed (失败)
```

## 下一步工作

### Backend (已完成)
- ✅ RabbitMQ 消息队列集成
- ✅ Redis 缓存实现
- ✅ SignalR 实时通知
- ✅ 后台 Worker 服务
- ✅ 异步 API 端点
- ✅ Docker 配置更新

### Frontend (待完成)
1. **添加依赖包**
   ```yaml
   dependencies:
     signalr_netcore: ^1.3.5
   ```

2. **创建异步 API 服务**
   ```dart
   // lib/services/async_task_service.dart
   class AsyncTaskService {
     Future<String> createTravelPlanTask(request) { }
     Future<TaskStatus> getTaskStatus(taskId) { }
   }
   ```

3. **实现 SignalR 客户端**
   ```dart
   // lib/services/signalr_service.dart
   class SignalRService {
     HubConnection? _connection;
     void subscribeToTask(String taskId) { }
   }
   ```

4. **更新 UI 流程**
   ```dart
   // 1. 显示加载对话框
   // 2. 创建异步任务
   // 3. 连接 SignalR / 开始轮询
   // 4. 更新进度条
   // 5. 完成后导航到结果页
   ```

5. **添加轮询备用方案**
   ```dart
   // 如果 SignalR 连接失败,每 3 秒轮询一次
   Timer.periodic(Duration(seconds: 3), (timer) {
     checkTaskStatus(taskId);
   });
   ```

## 部署步骤

### 1. 启动基础设施服务
```bash
docker-compose up -d redis rabbitmq consul
```

### 2. 验证服务运行
```bash
# RabbitMQ Management UI
http://localhost:15672  (guest/guest)

# Redis
docker exec -it go-nomads-redis redis-cli ping
```

### 3. 启动 AI Service
```bash
docker-compose up -d ai-service
```

### 4. 测试异步 API
```powershell
.\test-async-travel-plan.ps1
```

## 技术亮点

1. **可靠性**: RabbitMQ 消息持久化 + 手动确认 + 重试机制
2. **实时性**: SignalR 推送通知,用户无需轮询
3. **性能**: Redis 缓存快速查询任务状态
4. **可扩展**: Worker 服务可以水平扩展,多实例消费队列
5. **容错**: 轮询作为备用方案,SignalR 失败不影响功能
6. **监控**: 完整的日志记录,带 emoji 标记便于排查

## 解决的问题

- ❌ SSE 流式输出: 客户端兼容性差,连接不稳定
- ✅ 异步任务队列: 成熟方案,业界最佳实践
- ❌ 同步等待超时: AI 生成耗时过长
- ✅ 后台异步处理: 用户无需等待,体验更好
- ❌ 进度不可见: 用户不知道处理进展
- ✅ 实时进度推送: SignalR + Redis 双重保障

## 监控和维护

### RabbitMQ 管理
- Management UI: http://localhost:15672
- 查看队列: `travel-plan-tasks`
- 监控消息堆积和消费速率

### Redis 缓存
```bash
# 查看所有任务
redis-cli KEYS "task:*"

# 查看任务详情
redis-cli GET "task:abc123"

# 清理过期任务
redis-cli SCAN 0 MATCH "task:*" COUNT 100
```

### 日志查看
```bash
# AI Service 日志
docker logs -f go-nomads-ai-service

# 查找任务相关日志
docker logs go-nomads-ai-service | grep "任务"
```

## 总结

成功从失败的 SSE 流式方案转型到异步任务队列架构:
- **Backend**: 完整实现,可立即部署测试
- **Infrastructure**: RabbitMQ + Redis + SignalR 三层保障
- **API**: RESTful + SignalR 双通道通信
- **Docker**: 一键启动所有服务

下一步重点是 **Flutter 端集成**,包括 SignalR 客户端和 UI 流程更新。

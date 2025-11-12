# MessageService 设计文档

## 一、服务概述

**MessageService** 是一个独立的微服务，负责统一处理系统中的异步消息传递和实时通信。

### 核心功能
- **RabbitMQ 消息队列**: 异步任务调度、事件驱动通信
- **SignalR Hub**: 实时消息推送到 Flutter 客户端
- **消息持久化**: 存储重要消息历史
- **连接管理**: 管理客户端 SignalR 连接状态

---

## 二、技术架构

### 2.1 技术栈
```
- .NET 8.0
- ASP.NET Core SignalR
- RabbitMQ.Client
- MassTransit (推荐，简化 RabbitMQ 使用)
- Redis (SignalR Backplane，支持多实例部署)
- Entity Framework Core (消息持久化)
- Serilog (日志)
```

### 2.2 项目结构
```
MessageService/
├── API/
│   ├── Controllers/
│   │   └── MessageController.cs         # REST API
│   ├── Hubs/
│   │   ├── NotificationHub.cs          # 通用通知 Hub
│   │   ├── AIProgressHub.cs            # AI 进度推送 Hub
│   │   └── ChatHub.cs                  # 聊天消息 Hub
│   └── Program.cs
├── Application/
│   ├── Services/
│   │   ├── MessagePublisher.cs         # RabbitMQ 发布者
│   │   ├── SignalRNotifier.cs          # SignalR 推送服务
│   │   └── MessageHistoryService.cs    # 消息历史管理
│   └── DTOs/
│       ├── MessageDto.cs
│       └── NotificationDto.cs
├── Infrastructure/
│   ├── RabbitMQ/
│   │   ├── Consumers/
│   │   │   ├── AITaskConsumer.cs       # AI 任务消费者
│   │   │   ├── NotificationConsumer.cs # 通知消费者
│   │   │   └── EventUpdateConsumer.cs  # 事件更新消费者
│   │   ├── Publishers/
│   │   │   └── MessagePublisher.cs
│   │   └── Configuration/
│   │       └── RabbitMQSettings.cs
│   ├── SignalR/
│   │   ├── ConnectionManager.cs        # 连接管理
│   │   └── GroupManager.cs             # 群组管理
│   └── Persistence/
│       ├── MessageDbContext.cs
│       └── Repositories/
│           └── MessageRepository.cs
└── Domain/
    └── Entities/
        ├── Message.cs
        └── UserConnection.cs
```

---

## 三、核心功能设计

### 3.1 RabbitMQ 消息队列

#### 消息类型定义
```csharp
// AI 任务消息
public class AITaskMessage
{
    public string TaskId { get; set; }
    public string UserId { get; set; }
    public string TaskType { get; set; } // "plan" | "guide"
    public Dictionary<string, object> Parameters { get; set; }
    public DateTime CreatedAt { get; set; }
}

// AI 进度消息
public class AIProgressMessage
{
    public string TaskId { get; set; }
    public string UserId { get; set; }
    public int Progress { get; set; } // 0-100
    public string Status { get; set; } // "processing" | "completed" | "failed"
    public string CurrentStep { get; set; }
    public string Result { get; set; } // JSON 结果
}

// 通用通知消息
public class NotificationMessage
{
    public string UserId { get; set; }
    public string Type { get; set; } // "info" | "warning" | "error"
    public string Title { get; set; }
    public string Content { get; set; }
    public Dictionary<string, object> Data { get; set; }
}
```

#### Exchange 和 Queue 配置
```
Exchanges:
├── ai.tasks.exchange (Topic)
│   ├── Queue: ai.planner.queue
│   │   └── Routing Key: ai.task.plan
│   └── Queue: ai.guide.queue
│       └── Routing Key: ai.task.guide
│
├── ai.progress.exchange (Fanout)
│   └── Queue: ai.progress.signalr.queue
│       └── Consumer: AIProgressConsumer → SignalR Push
│
├── notifications.exchange (Topic)
│   ├── Queue: notification.user.queue
│   │   └── Routing Key: notification.user.*
│   └── Queue: notification.broadcast.queue
│       └── Routing Key: notification.broadcast
│
└── events.exchange (Topic)
    └── Queue: event.updates.queue
        └── Routing Key: event.update.*
```

### 3.2 SignalR Hub 设计

#### AIProgressHub.cs
```csharp
public class AIProgressHub : Hub
{
    private readonly IConnectionManager _connectionManager;

    // 客户端连接时，关联 UserId
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.AddConnection(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }

    // 订阅特定 AI 任务进度
    public async Task SubscribeToTask(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task-{taskId}");
    }

    // 取消订阅
    public async Task UnsubscribeFromTask(string taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task-{taskId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.RemoveConnection(userId, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
```

#### SignalR 推送服务
```csharp
public class SignalRNotifier
{
    private readonly IHubContext<AIProgressHub> _aiProgressHub;
    private readonly IHubContext<NotificationHub> _notificationHub;

    // 推送 AI 进度到特定用户
    public async Task SendAIProgress(string userId, AIProgressMessage progress)
    {
        await _aiProgressHub.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveProgress", progress);
    }

    // 推送到特定任务订阅者
    public async Task SendTaskUpdate(string taskId, object update)
    {
        await _aiProgressHub.Clients
            .Group($"task-{taskId}")
            .SendAsync("ReceiveTaskUpdate", update);
    }

    // 推送通知
    public async Task SendNotification(string userId, NotificationMessage notification)
    {
        await _notificationHub.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveNotification", notification);
    }

    // 广播通知
    public async Task BroadcastNotification(NotificationMessage notification)
    {
        await _notificationHub.Clients.All
            .SendAsync("ReceiveNotification", notification);
    }
}
```

### 3.3 RabbitMQ Consumer 示例

#### AIProgressConsumer.cs
```csharp
public class AIProgressConsumer : IConsumer<AIProgressMessage>
{
    private readonly SignalRNotifier _signalRNotifier;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<AIProgressConsumer> _logger;

    public async Task Consume(ConsumeContext<AIProgressMessage> context)
    {
        var progress = context.Message;
        
        _logger.LogInformation(
            "收到 AI 进度消息: TaskId={TaskId}, Progress={Progress}%, Status={Status}",
            progress.TaskId, progress.Progress, progress.Status);

        try
        {
            // 1. 推送到 SignalR
            await _signalRNotifier.SendAIProgress(progress.UserId, progress);

            // 2. 保存到数据库（可选）
            if (progress.Status == "completed" || progress.Status == "failed")
            {
                await _messageRepository.SaveProgressHistory(progress);
            }

            _logger.LogInformation("AI 进度推送成功: TaskId={TaskId}", progress.TaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送 AI 进度失败: TaskId={TaskId}", progress.TaskId);
            throw; // 让 RabbitMQ 重试
        }
    }
}
```

---

## 四、使用 MassTransit 简化配置

### 4.1 安装依赖
```bash
cd src/Services/MessageService
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ
dotnet add package MassTransit.AspNetCore
```

### 4.2 Program.cs 配置
```csharp
var builder = WebApplication.CreateBuilder(args);

// 配置 RabbitMQ + MassTransit
builder.Services.AddMassTransit(x =>
{
    // 注册消费者
    x.AddConsumer<AIProgressConsumer>();
    x.AddConsumer<NotificationConsumer>();
    x.AddConsumer<EventUpdateConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // 配置队列
        cfg.ReceiveEndpoint("ai.progress.signalr.queue", e =>
        {
            e.ConfigureConsumer<AIProgressConsumer>(context);
        });

        cfg.ReceiveEndpoint("notification.user.queue", e =>
        {
            e.ConfigureConsumer<NotificationConsumer>(context);
        });
    });
});

// 配置 SignalR
builder.Services.AddSignalR()
    .AddStackExchangeRedis("localhost:6379", options =>
    {
        options.Configuration.ChannelPrefix = "MessageService";
    });

// 注册服务
builder.Services.AddScoped<SignalRNotifier>();
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();

var app = builder.Build();

// 映射 SignalR Hubs
app.MapHub<AIProgressHub>("/hubs/ai-progress");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
```

---

## 五、Flutter 客户端集成

### 5.1 添加依赖
```yaml
dependencies:
  signalr_netcore: ^1.3.3
  get: ^4.6.6
```

### 5.2 SignalR 服务封装
```dart
class MessageService {
  late HubConnection _aiProgressConnection;
  late HubConnection _notificationConnection;
  
  final String messageServiceUrl = 'http://127.0.0.1:5005'; // MessageService 地址

  Future<void> initialize(String accessToken) async {
    // 1. 连接 AI Progress Hub
    _aiProgressConnection = HubConnectionBuilder()
        .withUrl('$messageServiceUrl/hubs/ai-progress',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => accessToken,
            ))
        .withAutomaticReconnect()
        .build();

    // 监听 AI 进度推送
    _aiProgressConnection.on('ReceiveProgress', (arguments) {
      final progress = arguments?[0] as Map<String, dynamic>;
      _handleAIProgress(progress);
    });

    await _aiProgressConnection.start();

    // 2. 连接 Notification Hub
    _notificationConnection = HubConnectionBuilder()
        .withUrl('$messageServiceUrl/hubs/notifications',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => accessToken,
            ))
        .withAutomaticReconnect()
        .build();

    _notificationConnection.on('ReceiveNotification', (arguments) {
      final notification = arguments?[0] as Map<String, dynamic>;
      _handleNotification(notification);
    });

    await _notificationConnection.start();
  }

  void _handleAIProgress(Map<String, dynamic> progress) {
    // 更新 UI
    final aiController = Get.find<AIStateController>();
    aiController.updateProgress(
      taskId: progress['taskId'],
      progress: progress['progress'],
      status: progress['status'],
      result: progress['result'],
    );
  }

  void _handleNotification(Map<String, dynamic> notification) {
    // 显示通知
    Get.snackbar(
      notification['title'],
      notification['content'],
      snackPosition: SnackPosition.TOP,
    );
  }

  Future<void> dispose() async {
    await _aiProgressConnection.stop();
    await _notificationConnection.stop();
  }
}
```

---

## 六、其他服务调用 MessageService

### 6.1 AIService 发布 AI 任务
```csharp
public class TravelPlannerService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task<string> CreatePlanAsync(PlanRequest request)
    {
        var taskId = Guid.NewGuid().ToString();

        // 发布 AI 任务到 RabbitMQ
        await _publishEndpoint.Publish(new AITaskMessage
        {
            TaskId = taskId,
            UserId = request.UserId,
            TaskType = "plan",
            Parameters = new Dictionary<string, object>
            {
                { "destination", request.Destination },
                { "days", request.Days },
                { "budget", request.Budget }
            },
            CreatedAt = DateTime.UtcNow
        });

        return taskId;
    }
}
```

### 6.2 AIService 推送进度
```csharp
public class AIWorkerService : BackgroundService
{
    private readonly IPublishEndpoint _publishEndpoint;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 模拟 AI 任务处理
        for (int i = 0; i <= 100; i += 10)
        {
            await _publishEndpoint.Publish(new AIProgressMessage
            {
                TaskId = "task-123",
                UserId = "user-456",
                Progress = i,
                Status = i == 100 ? "completed" : "processing",
                CurrentStep = $"正在生成行程... {i}%",
                Result = i == 100 ? "{\"plan\": \"...\"}" : null
            });

            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

---

## 七、部署配置

### 7.1 Docker Compose
```yaml
version: '3.8'

services:
  message-service:
    build:
      context: .
      dockerfile: src/Services/MessageService/Dockerfile
    ports:
      - "5005:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RabbitMQ__Host=rabbitmq
      - Redis__Connection=redis:6379
    depends_on:
      - rabbitmq
      - redis

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
```

### 7.2 appsettings.json
```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest"
  },
  "Redis": {
    "Connection": "localhost:6379",
    "InstanceName": "MessageService:"
  },
  "SignalR": {
    "MaxBufferSize": 32768,
    "KeepAliveInterval": "00:00:15"
  }
}
```

---

## 八、优势总结

✅ **统一消息管理**: 所有异步消息和实时推送集中管理  
✅ **解耦服务**: 其他服务只需要发布消息，不关心推送逻辑  
✅ **可扩展性**: 支持水平扩展（通过 Redis Backplane）  
✅ **可靠性**: RabbitMQ 消息持久化 + SignalR 自动重连  
✅ **易维护**: 消息流清晰，便于监控和调试  

---

## 九、后续优化建议

1. **消息追踪**: 集成 OpenTelemetry 追踪消息流转
2. **消息重试**: 配置 RabbitMQ 死信队列处理失败消息
3. **权限控制**: SignalR Hub 添加授权验证
4. **监控面板**: 开发管理后台监控消息队列状态
5. **消息压缩**: 对大消息进行压缩传输

# Coworking 助力认证人数实时监控功能

## 功能概述

在 Coworking List 和 Detail 页面添加实时显示助力认证人数的功能，当有用户提交认证时，其他正在查看该 Coworking 的用户能够实时看到人数变化。

## 实现架构

### 后端 (.NET 9 + SignalR)

```
CoworkingService
├── API/Hubs/
│   └── CoworkingHub.cs              # SignalR Hub，处理订阅/取消订阅
├── Application/Services/
│   ├── ICoworkingNotifier.cs        # 通知器接口
│   └── CoworkingApplicationService.cs  # 在验证提交时调用通知器
└── Infrastructure/Notifiers/
    └── SignalRCoworkingNotifier.cs  # SignalR 通知器实现
```

### 前端 (Flutter + GetX)

```
open-platform-app
├── lib/config/
│   └── api_config.dart              # 添加 coworkingServicePort 配置
├── lib/features/coworking/
│   ├── infrastructure/services/
│   │   └── signalr_coworking_service.dart  # SignalR 客户端服务
│   └── presentation/controllers/
│       └── coworking_state_controller.dart  # 集成 SignalR，管理实时状态
├── lib/widgets/
│   └── coworking_verification_badge.dart    # 显示实时验证人数
└── lib/pages/
    ├── coworking_list_page.dart     # 列表页订阅
    └── coworking_detail_page.dart   # 详情页订阅
```

## 数据流

```
用户A提交认证 
    ↓
CoworkingApplicationService.SubmitVerificationAsync()
    ↓
ICoworkingNotifier.NotifyVerificationVotesChangedAsync()
    ↓
SignalRCoworkingNotifier → HubContext.Clients.Group("coworking-{id}")
    ↓
SignalR 广播 "VerificationVotesUpdated" 事件
    ↓
Flutter SignalRCoworkingService 接收事件
    ↓
CoworkingStateController._handleVotesUpdate()
    ↓
更新 realtimeVerificationVotes + coworkingSpaces + filteredSpaces + currentCoworking
    ↓
CoworkingVerificationBadge 通过 Obx 自动刷新 UI
```

## 关键代码

### 后端 Hub

```csharp
// CoworkingHub.cs
public async Task SubscribeCoworking(string coworkingId)
{
    await Groups.AddToGroupAsync(Context.ConnectionId, $"coworking-{coworkingId}");
}

// SignalRCoworkingNotifier.cs
public async Task NotifyVerificationVotesChangedAsync(Guid coworkingId, int verificationVotes, bool isVerified)
{
    await _hubContext.Clients.Group($"coworking-{coworkingId}").SendAsync("VerificationVotesUpdated", new
    {
        CoworkingId = coworkingId.ToString(),
        VerificationVotes = verificationVotes,
        IsVerified = isVerified,
        Timestamp = DateTime.UtcNow
    });
}
```

### Flutter 服务

```dart
// signalr_coworking_service.dart
_hubConnection!.on('VerificationVotesUpdated', (arguments) {
    final data = arguments.first as Map<String, dynamic>;
    final update = VerificationVotesUpdate(
        coworkingId: data['coworkingId'],
        verificationVotes: data['verificationVotes'],
        isVerified: data['isVerified'],
    );
    _verificationVotesController.add(update);
});
```

### 验证徽章组件

```dart
// coworking_verification_badge.dart
final int verificationVotes = _coworkingController.getVerificationVotes(space);

// 显示验证人数（未验证时显示）
if (!space.isVerified) ...[
    Container(
        child: Text('$verificationVotes'),
    ),
],
```

## SignalR Hub 端点

- **URL**: `http://{host}:{port}/hubs/coworking`
- **端口**: 开发环境使用 5006

## Hub 方法

| 方法 | 参数 | 描述 |
|------|------|------|
| `Authenticate` | `userId` | 用户认证 |
| `SubscribeCoworking` | `coworkingId` | 订阅单个 Coworking |
| `SubscribeCoworkings` | `List<coworkingId>` | 批量订阅 |
| `UnsubscribeCoworking` | `coworkingId` | 取消订阅 |
| `UnsubscribeAll` | - | 取消所有订阅 |

## Hub 事件

| 事件 | 数据 | 描述 |
|------|------|------|
| `Authenticated` | `{ Success, UserId, Timestamp }` | 认证成功 |
| `VerificationVotesUpdated` | `{ CoworkingId, VerificationVotes, IsVerified, Timestamp }` | 验证人数更新 |
| `Error` | `string` | 错误消息 |

## 配置要求

### 后端

1. 确保 `Program.cs` 中添加了 SignalR 服务：
   ```csharp
   builder.Services.AddSignalR();
   builder.Services.AddSingleton<ICoworkingNotifier, SignalRCoworkingNotifier>();
   ```

2. CORS 配置支持 WebSocket：
   ```csharp
   policy.SetIsOriginAllowed(_ => true)
       .AllowAnyMethod()
       .AllowAnyHeader()
       .AllowCredentials();
   ```

3. Hub 端点映射：
   ```csharp
   app.MapHub<CoworkingHub>("/hubs/coworking");
   ```

### 前端

1. `api_config.dart` 中配置端口：
   ```dart
   static const int coworkingServicePort = 5006;
   ```

2. 依赖包：
   - `signalr_netcore: ^1.1.2`

## 注意事项

1. **端口配置**: 开发环境需要确认 CoworkingService 实际运行的端口，修改 `coworkingServicePort` 配置
2. **生产环境**: 生产环境应通过反向代理/负载均衡器访问 SignalR Hub
3. **连接管理**: SignalR 服务会在页面 `dispose` 时自动断开连接
4. **重连机制**: 使用 `withAutomaticReconnect()` 自动处理断线重连

## 测试步骤

1. 启动 CoworkingService 后端
2. 打开两个 Flutter 客户端
3. 两个客户端都进入同一个 Coworking 详情页
4. 其中一个客户端提交认证
5. 另一个客户端应该实时看到验证人数 +1

## 相关文件

### 后端新增
- `/go-noma/src/Services/CoworkingService/CoworkingService/API/Hubs/CoworkingHub.cs`
- `/go-noma/src/Services/CoworkingService/CoworkingService/Application/Services/ICoworkingNotifier.cs`
- `/go-noma/src/Services/CoworkingService/CoworkingService/Infrastructure/Notifiers/SignalRCoworkingNotifier.cs`

### 后端修改
- `/go-noma/src/Services/CoworkingService/CoworkingService/Program.cs`
- `/go-noma/src/Services/CoworkingService/CoworkingService/Application/Services/CoworkingApplicationService.cs`

### 前端新增
- `/open-platform-app/lib/features/coworking/infrastructure/services/signalr_coworking_service.dart`

### 前端修改
- `/open-platform-app/lib/config/api_config.dart`
- `/open-platform-app/lib/features/coworking/presentation/controllers/coworking_state_controller.dart`
- `/open-platform-app/lib/widgets/coworking_verification_badge.dart`
- `/open-platform-app/lib/pages/coworking_list_page.dart`
- `/open-platform-app/lib/pages/coworking_detail_page.dart`

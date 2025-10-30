# 流式文本接口日志增强说明

## 修改时间
2025年10月30日

## 修改目标
为后端流式文本接口 (`GenerateTravelPlanStreamText`) 添加详细的异常捕获和日志记录,以便诊断"response ended prematurely"等流中断问题。

## 已完成的改动

### 1. StreamText 方法增强 (ChatController.cs)
**位置**: `StreamText(string text)` 方法

**新增内容**:
- 在每次写入前检查 `HttpContext.RequestAborted.IsCancellationRequested` 是否已取消
- 在 WriteAsync 和 FlushAsync 中传递 `HttpContext.RequestAborted` token
- 添加 LogTrace 记录每次写入的字节数
- 分别捕获三类异常:
  - `OperationCanceledException` - 客户端断开
  - `IOException` - 网络IO错误
  - `Exception` - 其他未知错误
- 每种异常都会记录详细日志并重新抛出

**日志示例**:
```
📤 [StreamText] 准备写入 156 字节
✅ [StreamText] 写入并刷新完成
⚠️ [StreamText] 客户端已断开连接,停止写入
❌ [StreamText] IO异常 - 连接可能已断开
```

### 2. SendProgressEvent 方法增强 (ChatController.cs)
**位置**: `SendProgressEvent(string eventType, object data)` 方法

**新增内容**:
- 检查连接状态,防止向已断开的连接写入
- 记录事件类型和数据大小
- 使用 LogDebug 记录发送和完成状态
- 捕获并记录三类异常(同 StreamText)

**日志示例**:
```
📤 [SendProgressEvent] 发送事件: complete, 大小: 4523 字节
✅ [SendProgressEvent] 事件发送完成: complete
⚠️ [SendProgressEvent] 操作被取消,事件类型: complete
```

### 3. GenerateTravelPlanStreamText 主方法增强
**位置**: `GenerateTravelPlanStreamText([FromBody] GenerateTravelPlanRequest request)` 方法

**新增内容**:
- 为每个请求生成唯一的 `requestId` (8位短GUID) 用于日志追踪
- 在关键步骤添加日志:
  - 请求开始 (城市、天数)
  - 用户ID
  - 发送开始提示
  - 调用AI服务
  - AI服务返回
  - 开始流式输出
  - 准备发送 complete 事件
  - 输出完成
- 异常处理升级:
  - 分别处理 `OperationCanceledException` (Warning 级别,不抛出)
  - 分别处理 `IOException` (Error 级别,记录详细信息)
  - 通用 Exception 记录异常类型、消息和完整堆栈跟踪
  - 所有异常都尝试向客户端发送错误消息(忽略二次失败)

**日志示例**:
```
🌊 [流式文本-a3f9c2d1] 开始生成旅行计划 - 城市: 成都, Duration: 7
🌊 [流式文本-a3f9c2d1] 用户ID: 00000000-0000-0000-0000-000000000001
[a3f9c2d1] 发送开始提示
[a3f9c2d1] 开始调用 AI 服务
✅ [a3f9c2d1] AI 服务返回结果,计划ID: plan_12345
[a3f9c2d1] 开始流式输出内容
📤 [a3f9c2d1] 准备发送 complete 事件
✅ [流式文本-a3f9c2d1] 旅行计划输出完成 - 计划ID: plan_12345

--- 或异常情况 ---
⚠️ [流式文本-a3f9c2d1] 请求被取消 - 客户端可能断开连接
❌ [流式文本-a3f9c2d1] IO异常 - 网络连接问题: Unable to write data to the transport connection
❌ [流式文本-a3f9c2d1] 生成旅行计划失败: IOException, 远程主机强迫关闭了一个现有的连接, StackTrace: ...
```

## 如何验证日志增强

### 1. 启动服务(如果未启动)
```powershell
cd E:\Workspaces\WaldenProjects\go-nomads\deployment
.\deploy-services-local.ps1
```

### 2. 查看容器日志(实时)
```powershell
docker logs -f go-nomads-ai-service
```

### 3. 运行测试脚本
在另一个终端窗口:
```powershell
cd E:\Workspaces\WaldenProjects\go-nomads
.\test-travel-plan-stream-text-chengdu.ps1
```

### 4. 观察日志输出
在容器日志中你应该看到:
- 每次请求的唯一 requestId
- 详细的执行步骤跟踪
- 每次SSE写入的字节数(Trace级别,需启用)
- 如果发生断连,会看到详细的异常类型、消息和堆栈

### 5. 调整日志级别(可选)
如果需要看到 Trace 级别日志(每次写入详情),修改 `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AIService.API.Controllers.ChatController": "Trace"
    }
  }
}
```

然后重启容器:
```powershell
docker restart go-nomads-ai-service
```

## 预期诊断结果

根据日志,你应该能识别以下场景:

### 场景1: 客户端主动断开
```
⚠️ [流式文本-xxx] 请求被取消 - 客户端可能断开连接
```
**原因**: PowerShell 脚本超时或用户中断

### 场景2: 网络层断开
```
❌ [StreamText] IO异常 - 连接可能已断开
❌ [流式文本-xxx] IO异常 - 网络连接问题: 远程主机强迫关闭了一个现有的连接
```
**可能原因**:
- Dapr sidecar 超时策略
- Docker 网络层问题
- 反向代理(如有)缓冲设置
- 客户端 HTTP 库超时

### 场景3: 服务端代码异常
```
❌ [流式文本-xxx] 生成旅行计划失败: NullReferenceException, ...
```
**原因**: 代码逻辑错误或数据异常

### 场景4: AI服务调用失败
```
[xxx] 开始调用 AI 服务
❌ [流式文本-xxx] 生成旅行计划失败: HttpRequestException, ...
```
**原因**: DeepSeek API 超时或返回错误

## 下一步诊断建议

1. **对比 requestId**: 查看同一个请求ID的完整日志链路,找到中断点
2. **检查时间戳**: 计算从请求开始到异常发生的耗时,判断是否超时
3. **分析异常类型**:
   - `OperationCanceledException` → 检查客户端和中间件超时配置
   - `IOException` → 检查网络层(Docker/Dapr/proxy)
   - 其他异常 → 检查代码逻辑和 AI API

4. **如果是超时问题**,尝试:
   - 增加客户端超时(PowerShell 的 `-TimeoutSec` 或 Flutter 的 `receiveTimeout`)
   - 减少服务端延迟(减小 `Task.Delay` 的值)
   - 检查 Dapr timeout 配置

5. **如果是连接重置**,尝试:
   - 发送更频繁的心跳(定期 flush 或发送空白 SSE 注释)
   - 检查 Docker network 或 compose 配置
   - 检查防火墙/安全软件

## 文件修改列表
- `src/Services/AIService/AIService/API/Controllers/ChatController.cs` (增强异常捕获和日志)

## 相关文件
- 测试脚本: `test-travel-plan-stream-text-chengdu.ps1`
- 容器名: `go-nomads-ai-service`
- 日志配置: `src/Services/AIService/AIService/appsettings.json`

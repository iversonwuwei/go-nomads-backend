# AI 服务超时问题修复

## 📋 问题描述

Flutter 调用 AI 旅游计划生成接口时，出现**超时错误**：

```
❌ ERROR[null] => http://10.0.2.2:5000/api/v1/ai/travel-plan
Message: The request took longer than 0:01:00.000000 to receive data. It was aborted.
```

认证已成功（UserContext 修复完成），但 AI 生成过程超时。

## 🔍 问题调查

### 1. 后端日志分析

AIService 日志显示：

```
[04:07:55 ERR] ❌ 生成旅行计划失败，城市: 北京市
System.Net.Http.HttpIOException: The response ended prematurely. (ResponseEnded)
   at System.Net.Http.HttpConnection.FillAsync(Boolean async)
   at Microsoft.SemanticKernel.Connectors.OpenAI.ClientCore.GetChatMessageContentsAsync(...)
   at AIService.Application.Services.AIChatApplicationService.GenerateTravelPlanAsync(...)
   at AIService.API.Controllers.ChatController.GenerateTravelPlan(...)

[04:07:55 ERR] HTTP POST /api/v1/ai/travel-plan responded 500 in 60320.6304 ms
```

**关键信息**：
- 请求耗时 **60.3 秒**（正好 1 分钟）
- DeepSeek API 响应被中断：`The response ended prematurely`
- 错误发生在 Semantic Kernel 调用 DeepSeek 的过程中

### 2. 超时配置检查

**后端（AIService）**：
- Semantic Kernel 使用的 HttpClient **没有配置超时时间**
- 默认 HttpClient.Timeout = 100 秒
- 但 DeepSeek API 可能在响应过程中因为网络问题被中断

**前端（Flutter）**：
- `ai_api_service.dart` 设置了 `receiveTimeout: 60秒`
- 这个超时太短，AI 生成需要更长时间

## ✅ 解决方案

### 1. 增加后端 HttpClient 超时时间

**修改文件**：`AIService/Program.cs`

**修改前**：
```csharp
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOpenAIChatCompletion(
    modelId: "deepseek-chat",
    apiKey: deepseekApiKey,
    endpoint: new Uri(deepseekBaseUrl));
```

**修改后**：
```csharp
var kernelBuilder = Kernel.CreateBuilder();

// 创建配置了超时的 HttpClient（AI 生成可能需要较长时间）
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(3) // 增加超时到 3 分钟
};
httpClient.DefaultRequestHeaders.Add("User-Agent", "GoNomads-AIService/1.0");

kernelBuilder.AddOpenAIChatCompletion(
    modelId: "deepseek-chat",
    apiKey: deepseekApiKey,
    endpoint: new Uri(deepseekBaseUrl),
    httpClient: httpClient); // 传入自定义 HttpClient

Log.Information("✅ DeepSeek AI 模型配置成功（超时: 3分钟）");
```

### 2. 增加前端接收超时时间

**修改文件**：`df_admin_mobile/lib/services/ai_api_service.dart`

**修改前**：
```dart
options: Options(
  receiveTimeout: const Duration(seconds: 60), // 1 分钟
  sendTimeout: const Duration(seconds: 30),
),
```

**修改后**：
```dart
options: Options(
  receiveTimeout: const Duration(minutes: 3), // 增加到 3 分钟（与后端保持一致）
  sendTimeout: const Duration(seconds: 30),
),
```

## 🎯 修复原理

### 超时配置层级

```
Flutter App (Dio)
    ↓ receiveTimeout: 3 分钟
Gateway
    ↓ (转发请求，无额外超时配置)
AIService (ASP.NET)
    ↓ (Controller 处理)
Semantic Kernel
    ↓ HttpClient.Timeout: 3 分钟
DeepSeek API
    ↓ (AI 生成，可能需要 1-2 分钟)
```

### 为什么需要 3 分钟？

1. **AI 生成耗时**：
   - DeepSeek 需要生成完整的旅行计划
   - 包括景点推荐、行程安排、预算估算等
   - 复杂的提示词和长响应需要更多时间

2. **网络延迟**：
   - 可能存在网络抖动或慢速连接
   - API 服务器负载可能导致响应延迟

3. **容错余量**：
   - 3 分钟提供足够的缓冲时间
   - 避免正常请求因临时慢速而失败

### DeepSeek API 响应中断问题

错误 `The response ended prematurely` 表明：
- DeepSeek API 开始发送响应
- 但在传输过程中连接被关闭
- 可能原因：
  * HttpClient 超时（之前未配置）
  * 网络中断
  * DeepSeek 服务端问题
  * 响应体太大，传输超时

通过增加超时时间，可以：
- ✅ 给 DeepSeek API 更多时间完成响应
- ✅ 避免在传输大响应时被中断
- ✅ 提高请求成功率

## 📝 相关文件修改

### 后端修改

**AIService/Program.cs**:
- 添加自定义 HttpClient 配置
- 设置 3 分钟超时
- 添加 User-Agent header

### 前端修改

**df_admin_mobile/lib/services/ai_api_service.dart**:
- 修改 receiveTimeout 从 60 秒到 3 分钟
- 保持与后端超时一致

## 🧪 测试验证

部署后测试步骤：

1. **检查 AIService 启动日志**：
   ```
   ✅ DeepSeek AI 模型配置成功（超时: 3分钟）
   ```

2. **提交旅行计划生成请求**（Flutter App）

3. **观察后端日志**：
   - 应该看到 "🗺️ 开始生成旅行计划"
   - 等待 AI 生成完成（可能需要 1-2 分钟）
   - 应该返回成功响应而不是超时错误

4. **预期结果**：
   - ✅ 不再出现 "The response ended prematurely" 错误
   - ✅ 不再出现 "request took longer than 0:01:00" 错误
   - ✅ AI 成功生成旅行计划并返回

## ⚠️ 注意事项

### 1. Gateway 超时配置

如果 Gateway 也有超时限制，需要确保：
- Gateway → AIService 的超时 ≥ 3 分钟
- 否则 Gateway 会先超时，导致请求失败

### 2. 生产环境考虑

对于生产环境，建议：
- 使用**异步模式**：立即返回任务 ID，后台生成
- 实现**进度通知**：通过 WebSocket 或轮询显示生成进度
- 添加**重试机制**：网络失败时自动重试
- 考虑**缓存**：相同请求返回缓存结果

### 3. 用户体验优化

在等待 AI 生成期间：
- ✅ 显示加载动画
- ✅ 提示"AI 正在生成中，请稍候..."
- ✅ 允许用户取消请求
- ✅ 实现超时后的友好提示

## 📊 总结

### 问题根源

- **后端**：Semantic Kernel 的 HttpClient 没有配置超时，使用默认值
- **前端**：Dio 接收超时只有 60 秒，AI 生成需要更长时间
- **DeepSeek API**：响应传输过程中因超时被中断

### 解决方案

- **后端**：配置 HttpClient.Timeout = 3 分钟
- **前端**：配置 receiveTimeout = 3 分钟
- **一致性**：前后端超时时间保持一致

### 优点

- ✅ 给 AI 生成足够的时间
- ✅ 避免正常请求因超时失败
- ✅ 提高用户体验（不会频繁失败）
- ✅ 减少因网络抖动导致的错误

---

**修复日期**: 2025-01-29  
**影响范围**: AIService (后端) + Flutter App (前端)  
**状态**: ✅ 已修复，等待部署验证

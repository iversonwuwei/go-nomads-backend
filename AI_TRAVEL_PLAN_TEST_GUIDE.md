# AI 旅游计划功能测试指南

## ✅ 修复完成

已完成两个关键修复：

### 1. **认证问题修复** ✅
- **问题**：401 未授权错误
- **原因**：ChatController 使用 JWT Claims 而非 UserContext
- **修复**：改用 UserContextMiddleware.GetUserContext()
- **文档**：[AI_SERVICE_AUTH_FIX.md](./AI_SERVICE_AUTH_FIX.md)

### 2. **超时问题修复** ✅
- **问题**：60 秒超时，DeepSeek API 响应被中断
- **原因**：HttpClient 和 Dio 超时配置过短
- **修复**：
  - 后端 HttpClient.Timeout = 3 分钟
  - 前端 receiveTimeout = 3 分钟
- **文档**：[AI_SERVICE_TIMEOUT_FIX.md](./AI_SERVICE_TIMEOUT_FIX.md)

## 🧪 测试步骤

### 前置条件

1. ✅ 后端服务已部署（`.\deploy-services-local.ps1`）
2. ✅ Flutter 代码已更新（ai_api_service.dart 超时改为 3 分钟）
3. ✅ Flutter app 已重启

### 测试流程

#### 1. 检查服务状态

```powershell
# 检查 AIService 是否运行
docker ps | Select-String "ai-service"

# 检查 AIService 日志
docker logs go-nomads-ai-service --tail 50
```

**预期结果**：
- 容器状态为 "Up"
- 日志包含：`✅ DeepSeek AI 模型配置成功（超时: 3分钟）`
- 日志包含：`✅ 服务已注册到 Consul`

#### 2. 检查 Consul 注册

访问 Consul UI：http://localhost:8500

**预期结果**：
- 看到 `ai-service` 服务
- 健康检查状态为 **绿色（Passing）**

#### 3. 在 Flutter App 中测试

##### 步骤 1：登录
- 使用已有账号登录
- 确保登录成功，获取 token

##### 步骤 2：导航到城市详情
- 选择一个城市（如"北京"）
- 点击进入城市详情页

##### 步骤 3：生成旅行计划
- 点击"生成旅行计划"或相关按钮
- 填写旅行信息：
  - 天数：7
  - 预算：中等
  - 风格：文化
  - 兴趣：艺术、市场、历史景点等
  - 出发地点：（可选）
- 提交表单

##### 步骤 4：观察加载过程
- 应该看到加载动画
- 等待 1-2 分钟（AI 生成需要时间）
- **不应该**出现超时错误

##### 步骤 5：查看结果
- 应该显示生成的旅行计划
- 包含每日行程安排
- 包含景点推荐
- 包含预算估算

## 🔍 调试步骤

如果测试失败，按以下步骤调试：

### 1. 检查后端日志

```powershell
# 实时查看 AIService 日志
docker logs -f go-nomads-ai-service
```

**期望看到的日志**：

```
[时间] INF 用户上下文已设置 - UserId: xxx, Email: xxx, Role: user
[时间] INF 🗺️ 开始生成旅行计划 - 城市: xxx, 天数: x, 预算: xxx
[时间] INF 🗺️ 开始生成旅行计划，城市: xxx, 用户ID: xxx
... (等待 1-2 分钟)
[时间] INF ✅ 旅行计划生成成功
[时间] INF HTTP POST /api/v1/ai/travel-plan responded 200 in xxxxx.xx ms
```

**如果看到错误**：

#### 错误 1：401 未授权
```
[时间] WRN ⚠️ 用户 xxx 未认证
```
**解决方案**：
- 检查 Flutter 是否正确发送 token
- 检查 ChatController.GetUserId() 是否使用 UserContext
- 重新部署后端

#### 错误 2：超时错误
```
[时间] ERR ❌ 生成旅行计划失败
System.Net.Http.HttpIOException: The response ended prematurely
```
**解决方案**：
- 检查 HttpClient.Timeout 是否为 3 分钟
- 检查网络连接
- 检查 DeepSeek API Key 是否有效

#### 错误 3：DeepSeek API 错误
```
[时间] ERR DeepSeek API error: xxx
```
**解决方案**：
- 检查 appsettings.Development.json 中的 DeepSeek:ApiKey
- 检查 DeepSeek 账号余额
- 检查网络是否能访问 api.deepseek.com

### 2. 检查 Flutter 日志

在 VS Code 的 Debug Console 中查看：

**期望看到**：
```
I/flutter: 📤 POST http://10.0.2.2:5000/api/v1/ai/travel-plan
I/flutter: Headers: {Authorization: Bearer xxx, X-User-Id: xxx}
I/flutter: Data: {cityId: xxx, cityName: xxx, duration: 7, ...}
... (等待)
I/flutter: ✅ AI旅行计划生成成功
I/flutter: Response: {...}
```

**如果看到错误**：

#### 错误 1：401 错误
```
I/flutter: ❌ ERROR[401] => http://10.0.2.2:5000/api/v1/ai/travel-plan
I/flutter: Response: {success: false, message: 用户未认证，请先登录}
```
**解决方案**：
- 确认已登录
- 检查 token 是否存在
- 重新登录

#### 错误 2：超时错误
```
I/flutter: ❌ ERROR[null] => http://10.0.2.2:5000/api/v1/ai/travel-plan
I/flutter: Message: The request took longer than 0:03:00 to receive data
```
**解决方案**：
- 检查后端日志，看是否有错误
- 检查网络连接
- 如果持续超时，可能是 DeepSeek API 问题，稍后重试

### 3. 检查网络连接

```powershell
# 测试 AIService 健康检查
Invoke-RestMethod http://localhost:8009/health

# 测试通过 Gateway 访问
Invoke-RestMethod http://localhost:5000/api/v1/ai/health
```

**预期结果**：
- 都应返回 200 OK
- 响应包含健康状态信息

## 📊 性能预期

### 正常情况

- **请求时间**：60 秒 - 120 秒（1-2 分钟）
- **成功率**：> 95%（网络正常时）
- **响应大小**：通常 5-20 KB（JSON）

### 影响因素

1. **DeepSeek API 负载**：
   - 高峰期可能响应较慢
   - 通常在可接受范围内

2. **网络状况**：
   - 网络抖动可能导致响应延迟
   - 3 分钟超时提供足够缓冲

3. **提示复杂度**：
   - 更详细的兴趣列表 → 更长生成时间
   - 更多天数 → 更长生成时间

## ✅ 成功标志

测试成功的标志：

1. ✅ 用户上下文正确识别（UserContext 日志）
2. ✅ 不再出现 401 认证错误
3. ✅ 不再出现 60 秒超时错误
4. ✅ AI 成功生成旅行计划
5. ✅ Flutter App 显示完整的计划内容
6. ✅ 用户体验流畅（有加载提示）

## 🎯 下一步优化建议

虽然当前修复已解决核心问题，但可以考虑以下优化：

### 1. 异步生成模式

```
POST /api/v1/ai/travel-plan/request
→ 立即返回 { taskId: "xxx" }

GET /api/v1/ai/travel-plan/status/{taskId}
→ 轮询检查状态

GET /api/v1/ai/travel-plan/result/{taskId}
→ 获取生成结果
```

**优点**：
- 用户无需等待
- 可以关闭页面后继续生成
- 更好的用户体验

### 2. WebSocket 实时推送

```dart
// 连接 WebSocket
socket.on('travel-plan-progress', (data) {
  // 更新进度条：生成景点推荐... 30%
});

socket.on('travel-plan-complete', (data) {
  // 显示完整计划
});
```

**优点**：
- 实时进度反馈
- 不需要轮询
- 更好的交互体验

### 3. 缓存机制

```csharp
// 检查缓存
var cacheKey = $"travel-plan:{cityId}:{duration}:{budget}:{style}";
var cached = await _cache.GetAsync(cacheKey);
if (cached != null) return cached;

// 生成新计划
var plan = await GenerateAsync(...);
await _cache.SetAsync(cacheKey, plan, TimeSpan.FromHours(24));
```

**优点**：
- 相同请求立即返回
- 减少 DeepSeek API 调用
- 降低成本

### 4. 重试机制

```csharp
var result = await Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
    .ExecuteAsync(() => CallDeepSeekAsync(...));
```

**优点**：
- 网络抖动时自动重试
- 提高成功率
- 更好的容错性

---

**文档创建日期**: 2025-01-29  
**测试环境**: 本地开发环境  
**状态**: ✅ 可以开始测试

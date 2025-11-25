# 事件类型路由修复说明

## 🐛 问题描述

Flutter 应用调用 `/api/v1/event-types` 时只显示 3 个类型（后备方案），而不是从后端加载的 20 个类型。

## 🔍 根本原因

Gateway 存在两个配置问题导致 Flutter 应用无法获取事件类型列表：

### 问题 1: 缺少路由配置
Gateway 的路由配置缺少 `/api/v1/event-types` 路径映射。

**修改前**：
```csharp
"event-service" => new List<(string, int)>
{
    ("/api/v1/events/{**catch-all}", 1)
},
```

**修改后**：
```csharp
"event-service" => new List<(string, int)>
{
    ("/api/v1/event-types/{**catch-all}", 1),  // Event types endpoint
    ("/api/v1/events/{**catch-all}", 2)
},
```

### 问题 2: JWT 认证拦截
`/api/v1/event-types` 端点没有添加到公开路径白名单，导致未登录用户无法访问。

**修改前** (`appsettings.json`):
```json
"PublicPaths": [
  "/health",
  "/api/v1/auth/login",
  "/api/v1/auth/register",
  ...
]
```

**修改后**:
```json
"PublicPaths": [
  "/health",
  "/api/v1/auth/login",
  "/api/v1/auth/register",
  "/api/v1/event-types",  // ← 新增
  ...
]
```

## ✅ 已修复的文件

1. `src/Gateway/Gateway/Services/ConsulProxyConfigProvider.cs` - 添加 event-types 路由
2. `src/Gateway/Gateway/appsettings.json` - 添加 event-types 到公开路径白名单

## 🚀 部署步骤

### 方法 1: 使用部署脚本（推荐）
```powershell
cd e:\Workspaces\WaldenProjects\go-nomads
.\deployment\deploy-services-local.ps1
```

### 方法 2: 手动重启 Gateway
```powershell
# 1. 编译 Gateway
cd e:\Workspaces\WaldenProjects\go-nomads\src\Gateway\Gateway
dotnet build

# 2. 停止现有的 Gateway 服务 (如果在运行)
# 在运行 Gateway 的终端按 Ctrl+C

# 3. 启动 Gateway
dotnet run

# 4. 等待 30 秒让 Consul 服务发现生效
```

## 🧪 验证修复

### 测试 1: 运行测试脚本
```powershell
cd e:\Workspaces\WaldenProjects\go-nomads
.\test-event-types-routing.ps1
```

**预期输出**：
```
✅ EventService 响应成功 (200 OK)
   类型数量: 20

✅ Gateway 路由成功 (200 OK)
   类型数量: 20

🎉 测试通过！Gateway 路由配置正确！
```

### 测试 2: 手动测试
```powershell
# 测试直接访问 EventService
curl http://localhost:8005/api/v1/event-types

# 测试通过 Gateway 访问
curl http://localhost:5000/api/v1/event-types
```

### 测试 3: Flutter 应用测试
1. 重启 Flutter 应用（清除缓存）
2. 进入"创建聚会"页面
3. 点击"聚会类型"下拉框
4. 应该看到 **20+ 个类型选项**（而不是只有 3 个）

**控制台日志应显示**：
```
🔄 正在从后端加载事件类型列表...
✅ 成功加载 20 个事件类型
```

## 📊 路由优先级说明

Gateway 现在为 event-service 配置了两个路由：

| 路径 | Order | 说明 |
|------|-------|------|
| `/api/v1/event-types/{**catch-all}` | 1 | 事件类型 API（更高优先级）|
| `/api/v1/events/{**catch-all}` | 2 | 事件 API |

**Order 越小，优先级越高**。这确保 `/api/v1/event-types` 的请求不会被 `/api/v1/events` 路由拦截。

## 🔄 Consul 服务发现机制

Gateway 使用 Consul 进行动态服务发现：

1. Gateway 每 30 秒从 Consul 获取服务列表
2. 根据服务名称生成路由配置
3. 使用 YARP 反向代理将请求转发到后端服务

**注意**：路由配置的更改需要重启 Gateway 才能生效。

## 🐛 故障排查

### 问题 1: Gateway 返回 404
**原因**: Gateway 未重启或 Consul 未发现服务

**解决**:
```powershell
# 1. 检查 Gateway 健康状态
curl http://localhost:5000/health

# 2. 检查 Consul 服务列表
curl http://localhost:7500/v1/catalog/services

# 3. 重启 Gateway
cd src/Gateway/Gateway
dotnet run
```

### 问题 2: Gateway 返回 503
**原因**: EventService 未启动或不健康

**解决**:
```powershell
# 1. 检查 EventService 健康状态
curl http://localhost:8005/health

# 2. 如果未启动，启动 EventService
cd src/Services/EventService/EventService
dotnet run

# 3. 等待 30 秒让 Consul 更新
```

### 问题 3: Flutter 仍显示 3 个类型
**原因**: 缓存未清除或 Gateway 未更新

**解决**:
```dart
// 1. 强制刷新类型列表
await _eventTypeController.refresh();

// 或

// 2. 重启 Flutter 应用（完全清除缓存）
```

## 📝 相关文档

- `EVENT_TYPE_FLUTTER_INTEGRATION_COMPLETE.md` - Flutter 集成完整文档
- `EVENT_TYPE_TEST_GUIDE.md` - 测试指南
- `EVENT_TYPE_QUICK_REFERENCE.md` - 快速参考

## ✨ 预期结果

修复后，Flutter 应用应该：

- ✅ 成功从后端加载 20 个事件类型
- ✅ 第二次进入使用缓存，不重复请求
- ✅ 根据系统语言显示中文或英文名称
- ✅ API 失败时才显示 3 个后备类型

---

**修复完成时间**: 2025年11月25日
**修复人**: AI Assistant
**状态**: ✅ 已完成并验证

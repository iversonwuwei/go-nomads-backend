# Go Nomads Backend - Aspire 迁移指南

## 迁移概述

将现有的 Dapr + Consul + docker-compose 微服务架构迁移到 .NET Aspire 统一编排。

### 替换矩阵

| 现有技术 | Aspire 替换 | 状态 |
|---------|------------|------|
| docker-compose 编排 | Aspire AppHost | ✅ 阶段1完成 |
| 手动 OpenTelemetry | Aspire ServiceDefaults | ✅ 阶段2完成 |
| 手动 HealthChecks | Aspire ServiceDefaults | ✅ 阶段2完成 |
| Consul 服务注册 | Aspire 自动服务发现 | ✅ 阶段3完成 |
| Consul 动态路由 (Gateway) | AspireProxyConfigProvider | ✅ 阶段3完成 |
| Dapr Service Invocation | HttpClient + 服务发现 | ✅ 阶段4完成 |
| Dapr State Store | IDistributedCache (Redis) | ✅ 阶段4完成 |
| Dapr Sidecar 容器 | 移除 | ✅ 阶段4完成 |
| YARP 路由配置 (硬编码 C#) | yarp.json + LoadFromConfig + ServiceDiscovery.Yarp | ✅ 阶段5完成 |
| docker-compose + Dapr/Consul 清理 | Aspire AppHost 统一编排 | ✅ 阶段6完成 |

---

## 阶段1: 基础设施搭建 ✅ 已完成

### 已创建文件
- `src/GoNomads.AppHost/` - Aspire 编排主机
- `src/GoNomads.ServiceDefaults/` - 共享服务默认配置

### 验证
```bash
dotnet build src/GoNomads.AppHost/GoNomads.AppHost.csproj
# 结果: 0 个错误
```

---

## 阶段2: ServiceDefaults 集成 ✅ 已完成

### 完成内容
- 所有 13 个项目已引用 ServiceDefaults
- 替换 `AddGoNomadsObservability()` / `AddGoNomadsLogging()` → `builder.AddServiceDefaults()`
- 替换手动 `/health` 端点 → `app.MapDefaultEndpoints()`
- 构建验证: 0 个错误

### 目标
- 各服务渐进引用 `GoNomads.ServiceDefaults`
- 替换 `Shared` 中的 `AddGoNomadsObservability()` 
- 保留现有业务逻辑完全不变

### 每个服务的改动步骤
1. csproj 添加 `<ProjectReference Include="..\..\GoNomads.ServiceDefaults\GoNomads.ServiceDefaults.csproj" />`
2. Program.cs 添加 `builder.AddServiceDefaults();`（替换 `builder.Services.AddGoNomadsObservability(...)`）
3. Program.cs 添加 `app.MapDefaultEndpoints();`（替换手动 `app.MapGet("/health", ...)`）
4. 验证服务正常启动
5. 验证健康检查端点 `/health` 和 `/alive`

### 推荐迁移顺序（从简单到复杂）
1. CacheService (最简单，无外部依赖)
2. InnovationService
3. ProductService
4. DocumentService
5. AccommodationService
6. CoworkingService
7. UserService
8. CityService
9. EventService
10. SearchService
11. MessageService (最复杂)
12. Gateway (最后处理)

---

## 阶段3: 移除 Consul ✅ 已完成

### 完成内容
- 创建 `AspireProxyConfigProvider` 替代 `ConsulProxyConfigProvider`
  - 服务 URL 解析优先级: Aspire 注入 > ServiceUrls 配置 > 默认 URL
  - 完整保留所有路由映射 (11 个服务, 65+ 路由规则)
- Gateway/Program.cs: 移除 IConsulClient 注册，使用 AspireProxyConfigProvider
- 11 个服务: 移除 `await app.RegisterWithConsulAsync()`
- MessageService: 移除 70+ 行手动 Consul 注册/注销代码
- 移除 Consul NuGet 包 (Gateway, AIService, MessageService)
- 删除 `ConsulServiceRegistration.cs` 和 `ConsulProxyConfigProvider.cs`
- 构建验证: 0 个错误

### 目标
- Gateway 的路由发现从 Consul 改为 Aspire 服务发现
- 各服务移除 `app.RegisterWithConsulAsync()` 
- 移除 Consul 容器

### 关键改动
1. 移除 `Shared/Extensions/ConsulServiceRegistration.cs`
2. Gateway: 移除 `ConsulProxyConfigProvider`，改用 Aspire 注入的服务地址
3. 各服务 Program.cs: 移除 `await app.RegisterWithConsulAsync()`
4. docker-compose-infras: 移除 consul 服务

---

## 阶段4: 移除 Dapr ✅ 已完成

### 完成内容
- **18 个服务客户端类**重构: `DaprClient` → `HttpClient`，保留所有 try-catch 和日志
- **11 个控制器/服务类**重构: `DaprClient` → `IHttpClientFactory`
- Dapr State Store (`GetStateAsync`/`SaveStateAsync`) → `IDistributedCache` (Redis)
- Dapr Pub/Sub (`PublishEventAsync`) → MassTransit `IPublishEndpoint`
- 所有 12 个 Program.cs: 移除 `AddDaprClient()` + `.AddDapr()`，添加 typed/named `AddHttpClient` 注册
- InnovationService: 移除 `UseCloudEvents()` + `MapSubscribeHandler()`
- 13 个 .csproj: 移除 `Dapr.AspNetCore` / `Dapr.Client` 包引用
- 删除 `DaprClientExtensions.cs` (Shared + Gateway)
- 构建验证: 0 个错误

### 跨服务调用映射 (65+ 调用点)

| 调用方 | 被调用方 | 方法 |
|--------|---------|------|
| CityService | UserService | 获取用户信息 |
| CityService | AIService | AI 内容生成 |
| CityService | CacheService | 缓存查询 |
| SearchService | CityService | 索引同步 |
| SearchService | CoworkingService | 索引同步 |
| EventService | CityService | 城市信息 |
| EventService | UserService | 用户信息 |
| MessageService | UserService | 用户信息 |

---

## 阶段5: YARP 集成到 Aspire ✅ 已完成

### 完成内容
- **路由配置迁移**: 硬编码 C# (`AspireProxyConfigProvider`) → 声明式 JSON (`yarp.json`)
  - 11 个服务集群, 30+ 路由规则, 全部配置化
  - 支持热重载 (`reloadOnChange: true`)
- **YARP 升级**: v2.1.0 → v2.3.0 (支持 ServiceDiscovery 集成)
- **新增 `Microsoft.Extensions.ServiceDiscovery.Yarp` v9.2.1**
  - YARP 目标地址通过 Aspire 服务发现自动解析
  - `AddServiceDiscoveryDestinationResolver()` 注册到 YARP 管线
- **Aspire 端点自动覆盖**: 运行在 Aspire 下时, `services:{name}:http:0` 自动覆盖默认地址
- **保留全部自定义中间件**: JWT 认证拦截、限流、请求转换、WebSocket 代理
- **移除 `Aspire.Hosting.Yarp`** 包 (AppHost 不需要, 使用项目内集成)
- 删除 `AspireProxyConfigProvider.cs` (260+ 行, 被 yarp.json 替代)
- 构建验证: 0 个错误

### 关键文件
- `src/Gateway/Gateway/yarp.json` - YARP 路由/集群声明式配置
- `src/Gateway/Gateway/Program.cs` - LoadFromConfig + ServiceDiscovery
- `src/Gateway/Gateway/Gateway.csproj` - 新增 ServiceDiscovery.Yarp, 升级 YARP

---

## 阶段6: 基础设施统一编排 + 遗留清理 ✅ 已完成

### 完成内容
- **删除整个 `dapr/` 目录** (Dapr 组件 YAML 配置)
- **删除 `deployment/dapr/`** (8 个 Dapr 组件/配置文件)
- **删除 `deployment/consul/`** (Consul Agent 配置)
- **删除 3 个 docker-compose 文件** (包含完整 Dapr sidecar 编排):
  - `docker-compose.yml` (678 行, 13 个 Dapr sidecar + placement)
  - `docker-compose-swr.yml` (621 行, SWR 版本)
  - `docker-compose-services-swr.yml` (602 行, 纯服务层)
- **清理 docker-compose-infras.yml**: 移除 Consul 服务 + consul_data volume
- **清理 docker-compose-infras-swr.yml**: 同上
- **清理 deployment 脚本**: 移除 3 个脚本中的 Dapr/Consul 引用
- 构建验证: 0 个错误

### 保留的 docker-compose 文件
| 文件 | 用途 |
|------|------|
| `docker-compose-infras.yml` | 纯基础设施 (Redis/RabbitMQ/ES/Jaeger/Prometheus/Grafana/Nginx) |
| `docker-compose-infras-swr.yml` | SWR 版基础设施 |
| `docker-compose-observability.yml` | 可观测性栈 (Jaeger/Prometheus/Grafana/OTel) |
| `docker-compose.messageservice.yml` | MessageService 独立栈 |

### 注意事项
- `k8s/` 目录中的 Kubernetes manifests 仍包含 Consul 引用，待 K8s 部署方案更新时一并清理

---

## 运行方式

### 开发环境（Aspire 模式）- 推荐
```bash
cd src/GoNomads.AppHost
dotnet run
# 打开 Aspire Dashboard: https://localhost:18888
# 所有服务 + Redis/RabbitMQ/Elasticsearch 由 Aspire 统一编排
```

### 基础设施独立启动（可选，用于非 Aspire 场景）
```bash
docker compose -f docker-compose-infras.yml up -d
```

---

## 迁移完成总结

全部 6 个阶段已完成。现有架构:
- **编排**: .NET Aspire AppHost
- **服务发现**: Aspire ServiceDiscovery (自动)
- **服务间通信**: HttpClient + Aspire 服务发现 + 弹性处理
- **异步消息**: MassTransit + RabbitMQ
- **API 网关**: YARP (yarp.json + ServiceDiscovery.Yarp + 自定义中间件)
- **可观测性**: OpenTelemetry (Aspire ServiceDefaults)
- **缓存**: Redis (IDistributedCache)
- **搜索**: Elasticsearch
- **数据库**: Supabase PostgreSQL (外部服务)

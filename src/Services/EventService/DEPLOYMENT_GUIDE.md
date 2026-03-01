# EventService 部署指南

## ✅ 已完成的配置

### 1. EventService 基础配置

**Program.cs**:
- ✅ 配置 Serilog 日志
- ✅ 添加 Supabase 客户端
- ✅ 配置 DaprClient 使用 **gRPC 端口 50001**
- ✅ 添加 Scalar API 文档
- ✅ 添加 Prometheus 指标

**appsettings.json**:
- ✅ Supabase 连接配置
- ✅ Dapr gRPC 配置 (端口 50001)
- ✅ Serilog 日志配置

**EventService.csproj**:
- ✅ 添加必要的 NuGet 包：
  - Dapr.AspNetCore (1.16.0)
  - postgrest-csharp (3.5.1)
  - supabase-csharp (0.16.2)
  - prometheus-net.AspNetCore (8.2.1)
  - Serilog.AspNetCore (9.0.0)
  - Scalar.AspNetCore (1.2.42)

### 2. Event 数据模型

**Models/Event.cs**:
- ✅ 添加自定义预算字段：
  - `CustomBudget` (decimal?) - 用户自定义预算金额
  - `CustomBudgetCurrency` (string?) - 预算币种

**数据库迁移**:
- ✅ 创建了 SQL 迁移脚本：`Database/add-custom-budget-fields.sql`
- ⏳ 需要在 Supabase 执行 SQL 脚本

### 3. 部署脚本集成

**deploy-services-local.sh**:
- ✅ 添加 EventService 部署配置
- ✅ 端口分配：
  - 应用端口: 8005
  - Dapr HTTP 端口: 3505
  - Dapr gRPC 端口: 50001 (共享)
- ✅ Container Sidecar 模式

## 🚀 部署信息

### 服务访问地址

```
EventService:        http://localhost:8005
Health Check:        http://localhost:8005/health
Scalar API Docs:     http://localhost:8005/scalar/v1
OpenAPI JSON:        http://localhost:8005/openapi/v1.json
Prometheus Metrics:  http://localhost:8005/metrics
```

### Dapr 配置

```yaml
App ID: event-service
HTTP Port: 3505 (Dapr sidecar)
gRPC Port: 50001 (Dapr sidecar)
Protocol: gRPC (强制使用)
```

## 🔧 Dapr gRPC 配置详情

### Program.cs 配置

```csharp
builder.Services.AddDaprClient(daprClientBuilder =>
{
    // 使用 gRPC 端点（默认端口 50001）
    var daprGrpcPort = builder.Configuration.GetValue<int>("Dapr:GrpcPort", 50001);
    var daprGrpcEndpoint = $"http://localhost:{daprGrpcPort}";
    
    daprClientBuilder.UseGrpcEndpoint(daprGrpcEndpoint);
    
    var logger = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole())
        .CreateLogger("DaprSetup");
    logger.LogInformation("🚀 Dapr Client 配置使用 gRPC: {Endpoint}", daprGrpcEndpoint);
});
```

### appsettings.json 配置

```json
"Dapr": {
  "GrpcPort": 50001,
  "HttpPort": 3505,
  "UseGrpc": true
}
```

## 📋 验证清单

### 1. 服务健康检查

```bash
curl http://localhost:8005/health
```

预期输出:
```json
{
  "status": "healthy",
  "service": "EventService",
  "timestamp": "2025-10-23T06:02:40.6563652Z"
}
```

### 2. Scalar API 文档访问

访问 http://localhost:8005/scalar/v1

预期:
- ✅ 页面正常加载
- ✅ 显示 "Event Service API" 标题
- ✅ 服务器 URL 显示为 http://localhost:8005

### 3. Dapr Sidecar 验证

```bash
docker logs go-nomads-event-service-dapr 2>&1 | grep "gRPC"
```

预期:
- ✅ 显示 "gRPC server listening on TCP address: :50001"
- ✅ 显示 "API gRPC server is running on port 50001"

### 5. Prometheus 指标验证

```bash
curl http://localhost:8005/metrics
```

预期:
- ✅ 返回 Prometheus 格式的指标数据

## 🔄 部署命令

### 完整部署所有服务

```bash
cd deployment
./deploy-services-local.sh
```

### 单独部署 EventService

```bash
cd deployment
./deploy-services-local.sh --service event-service
```

### 查看 EventService 日志

```bash
docker logs go-nomads-event-service
docker logs go-nomads-event-service-dapr
```

### 重启 EventService

```bash
docker restart go-nomads-event-service go-nomads-event-service-dapr
```

## 📊 与其他服务的对比

| 服务 | 端口 | Dapr HTTP | Dapr gRPC | Scalar | Prometheus |
|-----|------|----------|-----------|---------|-----------|
| Gateway | 5000 | 3500 | 50001 | ✅ | ✅ |
| UserService | 5001 | 3502 | 50001 | ✅ | ✅ |
| ProductService | 5002 | 3501 | 50001 | ✅ | ❌ |
| DocumentService | 5003 | 3503 | 50001 | ✅ | ❌ |
| CityService | 8002 | 3504 | 50001 | ✅ | ✅ |
| **EventService** | **8005** | **3505** | **50001** | **✅** | **✅** |

## 🎯 下一步

1. ⏳ 执行数据库迁移脚本添加自定义预算字段
2. ⏳ 创建 Event Controller 实现 CRUD 操作
3. ⏳ 创建 Repository 层实现数据访问
4. ⏳ 实现活动参与者管理功能
5. ⏳ 添加单元测试
6. ⏳ 集成到 Gateway 的 Home Feed

## 🐛 故障排查

### EventService 无法启动

```bash
# 检查日志
docker logs go-nomads-event-service

# 检查容器状态
docker ps --filter "name=event-service"

# 检查端口占用
lsof -i :8005
```

### Dapr Sidecar 连接失败

```bash
# 检查 Dapr sidecar 日志
docker logs go-nomads-event-service-dapr

# 验证网络配置
docker inspect go-nomads-event-service | grep -A 10 "NetworkMode"
```

## 📝 重要说明

1. **gRPC 强制使用**: EventService 的 Dapr 客户端配置为强制使用 gRPC 协议
2. **Container Sidecar 模式**: EventService 和 Dapr sidecar 共享网络命名空间
3. **端口映射**: 应用端口 8005 映射到容器内部的 8080
4. **日志输出**: 使用 Serilog 输出到控制台和文件（logs/eventservice-*.txt）

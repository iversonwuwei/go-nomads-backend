# AIService 本地部署配置完成

## 概述
AIService 已成功添加到本地部署脚本中，并完成了 Docker 开发环境的配置同步。

## 完成的工作

### 1. 配置文件同步 ✅
- **appsettings.Development.json**: 已完全同步，适配 Docker 容器环境
  - 数据库连接：从 Supabase 切换到本地 postgres 容器
  - 服务发现：Consul 地址从 localhost 改为 consul 服务名
  - 消息代理：添加 RabbitMQ 配置，使用 rabbitmq 服务名
  - 日志配置：优化为开发环境，包含 Semantic Kernel 调试日志

### 2. 本地部署脚本更新 ✅

#### PowerShell 脚本 (deploy-services-local.ps1)
- 添加 ai-service 到服务列表：
  - 名称: `ai-service`
  - 端口: `8009`
  - Dapr 端口: `3509`
  - 应用 ID: `ai-service`
  - 路径: `src/Services/AIService/AIService`
  - DLL: `AIService.dll`
  - 容器: `go-nomads-ai-service`
- 更新清理脚本，包含 ai-service 容器清理

#### Bash 脚本 (deploy-services-local.sh)
- 添加 AIService 部署调用：
  ```bash
  deploy_service_local \
      "ai-service" \
      "src/Services/AIService/AIService" \
      "8009" \
      "AIService.dll" \
      "3509" \
      "ai-service"
  ```
- 更新部署摘要，显示 AI Service 访问地址

### 3. Docker Compose 增强 ✅
- 添加 Consul 服务定义：
  - 镜像: `consul:1.15-alpine`
  - 端口: `8500` (HTTP), `8600` (DNS)
  - 配置文件: `./deployment/consul/consul-local.json`
  - 数据卷: `consul_data`
- 修复 ai-service 依赖问题

## 服务配置详情

### AIService 配置
- **端口**: 8009 (HTTP API)
- **Dapr 端口**: 3509 (sidecar)
- **健康检查**: `/health` 和 `/health/ai`
- **API 文档**: `/scalar/v1`
- **Prometheus 指标**: `/metrics`

### 环境变量
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://+:8009`
- `ConnectionStrings__SupabaseDb`: 本地 postgres 容器
- `ConnectionStrings__Redis`: redis:6379
- `Consul__Address`: http://consul:8500
- `MessageBroker__Host`: rabbitmq
- `QIANWEN_API_KEY`: 需要设置千问 API 密钥

## 使用方法

### 1. 启动基础设施服务
```powershell
# PowerShell
.\deployment\deploy-infrastructure-local.ps1

# Bash
bash ./deployment/deploy-infrastructure-local.sh
```

### 2. 部署所有服务（包括 AIService）
```powershell
# PowerShell
.\deployment\deploy-services-local.ps1

# Bash  
bash ./deployment/deploy-services-local.sh
```

### 3. 仅部署 AIService（如果需要）
```powershell
# 构建并发布 AIService
cd src\Services\AIService\AIService
dotnet publish -c Release

# 单独运行容器（需要先启动基础设施）
docker run -d --name go-nomads-ai-service \
  --network go-nomads-network \
  -p 8009:8009 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__SupabaseDb="Host=postgres;Port=5432;Database=aiservice_db;Username=postgres;Password=postgres" \
  -e Consul__Address=http://consul:8500 \
  go-nomads-ai-service
```

## 访问地址

### 服务端点
- **AIService API**: http://localhost:8009
- **健康检查**: http://localhost:8009/health
- **AI 健康检查**: http://localhost:8009/health/ai  
- **API 文档**: http://localhost:8009/scalar/v1
- **Prometheus 指标**: http://localhost:8009/metrics

### 基础设施
- **Consul UI**: http://localhost:8500
- **RabbitMQ 管理**: http://localhost:15672 (admin/admin)
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090

## 验证部署

### 1. 检查服务状态
```bash
# 检查容器运行状态
docker ps | grep ai-service

# 检查健康状态
curl http://localhost:8009/health
```

### 2. 验证 Consul 注册
- 访问 http://localhost:8500
- 确认 ai-service 已注册并健康

### 3. 测试 API
```bash
# 测试聊天接口（需要设置 QIANWEN_API_KEY）
curl -X POST http://localhost:8009/api/v1/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello", "conversationId": "test"}'
```

## 注意事项

1. **API 密钥**: 确保设置 `QIANWEN_API_KEY` 环境变量
2. **数据库**: 本地使用 postgres 容器，生产环境使用 Supabase
3. **服务发现**: 开发环境使用本地 Consul，生产环境根据需要配置
4. **日志**: 开发环境日志包含详细的 Semantic Kernel 调试信息
5. **依赖顺序**: AIService 依赖 postgres, redis, rabbitmq, consul 服务

## 下一步

1. 设置实际的千问 API 密钥
2. 测试完整的聊天功能
3. 验证与其他服务的集成
4. 配置生产环境部署

---
*最后更新: 2025年10月28日*
*配置状态: ✅ 完成*
# 部署脚本更新总结

## ✅ 已更新的文件

### 1. `deployment/deploy-services-local.sh`
- **用途**: 本地构建后部署到容器
- **变更**: Gateway 使用 `Production` 环境，其他服务使用 `Development` 环境
- **原因**: Gateway 需要访问容器化的 Consul（`go-nomads-consul:8500`）

### 2. `deployment/deploy-services.sh`
- **用途**: 使用 Dockerfile 构建镜像并部署
- **变更**: 与 deploy-services-local.sh 相同的环境配置逻辑
- **原因**: 保持两种部署方式的一致性

## 🔧 核心变更

### 之前（所有服务）
```bash
-e ASPNETCORE_ENVIRONMENT=Development
```

### 现在（区分 Gateway 和其他服务）
```bash
# Gateway
-e ASPNETCORE_ENVIRONMENT=Production  # 使用 appsettings.json

# 其他服务
-e ASPNETCORE_ENVIRONMENT=Development  # 使用 appsettings.Development.json
```

## 📋 配置文件说明

### Gateway 配置

#### appsettings.json (Production) ✅ 容器使用
```json
{
  "Consul": {
    "Address": "http://go-nomads-consul:8500"  // 容器名，在 Docker 网络中可解析
  }
}
```

#### appsettings.Development.json (Development) ❌ 容器中不适用
```json
{
  "Consul": {
    "Address": "http://localhost:8500"  // 容器内无法访问宿主机 localhost
  }
}
```

## 🚀 使用方法

### 方式 1: 本地构建 + 容器部署（推荐用于开发）

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment

# 1. 部署基础设施
./deploy-infrastructure-local.sh

# 2. 部署服务（自动使用正确的环境配置）
./deploy-services-local.sh
```

**优点**:
- 构建速度快（本地构建）
- 可以快速测试代码变更
- 不需要重新构建 Docker 镜像

### 方式 2: Docker 镜像构建 + 部署（推荐用于生产）

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment

# 1. 部署基础设施
./deploy-infrastructure.sh

# 2. 构建并部署服务
./deploy-services.sh
```

**优点**:
- 完整的容器化
- 更接近生产环境
- 镜像可以推送到仓库

## ✅ 验证步骤

### 1. 检查 Gateway 环境配置

```bash
docker inspect go-nomads-gateway | grep ASPNETCORE_ENVIRONMENT
```

**预期输出**:
```
"ASPNETCORE_ENVIRONMENT=Production"
```

### 2. 验证 Consul 连接

```bash
docker logs go-nomads-gateway 2>&1 | grep -E "Consul|Loading|Loaded"
```

**预期输出**:
```
info: Loading service configuration from Consul...
info: Loaded 10 routes from Consul
```

**如果看到错误**:
```
❌ Failed to load configuration from Consul
❌ Connection refused (localhost:8500)
```
说明环境配置不正确。

### 3. 测试 Gateway 路由

```bash
# 健康检查
curl http://localhost:5000/health
# 预期: {"status":"healthy","timestamp":"..."}

# 测试路由
curl http://localhost:5000/api/users
# 预期: 401 Unauthorized（需要认证，说明路由工作）
```

### 4. 测试限流功能

```bash
# 测试登录限流（5次/分钟）
for i in {1..7}; do
  echo -n "请求 $i: "
  curl -s -o /dev/null -w "%{http_code}\n" \
    -X POST http://localhost:5000/api/test/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com"}'
  sleep 0.5
done
```

**预期输出**:
```
请求 1: 200
请求 2: 200
请求 3: 200
请求 4: 200
请求 5: 200
请求 6: 429  ← 限流触发
请求 7: 429  ← 限流触发
```

## 📊 服务配置对比

| 服务 | 脚本环境变量 | 实际 Consul 地址 | 说明 |
|------|------------|----------------|------|
| **Gateway** | `Production` | `go-nomads-consul:8500` | ✅ 正确 - 使用容器名 |
| UserService | `Development` | `go-nomads-consul:8500` | ✅ 正确 - 脚本覆盖 |
| ProductService | `Development` | `go-nomads-consul:8500` | ✅ 正确 - 脚本覆盖 |
| DocumentService | `Development` | `go-nomads-consul:8500` | ✅ 正确 - 脚本覆盖 |

**注意**: 所有服务在脚本中都会被设置 `Consul__Address="http://go-nomads-consul:8500"`，但 Gateway 需要特别使用 Production 环境以避免 Development 配置覆盖。

## 🔍 故障排查

### 问题 1: Gateway 返回 404

**症状**:
```bash
curl http://localhost:5000/api/users
# 返回: 404 Not Found
```

**原因**: Gateway 未能从 Consul 加载路由配置

**解决**:
```bash
# 1. 检查环境配置
docker inspect go-nomads-gateway | grep ASPNETCORE_ENVIRONMENT

# 2. 如果是 Development，重新部署
docker stop go-nomads-gateway && docker rm go-nomads-gateway
cd deployment
./deploy-services-local.sh  # 或 ./deploy-services.sh
```

### 问题 2: 限流未生效

**症状**: 连续多次请求都返回 200

**可能原因**:
1. Docker 镜像未包含最新限流代码
2. 测试端点路径不正确

**解决**:
```bash
# 1. 重新构建 Gateway
cd src/Gateway/Gateway
dotnet publish -c Release

# 2. 重新部署
cd ../../deployment
./deploy-services-local.sh

# 3. 使用正确的测试端点
curl -X POST http://localhost:5000/api/test/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com"}'
```

### 问题 3: 其他服务无法连接 Consul

**症状**: UserService/ProductService 日志显示 Consul 连接失败

**原因**: 环境变量覆盖不正确

**解决**: 检查脚本中的 Consul 地址设置
```bash
# 应该包含这行：
-e Consul__Address="http://go-nomads-consul:8500"
```

## 📝 相关文档

- [DEPLOYMENT_UPDATE.md](../DEPLOYMENT_UPDATE.md) - 详细部署更新说明
- [RATE_LIMIT_STATUS.md](../RATE_LIMIT_STATUS.md) - 限流功能状态
- [GATEWAY_ACCESS_FIXED.md](../GATEWAY_ACCESS_FIXED.md) - Gateway 访问问题修复

## 🎯 下一步

1. ✅ 使用更新后的脚本部署服务
2. ✅ 验证 Gateway 能否正确加载路由
3. ✅ 测试限流功能是否工作
4. 📝 根据测试结果更新文档
5. 🚀 准备生产环境部署

---

**更新时间**: 2025-10-20 23:30 UTC
**版本**: 1.0.0
**状态**: ✅ 已测试验证

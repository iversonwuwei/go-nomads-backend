# 部署脚本更新说明

## 📋 更新内容

### `deployment/deploy-services-local.sh`

**变更原因**: Gateway 需要在 Docker 容器中使用正确的 Consul 地址

**具体修改**:

```bash
# 之前：所有服务都使用 Development 环境
-e ASPNETCORE_ENVIRONMENT=Development

# 现在：Gateway 使用 Production 环境，其他服务使用 Development
if [[ "$service_name" == "gateway" ]]; then
    # Gateway 使用生产配置（appsettings.json 中的 go-nomads-consul:8500）
    env_config+=("-e" "ASPNETCORE_ENVIRONMENT=Production")
else
    # 其他服务使用 Development 环境
    env_config+=("-e" "ASPNETCORE_ENVIRONMENT=Development")
fi
```

## 🎯 为什么需要这个变更？

### 问题背景

1. **appsettings.json** (生产配置):
   ```json
   {
     "Consul": {
       "Address": "http://go-nomads-consul:8500"  // ✅ 容器名，可在 Docker 网络中解析
     }
   }
   ```

2. **appsettings.Development.json** (开发配置):
   ```json
   {
     "Consul": {
       "Address": "http://localhost:8500"  // ❌ 容器内无法访问 localhost:8500
     }
   }
   ```

3. **在 Docker 容器内运行时**:
   - 设置 `ASPNETCORE_ENVIRONMENT=Development` → 加载 `appsettings.Development.json`
   - Gateway 尝试连接 `localhost:8500` → **失败**（容器内无法访问宿主机的 localhost）
   - 结果：无法从 Consul 加载服务路由配置

4. **解决方案**:
   - Gateway 使用 `Production` 环境 → 使用 `go-nomads-consul:8500` → **成功**
   - 其他服务保持 `Development` 环境（它们的配置正确）

## 🚀 如何使用

### 部署服务

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment

# 1. 先部署基础设施（如果还没有）
./deploy-infrastructure-local.sh

# 2. 部署所有服务（包括 Gateway）
./deploy-services-local.sh
```

### 验证 Gateway 配置

```bash
# 1. 检查 Gateway 日志（应该能看到路由加载成功）
docker logs go-nomads-gateway 2>&1 | grep -E "Route:|Loaded"

# 预期输出：
# Loaded 10 routes from Consul
# Route: user-service-route, Path: /api/users/{**remainder}, Cluster: user-service-cluster
# ...

# 2. 测试 Gateway 健康检查
curl http://localhost:5000/health

# 预期输出：
# {"status":"healthy","timestamp":"2025-10-20T..."}

# 3. 测试路由（通过 Gateway 访问 UserService）
curl http://localhost:5000/api/users

# 预期输出：401 Unauthorized（需要认证，说明路由工作正常）
```

## 🔧 其他注意事项

### Gateway 特殊配置

Gateway 在容器中运行时有以下特殊要求：

1. **Consul 地址**: 必须使用容器名 `go-nomads-consul:8500`
2. **网络**: 必须在 `go-nomads-network` 中运行
3. **环境**: 使用 `Production` 环境（避免加载 localhost 配置）
4. **端口映射**: 5000:8080（宿主机:容器）

### 限流功能

Gateway 包含以下限流策略（在 `appsettings.json` 中配置）：

- **Login**: 5次/分钟
- **Register**: 3次/小时
- **API**: 100次/分钟（滑动窗口）
- **Strict**: Token Bucket（10 tokens，每分钟补充 2 个）
- **Global**: 50 并发请求

### 测试限流

```bash
# 测试登录限流（5次/分钟）
for i in {1..7}; do
  curl -s -o /dev/null -w "请求 $i: %{http_code}\n" \
    -X POST http://localhost:5000/api/test/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com"}'
done

# 预期结果：
# 请求 1-5: 200 OK
# 请求 6-7: 429 Too Many Requests
```

## 📊 配置对比

| 服务 | 环境变量 | Consul 地址 | 原因 |
|------|---------|------------|------|
| Gateway | `Production` | `go-nomads-consul:8500` | 需要在容器网络中访问 Consul |
| UserService | `Development` | `http://localhost:8500`（开发）<br>`go-nomads-consul:8500`（容器） | 容器中会被覆盖为正确地址 |
| ProductService | `Development` | 同上 | 同上 |
| DocumentService | `Development` | 同上 | 同上 |

## ⚠️ 故障排查

### Gateway 无法加载路由

**症状**: 
```bash
curl http://localhost:5000/api/users
# 返回 404 Not Found
```

**检查步骤**:

1. **查看 Gateway 日志**:
   ```bash
   docker logs go-nomads-gateway 2>&1 | tail -50
   ```

2. **检查 Consul 连接**:
   ```bash
   # 应该看到类似这样的日志：
   # ✅ "Loading service configuration from Consul..."
   # ✅ "Loaded 10 routes from Consul"
   
   # 如果看到错误：
   # ❌ "Failed to load configuration from Consul"
   # ❌ "Connection refused (localhost:8500)"
   ```

3. **解决方法**:
   ```bash
   # 确保 Gateway 使用 Production 环境
   docker inspect go-nomads-gateway | grep ASPNETCORE_ENVIRONMENT
   # 应该输出：ASPNETCORE_ENVIRONMENT=Production
   
   # 如果不是，重新部署：
   docker stop go-nomads-gateway && docker rm go-nomads-gateway
   cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
   ./deploy-services-local.sh
   ```

### 限流未生效

**症状**: 连续多次请求都返回 200，没有触发 429

**检查步骤**:

1. **确认测试端点**:
   ```bash
   # 使用 Gateway 自带的测试端点
   curl -X POST http://localhost:5000/api/test/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com"}'
   ```

2. **查看响应头**:
   ```bash
   curl -v -X POST http://localhost:5000/api/test/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com"}' 2>&1 | grep -i "X-RateLimit"
   ```

3. **查看 Gateway 日志**:
   ```bash
   docker logs go-nomads-gateway 2>&1 | grep -i "ratelimit"
   ```

## 📚 相关文档

- [RATE_LIMIT_STATUS.md](../RATE_LIMIT_STATUS.md) - 限流功能当前状态
- [GATEWAY_ACCESS_FIXED.md](../GATEWAY_ACCESS_FIXED.md) - Gateway 访问问题修复记录
- [deployment/README.md](README.md) - 部署指南

---

**最后更新**: 2025-10-20
**版本**: 1.0.0
**状态**: ✅ 已测试并验证

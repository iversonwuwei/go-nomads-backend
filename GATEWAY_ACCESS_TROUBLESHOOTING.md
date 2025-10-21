# 🔧 Gateway 无法访问 UserService 问题解决方案

## 📋 问题描述

无法通过 Gateway (http://localhost:5000) 访问 UserService API。

## 🔍 问题分析

### 发现的问题

1. **Consul 地址配置问题** ❌
   - Gateway 配置: `http://go-nomads-consul:8500` (Docker 容器名)
   - 本地运行时无法解析 Docker 容器名
   
2. **服务发现过滤问题** ✅ 已修复
   - ConsulProxyConfigProvider 过滤掉了所有没有 `dapr` 标签的服务
   - UserService 没有 `dapr` 标签
   - 修改：移除了 `dapr` 标签要求

3. **服务注册信息** ✅
   - UserService 已在 Consul 注册
   - 地址: `go-nomads-user-service:8080`
   - 健康检查通过

## ✅ 解决方案

### 方案 1: 修改 Consul 地址配置（推荐）

在本地开发时，Gateway 应该使用 `localhost:8500` 而不是 Docker 容器名。

**修改文件**: `appsettings.Development.json`

```json
{
  "Consul": {
    "Address": "http://localhost:8500"  // 改为 localhost
  }
}
```

### 方案 2: 使用环境变量

```bash
export CONSUL__ADDRESS=http://localhost:8500
dotnet run
```

### 方案 3: 通过 Docker 运行 Gateway

如果通过 Docker Compose 运行，容器名解析就没问题：

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma
docker-compose up gateway
```

## 🚀 完整启动步骤

### 步骤 1: 更新配置

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/src/Gateway/Gateway
```

编辑 `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Information",
      "Gateway.Services.ConsulProxyConfigProvider": "Debug"
    }
  },
  "Consul": {
    "Address": "http://localhost:8500"  // 👈 关键修改
  },
  "RateLimit": {
    "Login": {
      "PermitLimit": 10    // 开发环境放宽限制
    },
    "Api": {
      "PermitLimit": 200
    }
  }
}
```

### 步骤 2: 启动 Gateway

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/src/Gateway/Gateway

# 设置开发环境
export ASPNETCORE_ENVIRONMENT=Development

# 启动 Gateway
dotnet run
```

### 步骤 3: 验证服务发现

在新终端中运行：

```bash
# 等待 Gateway 启动
sleep 5

# 检查健康状态
curl http://localhost:5000/health

# 测试登录端点（公开，不需要 Token）
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123456"}'
```

**期望结果**:
- ✅ 如果配置正确：返回登录响应（可能是 401 或成功的 JWT token）
- ❌ 如果还是 404：说明路由未正确加载

### 步骤 4: 调试路由加载

查看 Gateway 启动日志，应该看到：

```
info: Gateway.Services.ConsulProxyConfigProvider[0]
      Loading service configuration from Consul...
info: Gateway.Services.ConsulProxyConfigProvider[0]
      Discovered 1 healthy instance(s) for service: user-service
info: Gateway.Services.ConsulProxyConfigProvider[0]
      Loaded 2 routes and 1 clusters from Consul
```

## 🧪 测试用例

### 测试 1: 公开端点（不需要认证）

```bash
# 登录
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'

# 注册
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "Password@123",
    "name": "New User"
  }'
```

### 测试 2: 需要认证的端点

```bash
# 先登录获取 Token
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123456"}' \
  | jq -r '.data.accessToken')

# 使用 Token 访问受保护的端点
curl http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN"

# 获取当前用户信息
curl http://localhost:5000/api/users/me \
  -H "Authorization: Bearer $TOKEN"
```

## 📊 故障排查

### 问题 1: Gateway 返回 404

**原因**: 路由未从 Consul 加载

**检查**:
```bash
# 1. 确认 Consul 可访问
curl http://localhost:8500/v1/catalog/services

# 2. 确认 UserService 已注册
curl http://localhost:8500/v1/health/service/user-service

# 3. 检查 Gateway 日志
grep "Loading service configuration" /tmp/gateway.log
grep "Discovered.*healthy instance" /tmp/gateway.log
```

**解决**:
- 确保 `appsettings.Development.json` 中 Consul 地址为 `http://localhost:8500`
- 确保环境变量 `ASPNETCORE_ENVIRONMENT=Development`

### 问题 2: Gateway 返回 502 Bad Gateway

**原因**: UserService 不可达

**检查**:
```bash
# 直接访问 UserService
curl http://localhost:5001/health
```

**解决**:
- 启动 UserService: `cd src/Services/UserService/UserService && dotnet run`
- 检查端口是否被占用: `lsof -i:5001`

### 问题 3: Gateway 返回 401 Unauthorized

**原因**: 这是**正常的**！说明路由工作了，但需要 JWT Token

**解决**:
- 对于公开端点（login, register）：检查 `RouteAuthorizationConfig.cs` 确保这些路径在 `PublicRoutes` 中
- 对于受保护端点：先登录获取 Token，然后在请求头中添加 `Authorization: Bearer {token}`

### 问题 4: Consul 连接失败

**错误日志**:
```
Consul.ConsulRequestException: Unexpected response, status code BadGateway
```

**原因**: Consul 地址配置为 Docker 容器名，但 Gateway 在本地运行

**解决**:
```bash
# 方案 A: 修改配置文件
echo '{
  "Consul": {
    "Address": "http://localhost:8500"
  }
}' > appsettings.Development.json

# 方案 B: 使用环境变量
export CONSUL__ADDRESS=http://localhost:8500
dotnet run
```

## 📝 配置文件参考

### appsettings.Development.json（完整）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Information",
      "Yarp.ReverseProxy": "Information",
      "Gateway.Services.ConsulProxyConfigProvider": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Consul": {
    "Address": "http://localhost:8500"
  },
  "Jwt": {
    "Issuer": "https://lcfbajrocmjlqndkrsao.supabase.co/auth/v1",
    "Audience": "authenticated",
    "Secret": "fM8uYPXzh+bG9dIPFnlQcEWjAa4ZXMfQVxxXWajI62CbwZvdqjCIwdR3YzvP8NYGj+NUlC6WNPnmHT73uTT45A==",
    "ValidateIssuerSigningKey": true,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true
  },
  "RateLimit": {
    "Login": {
      "Window": "00:01:00",
      "PermitLimit": 10,
      "QueueLimit": 5
    },
    "Register": {
      "Window": "01:00:00",
      "PermitLimit": 5,
      "QueueLimit": 2
    },
    "Api": {
      "Window": "00:01:00",
      "PermitLimit": 200,
      "SegmentsPerWindow": 6,
      "QueueLimit": 20
    }
  }
}
```

## ✅ 验证清单

在启动 Gateway 前，确认：

- [ ] Consul 正在运行: `curl http://localhost:8500/v1/status/leader`
- [ ] UserService 正在运行: `curl http://localhost:5001/health`
- [ ] Gateway 配置正确: `appsettings.Development.json` 中 Consul 地址为 `localhost:8500`
- [ ] 环境变量设置: `ASPNETCORE_ENVIRONMENT=Development`

在 Gateway 启动后，验证：

- [ ] 健康检查: `curl http://localhost:5000/health` 返回 200
- [ ] 公开端点可访问: `curl -X POST http://localhost:5000/api/users/login` 不返回 404
- [ ] 路由加载日志: Gateway 日志中有 "Discovered X healthy instance(s)"

## 🎯 快速测试命令

```bash
# 一键测试脚本
cat << 'EOF' > test-gateway.sh
#!/bin/bash
set -e

echo "=== 测试 Gateway 访问 UserService ==="
echo ""

echo "1. 测试健康检查..."
curl -s http://localhost:5000/health | jq .
echo ""

echo "2. 测试登录端点..."
curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123456"}' \
  | jq .
echo ""

echo "3. 获取 Token..."
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123456"}' \
  | jq -r '.data.accessToken // empty')

if [ -n "$TOKEN" ]; then
  echo "Token: $TOKEN"
  echo ""
  echo "4. 使用 Token 访问受保护端点..."
  curl -s http://localhost:5000/api/users/me \
    -H "Authorization: Bearer $TOKEN" \
    | jq .
else
  echo "❌ 未能获取 Token"
fi

echo ""
echo "=== 测试完成 ==="
EOF

chmod +x test-gateway.sh
./test-gateway.sh
```

---

**最后更新**: 2025年10月20日  
**问题状态**: ✅ 已识别，方案已提供  
**核心修复**: 修改 Consul 地址配置 + 移除 dapr 标签过滤

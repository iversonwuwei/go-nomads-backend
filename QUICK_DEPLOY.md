# 快速部署指南 🚀

## 一键部署命令

```bash
# 进入部署目录
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment

# 1️⃣ 部署基础设施（Consul, Redis, Prometheus, Grafana）
./deploy-infrastructure-local.sh

# 2️⃣ 部署所有服务（Gateway, UserService, ProductService, DocumentService）
./deploy-services-local.sh
```

## 验证部署

```bash
# ✅ 检查所有容器状态
docker ps --filter "name=go-nomads-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# ✅ 测试 Gateway 健康检查
curl http://localhost:5000/health

# ✅ 测试路由（通过 Gateway 访问 UserService）
curl http://localhost:5000/api/users
# 预期: 401 Unauthorized（需要认证，说明路由正常）

# ✅ 查看 Consul UI（查看服务注册）
open http://localhost:8500
```

## 测试限流功能

```bash
# 🔥 测试登录限流（5次/分钟）
for i in {1..7}; do
  http_code=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST http://localhost:5000/api/test/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com"}')
  echo "请求 $i: $http_code"
done

# 预期结果：
# 请求 1-5: 200 ✅
# 请求 6-7: 429 🛑 (限流触发)
```

## 服务端口

| 服务 | 端口 | URL |
|------|------|-----|
| Gateway | 5000 | http://localhost:5000 |
| UserService | 5001 | http://localhost:5001 |
| ProductService | 5002 | http://localhost:5002 |
| DocumentService | 5003 | http://localhost:5003 |
| Consul UI | 8500 | http://localhost:8500 |
| Prometheus | 9090 | http://localhost:9090 |
| Grafana | 3000 | http://localhost:3000 |

## 常用命令

```bash
# 查看 Gateway 日志
docker logs go-nomads-gateway

# 查看所有服务日志
docker logs go-nomads-user-service
docker logs go-nomads-product-service
docker logs go-nomads-document-service

# 重启 Gateway
docker restart go-nomads-gateway

# 停止所有服务
cd deployment
./stop-services.sh
```

## 🆕 重要变更

**Gateway 现在使用 Production 环境！**

- ✅ 这样才能连接容器化的 Consul（`go-nomads-consul:8500`）
- ✅ 限流功能已集成
- ✅ JWT 认证已配置
- ✅ 动态路由已启用

## 📚 详细文档

- [DEPLOYMENT_SCRIPTS_UPDATE.md](DEPLOYMENT_SCRIPTS_UPDATE.md) - 脚本更新总结
- [DEPLOYMENT_UPDATE.md](DEPLOYMENT_UPDATE.md) - 详细部署说明
- [RATE_LIMIT_STATUS.md](RATE_LIMIT_STATUS.md) - 限流功能状态

---

**最后更新**: 2025-10-20

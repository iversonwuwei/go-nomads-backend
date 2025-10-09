# Go-Nomads 快速参考

## 🚀 服务地址

| 服务 | 地址 | 用途 |
|------|------|------|
| **Gateway** | http://localhost:5000 | API网关 - 统一入口 |
| **Product Service** | http://localhost:5001 | 产品服务 - 直接访问 |
| **User Service** | http://localhost:5002 | 用户服务 - 直接访问 |

---

## 🧪 API测试

### 通过Gateway访问（推荐）

```powershell
# 获取用户列表
curl http://localhost:5000/api/users

# 获取产品列表
curl http://localhost:5000/api/products

# 健康检查
curl http://localhost:5000/health
```

### 直接访问服务

```powershell
# Product Service
curl http://localhost:5001/health

# User Service
curl http://localhost:5002/health
```

---

## 🔧 常用命令

### 查看服务状态
```powershell
podman ps
```

### 查看日志
```powershell
# Gateway
podman logs -f go-nomads-gateway

# Product Service
podman logs -f go-nomads-product-service

# User Service
podman logs -f go-nomads-user-service
```

### 重启服务
```powershell
# 重启Gateway
podman restart go-nomads-gateway

# 重启所有服务
podman restart go-nomads-gateway go-nomads-product-service go-nomads-user-service
```

### 停止服务
```powershell
# 停止所有服务
podman stop go-nomads-gateway go-nomads-product-service go-nomads-user-service

# 删除所有容器
podman rm -f go-nomads-gateway go-nomads-product-service go-nomads-user-service
```

### 清理资源
```powershell
# 删除所有容器
podman rm -f go-nomads-gateway go-nomads-product-service go-nomads-user-service

# 删除镜像
podman rmi go-nomads-gateway go-nomads-product-service go-nomads-user-service

# 删除网络
podman network rm go-nomads-network
```

---

## 📖 详细文档

- [部署成功报告](DEPLOYMENT_SUCCESS.md) - 完整的部署结果和测试
- [Podman部署指南](PODMAN_DEPLOYMENT.md) - 详细的部署说明
- [部署文件清单](DEPLOYMENT_SUMMARY.md) - 所有配置文件说明
- [项目README](README.md) - 项目概述

---

## 🆘 故障排查

### 服务无法访问
```powershell
# 检查容器是否运行
podman ps

# 查看服务日志
podman logs go-nomads-<service-name>

# 检查端口占用
netstat -ano | findstr "5000"
```

### 502 Bad Gateway
- 检查后端服务是否正常运行
- 查看Gateway日志确认路由配置

### 容器启动失败
- 检查端口是否被占用
- 查看容器日志排查错误
- 确认镜像构建成功

---

**最后更新**: 2025年10月9日

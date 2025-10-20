# Go-Nomads Windows 部署脚本使用指南

本文档说明如何在 Windows 环境下使用 PowerShell 脚本部署 Go-Nomads 微服务系统。

## 📋 前置条件

1. **安装 .NET SDK 9.0**
   ```powershell
   winget install Microsoft.DotNet.SDK.9
   ```

2. **安装 Podman 或 Docker**
   - Podman Desktop: https://podman-desktop.io/downloads
   - Docker Desktop: https://www.docker.com/products/docker-desktop

3. **确保 PowerShell 执行策略允许运行脚本**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

## 🚀 快速开始

### 第一步: 部署基础设施

```powershell
cd deployment
.\deploy-infrastructure-local.ps1
```

这将启动:
- ✅ Redis (端口 6379)
- ✅ Consul (端口 8500)
- ✅ Zipkin (端口 9411)
- ✅ Prometheus (端口 9090)
- ✅ Grafana (端口 3000, 用户名/密码: admin/admin)

### 第二步: 部署应用服务

```powershell
.\deploy-services-local.ps1
```

这将构建并部署:
- ✅ Gateway (端口 5000)
- ✅ User Service (端口 5001)
- ✅ Product Service (端口 5002)
- ✅ Document Service (端口 5003)

每个服务都会自动:
- 本地构建 .NET 项目
- 创建 Docker 镜像
- 启动应用容器
- 启动 Dapr Sidecar
- 自动注册到 Consul

## 📝 脚本详细说明

### deploy-infrastructure-local.ps1

基础设施部署脚本,支持以下命令:

```powershell
# 启动所有基础设施 (默认)
.\deploy-infrastructure-local.ps1
.\deploy-infrastructure-local.ps1 start

# 查看运行状态
.\deploy-infrastructure-local.ps1 status

# 停止所有基础设施
.\deploy-infrastructure-local.ps1 stop

# 重启所有基础设施
.\deploy-infrastructure-local.ps1 restart

# 清理所有容器和配置文件
.\deploy-infrastructure-local.ps1 clean

# 显示帮助
.\deploy-infrastructure-local.ps1 help
```

### deploy-services-local.ps1

应用服务部署脚本:

```powershell
# 构建并部署所有服务 (默认)
.\deploy-services-local.ps1

# 跳过构建,直接使用已有镜像部署
.\deploy-services-local.ps1 -SkipBuild

# 显示帮助
.\deploy-services-local.ps1 -Help
```

**参数说明:**
- `-SkipBuild`: 跳过 `dotnet publish` 和镜像构建步骤,适合代码未修改时快速重启
- `-Help`: 显示帮助信息

### stop-services.ps1

服务停止脚本:

```powershell
# 仅停止服务 (保留容器)
.\stop-services.ps1

# 停止并删除所有服务容器
.\stop-services.ps1 -Clean

# 显示帮助
.\stop-services.ps1 -Help
```

## 🔍 常用管理命令

### 查看容器状态

```powershell
# 查看所有 go-nomads 容器
podman ps --filter "name=go-nomads"

# 或使用 Docker
docker ps --filter "name=go-nomads"
```

### 查看服务日志

```powershell
# 查看 Gateway 日志
podman logs go-nomads-gateway

# 查看 Gateway 的 Dapr Sidecar 日志
podman logs go-nomads-gateway-dapr

# 实时跟踪日志
podman logs -f go-nomads-gateway
```

### 验证服务健康

```powershell
# 检查服务健康端点
Invoke-WebRequest http://localhost:5000/health
Invoke-WebRequest http://localhost:5001/health
Invoke-WebRequest http://localhost:5002/health
Invoke-WebRequest http://localhost:5003/health
```

### 查看 Consul 服务注册

```powershell
# 查看所有已注册服务
Invoke-WebRequest http://localhost:8500/v1/catalog/services | Select-Object -ExpandProperty Content

# 查看特定服务健康状态
Invoke-WebRequest http://localhost:8500/v1/health/service/gateway
```

## 🌐 访问地址

部署完成后,可以通过以下地址访问各个服务:

### 应用服务
- **Gateway**: http://localhost:5000
- **User Service**: http://localhost:5001
- **Product Service**: http://localhost:5002
- **Document Service**: http://localhost:5003

### API 文档 (Scalar UI)
- **Gateway**: http://localhost:5000/scalar/v1
- **User Service**: http://localhost:5001/scalar/v1
- **Product Service**: http://localhost:5002/scalar/v1
- **Document Service** (统一文档中心): http://localhost:5003/scalar/v1

### 基础设施
- **Consul UI**: http://localhost:8500
- **Zipkin 追踪**: http://localhost:9411
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)

## 🛠️ 故障排查

### 问题: 脚本无法运行

**解决方案:**
```powershell
# 检查执行策略
Get-ExecutionPolicy

# 如果是 Restricted,需要修改
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### 问题: 容器启动失败

**解决方案:**
```powershell
# 查看容器日志
podman logs go-nomads-[服务名]

# 检查端口是否被占用
netstat -ano | findstr "5000"
netstat -ano | findstr "8500"

# 停止占用端口的进程
Stop-Process -Id [PID]
```

### 问题: 服务未注册到 Consul

**解决方案:**
```powershell
# 等待 15-30 秒,服务会自动注册
# 检查服务日志
podman logs go-nomads-gateway

# 验证 Consul 可访问
Invoke-WebRequest http://localhost:8500/v1/status/leader
```

### 问题: Prometheus 无法抓取指标

**解决方案:**
```powershell
# 检查 /metrics 端点
Invoke-WebRequest http://localhost:5000/metrics

# 查看 Prometheus targets
# 访问 http://localhost:9090/targets
```

## 🔄 完整部署流程示例

```powershell
# 1. 进入部署目录
cd E:\Workspaces\WaldenProjects\go-nomads\deployment

# 2. 部署基础设施
.\deploy-infrastructure-local.ps1

# 3. 等待 5-10 秒,确保基础设施就绪
Start-Sleep -Seconds 10

# 4. 部署应用服务
.\deploy-services-local.ps1

# 5. 等待服务启动和注册 (约 30 秒)
Start-Sleep -Seconds 30

# 6. 验证部署
Invoke-WebRequest http://localhost:8500/v1/catalog/services
Invoke-WebRequest http://localhost:5003/scalar/v1

# 7. 停止所有服务 (可选)
# .\stop-services.ps1 -Clean
```

## 📊 与 Linux/Mac 脚本的对比

| 功能 | Linux/Mac (.sh) | Windows (.ps1) |
|------|----------------|----------------|
| 容器运行时检测 | ✅ | ✅ |
| 自动创建网络 | ✅ | ✅ |
| 本地构建 | ✅ | ✅ |
| Docker 镜像构建 | ✅ | ✅ |
| Dapr Sidecar | ✅ | ✅ |
| 自动 Consul 注册 | ✅ | ✅ |
| 彩色输出 | ✅ | ✅ |
| 错误处理 | ✅ | ✅ |

## 📚 更多资源

- [Go-Nomads 架构文档](../README.md)
- [Dapr 文档](https://docs.dapr.io/)
- [Consul 服务发现](https://www.consul.io/docs)
- [Prometheus 监控](https://prometheus.io/docs/)
- [Grafana 可视化](https://grafana.com/docs/)

## 🤝 贡献

如果发现问题或有改进建议,请提交 Issue 或 Pull Request。

---

**注意**: 这些 PowerShell 脚本与 Linux/Mac 的 Bash 脚本功能完全一致,只是针对 Windows 环境进行了优化。

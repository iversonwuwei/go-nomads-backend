# Go-Nomads Infrastructure Deployment

一键部署 Go-Nomads 微服务基础设施,支持 Windows 和 Linux 平台。

## 基础设施组件

- **Redis** - 配置中心 & 状态存储
- **Consul** - 服务注册与发现
- **Zipkin** - 分布式链路追踪
- **Prometheus** - 指标收集
- **Grafana** - 指标可视化

## 系统要求

### Windows
- PowerShell 5.1+ 或 PowerShell Core 7+
- Docker Desktop 或 Podman Desktop

### Linux
- Bash 4.0+
- Docker 或 Podman

## 快速开始

### Windows (PowerShell)

```powershell
# 部署基础设施
pwsh deploy-infrastructure.ps1

# 或者直接运行
.\deploy-infrastructure.ps1
```

### Linux (Bash)

```bash
# 添加执行权限
chmod +x deploy-infrastructure.sh

# 部署基础设施
./deploy-infrastructure.sh
```

## 命令说明

| 命令 | 说明 |
|------|------|
| `start` | 部署所有基础设施组件(默认) |
| `stop` | 停止所有基础设施容器 |
| `restart` | 重启所有基础设施容器 |
| `status` | 显示当前基础设施状态 |
| `clean` | 删除所有基础设施资源 |
| `help` | 显示帮助信息 |

### 使用示例

```powershell
# Windows PowerShell
.\deploy-infrastructure.ps1 status
.\deploy-infrastructure.ps1 stop
.\deploy-infrastructure.ps1 clean
```

```bash
# Linux Bash
./deploy-infrastructure.sh status
./deploy-infrastructure.sh stop
./deploy-infrastructure.sh clean
```

## 访问地址

部署完成后,可以通过以下地址访问各个组件:

| 组件 | 地址 | 说明 |
|------|------|------|
| Consul | http://localhost:7500 | 服务注册中心 Web UI |
| Prometheus | http://localhost:9090 | 指标查询与监控 |
| Grafana | http://localhost:3000 | 可视化仪表盘 (admin/admin) |
| Zipkin | http://localhost:9811 | 分布式追踪 UI |
| Elasticsearch | http://localhost:10200 | 日志聚合和搜索 |

## 配置说明

### Redis 配置

脚本会自动导入 21 个配置项到 Redis,包括:
- 应用版本和环境配置
- 服务端点配置
- 日志和追踪配置
- 功能开关
- 业务规则

### Consul 服务注册

自动注册以下服务到 Consul:
- `gateway` - API 网关服务
- `product-service` - 产品服务
- `user-service` - 用户服务

### Prometheus 监控

自动配置以下监控目标:
- Dapr sidecar metrics (端口 9090)
- 应用 metrics (端口 8080)
- Redis 和 Zipkin metrics

通过 Consul 服务发现自动更新监控目标。

## 网络配置

所有容器运行在 `go-nomads-network` 网络中,可以通过容器名称互相访问。

## 数据持久化

当前配置不包含数据持久化。如需持久化,请修改脚本添加卷挂载:

```powershell
# 示例:为 Redis 添加数据卷
-v redis-data:/data
```

## 故障排查

### 容器无法启动

1. 检查端口占用:
   ```powershell
   # Windows
   netstat -ano | findstr "7500|9090|3000|9811|6379|10200"
   
   # Linux
   netstat -tuln | grep -E "7500|9090|3000|9811|6379|10200"
   ```

2. 检查容器日志:
   ```bash
   podman logs go-nomads-consul
   podman logs go-nomads-redis
   ```

### Prometheus 无法发现服务

1. 确认 Consul 服务已注册:
   ```bash
   podman exec go-nomads-consul consul catalog services
   ```

2. 检查 Prometheus targets:
   访问 http://localhost:9090/targets

### 网络问题

重新创建网络:
```bash
# 停止所有容器
./deploy-infrastructure.sh stop

# 清理并重新部署
./deploy-infrastructure.sh clean
./deploy-infrastructure.sh start
```

## 架构说明

```
┌─────────────────────────────────────────────────────┐
│              Go-Nomads 基础设施架构                   │
└─────────────────────────────────────────────────────┘

应用层 (需单独部署)
├─ Gateway (API网关)
├─ Product Service (产品服务)
└─ User Service (用户服务)
         │
         ├─ Dapr Sidecar (服务网格)
         │  ├─ mDNS (服务发现)
         │  ├─ Redis (配置/状态)
         │  └─ Zipkin (追踪)
         │
基础设施层 (本脚本部署)
├─ Redis (配置中心 & 状态存储)
├─ Consul (服务注册 & 健康检查)
├─ Zipkin (分布式追踪)
├─ Prometheus (指标收集 & 查询)
└─ Grafana (可视化监控)
```

## 下一步

基础设施部署完成后:

1. **部署应用服务**
   - 使用 Dapr CLI 运行微服务
   - 或使用容器化部署

2. **配置 Grafana 仪表盘**
   - 添加 Prometheus 数据源
   - 导入 Dapr 官方仪表盘

3. **验证服务注册**
   - 访问 Consul UI 查看服务列表
   - 检查健康检查状态

## 清理资源

```powershell
# Windows
.\deploy-infrastructure.ps1 clean

# Linux
./deploy-infrastructure.sh clean
```

**警告**: 此操作会删除所有容器、网络和配置文件,无法恢复!

## 许可证

MIT License

## 支持

如有问题,请提交 Issue 或查看项目文档。

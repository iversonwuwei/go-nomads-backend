# 快速开始指南

## 一键部署基础设施

### Windows

```powershell
# 方式 1: PowerShell 7+
pwsh deploy-infrastructure.ps1

# 方式 2: Windows PowerShell 5.1
.\deploy-infrastructure.ps1
```

### Linux

```bash
chmod +x deploy-infrastructure.sh
./deploy-infrastructure.sh
```

## 部署完成后

访问以下地址验证部署:

- **Consul UI**: <http://localhost:8500>
- **Prometheus**: <http://localhost:9090/targets>
- **Grafana**: <http://localhost:3000> (admin/admin)
- **Zipkin**: <http://localhost:9411>

## 常用命令

```bash
# 查看状态
./deploy-infrastructure.ps1 status

# 停止所有服务
./deploy-infrastructure.ps1 stop

# 重启
./deploy-infrastructure.ps1 restart

# 完全清理(危险操作!)
./deploy-infrastructure.ps1 clean
```

## 下一步

1. 验证所有容器运行正常:

   ```
   podman ps --filter "name=go-nomads"
   ```

2. 检查 Consul 服务注册:
   - 打开 <http://localhost:8500>
   - 应该看到 3 个服务: gateway, product-service, user-service

3. 检查 Prometheus 目标:
   - 打开 <http://localhost:9090/targets>
   - 应该看到通过 Consul 发现的服务

4. 配置 Grafana:
   - 登录 <http://localhost:3000> (admin/admin)
   - 添加 Prometheus 数据源: <http://go-nomads-prometheus:9090>
   - 导入 Dapr 仪表盘

## 故障排查

如果遇到问题:

1. 检查容器日志:

   ```
   podman logs go-nomads-consul
   podman logs go-nomads-redis
   ```

2. 检查端口占用:

   ```
   netstat -ano | findstr "8500 9090 3000 9411 6379"
   ```

3. 重新部署:

   ```
   .\deploy-infrastructure.ps1 clean
   .\deploy-infrastructure.ps1 start
   ```

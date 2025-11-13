# 基础设施端口修复说明

## 问题描述

在 Windows 环境下，Consul、Zipkin 和 Elasticsearch 容器启动失败，原因是它们使用的端口被 Windows Hyper-V 保留。

## Windows 保留端口范围

通过 `netsh interface ipv4 show excludedportrange protocol=tcp` 检查发现以下端口范围被保留：

- `8488-8587` (包含原 Consul 端口 8500, 8502)
- `9129-9228` (包含原 Elasticsearch 端口 9200)
- `9229-9328` (包含原 Elasticsearch 端口 9300)
- `9329-9428` (包含原 Zipkin 端口 9411)

## 解决方案

修改 `deploy-infrastructure-local.ps1` 脚本，将冲突的端口映射到安全范围。

## 端口变更对照表

| 服务 | 原端口 | 新端口 | 访问 URL |
|------|--------|--------|----------|
| **Consul HTTP** | 8500 | **7500** | http://localhost:7500 |
| **Consul GRPC** | 8502 | **7502** | - |
| **Consul DNS** | 8600 | **7600** | - |
| **Zipkin** | 9411 | **9811** | http://localhost:9811 |
| **Elasticsearch HTTP** | 9200 | **10200** | http://localhost:10200 |
| **Elasticsearch Transport** | 9300 | **10300** | - |

## 修改的文件

### 1. `deploy-infrastructure-local.ps1`

#### Consul 配置
```powershell
# 修改配置中的端口
ports = @{
    http = 7500  # 原 8500
    grpc = 7502  # 原 8502
    dns = 7600   # 原 8600
}

# 修改容器端口映射
-p 7500:7500
-p 7502:7502
-p 7600:7600/udp
```

#### Zipkin 配置
```powershell
# 修改端口映射（容器内仍然是 9411）
-p 9811:9411  # 原 9411:9411
```

#### Elasticsearch 配置
```powershell
# 修改端口映射（容器内仍然是 9200 和 9300）
-p 10200:9200  # 原 9200:9200
-p 10300:9300  # 原 9300:9300
```

#### Prometheus 配置
```yaml
# 修改 Consul 服务发现地址
consul_sd_configs:
  - server: 'go-nomads-consul:7500'  # 原 8500
```

## 验证结果

运行 `.\deploy-infrastructure-local.ps1 status` 显示所有容器正常运行：

```
✓ Consul:         Up - http://localhost:7500
✓ Zipkin:         Up (healthy) - http://localhost:9811
✓ Elasticsearch:  Up - http://localhost:10200
✓ Prometheus:     Up - http://localhost:9090
✓ Grafana:        Up - http://localhost:3000
✓ Redis:          Up - redis://localhost:6379
✓ RabbitMQ:       Up - amqp://localhost:5672
✓ Nginx:          Up - http://localhost
```

## 影响范围

### 需要更新的配置

1. **微服务 Consul 注册配置**
   - 如果微服务配置文件中硬编码了 Consul 地址 `localhost:8500`
   - 需要改为 `localhost:7500`

2. **Dapr 组件配置**
   - 检查 `components/` 目录下是否有硬编码 Consul/Zipkin/Elasticsearch 地址
   - 更新为新端口

3. **应用程序配置**
   - `appsettings.json` 中的服务发现配置
   - Zipkin tracing 配置
   - Elasticsearch 连接配置

### 不受影响的配置

- 容器内部通信（通过 Docker 网络）使用服务名称，不受影响
- 例如：`http://go-nomads-consul:7500` (容器内是 7500 端口)

## 快速检查脚本

```powershell
# 检查所有服务健康状态
Write-Host "检查 Consul..." -ForegroundColor Cyan
Invoke-RestMethod http://localhost:7500/v1/status/leader

Write-Host "检查 Zipkin..." -ForegroundColor Cyan
Invoke-RestMethod http://localhost:9811/health

Write-Host "检查 Elasticsearch..." -ForegroundColor Cyan
Invoke-RestMethod http://localhost:10200
```

## 故障排查

如果遇到端口冲突：

1. **检查 Windows 保留端口**
   ```powershell
   netsh interface ipv4 show excludedportrange protocol=tcp
   ```

2. **检查端口占用**
   ```powershell
   netstat -ano | findstr :<端口号>
   ```

3. **清理并重启**
   ```powershell
   .\deploy-infrastructure-local.ps1 clean
   .\deploy-infrastructure-local.ps1 start
   ```

## 注意事项

- Windows Hyper-V 保留端口范围可能随系统更新变化
- 如果将来端口再次冲突，选择 6000-7000 或 10000-12000 范围的端口相对安全
- 容器内部端口不需要修改，只修改主机映射端口

## 更新时间

2025-01-13

## 相关文档

- [Windows Dynamic Port Range](https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/default-dynamic-port-range-tcpip-chang)
- [Consul Configuration](https://www.consul.io/docs/agent/config)
- [Elasticsearch Docker](https://www.elastic.co/guide/en/elasticsearch/reference/current/docker.html)

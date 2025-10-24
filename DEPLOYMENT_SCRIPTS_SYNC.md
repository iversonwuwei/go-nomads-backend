# 部署脚本同步完成

## 概述
已成功将 PowerShell 部署脚本与 Bash 部署脚本同步,确保 Windows 和 Linux/Mac 环境下的部署体验一致。

## 同步时间
2025年10月24日

## 修改的文件

### 1. `deployment/deploy-infrastructure-local.ps1`

#### 新增功能
- ✅ 添加 `Start-Elasticsearch` 函数
  - 容器名称: `go-nomads-elasticsearch`
  - 端口映射: 9200:9200, 9300:9300
  - 镜像: `docker.elastic.co/elasticsearch/elasticsearch:8.11.0`
  - 配置: 单节点模式,禁用安全特性,内存限制 512MB

#### 更新的函数
- ✅ `Start-Infrastructure` - 添加 `Start-Elasticsearch` 调用
- ✅ `Stop-Infrastructure` - 容器列表中添加 `go-nomads-elasticsearch`
- ✅ `Remove-Infrastructure` - 容器列表中添加 `go-nomads-elasticsearch`
- ✅ 访问 URL 显示 - 添加 Elasticsearch URL

### 2. `deployment/deploy-services-local.ps1`

#### 新增服务
- ✅ **CityService**
  - 端口: 8002
  - Dapr HTTP 端口: 3504
  - App ID: city-service
  - 容器名: go-nomads-city-service

- ✅ **EventService**
  - 端口: 8005
  - Dapr HTTP 端口: 3505
  - App ID: event-service
  - 容器名: go-nomads-event-service

#### 更新的配置
- ✅ 服务列表从 4 个扩展到 6 个
- ✅ 旧容器清理列表更新,包含所有 6 个服务
- ✅ 端口分配与 Bash 脚本保持一致

## 当前服务列表

### 基础设施服务 (deploy-infrastructure-local)
1. **Redis** - localhost:6379
2. **PostgreSQL** - localhost:5432
3. **Consul** - http://localhost:8500
4. **Zipkin** - http://localhost:9411
5. **Prometheus** - http://localhost:9090
6. **Grafana** - http://localhost:3000
7. **Elasticsearch** - http://localhost:9200 (新增)
8. **Nginx** - http://localhost

### 应用服务 (deploy-services-local)
1. **Gateway** - http://localhost:5000 (Dapr: 3500)
2. **UserService** - http://localhost:5001 (Dapr: 3502)
3. **ProductService** - http://localhost:5002 (Dapr: 3501)
4. **DocumentService** - http://localhost:5003 (Dapr: 3503)
5. **CityService** - http://localhost:8002 (Dapr: 3504) ✨新增
6. **EventService** - http://localhost:8005 (Dapr: 3505) ✨新增

## Bash 与 PowerShell 对比

### deploy-infrastructure-local
| 组件 | Bash (.sh) | PowerShell (.ps1) | 状态 |
|------|-----------|------------------|------|
| Redis | ✅ | ✅ | 一致 |
| PostgreSQL | ❌ | ✅ | PowerShell 专有 |
| Consul | ✅ | ✅ | 一致 |
| Zipkin | ✅ | ✅ | 一致 |
| Prometheus | ✅ | ✅ | 一致 |
| Grafana | ✅ | ✅ | 一致 |
| Elasticsearch | ✅ | ✅ | ✅ 已同步 |
| Nginx | ✅ | ✅ | 一致 |

### deploy-services-local
| 服务 | Bash (.sh) | PowerShell (.ps1) | 状态 |
|------|-----------|------------------|------|
| Gateway | ✅ Port 5000 | ✅ Port 5000 | 一致 |
| UserService | ✅ Port 5001 | ✅ Port 5001 | 一致 |
| ProductService | ✅ Port 5002 | ✅ Port 5002 | 一致 |
| DocumentService | ✅ Port 5003 | ✅ Port 5003 | 一致 |
| CityService | ✅ Port 8002 | ✅ Port 8002 | ✅ 已同步 |
| EventService | ✅ Port 8005 | ✅ Port 8005 | ✅ 已同步 |

## 使用方式

### Windows (PowerShell)
```powershell
# 部署基础设施
.\deployment\deploy-infrastructure-local.ps1 start

# 部署服务
.\deployment\deploy-services-local.ps1

# 停止基础设施
.\deployment\deploy-infrastructure-local.ps1 stop

# 清理环境
.\deployment\deploy-infrastructure-local.ps1 clean
```

### Linux/Mac (Bash)
```bash
# 部署基础设施
./deployment/deploy-infrastructure-local.sh start

# 部署服务
./deployment/deploy-services-local.sh

# 停止基础设施
./deployment/deploy-infrastructure-local.sh stop

# 清理环境
./deployment/deploy-infrastructure-local.sh clean
```

## 技术特性

### Container Sidecar 模式
两个脚本都使用 Dapr Container Sidecar 模式:
- 应用容器和 Dapr sidecar 共享网络命名空间
- 通过 `--network container:<app-container>` 实现
- 应用和 Dapr 通过 localhost 通信
- Dapr gRPC 端口: 50001 (通过环境变量配置)
- Dapr HTTP 端口: 各服务独立 (3500-3505)

### 环境配置
- **Gateway**: 使用 Production 环境,连接容器化 Consul
- **其他服务**: 使用 Development 环境
- **Consul 地址**: `http://go-nomads-consul:8500`

### 容器运行时支持
- 自动检测 Docker 或 Podman
- 优先使用已有容器的运行时
- 跨平台兼容

## 注意事项

1. **PostgreSQL vs Elasticsearch**
   - PowerShell 脚本保留了 PostgreSQL (Windows 开发环境可能需要)
   - 现在也包含 Elasticsearch (与 Bash 一致)
   - Bash 脚本只有 Elasticsearch,没有 PostgreSQL

2. **端口分配**
   - 基础服务: 5000-5003
   - 业务服务: 8000+ 范围
   - Dapr HTTP: 3500-3505

3. **前置条件**
   - Redis 和 Consul 必须运行才能部署服务
   - 脚本会自动检查并提示

## 后续建议

### 可选优化
1. **统一数据库**
   - 考虑在 Bash 脚本中也添加 PostgreSQL
   - 或从 PowerShell 中移除 PostgreSQL,统一使用 Elasticsearch

2. **添加更多服务**
   - AccommodationService
   - CoworkingService
   - InnovationService
   - TravelPlanningService
   - EcommerceService

3. **健康检查**
   - 添加容器健康检查逻辑
   - 等待服务就绪后再继续

4. **日志聚合**
   - 考虑添加 ELK Stack (Elasticsearch + Logstash + Kibana)
   - 统一日志收集和查看

## 验证清单

- ✅ deploy-infrastructure-local.ps1 添加 Elasticsearch
- ✅ deploy-infrastructure-local.ps1 所有函数更新容器列表
- ✅ deploy-services-local.ps1 添加 CityService
- ✅ deploy-services-local.ps1 添加 EventService
- ✅ 服务端口与 Bash 脚本一致
- ✅ Dapr 端口配置正确
- ✅ 容器清理列表完整

## 同步完成 ✅

所有 PowerShell 脚本已与 Bash 脚本同步,确保跨平台部署体验一致。

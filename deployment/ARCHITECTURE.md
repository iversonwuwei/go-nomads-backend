# Go-Nomads 基础设施架构

## 组件清单

| 组件 | 版本 | 端口 | 用途 |
|------|------|------|------|
| Redis | latest | 6379 | 配置中心 & 状态存储 |
| Consul | latest | 8500, 8502, 8600 | 服务注册 & 发现 |
| Zipkin | latest | 9411 | 分布式链路追踪 |
| Prometheus | latest | 9090 | 指标收集 & 查询 |
| Grafana | latest | 3000 | 监控可视化 |

## 网络架构

```
go-nomads-network (bridge)
├─ go-nomads-redis:6379
├─ go-nomads-consul:8500
├─ go-nomads-zipkin:9411
├─ go-nomads-prometheus:9090
└─ go-nomads-grafana:3000
```

## 数据流

### 配置管理
```
应用服务 → Dapr Sidecar → Redis (配置中心)
                          └─ 21 个配置项
```

### 服务注册
```
部署脚本 → Consul API → 服务注册表
                       ├─ gateway
                       ├─ product-service
                       └─ user-service
```

### 监控链路
```
应用/Dapr → Prometheus ← Consul SD (自动发现)
             ↓
          Grafana (可视化)
```

### 追踪链路
```
应用请求 → Dapr Sidecar → Zipkin Collector
                          ↓
                       Zipkin UI (查询)
```

## 自动化功能

### 1. 网络自动创建
- 检测网络是否存在
- 不存在则自动创建 `go-nomads-network`

### 2. 配置自动导入
Redis 自动导入 21 个配置项:
- `app-version`: 应用版本
- `environment`: 运行环境
- `*:endpoint`: 服务端点
- `logging:*`: 日志配置
- `tracing:*`: 追踪配置
- `features:*`: 功能开关
- `business:*`: 业务规则

### 3. 服务自动注册
Consul 自动注册服务:
- 服务名称: gateway, product-service, user-service
- 服务地址: 容器名称
- 服务端口: 8080
- 服务标签: dapr

### 4. 监控自动配置
Prometheus 自动配置:
- Consul 服务发现
- Dapr metrics (端口 9090)
- 应用 metrics (端口 8080)
- 基础设施 metrics (Redis, Zipkin)

## 服务发现机制

### Dapr 服务发现
- **机制**: mDNS (容器 DNS)
- **范围**: 同一网络内的容器
- **方式**: 通过容器名称解析 (如 `go-nomads-product-service`)

### Prometheus 服务发现
- **机制**: Consul SD API
- **刷新**: 每 30 秒自动更新
- **查询**: `http://go-nomads-consul:8500/v1/catalog/service/<service>`

## 容器依赖关系

```
无依赖层
├─ go-nomads-redis
├─ go-nomads-zipkin
└─ go-nomads-consul

配置依赖层
├─ go-nomads-prometheus (依赖 Consul)
└─ go-nomads-grafana

应用层 (需单独部署)
├─ go-nomads-gateway (依赖 Redis, Consul, Zipkin)
├─ go-nomads-product-service (依赖 Redis, Consul, Zipkin)
└─ go-nomads-user-service (依赖 Redis, Consul, Zipkin)
```

## 健康检查

当前配置不包含健康检查。如需添加:

### Consul 健康检查示例
```bash
consul services register \
  -name=product-service \
  -address=go-nomads-product-service \
  -port=8080 \
  -check-http=http://go-nomads-product-service:8080/health \
  -check-interval=10s \
  -check-timeout=3s
```

### 容器健康检查示例
```powershell
podman run -d \
  --health-cmd "curl -f http://localhost:8080/health || exit 1" \
  --health-interval=30s \
  --health-timeout=3s \
  --health-retries=3 \
  your-service-image
```

## 扩展性

### 水平扩展
支持多实例部署:
```bash
# 启动多个实例
podman run --name product-service-1 ...
podman run --name product-service-2 ...

# 分别注册到 Consul
consul services register -id=product-1 -name=product-service ...
consul services register -id=product-2 -name=product-service ...
```

Prometheus 会自动发现所有实例。

### 垂直扩展
为容器分配更多资源:
```bash
podman run -d \
  --cpus=2 \
  --memory=4g \
  your-service-image
```

## 安全考虑

### 当前配置 (开发环境)
- ❌ 无 TLS 加密
- ❌ 无访问控制
- ❌ 默认密码 (Grafana: admin/admin)
- ❌ 无网络隔离

### 生产环境建议
- ✅ 启用 Consul ACL
- ✅ 启用 mTLS (Dapr + Consul Connect)
- ✅ 使用强密码和密钥管理
- ✅ 网络隔离和防火墙规则
- ✅ 日志审计和监控告警

## 数据持久化

当前配置数据存储在容器内部,容器删除后数据丢失。

### 添加持久化卷
```powershell
# Redis 数据持久化
-v redis-data:/data

# Prometheus 数据持久化
-v prometheus-data:/prometheus

# Grafana 数据持久化
-v grafana-data:/var/lib/grafana

# Consul 数据持久化
-v consul-data:/consul/data
```

## 故障恢复

### 单容器故障
```bash
# 检查状态
podman ps -a

# 查看日志
podman logs go-nomads-redis

# 重启容器
podman restart go-nomads-redis
```

### 全部重启
```bash
.\deploy-infrastructure.ps1 restart
```

### 完全重建
```bash
.\deploy-infrastructure.ps1 clean
.\deploy-infrastructure.ps1 start
```

## 性能调优

### Redis
```bash
# 最大内存限制
-e REDIS_MAXMEMORY=2gb
-e REDIS_MAXMEMORY_POLICY=allkeys-lru
```

### Prometheus
```bash
# 数据保留期
--storage.tsdb.retention.time=30d

# 内存限制
--storage.tsdb.max-block-duration=2h
```

### Grafana
```bash
# 并发限制
-e GF_USERS_VIEWERS_CAN_EDIT=false
-e GF_ANALYTICS_REPORTING_ENABLED=false
```

## 监控指标

### 系统指标
- 容器 CPU/内存使用率
- 网络流量
- 磁盘 I/O

### 应用指标
- 请求延迟 (p50, p95, p99)
- 错误率
- 吞吐量 (QPS)

### 基础设施指标
- Redis: 连接数, 命令数, 内存使用
- Consul: 服务健康状态, 集群节点
- Prometheus: 查询延迟, 存储大小

## 日志管理

当前配置使用容器标准输出/错误输出。

### 查看日志
```bash
# 实时日志
podman logs -f go-nomads-consul

# 最近 100 行
podman logs --tail 100 go-nomads-redis

# 带时间戳
podman logs --timestamps go-nomads-prometheus
```

### 集中式日志 (可选)
集成 ELK/EFK Stack:
- Elasticsearch: 日志存储
- Logstash/Fluentd: 日志收集
- Kibana: 日志查询可视化

## 备份策略

### 配置备份
```bash
# 导出 Redis 配置
podman exec go-nomads-redis redis-cli BGSAVE

# 导出 Consul 数据
podman exec go-nomads-consul consul snapshot save backup.snap

# 备份 Prometheus 配置
cp prometheus/prometheus.yml prometheus.yml.backup
```

### 恢复
```bash
# 恢复 Redis
podman exec -i go-nomads-redis redis-cli < backup.rdb

# 恢复 Consul
podman exec go-nomads-consul consul snapshot restore backup.snap
```

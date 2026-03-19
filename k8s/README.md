# Go-Nomads Kubernetes 部署指南

## 目录结构

```
k8s/
├── base/                       # 基础配置
│   ├── namespace.yaml          # 命名空间
│   ├── configmap.yaml          # 配置映射
│   └── secrets.yaml            # 密钥配置
├── infrastructure/             # 基础设施服务
│   ├── redis.yaml              # Redis 缓存
│   ├── rabbitmq.yaml           # RabbitMQ 消息队列
│   ├── elasticsearch.yaml      # Elasticsearch 搜索
│   └── consul.yaml             # Consul 服务发现
├── services/                   # 业务服务
│   ├── gateway.yaml            # API 网关
│   ├── user-service.yaml       # 用户服务
│   ├── city-service.yaml       # 城市服务
│   ├── coworking-service.yaml  # 共享办公服务
│   ├── event-service.yaml      # 活动服务
│   ├── ai-service.yaml         # AI 服务
│   ├── message-service.yaml    # 消息服务
│   └── cache-service.yaml      # 缓存服务
├── overlays/                   # 环境特定配置
│   ├── dev/                    # 开发环境
│   ├── staging/                # 预发布环境
│   └── prod/                   # 生产环境
├── kustomization.yaml          # Kustomize 配置
├── deploy.sh                   # Linux/Mac 部署脚本
├── deploy.ps1                  # Windows 部署脚本
└── README.md                   # 本文档
```

## 快速开始

### 前提条件

1. 安装 kubectl
2. 配置好 Kubernetes 集群连接
3. 安装 Docker（用于构建镜像）
4. 配置 Docker Registry 凭证
5. 根据集群需要安装 Helm（可选，用于管理额外组件）

### 配置 Docker Registry

在部署前，需要设置 Docker Registry 环境变量：

```bash
# Linux/Mac
export DOCKER_REGISTRY="your-registry.com"
export IMAGE_TAG="latest"

# Windows PowerShell
$env:DOCKER_REGISTRY = "your-registry.com"
$env:IMAGE_TAG = "latest"
```

### 配置 Secrets

在 `k8s/base/secrets.yaml` 中配置实际的密钥值：

1. 数据库连接字符串
2. Supabase 配置
3. AI API Keys (OpenAI, QianWen, DeepSeek)
4. RabbitMQ 凭证

### 一键部署

**Linux/Mac:**
```bash
cd k8s
chmod +x deploy.sh
./deploy.sh deploy
```

**Windows PowerShell:**
```powershell
cd k8s
.\deploy.ps1 -Action deploy
```

## 部署脚本使用说明

### 可用操作

| 操作 | 说明 |
|------|------|
| `deploy` | 完整部署所有组件 |
| `delete` | 删除所有资源 |
| `status` | 查看部署状态 |
| `build` | 构建并推送 Docker 镜像 |
| `infrastructure` | 仅部署基础设施 |
| `services` | 仅部署业务服务 |

### 示例

```bash
# 完整部署所有服务
./deploy.sh deploy

# 查看状态
./deploy.sh status

# 删除所有资源
./deploy.sh delete

# 构建并推送镜像
./deploy.sh build

# 仅部署基础设施
./deploy.sh infrastructure

```

## 当前通信模型

### 服务发现与路由

- Kubernetes 内部服务通过 `Service` + DNS 互相访问
- 对外统一经由 Gateway 暴露 API
- 运行时服务发现与健康状态由 Consul 维护

### 服务间调用

服务间通过内部 HTTP API 和共享的 `ServiceInvocationClient` 进行通信，示例：

```text
http://user-service:8080/api/users/{id}
http://product-service:8080/api/products/user/{userId}
```

### 验证服务状态

```bash
# 查看业务 Pod
kubectl get pods -n go-nomads

# 查看业务 Service
kubectl get svc -n go-nomads

# 查看特定服务日志
kubectl logs <pod-name> -n go-nomads
```

## 使用 Kustomize 部署

### 开发环境

```bash
kubectl apply -k overlays/dev/
```

### 预发布环境

```bash
kubectl apply -k overlays/staging/
```

### 生产环境

```bash
kubectl apply -k overlays/prod/
```

## 服务架构

```
                    ┌─────────────────┐
                    │    Ingress      │
                    │  (Nginx/Traefik)│
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │   API Gateway   │
                    │    (port 80)    │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│ User Service  │   │ City Service  │   │  AI Service   │
│   (port 80)   │   │  (port 8002)  │   │  (port 8080)  │
└───────┬───────┘   └───────┬───────┘   └───────┬───────┘
        │                   │                   │
        └───────────────────┼───────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│     Redis     │   │   RabbitMQ    │   │ Elasticsearch │
│  (port 6379)  │   │  (port 5672)  │   │  (port 9200)  │
└───────────────┘   └───────────────┘   └───────────────┘
```

## 服务端口映射

| 服务 | 容器端口 | K8s Service 端口 |
|------|----------|------------------|
| Gateway | 8080 | 80 |
| User Service | 80 | 80 |
| City Service | 8002 | 8002 |
| Coworking Service | 8003 | 8003 |
| Event Service | 8005 | 8005 |
| AI Service | 8080 | 8080 |
| Message Service | 8010 | 8010 |
| Cache Service | 8011 | 8011 |
| Redis | 6379 | 6379 |
| RabbitMQ | 5672, 15672 | 5672, 15672 |
| Elasticsearch | 9200, 9300 | 9200, 9300 |
| Consul | 8500 | 8500 |

## 资源配额

### 开发环境

- 各服务副本数: 1
- 内存请求: 128Mi
- CPU 请求: 100m

### 预发布环境

- Gateway 副本数: 2
- 其他服务副本数: 1
- 默认资源配置

### 生产环境

- Gateway 副本数: 3
- AI Service 副本数: 3
- 其他服务副本数: 2
- 内存请求: 512Mi - 1Gi
- CPU 请求: 300m - 500m

## 健康检查

所有服务都配置了健康检查：

- **Readiness Probe**: 用于判断 Pod 是否可以接收流量
  - 初始延迟: 10秒
  - 检查间隔: 5秒
  
- **Liveness Probe**: 用于判断 Pod 是否需要重启
  - 初始延迟: 30秒
  - 检查间隔: 15秒

## 水平自动扩缩容 (HPA)

所有业务服务都配置了 HPA：

- CPU 利用率阈值: 70%
- 内存利用率阈值: 80%
- 最小副本数: 2 (生产环境)
- 最大副本数: 5-10

## 运维访问

部署完成后，可按需使用 Port Forward 访问基础设施服务：

```bash
# RabbitMQ 管理界面
kubectl port-forward svc/rabbitmq-service 15672:15672 -n go-nomads

# Consul UI
kubectl port-forward svc/consul-service 8500:8500 -n go-nomads
```

## 故障排查

### 查看 Pod 日志

```bash
kubectl logs -f <pod-name> -n go-nomads
```

### 查看 Pod 详情

```bash
kubectl describe pod <pod-name> -n go-nomads
```

### 进入 Pod 容器

```bash
kubectl exec -it <pod-name> -n go-nomads -- /bin/sh
```

### 查看资源使用情况

```bash
kubectl top pods -n go-nomads
kubectl top nodes
```

## 注意事项

1. **Secrets 管理**: 生产环境建议使用 Kubernetes Secrets 管理工具（如 Sealed Secrets 或 HashiCorp Vault）
2. **存储类**: 确保集群中有名为 `standard` 的 StorageClass，或修改 PVC 配置
3. **网络策略**: 生产环境建议添加 NetworkPolicy 限制服务间通信
4. **资源限制**: 根据实际负载调整资源配额
5. **备份**: 定期备份 PersistentVolume 中的数据

## 更新部署

### 滚动更新

```bash
# 更新镜像标签
kubectl set image deployment/gateway gateway=your-registry.com/go-nomads-gateway:v1.0.1 -n go-nomads

# 或使用 Kustomize
cd k8s/overlays/prod
kustomize edit set image ${DOCKER_REGISTRY}/go-nomads-gateway:v1.0.1
kubectl apply -k .
```

### 回滚

```bash
kubectl rollout undo deployment/gateway -n go-nomads
```

### 查看部署历史

```bash
kubectl rollout history deployment/gateway -n go-nomads
```

# Go-Nomads Infrastructure Helm Chart

## 概述

本 Helm Chart **仅用于部署基础设施服务**（对应 `docker-compose-infras.yml`）。

**业务服务**（`docker-compose.yml` 中定义）继续使用 **Kustomize** 部署（`k8s/` 目录）。

## 部署架构

```
┌─────────────────────────────────────────────────────┐
│                    Go-Nomads 部署                    │
├─────────────────────────────────────────────────────┤
│                                                     │
│  基础设施 (Helm)              业务服务 (Kustomize)   │
│  ──────────────              ──────────────────     │
│  • Redis                     • Gateway              │
│  • RabbitMQ                  • User Service         │
│  • Elasticsearch             • City Service         │
│  • Consul                    • Coworking Service    │
│  • Zipkin                    • Event Service        │
│  • Prometheus                • AI Service           │
│  • Grafana                   • Message Service      │
│                              • Cache Service        │
│                              • Accommodation Svc    │
│                                                     │
│  ./helm-deploy.sh            ./k8s/deploy.sh        │
└─────────────────────────────────────────────────────┘
```

## 前提条件

- Kubernetes 1.23+
- Helm 3.0+

## 目录结构

```
helm/go-nomads/
├── Chart.yaml              # Chart 元数据
├── values.yaml             # 默认配置
├── values-cce.yaml         # 华为云 CCE 配置（低资源）
├── .helmignore
└── templates/
    ├── _helpers.tpl        # 模板辅助函数
    ├── redis.yaml          # Redis
    ├── rabbitmq.yaml       # RabbitMQ
    ├── elasticsearch.yaml  # Elasticsearch
    ├── consul.yaml         # Consul
    ├── zipkin.yaml         # Zipkin
    ├── prometheus.yaml     # Prometheus
    └── grafana.yaml        # Grafana
```

## 快速开始

### 1. 部署基础设施 (Helm)

```bash
# 开发环境
./helm-deploy.sh dev install

# 华为云 CCE
./helm-deploy.sh cce install

# 或直接用 Helm
helm install go-nomads-infra ./helm/go-nomads -n go-nomads --create-namespace
```

### 2. 部署业务服务 (Kustomize)

```bash
# 使用 Kustomize
kubectl apply -k k8s/overlays/dev

# 或使用部署脚本
./k8s/deploy.sh dev deploy
```

### 3. 一键部署全部

```bash
# 先部署基础设施
./helm-deploy.sh dev install

# 再部署业务服务
./k8s/deploy.sh dev deploy
```

## 配置说明

### 启用/禁用组件

```yaml
# values.yaml
redis:
  enabled: true       # 默认启用

rabbitmq:
  enabled: true       # 默认启用

elasticsearch:
  enabled: false      # 默认禁用（资源消耗大）

consul:
  enabled: false      # 默认禁用

zipkin:
  enabled: false      # 默认禁用

prometheus:
  enabled: false      # 默认禁用

grafana:
  enabled: false      # 默认禁用
```

### 启用监控套件

```bash
helm install go-nomads-infra ./helm/go-nomads \
  --set prometheus.enabled=true \
  --set grafana.enabled=true \
  --set zipkin.enabled=true
```

### 持久化存储

```yaml
redis:
  persistence:
    enabled: true
    size: 5Gi

rabbitmq:
  persistence:
    enabled: true
    size: 5Gi
```

## 常用命令

```bash
# 安装
helm install go-nomads-infra ./helm/go-nomads -n go-nomads --create-namespace

# 升级
helm upgrade go-nomads-infra ./helm/go-nomads -n go-nomads

# 查看状态
helm status go-nomads-infra -n go-nomads

# 渲染模板（调试）
./helm-deploy.sh dev template

# 卸载
helm uninstall go-nomads-infra -n go-nomads
```

## CCE 部署

华为云 CCE 使用 `values-cce.yaml`，特点：

- 使用 `default` 命名空间
- 禁用持久化（需单独配置存储类）
- 降低资源请求

```bash
./helm-deploy.sh cce install
```

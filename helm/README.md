# Go-Nomads Helm Chart

## 概述

本 Helm Chart 用于部署 Go-Nomads 微服务平台到 Kubernetes 集群。

## 前提条件

- Kubernetes 1.23+
- Helm 3.0+
- 已配置的镜像仓库（默认: 华为云 SWR）

## 目录结构

```
helm/go-nomads/
├── Chart.yaml              # Chart 元数据
├── values.yaml             # 默认配置
├── values-cce.yaml         # 华为云 CCE 配置
├── templates/
│   ├── _helpers.tpl        # 模板辅助函数
│   ├── _microservice.tpl   # 微服务通用模板
│   ├── namespace.yaml      # 命名空间
│   ├── configmap.yaml      # 配置映射
│   ├── secrets.yaml        # 密钥
│   ├── gateway.yaml        # API 网关
│   ├── user-service.yaml   # 用户服务
│   ├── city-service.yaml   # 城市服务
│   ├── coworking-service.yaml
│   ├── accommodation-service.yaml
│   ├── event-service.yaml
│   ├── ai-service.yaml
│   ├── message-service.yaml
│   ├── cache-service.yaml
│   ├── redis.yaml          # Redis
│   └── rabbitmq.yaml       # RabbitMQ
```

## 快速开始

### 1. 安装到本地集群

```bash
# 使用默认配置
helm install go-nomads ./helm/go-nomads

# 或使用部署脚本
./helm-deploy.sh dev install
```

### 2. 部署到华为云 CCE

```bash
# 设置 SWR 凭证
export SWR_USERNAME="ap-southeast-3@your-ak"
export SWR_PASSWORD="your-login-key"

# 部署
./helm-deploy.sh cce install

# 或手动部署
helm install go-nomads ./helm/go-nomads \
  -f ./helm/go-nomads/values-cce.yaml \
  -n default
```

### 3. 渲染模板（调试）

```bash
# 渲染为 YAML 文件
./helm-deploy.sh cce template

# 查看输出
cat k8s/manifests/helm-rendered-cce.yaml
```

## 配置说明

### 全局配置

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `global.namespace` | 命名空间 | `go-nomads` |
| `global.imageRegistry` | 镜像仓库 | `swr.ap-southeast-3.myhuaweicloud.com/go-nomads` |
| `global.imageTag` | 镜像标签 | `latest` |
| `global.imagePullPolicy` | 拉取策略 | `Always` |

### 服务配置

每个服务都支持以下配置：

| 参数 | 说明 |
|------|------|
| `<service>.enabled` | 是否启用 |
| `<service>.replicaCount` | 副本数 |
| `<service>.image.repository` | 镜像名称 |
| `<service>.image.tag` | 镜像标签（覆盖全局） |
| `<service>.resources` | 资源限制 |
| `<service>.autoscaling.enabled` | 是否启用 HPA |

### Secrets 配置

部署时需要覆盖以下敏感配置：

```yaml
secrets:
  databaseConnectionString: "实际数据库连接字符串"
  supabaseUrl: "https://your-project.supabase.co"
  supabaseKey: "your-supabase-key"
  # ...
```

可以通过以下方式覆盖：

```bash
helm install go-nomads ./helm/go-nomads \
  --set secrets.supabaseUrl="https://xxx.supabase.co" \
  --set secrets.supabaseKey="your-key"
```

## 常用命令

```bash
# 安装
helm install go-nomads ./helm/go-nomads -n go-nomads --create-namespace

# 升级
helm upgrade go-nomads ./helm/go-nomads -n go-nomads

# 查看状态
helm status go-nomads -n go-nomads

# 查看已渲染的值
helm get values go-nomads -n go-nomads

# 卸载
helm uninstall go-nomads -n go-nomads
```

## 从 Kustomize 迁移

原有的 Kustomize 配置位于 `k8s/` 目录，现已迁移到 Helm。主要变化：

1. **配置管理**: 从 `kustomization.yaml` 迁移到 `values.yaml`
2. **环境区分**: 从 `overlays/` 迁移到 `values-<env>.yaml`
3. **模板化**: 使用 Helm 模板替代 Kustomize patches
4. **部署脚本**: 从 `deploy.sh` 迁移到 `helm-deploy.sh`

## 故障排除

### 镜像拉取失败

```bash
# 检查 Secret 是否存在
kubectl get secret docker-registry-secret -n <namespace>

# 手动创建 Secret
kubectl create secret docker-registry docker-registry-secret \
  --docker-server=swr.ap-southeast-3.myhuaweicloud.com \
  --docker-username="ap-southeast-3@xxx" \
  --docker-password="xxx" \
  -n <namespace>
```

### 资源不足

修改 `values-cce.yaml` 中的资源限制：

```yaml
gateway:
  resources:
    requests:
      memory: "64Mi"
      cpu: "25m"
```

### 查看 Pod 日志

```bash
kubectl logs -l app=gateway -n <namespace> -f
```

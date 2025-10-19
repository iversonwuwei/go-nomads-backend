# Prometheus 配置清理记录

## 清理日期
2025-10-20

## 清理内容

### 删除的文件
- ✅ `deployment/prometheus/prometheus-local.yml` - 静态配置文件已删除

### 原因
该配置文件**不是必需的**，因为：
1. `deploy-infrastructure-local.sh` 脚本每次运行时会**自动生成**配置文件
2. 静态配置文件只是上次运行的遗留物
3. 删除后不影响系统运行

## 更新的脚本

### 1. `deploy-infrastructure-local.sh`
**更新内容**：配置生成逻辑改为全自动 Consul 服务发现

**修改前**：
```yaml
scrape_configs:
  - job_name: 'services'
    static_configs:
      - targets:
          - 'go-nomads-gateway:8080'
          - 'go-nomads-user-service:8080'
          - 'go-nomads-product-service:8080'
          - 'go-nomads-document-service:8080'
  
  - job_name: 'consul-services'
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        services:
          - 'gateway'
          - 'user-service'
          - 'product-service'
          - 'document-service'
```

**修改后**：
```yaml
scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
  
  # 完全依赖 Consul 自动服务发现 - 无需手动配置服务列表
  - job_name: 'consul-services'
    metrics_path: /metrics
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        # 不指定 services，自动发现所有已注册的服务
    relabel_configs:
      # 只抓取有 metrics_path 元数据的服务
      - source_labels: [__meta_consul_service_metadata_metrics_path]
        action: keep
        regex: /.+
      
      # 服务名称、版本、协议等标签配置
      - source_labels: [__meta_consul_service]
        target_label: service
      - source_labels: [__meta_consul_service_metadata_version]
        target_label: version
      - source_labels: [__meta_consul_service_metadata_protocol]
        target_label: protocol
      - source_labels: [__address__]
        target_label: instance
```

### 2. `deploy-infrastructure.sh`
**更新内容**：同样改为全自动 Consul 服务发现，移除了旧的 Dapr 特定配置

**修改前**：
```yaml
scrape_configs:
  - job_name: 'dapr-services'
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        services: ['product-service', 'user-service', 'gateway']
        tags: ['dapr']
  
  - job_name: 'app-services'
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        services: ['product-service', 'user-service', 'gateway']
        tags: ['dapr']
```

**修改后**：
```yaml
scrape_configs:
  - job_name: 'consul-services'
    metrics_path: /metrics
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        # 不指定 services，自动发现所有已注册的服务
    relabel_configs:
      # （同上）
```

## 工作流程

### 部署时的配置生成流程
```bash
# 1. 运行部署脚本
./deploy-infrastructure-local.sh

# 2. 脚本自动执行以下操作：
#    a. 创建 deployment/prometheus 目录
#    b. 生成 prometheus-local.yml 配置文件（使用 cat <<'EOF'）
#    c. 启动 Prometheus 容器，挂载生成的配置文件

# 3. Prometheus 启动后：
#    a. 连接到 Consul (go-nomads-consul:8500)
#    b. 自动发现所有注册的服务
#    c. 抓取服务的 /metrics 端点
```

### 添加新服务的流程
```bash
# 1. 创建新服务项目
# 2. 在 Program.cs 中添加：
#    - using Shared.Extensions;
#    - await app.RegisterWithConsulAsync();

# 3. 在 appsettings.Development.json 中配置 Consul 信息

# 4. 部署服务
./deploy-services-local.sh

# 5. 无需任何其他操作！
#    ✅ 服务自动注册到 Consul
#    ✅ Prometheus 自动发现服务
#    ✅ Grafana 自动显示监控指标
```

## 验证结果

### Prometheus 配置自动生成
```bash
$ ls -lh deployment/prometheus/
total 8
-rw-r--r--  1 walden  staff   1.2K 10月 20 00:02 prometheus-local.yml
# ✅ 配置文件由脚本自动生成
```

### Consul 服务注册
```bash
$ curl -s http://localhost:8500/v1/agent/services | jq 'keys'
[
  "document-service-...",
  "gateway-...",
  "product-service-...",
  "user-service-..."
]
# ✅ 所有 4 个服务自动注册
```

### Prometheus 服务发现
```bash
$ curl -s http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | select(.labels.job == "consul-services")'
{
  "service": "product-service",
  "instance": "go-nomads-product-service:8080",
  "health": "up"
}
{
  "service": "gateway",
  "instance": "go-nomads-gateway:8080",
  "health": "up"
}
{
  "service": "document-service",
  "instance": "go-nomads-document-service:8080",
  "health": "up"
}
{
  "service": "user-service",
  "instance": "go-nomads-user-service:8080",
  "health": "up"
}
# ✅ Prometheus 自动发现所有服务
```

## 优势

### 1. 零手动配置
- ❌ 不需要手动编辑 `prometheus-local.yml`
- ❌ 不需要手动添加服务目标
- ❌ 不需要重启 Prometheus
- ✅ 只需部署服务即可

### 2. 配置一致性
- 所有环境使用相同的配置生成逻辑
- 脚本确保配置格式正确
- 消除手动编辑导致的语法错误

### 3. 简化维护
- 新增服务：只需部署即可
- 删除服务：停止容器即自动注销
- 更新服务：重启容器即可

## 总结

通过删除静态配置文件并更新部署脚本，我们实现了：

1. **完全自动化**：从服务注册到监控发现的全流程自动化
2. **零手动配置**：无需编辑任何配置文件
3. **配置一致性**：脚本保证配置正确性
4. **简化运维**：添加新服务只需 3 步（创建、配置、部署）

现在整个系统真正实现了"**全自动服务发现和监控**"！🚀

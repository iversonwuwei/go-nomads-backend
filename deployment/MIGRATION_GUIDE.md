# 📝 将现有服务迁移到自动注册

## 快速迁移步骤

### 1️⃣ 更新 Program.cs

在现有服务的 `Program.cs` 中：

```diff
+ using Shared.Extensions;
  using Prometheus;

  var app = builder.Build();
  
  // ... 现有配置 ...
  
  app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "YourService" }));
  app.MapMetrics();
  
+ // 添加自动注册
+ await app.RegisterWithConsulAsync();

  app.Run();
```

### 2️⃣ 更新 appsettings.Development.json

添加 Consul 配置节：

```json
{
  "Consul": {
    "Address": "http://go-nomads-consul:8500",
    "ServiceName": "your-service-name",
    "ServiceAddress": "go-nomads-your-service",
    "ServicePort": 8080,
    "HealthCheckPath": "/health",
    "HealthCheckInterval": "10s",
    "ServiceVersion": "1.0.0"
  }
}
```

### 3️⃣ 重新构建和部署

```bash
# 重新构建镜像
docker build -t go-nomads-your-service:latest -f path/to/Dockerfile .

# 停止旧容器
docker stop go-nomads-your-service
docker rm go-nomads-your-service

# 启动新容器（会自动注册）
docker run -d \
  --name go-nomads-your-service \
  --network go-nomads-network \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -p 5001:8080 \
  go-nomads-your-service:latest
```

### 4️⃣ 验证

```bash
# 检查日志中的注册信息
docker logs go-nomads-your-service | grep Consul

# 验证 Consul 注册
curl http://localhost:8500/v1/catalog/service/your-service-name

# 等待 15 秒后检查 Prometheus
sleep 15
curl http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | select(.labels.service=="your-service-name")'
```

## ✅ 迁移检查清单

- [ ] 添加 `using Shared.Extensions;`
- [ ] 调用 `await app.RegisterWithConsulAsync();`
- [ ] 添加 Consul 配置到 `appsettings.Development.json`
- [ ] 确保有 `/health` 端点
- [ ] 确保有 `/metrics` 端点
- [ ] 重新构建 Docker 镜像
- [ ] 重新部署容器
- [ ] 验证 Consul 注册成功
- [ ] 验证 Prometheus 抓取指标
- [ ] 验证 Grafana 显示数据

## 🔄 批量迁移脚本

如果需要同时迁移多个服务：

```bash
#!/bin/bash
SERVICES=("user-service" "product-service" "document-service" "gateway")

for service in "${SERVICES[@]}"; do
  echo "🔄 Migrating ${service}..."
  
  # 添加配置（假设服务已更新代码）
  # 重新构建
  docker build -t go-nomads-${service}:latest -f src/Services/${service}/Dockerfile .
  
  # 重启服务
  docker stop go-nomads-${service}
  docker rm go-nomads-${service}
  docker run -d \
    --name go-nomads-${service} \
    --network go-nomads-network \
    -e ASPNETCORE_ENVIRONMENT=Development \
    go-nomads-${service}:latest
    
  echo "✅ ${service} migrated"
  sleep 5
done

echo "🎉 All services migrated!"
```

## 📊 迁移后的优势

### 之前的流程：
```
创建服务 → 编写代码 → 创建 Consul JSON → 手动注册 → 更新 Prometheus 配置 → 重启 Prometheus → 验证
```

### 现在的流程：
```
创建服务 → 编写代码 → 添加 2 行配置 → 部署 ✨
```

**节省时间：** 从 15 分钟缩短到 2 分钟！

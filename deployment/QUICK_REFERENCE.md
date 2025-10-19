# ⚡ 快速参考：创建自动注册服务

## 🎯 一行命令创建新服务

```bash
./scripts/create-auto-register-service.sh order-service 5005
```

## 📝 手动创建（3 步骤）

### 1. Program.cs
```csharp
using Shared.Extensions;

await app.RegisterWithConsulAsync();
app.Run();
```

### 2. appsettings.Development.json
```json
{
  "Consul": {
    "Address": "http://go-nomads-consul:8500",
    "ServiceName": "order-service",
    "ServiceAddress": "go-nomads-order-service",
    "ServicePort": 8080
  }
}
```

### 3. 部署
```bash
docker build -t go-nomads-order-service:latest .
docker run -d --name go-nomads-order-service --network go-nomads-network go-nomads-order-service:latest
```

## ✅ 验证

```bash
# 1. 检查 Consul
curl http://localhost:8500/v1/catalog/service/order-service

# 2. 检查 Prometheus（等待 30 秒）
curl http://localhost:9090/api/v1/targets | jq '.data.activeTargets[] | select(.labels.service=="order-service")'

# 3. 查看 Grafana
open http://localhost:3000/d/go-nomads-services
```

## 🎉 完成！

服务会自动：
- ✅ 注册到 Consul
- ✅ 被 Prometheus 发现
- ✅ 出现在 Grafana Dashboard
- ✅ 下线时自动注销

**无需手动配置任何文件！**

# 🚀 自动化服务注册方案总结

## ❌ 之前的问题

每次创建新服务都需要：

1. **手动创建 Consul 服务定义文件**
   ```bash
   deployment/consul/services/new-service.json
   ```

2. **手动更新 Prometheus 配置**
   ```yaml
   # 需要编辑 prometheus-local.yml
   - job_name: 'services'
     static_configs:
       - targets: ['go-nomads-new-service:8080']
   ```

3. **手动注册到 Consul**
   ```bash
   curl -X PUT --data @new-service.json http://localhost:8500/v1/agent/service/register
   ```

4. **重启 Prometheus 加载新配置**
   ```bash
   docker restart go-nomads-prometheus
   ```

**总耗时：15-20 分钟，容易出错！**

---

## ✅ 新的自动化方案

### 核心改进

#### 1. 服务自注册机制

创建了 `Shared/Extensions/ConsulServiceRegistration.cs`：

```csharp
public static async Task RegisterWithConsulAsync(this WebApplication app)
{
    // 自动从配置读取服务信息
    // 启动时注册到 Consul
    // 关闭时自动注销
}
```

**特性：**
- ✅ 从 `appsettings.json` 读取配置
- ✅ 自动获取服务地址和端口
- ✅ 自动配置健康检查
- ✅ 服务下线时自动注销
- ✅ 支持容器和本地环境

#### 2. Prometheus 完全自动发现

更新了 `prometheus-local.yml`：

```yaml
- job_name: 'consul-services'
  consul_sd_configs:
    - server: 'go-nomads-consul:8500'
      # 不指定 services，自动发现所有服务
  relabel_configs:
    # 自动添加 service、version、protocol 标签
```

**特性：**
- ✅ 无需手动配置 targets
- ✅ 新服务自动被发现（15-30 秒内）
- ✅ 服务下线自动移除
- ✅ 无需重启 Prometheus

---

## 📦 创建新服务现在只需 3 步

### 方法 1: 使用自动化脚本（推荐）

```bash
# 一键创建完整的服务结构
./scripts/create-auto-register-service.sh order-service 5005

# 构建镜像
docker build -t go-nomads-order-service:latest -f src/Services/OrderService/OrderService/Dockerfile .

# 启动服务（自动注册！）
docker run -d \
  --name go-nomads-order-service \
  --network go-nomads-network \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -p 5005:8080 \
  go-nomads-order-service:latest
```

**完成！服务会自动：**
1. 注册到 Consul
2. 被 Prometheus 发现
3. 出现在 Grafana Dashboard

### 方法 2: 手动创建服务

#### Step 1: 在 Program.cs 中添加 2 行代码

```csharp
using Shared.Extensions;

// ... 现有代码 ...

await app.RegisterWithConsulAsync();  // ← 只需添加这一行
app.Run();
```

#### Step 2: 配置 appsettings.Development.json

```json
{
  "Consul": {
    "Address": "http://go-nomads-consul:8500",
    "ServiceName": "new-service",
    "ServiceAddress": "go-nomads-new-service",
    "ServicePort": 8080,
    "HealthCheckPath": "/health",
    "HealthCheckInterval": "10s",
    "ServiceVersion": "1.0.0"
  }
}
```

#### Step 3: 部署

```bash
docker build -t go-nomads-new-service:latest -f path/to/Dockerfile .
docker run -d --name go-nomads-new-service --network go-nomads-network go-nomads-new-service:latest
```

---

## 🔄 工作流程对比

### ❌ 之前（手动配置）

```
创建服务代码
    ↓
创建 Consul JSON 配置
    ↓
编辑 Prometheus YAML
    ↓
手动注册到 Consul
    ↓
重启 Prometheus
    ↓
等待 5-10 分钟验证
```

### ✅ 现在（自动化）

```
创建服务代码 + 2 行配置
    ↓
部署容器
    ↓
等待 30 秒
    ↓
完成！✨
```

---

## 📊 技术实现细节

### 自动注册过程

1. **服务启动**
   ```
   app.Run() 前调用 RegisterWithConsulAsync()
   ```

2. **读取配置**
   ```csharp
   var consulConfig = configuration.GetSection("Consul");
   var serviceName = consulConfig["ServiceName"];
   ```

3. **向 Consul 注册**
   ```http
   PUT /v1/agent/service/register
   {
     "ID": "user-service-abc123",
     "Name": "user-service",
     "Address": "go-nomads-user-service",
     "Port": 8080,
     "Check": {
       "HTTP": "http://go-nomads-user-service:8080/health",
       "Interval": "10s"
     },
     "Meta": {
       "metrics_path": "/metrics",
       "version": "1.0.0"
     }
   }
   ```

4. **Prometheus 定期查询 Consul**
   ```
   每 15 秒: GET /v1/catalog/services
   发现新服务 → 开始抓取 /metrics
   ```

5. **服务关闭时自动注销**
   ```csharp
   lifetime.ApplicationStopping.Register(async () => {
       await httpClient.PutAsync($"{consulAddress}/v1/agent/service/deregister/{serviceId}");
   });
   ```

---

## 🎯 配置说明

### Consul 配置项

| 配置项 | 说明 | 示例 | 必需 |
|--------|------|------|------|
| `Address` | Consul 服务器地址 | `http://go-nomads-consul:8500` | ✅ |
| `ServiceName` | 服务名称（kebab-case） | `user-service` | ✅ |
| `ServiceAddress` | 服务地址/主机名 | `go-nomads-user-service` | ✅ |
| `ServicePort` | 服务端口 | `8080` | ✅ |
| `HealthCheckPath` | 健康检查路径 | `/health` | ❌ (默认 `/health`) |
| `HealthCheckInterval` | 检查间隔 | `10s` | ❌ (默认 `10s`) |
| `ServiceVersion` | 服务版本 | `1.0.0` | ❌ (默认 `1.0.0`) |

---

## 🔍 验证和监控

### 1. 检查服务注册

```bash
# Consul UI
open http://localhost:8500/ui/dc1/services

# API
curl http://localhost:8500/v1/catalog/service/your-service-name
```

### 2. 检查 Prometheus 发现

```bash
# Targets 页面
open http://localhost:9090/targets

# API 查询
curl 'http://localhost:9090/api/v1/targets' | jq '.data.activeTargets[] | select(.labels.service=="your-service-name")'
```

### 3. 查看 Grafana Dashboard

```bash
open http://localhost:3000/d/go-nomads-services
```

新服务会在 15-30 秒内自动出现！

---

## 📁 项目文件清单

### 新增文件

1. **`src/Shared/Shared/Extensions/ConsulServiceRegistration.cs`**
   - 服务自注册扩展方法

2. **`scripts/create-auto-register-service.sh`**
   - 一键创建新服务脚本

3. **`deployment/AUTO_SERVICE_REGISTRATION.md`**
   - 自动注册完整文档

4. **`deployment/MIGRATION_GUIDE.md`**
   - 现有服务迁移指南

### 修改文件

1. **`deployment/prometheus/prometheus-local.yml`**
   - 移除 static_configs 的 services job
   - 启用完全自动发现

2. **`src/Services/UserService/UserService/Program.cs`**
   - 添加 `await app.RegisterWithConsulAsync();`

3. **`src/Services/UserService/UserService/appsettings.Development.json`**
   - 添加 Consul 配置节

---

## 🎉 优势总结

| 方面 | 之前 | 现在 |
|------|------|------|
| **配置复杂度** | 需要 3 个文件 | 只需 1 个配置节 |
| **部署时间** | 15-20 分钟 | 2-3 分钟 |
| **错误风险** | 高（手动配置） | 低（自动化） |
| **服务发现时间** | 需手动重启 | 15-30 秒自动 |
| **维护成本** | 每个服务都需操作 | 零维护 |
| **可扩展性** | 差（手动扩展） | 优秀（自动扩展） |

---

## 🚀 下一步建议

### 已完成
- ✅ 服务自注册机制
- ✅ Prometheus 自动发现
- ✅ 自动化创建脚本
- ✅ 完整文档

### 可选优化
- [ ] 添加服务健康检查配置选项（TTL、TCP等）
- [ ] 支持多数据中心 Consul
- [ ] 添加服务标签自定义功能
- [ ] 集成分布式追踪自动注册
- [ ] 添加 CI/CD 自动化部署

---

## 💡 最佳实践

1. **服务命名规范**
   - 使用 kebab-case: `user-service`, `order-service`
   - 保持与容器名一致性

2. **健康检查端点**
   - 必须返回 200 OK
   - 响应时间 < 5 秒
   - 检查数据库连接等关键依赖

3. **Metrics 端点**
   - 使用标准 `/metrics` 路径
   - 包含基础指标（HTTP、CPU、内存）
   - 添加业务指标

4. **版本管理**
   - 使用语义化版本号
   - 在 Consul 元数据中记录版本
   - 支持蓝绿部署

---

## 📞 支持和故障排查

### 常见问题

**Q: 服务没有注册到 Consul？**
- 检查 Consul 地址是否正确
- 查看服务日志: `docker logs go-nomads-your-service | grep Consul`
- 验证网络连接: `docker exec go-nomads-your-service ping go-nomads-consul`

**Q: Prometheus 没有抓取指标？**
- 等待 15-30 秒（Consul SD 刷新周期）
- 检查服务是否有 `metrics_path` 元数据
- 验证 `/metrics` 端点可访问

**Q: Grafana 没有显示数据？**
- 确认 Prometheus 正在抓取指标
- 检查 Dashboard datasource UID
- 生成测试流量

---

## 📚 参考资料

- [Consul Service Discovery](https://developer.hashicorp.com/consul/docs/discovery)
- [Prometheus Consul SD](https://prometheus.io/docs/prometheus/latest/configuration/configuration/#consul_sd_config)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [Prometheus .NET Exporter](https://github.com/prometheus-net/prometheus-net)

---

**创建时间:** 2025-10-19
**最后更新:** 2025-10-19
**版本:** 1.0.0

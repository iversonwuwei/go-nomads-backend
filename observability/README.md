# Go Nomads 可观测性方案

企业级可观测性解决方案：**OpenTelemetry + Jaeger + Prometheus + Grafana**

## 架构概览

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Go Nomads 微服务                              │
│  ┌─────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │ Gateway │  │ UserService │  │ CityService │  │ ... Others  │    │
│  └────┬────┘  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘    │
│       │              │                │                │            │
│       └──────────────┴────────────────┴────────────────┘            │
│                              │                                       │
│                    OpenTelemetry SDK                                 │
│                    (Traces + Metrics + Logs)                        │
└──────────────────────────────┼──────────────────────────────────────┘
                               │
           ┌───────────────────┼───────────────────┐
           │                   │                   │
           ▼                   ▼                   ▼
    ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
    │    Jaeger    │   │  Prometheus  │   │   Grafana    │
    │   (Traces)   │   │  (Metrics)   │   │ (Dashboard)  │
    │  :16686      │   │  :9090       │   │  :3000       │
    └──────────────┘   └──────────────┘   └──────────────┘
```

## 快速开始

### 1. 启动可观测性基础设施

```bash
# Windows PowerShell
.\start-observability.ps1

# Linux/Mac
./start-observability.sh
```

### 2. 启动微服务

```bash
docker-compose up -d
```

### 3. 访问可观测性工具

| 工具 | 地址 | 用途 |
|------|------|------|
| Jaeger UI | http://localhost:16686 | 分布式追踪查看 |
| Prometheus | http://localhost:9090 | 指标查询 |
| Grafana | http://localhost:3000 | 可视化仪表板 |

> Grafana 默认登录：`admin` / `admin`

## 核心功能

### 1. 分布式追踪 (Traces)

- **自动 HTTP 请求追踪**：自动追踪所有入站和出站 HTTP 请求
- **跨服务追踪**：通过 Trace ID 关联跨服务的请求链路
- **自定义 Span**：支持创建业务操作的自定义追踪

```csharp
// 使用 ActivitySource 创建自定义 Span
using var activity = _activitySource.StartActivity("ProcessOrder");
activity?.SetTag("order.id", orderId);
activity?.SetTag("customer.id", customerId);

// 记录事件
activity?.AddEvent(new ActivityEvent("OrderValidated"));

// 记录异常
if (error != null)
{
    activity?.RecordException(error);
    activity?.SetStatus(ActivityStatusCode.Error);
}
```

### 2. 指标监控 (Metrics)

#### 内置指标

- **HTTP 请求指标**：请求数、响应时间、错误率
- **运行时指标**：GC、线程池、内存使用
- **进程指标**：CPU、内存、文件描述符

#### 自定义业务指标

```csharp
// 注入 GoNomadsMetrics
public class OrderService
{
    private readonly GoNomadsMetrics _metrics;

    // 记录业务操作
    using var timer = _metrics.StartBusinessOperation("CreateOrder");
    try
    {
        // 业务逻辑
        timer.SetResult("success");
    }
    catch (Exception ex)
    {
        timer.SetError(ex.GetType().Name);
        throw;
    }

    // 记录外部服务调用
    using var serviceTimer = _metrics.StartExternalServiceCall("PaymentService", "ProcessPayment");
    
    // 记录缓存操作
    _metrics.RecordCacheHit("redis", "user:123");
    _metrics.RecordCacheMiss("redis", "user:456");
    
    // 记录认证
    _metrics.RecordAuthenticationAttempt(success: true);
}
```

### 3. 日志关联

日志自动与追踪关联，支持通过 Trace ID 查找相关日志：

```csharp
// 日志自动包含 TraceId 和 SpanId
_logger.LogInformation("Processing order {OrderId}", orderId);
```

## 配置说明

### 应用配置 (appsettings.json)

```json
{
  "Observability": {
    "JaegerEnabled": true,
    "JaegerEndpoint": "http://jaeger:4317",
    "PrometheusEnabled": true,
    "OtlpEnabled": false,
    "OtlpEndpoint": "http://otel-collector:4317",
    "ConsoleExporterEnabled": false,
    "SamplingRate": 1.0
  }
}
```

### 环境变量配置

```yaml
environment:
  - Observability__JaegerEnabled=true
  - Observability__JaegerEndpoint=http://go-nomads-jaeger:4317
  - Observability__PrometheusEnabled=true
```

## 在服务中集成

### 1. 添加依赖

项目已引用 `Shared` 项目，无需额外添加包。

### 2. 配置 Program.cs

```csharp
using GoNomads.Shared.Observability;

const string serviceName = "my-service";

var builder = WebApplication.CreateBuilder(args);

// 添加可观测性
builder.Services.AddGoNomadsObservability(builder.Configuration, serviceName);
builder.Logging.AddGoNomadsLogging(builder.Configuration, serviceName);

var app = builder.Build();

// 使用追踪中间件
app.UseGoNomadsTracing();

// 使用 Prometheus 端点
app.UseGoNomadsObservability();
```

## Grafana 仪表板

预配置的仪表板包含：

1. **服务概览**
   - 在线服务数
   - 请求速率 (RPS)
   - 响应时间 (P95)
   - 成功率

2. **资源使用**
   - 内存使用
   - CPU 使用率
   - GC 统计

3. **业务指标**
   - 业务操作数
   - 缓存命中率
   - 错误分布

## 告警规则

预配置的告警规则：

| 告警名称 | 条件 | 级别 |
|---------|------|------|
| ServiceDown | 服务 1 分钟无响应 | Critical |
| HighResponseTime | P95 响应时间 > 2s | Warning |
| HighErrorRate | 5xx 错误率 > 5% | Critical |
| HighMemoryUsage | 内存 > 500MB | Warning |
| HighAuthenticationFailureRate | 认证失败率 > 10% | Warning |
| LowCacheHitRate | 缓存命中率 < 50% | Warning |

## 最佳实践

### 1. 追踪命名规范

```csharp
// 使用 verb.noun 格式
"db.query"
"http.get"
"cache.lookup"
"message.publish"
"order.process"
```

### 2. 标签使用

```csharp
// 添加有意义的标签
activity?.SetTag("customer.tier", "premium");
activity?.SetTag("order.total", 199.99);
activity?.SetTag("payment.method", "credit_card");
```

### 3. 采样策略

生产环境建议使用 Tail Sampling：

```yaml
# 只采样有错误或高延迟的追踪
tail_sampling:
  policies:
    - name: errors
      type: status_code
      status_codes: [ERROR]
    - name: slow
      type: latency
      threshold_ms: 1000
```

### 4. 敏感数据处理

```csharp
// 不要记录敏感信息
// ❌ 错误
activity?.SetTag("user.password", password);
activity?.SetTag("credit.card", cardNumber);

// ✅ 正确
activity?.SetTag("user.id", userId);
activity?.SetTag("payment.masked", "****1234");
```

## 故障排查

### Jaeger 无法接收追踪

1. 检查 Jaeger 服务状态：`docker logs go-nomads-jaeger`
2. 确认端口 4317 可访问
3. 检查防火墙设置

### Prometheus 无指标数据

1. 访问 `/metrics` 端点确认指标暴露
2. 检查 Prometheus targets：http://localhost:9090/targets
3. 确认 prometheus.yml 配置正确

### Grafana 无数据

1. 检查数据源配置
2. 确认时间范围选择正确
3. 检查查询语句

## 扩展阅读

- [OpenTelemetry .NET 文档](https://opentelemetry.io/docs/instrumentation/net/)
- [Jaeger 文档](https://www.jaegertracing.io/docs/)
- [Prometheus 文档](https://prometheus.io/docs/)
- [Grafana 文档](https://grafana.com/docs/)

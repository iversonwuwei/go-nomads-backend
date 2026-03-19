# EventService 部署指南

## 服务概览

- 服务地址: <http://localhost:8005>
- Health Check: <http://localhost:8005/health>
- Scalar 文档: <http://localhost:8005/scalar/v1>
- OpenAPI JSON: <http://localhost:8005/openapi/v1.json>
- Prometheus Metrics: <http://localhost:8005/metrics>

## 当前运行模型

EventService 已不再依赖 sidecar 运行时。

- 服务注册: Consul
- 同步调用: 内部 HTTP API + ServiceInvocationClient
- 异步消息: RabbitMQ + MassTransit
- 数据存储: Supabase / PostgreSQL
- 日志与指标: Serilog + Prometheus

## 本地部署

### 前置条件

1. 启动 Redis、RabbitMQ、Consul 与其他基础设施
2. 配置 Supabase 连接信息
3. 确认 Gateway 与依赖服务已可访问

### 使用脚本部署

在后端根目录执行：

```bash
./deploy-all.sh
```

或使用现有本地服务脚本单独启动 EventService 所需依赖。

### 使用 dotnet 直接运行

```bash
dotnet run --project src/Services/EventService/EventService/EventService.csproj
```

## 配置项

重点检查以下配置：

- ConnectionStrings / Supabase 连接
- Consul 服务注册地址
- RabbitMQ 连接
- Serilog 输出配置
- ASPNETCORE_URLS / 监听端口

## 验证清单

```bash
curl http://localhost:8005/health
curl http://localhost:8005/openapi/v1.json
```

检查服务注册：

```bash
curl http://localhost:8500/v1/catalog/service/event-service
```

检查容器或进程日志，确认：

- 服务成功启动
- 已完成 Consul 注册
- RabbitMQ 连接正常
- 没有未处理异常

## 故障排查

### 服务无法启动

- 检查 8005 端口是否被占用
- 检查 Supabase 配置是否完整
- 检查 RabbitMQ / Consul 是否可达

### 服务未注册到 Consul

- 检查 Consul 地址配置
- 检查健康检查端点是否返回 200
- 查看启动日志中的注册异常

### 消息发布或消费异常

- 检查 RabbitMQ 连接串
- 检查消息总线配置与交换机声明
- 查看 MassTransit 启动日志

## 相关文件

- src/Services/EventService/EventService/Program.cs
- src/Services/EventService/EventService/appsettings.json
- deployment/deploy-services-local.sh
- deployment/deploy-services-local.ps1

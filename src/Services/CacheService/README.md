# CacheService - 分数缓存服务

## 概述

CacheService 是一个独立的微服务,专门负责缓存 City 和 Coworking 的评分数据,避免重复计算,提升系统性能。

## 架构设计

### DDD 分层架构
```
CacheService/
├── Domain/                    # 领域层
│   ├── Entities/             # 实体
│   │   └── ScoreCache.cs    # 分数缓存实体
│   └── Repositories/         # 仓储接口
│       └── IScoreCacheRepository.cs
├── Application/               # 应用层
│   ├── Abstractions/
│   │   └── Services/
│   │       └── IScoreCacheService.cs
│   ├── DTOs/
│   │   └── ScoreDtos.cs
│   └── Services/
│       └── ScoreCacheApplicationService.cs
├── Infrastructure/            # 基础设施层
│   ├── Repositories/
│   │   └── RedisScoreCacheRepository.cs
│   └── Integrations/
│       ├── CityServiceClient.cs      # 调用 CityService
│       └── CoworkingServiceClient.cs # 调用 CoworkingService
└── API/                       # API 层
    └── Controllers/
        └── ScoreController.cs
```

## 核心功能

### 1. 城市评分缓存
- `GET /api/scores/city/{cityId}` - 获取单个城市评分
- `POST /api/scores/city/batch` - 批量获取城市评分
- `DELETE /api/scores/city/{cityId}` - 使缓存失效
- `POST /api/scores/city/invalidate-batch` - 批量使缓存失效

### 2. 共享办公空间评分缓存
- `GET /api/scores/coworking/{coworkingId}` - 获取单个空间评分
- `POST /api/scores/coworking/batch` - 批量获取空间评分
- `DELETE /api/scores/coworking/{coworkingId}` - 使缓存失效
- `POST /api/scores/coworking/invalidate-batch` - 批量使缓存失效

## 技术栈

- **框架**: ASP.NET Core 9.0
- **缓存**: Redis (StackExchange.Redis)
- **服务发现**: Consul
- **服务调用**: Dapr Service Invocation
- **日志**: Serilog
- **API 文档**: Scalar

## 缓存策略

### TTL (Time To Live)
- 默认: 24小时
- 可通过 `appsettings.json` 配置: `Cache:ScoreTtlHours`

### 缓存键格式
- 城市评分: `city:score:{cityId}`
- 共享办公: `coworking:score:{coworkingId}`

### 缓存失效策略
1. **自动过期**: 缓存达到 TTL 后自动失效
2. **主动失效**: 评分更新时,通过 DELETE API 主动清除缓存

## 集成指南

### CityService 集成

在 `CityRatingsController.cs` 中已集成缓存失效调用:

```csharp
// 评分提交后,自动使缓存失效
await _daprClient.InvokeMethodAsync(
    HttpMethod.Delete,
    "cache-service",
    $"api/scores/city/{cityId}"
);
```

### Flutter 客户端集成

```dart
// 1. 获取城市评分 (带缓存)
final response = await http.get(
  Uri.parse('$cacheServiceUrl/api/scores/city/$cityId')
);

// 2. 批量获取城市评分
final response = await http.post(
  Uri.parse('$cacheServiceUrl/api/scores/city/batch'),
  body: jsonEncode(['city-id-1', 'city-id-2', 'city-id-3'])
);
```

## 配置文件

### appsettings.json
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "Cache": {
    "ScoreTtlHours": 24
  },
  "Consul": {
    "Address": "http://go-nomads-consul:7500",
    "ServiceName": "cache-service",
    "HealthCheckPath": "/health"
  }
}
```

## 部署

### Docker 部署
```bash
# 构建镜像
docker build -t cache-service:latest .

# 运行容器
docker run -d \
  -p 8010:8010 \
  -e ConnectionStrings__Redis=go-nomads-redis:6379 \
  --name cache-service \
  cache-service:latest
```

### Dapr Sidecar
```bash
dapr run \
  --app-id cache-service \
  --app-port 8010 \
  --dapr-http-port 3510 \
  --dapr-grpc-port 50010 \
  -- dotnet run
```

## 监控与日志

### Health Check
```bash
curl http://localhost:8010/health
```

### 日志位置
- Console: 实时输出
- File: `logs/cacheservice-{Date}.txt`

### 关键指标
- 缓存命中率: `CachedCount / TotalCount`
- 响应时间: 查看日志中的请求耗时
- Redis 连接状态: 通过 health check 监控

## 性能优化

### 批量查询优化
使用 Redis Pipeline 批量获取缓存,减少网络往返:

```csharp
var keys = entityIdList.Select(id => (RedisKey)key).ToArray();
var values = await _database.StringGetAsync(keys); // 一次性获取
```

### 并发调用优化
缺失缓存时,并发调用后端服务计算分数:

```csharp
var tasks = missingIds.Select(id => CalculateScoreAsync(id));
var results = await Task.WhenAll(tasks);
```

## 故障处理

### Redis 连接失败
- 自动重连: `AbortOnConnectFail = false`
- 降级策略: 缓存失败时直接调用后端服务

### 后端服务不可用
- 抛出异常,由调用方处理
- 建议客户端实现重试机制

## API 响应示例

### 获取城市评分
```json
{
  "entityId": "550e8400-e29b-41d4-a716-446655440000",
  "overallScore": 4.5,
  "fromCache": true,
  "statistics": "{\"categories\":[...]}"
}
```

### 批量获取评分
```json
{
  "scores": [
    {
      "entityId": "city-1",
      "overallScore": 4.5,
      "fromCache": true
    },
    {
      "entityId": "city-2",
      "overallScore": 3.8,
      "fromCache": false
    }
  ],
  "totalCount": 2,
  "cachedCount": 1,
  "calculatedCount": 1
}
```

## 开发指南

### 本地开发
1. 启动 Redis: `docker run -d -p 6379:6379 redis:latest`
2. 启动 Consul: `docker run -d -p 7500:8500 consul:latest`
3. 启动服务: `dotnet run`
4. 访问 API 文档: `http://localhost:8010/scalar/v1`

### 添加新的缓存类型
1. 在 `Domain/Entities/ScoreCache.cs` 添加新的 `ScoreEntityType`
2. 创建新的 Service Client (如 `IMeetupServiceClient`)
3. 在 `IScoreCacheService` 添加新方法
4. 在 `ScoreCacheApplicationService` 实现方法
5. 在 `ScoreController` 添加新端点

## 常见问题

### Q: 为什么不把缓存逻辑集成到 CityService 中?
A: 
- ✅ 单一职责原则: 缓存是横切关注点,应独立管理
- ✅ 代码复用: City 和 Coworking 都需要缓存
- ✅ 易于扩展: 未来其他服务也能使用
- ✅ 独立部署: 缓存层可以独立扩展和优化

### Q: 缓存失效是同步还是异步的?
A: 异步调用,不影响评分提交的主流程。即使缓存失效失败,也只记录日志,不抛出异常。

### Q: 如何强制刷新所有缓存?
A: 调用批量失效 API,或者直接清空 Redis 中对应的 key pattern。

## 更新日志

### v1.0.0 (2025-11-20)
- ✅ 初始版本
- ✅ 支持 City 和 Coworking 评分缓存
- ✅ Redis 持久化
- ✅ Dapr 服务调用集成
- ✅ Consul 服务注册
- ✅ 完整的 DDD 架构

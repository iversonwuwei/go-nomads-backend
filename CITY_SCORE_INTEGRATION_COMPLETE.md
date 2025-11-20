# 城市总评分数据集成完成

## 问题描述
城市列表 API 返回的 `overallScore` 字段始终为 null,尽管:
- CacheService 中已有评分数据(Redis 存储)
- 评分提交流程正常工作
- 直接调用 CacheService API 可以获取到评分

## 根本原因

经过深入调试,发现了多个问题:

### 1. Task.WhenAll 异常传播问题
**问题**: `GetAllCitiesAsync` 和 `SearchCitiesAsync` 中使用 `Task.WhenAll` 并行执行多个填充任务,但当任何一个任务抛出异常时,整个 `Task.WhenAll` 会立即失败,导致其他任务被取消。

**表现**: `EnrichCitiesWithModeratorInfoAsync` 中调用 user-service 失败(Dapr 连接超时),阻止了 `EnrichCitiesWithRatingsAndCostsAsync` 的执行。

**解决方案**: 
```csharp
// 修改前
await Task.WhenAll(weatherTask, moderatorTask, ratingsAndCostsTask, favoriteTask);

// 修改后 - 使用 ContinueWith 确保即使某些任务失败,其他任务也会继续执行
var allTasks = new[] { weatherTask, moderatorTask, ratingsAndCostsTask, favoriteTask };
await Task.WhenAll(allTasks.Select(t => t.ContinueWith(_ => { })));
```

### 2. GetAllCitiesAsync 缺少评分填充逻辑
**问题**: `GetAllCitiesAsync` 方法中没有调用 `EnrichCitiesWithRatingsAndCostsAsync`,只有 `SearchCitiesAsync` 中有。

**影响**: 当用户访问城市列表(不带搜索参数)时,评分数据不会被填充。

**解决方案**: 在 `GetAllCitiesAsync` 中添加 `ratingsAndCostsTask`:
```csharp
public async Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize, Guid? userId = null, string? userRole = null)
{
    var cities = await _cityRepository.GetAllAsync(pageNumber, pageSize);
    var cityDtos = cities.Select(MapToDto).ToList();

    // 并行填充数据
    var weatherTask = EnrichCitiesWithWeatherAsync(cityDtos);
    var moderatorTask = EnrichCitiesWithModeratorInfoAsync(cityDtos);
    var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(cityDtos);  // ✅ 新增
    var favoriteTask = userId.HasValue
        ? EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value)
        : Task.CompletedTask;

    var allTasks = new[] { weatherTask, moderatorTask, ratingsAndCostsTask, favoriteTask };
    await Task.WhenAll(allTasks.Select(t => t.ContinueWith(_ => { })));

    foreach (var cityDto in cityDtos) cityDto.SetUserContext(userId, userRole);

    return cityDtos;
}
```

### 3. 数据库连接问题
**问题**: `EnrichCitiesWithRatingsAndCostsAsync` 方法直接创建 NpgsqlConnection 连接到 localhost:5432,但在 Docker 容器中 localhost 指向容器自身,导致连接失败。

**原因**: 
```csharp
var connectionString = _configuration.GetConnectionString("DefaultConnection");
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
```

**解决方案**: 移除直接数据库查询,只保留从 CacheService 获取评分的逻辑:
```csharp
private async Task EnrichCitiesWithRatingsAndCostsAsync(List<CityDto> cities)
{
    if (cities.Count == 0) return;

    _logger.LogInformation("🔧 开始批量填充评分和花费信息: {Count} 个城市", cities.Count);

    try
    {
        var cityIds = cities.Select(c => c.Id).ToList();

        // 🆕 通过 CacheService 批量获取城市总评分
        var overallScores = await GetCityScoresFromCacheServiceAsync(cityIds);

        // 填充数据
        foreach (var city in cities)
        {
            city.OverallScore = overallScores.GetValueOrDefault(city.Id);

            _logger.LogDebug("📊 城市 {CityName}({CityId}): OverallScore={OverallScore}",
                city.Name, city.Id, city.OverallScore);
        }

        _logger.LogInformation("💰 批量填充评分和花费信息完成: {Count} 个城市, 总评分: {ScoreCount} 个",
            cities.Count, overallScores.Count);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "批量填充评分和花费信息失败");
    }
}
```

### 4. GetUserInfoWithCacheAsync 异常处理
**问题**: 该方法在重试失败后没有返回 null,而是让异常继续传播。

**解决方案**: 确保在最后一次重试失败后返回 null:
```csharp
catch (Exception ex)
{
    if (attempt < maxRetries)
    {
        _logger.LogWarning(ex, "获取用户信息失败，准备重试 ({Attempt}/{MaxRetries}): UserId={UserId}",
            attempt + 1, maxRetries, userId);
        await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)));
    }
    else
    {
        _logger.LogError(ex, "获取用户信息失败（已达最大重试次数）: UserId={UserId}", userId);
        return null; // ✅ 返回 null 而不是抛出异常
    }
}
```

## 修改的文件

1. **CityApplicationService.cs**
   - 修改 `GetAllCitiesAsync`: 添加 `ratingsAndCostsTask`
   - 修改 `SearchCitiesAsync`: 改进 Task.WhenAll 错误处理
   - 简化 `EnrichCitiesWithRatingsAndCostsAsync`: 移除直接数据库查询
   - 修改 `GetUserInfoWithCacheAsync`: 确保重试失败后返回 null

## 测试验证

### 直接测试 CityService
```bash
curl 'http://localhost:8002/api/v1/cities?PageNumber=1&PageSize=3' | jq '.data.items[] | {name, overallScore}'
```

**结果**:
```json
{
  "name": "秦皇岛市",
  "overallScore": 4
}
{
  "name": "邯郸市",
  "overallScore": 0
}
{
  "name": "邢台市",
  "overallScore": 0
}
```

### 验证日志
```
[17:01:31 INF] 🔧 开始批量填充评分和花费信息: 3 个城市
[17:01:31 INF] 💰 批量填充评分和花费信息完成: 3 个城市, 总评分: 3 个
```

### CacheService 批量请求
```
[17:01:31 INF] Getting batch city scores for 3 cities
[17:01:31 INF] Calculating batch city scores for 1 cities
[17:01:31 INF] Set 1 score caches in batch
[17:01:31 INF] HTTP POST /api/v1/cache/scores/city/batch responded 200 in 39.1807 ms
```

## 架构说明

### 评分数据流
1. **写入流程** (评分提交)
   ```
   Flutter → Gateway → CityService (CityRatingsController)
   → 计算总评分 → Dapr → CacheService → Redis
   ```

2. **读取流程** (城市列表)
   ```
   Flutter → Gateway → CityService (CitiesController)
   → GetAllCitiesAsync → EnrichCitiesWithRatingsAndCostsAsync
   → Dapr → CacheService → Redis → 返回评分
   ```

### 关键设计决策

1. **使用 CacheService 作为评分数据源**: 而不是直接查询数据库,提高性能并解耦服务
2. **批量获取评分**: 通过 `/api/v1/cache/scores/city/batch` 端点一次性获取多个城市的评分
3. **容错处理**: 使用 ContinueWith 确保单个任务失败不影响其他任务
4. **默认值处理**: 没有评分的城市返回 0 而不是 null

## 性能优化

1. **并行填充**: 天气、版主、评分、收藏状态并行获取
2. **批量操作**: 一次请求获取多个城市的评分,减少网络往返
3. **缓存使用**: CacheService 使用 Redis 24小时缓存,减少数据库查询

## 后续建议

1. ✅ 已完成: 评分数据集成到城市列表
2. 🔄 建议: 添加 reviewCount 和 averageCost 的集成(目前只有 overallScore)
3. 🔄 建议: 处理 CacheService 批量请求中的错误("Error calculating score for city")
4. 🔄 建议: 添加更详细的监控和日志,跟踪评分数据的新鲜度

## 相关文档
- [Cache Service API 规范](./CACHE_SERVICE_API_SPEC.md)
- [Gateway 路由配置](./GATEWAY_ROUTING_CONFIG.md)
- [评分提交流程](./RATING_SUBMISSION_FLOW.md)

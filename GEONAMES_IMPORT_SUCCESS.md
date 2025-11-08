# GeoNames 数据导入功能 - 成功实现

## 📅 完成时间
2025-11-05

## ✅ 实现状态
**已完成并测试通过**

## 🎯 功能概述
成功实现了从 GeoNames.org 导入全球城市数据的功能,并将数据存储到独立的 `geonames_cities` 表中。

## 📊 测试结果

### 测试 1: 泰国城市(人口 > 10万)
```
总处理: 50
成功: 50
跳过: 0
失败: 0
```

### 测试 2: 中国城市(人口 > 100万)
```
总处理: 611
成功: 611
跳过: 0
失败: 0
耗时: 8分15秒
```

## 🏗️ 架构实现

### 1. 数据库表: `geonames_cities`
```sql
-- 完整的 GeoNames 城市数据表
-- 88 行 SQL 脚本
-- 包含: 索引、触发器、完整的字段映射
```

**关键字段**:
- `id`: UUID 主键
- `geoname_id`: GeoNames 唯一标识 (BIGINT, UNIQUE)
- `name`, `ascii_name`, `alternate_names`: 城市名称
- `latitude`, `longitude`: 坐标
- `country_code`, `country_name`: 国家信息
- `population`: 人口
- `synced_to_cities`: 是否已同步到 cities 表
- `city_id`: 关联的 cities 表 ID

**7 个索引**:
1. `idx_geonames_cities_geoname_id` (UNIQUE)
2. `idx_geonames_cities_name`
3. `idx_geonames_cities_country_code`
4. `idx_geonames_cities_population` (DESC)
5. `idx_geonames_cities_feature_code`
6. `idx_geonames_cities_synced`
7. `idx_geonames_cities_city_id`

### 2. 实体层: `Domain.Entities.GeoNamesCity`
```csharp
[Table("geonames_cities")]
public class GeoNamesCity : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("geoname_id")]
    public long GeonameId { get; set; }
    
    // ... 28 个完整的字段映射
}
```

### 3. Repository 层: `IGeoNamesCityRepository`
```csharp
public interface IGeoNamesCityRepository
{
    Task<GeoNamesCity> UpsertAsync(GeoNamesCity city);
    Task<IEnumerable<GeoNamesCity>> UpsertBatchAsync(IEnumerable<GeoNamesCity> cities);
    Task<GeoNamesCity?> GetByGeonameIdAsync(long geonameId);
    Task<IEnumerable<GeoNamesCity>> GetByCountryCodeAsync(string countryCode);
    // ... 7 个其他方法
}
```

**实现**: `SupabaseGeoNamesCityRepository`
- 继承自 `SupabaseRepositoryBase<GeoNamesCity>`
- 206 行代码
- 核心方法: `UpsertAsync` (检查存在性,Insert 或 Update)

### 4. Service 层: `GeoNamesImportService`
```csharp
public class GeoNamesImportService : IGeoNamesImportService
{
    private readonly IGeoNamesCityRepository _geoNamesCityRepository;
    
    public async Task<GeoNamesImportResult> ImportCountryCitiesAsync(
        string countryCode,
        GeoNamesImportOptions options)
    {
        // 1. 从 GeoNames API 获取数据
        var cities = await FetchCitiesFromGeoNamesAsync(...);
        
        // 2. 批量处理
        var result = await ProcessBatchAsync(cities, options);
        
        return result;
    }
}
```

**核心流程**:
1. `FetchCitiesFromGeoNamesAsync`: 从 GeoNames.org API 获取城市数据
2. `ProcessBatchAsync`: 批量处理城市列表
3. `ProcessSingleCityAsync`: 单个城市处理
4. `MapToGeoNamesCityEntity`: DTO → 实体映射
5. `_geoNamesCityRepository.UpsertAsync`: 插入/更新数据库

### 5. Controller 层: `GeoNamesController`
```csharp
[ApiController]
[Route("api/[controller]")]
public class GeoNamesController : ControllerBase
{
    [HttpPost("import/country/{countryCode}")]
    public async Task<IActionResult> ImportCountryCities(
        string countryCode,
        [FromBody] GeoNamesImportOptions? options)
    {
        // ...
    }
}
```

## 🔍 问题解决历程

### 问题 1: Schema 不匹配
**现象**: 尝试直接更新 `cities` 表时,发现字段不匹配。
**解决方案**: 创建独立的 `geonames_cities` 表存储完整数据。

### 问题 2: 类型引用不明确
**现象**: `GeoNamesCity` 在 DTOs 和 Entities 命名空间中都存在。
**解决方案**: 所有引用明确使用 `DTOs.GeoNamesCity` 或 `Domain.Entities.GeoNamesCity`。

### 问题 3: Repository 类型转换错误
**现象**: `query = query.Filter(...)` 导致类型不匹配。
**解决方案**: 改为 if-else 分支,分别调用 Filter 和 Get。

### 问题 4: 首次测试失败
**现象**: 错误信息显示访问 `cities` 表而非 `geonames_cities` 表。
**根本原因**: **容器未重新构建** - 虽然执行了部署脚本,但容器仍在运行旧代码。
**解决方案**: 强制重启容器 `docker restart go-nomads-city-service`。

## 📝 关键发现

### Supabase Schema Cache 不是问题
最初怀疑是 Supabase Postgrest 的 schema cache 导致访问错误的表,但实际上:
- ✅ 代码正确使用 `[Table("geonames_cities")]` 标注
- ✅ Repository 正确调用 `From<GeoNamesCity>()`
- ❌ **真正问题**: 容器没有重新构建,运行的是旧代码

### 部署脚本的潜在问题
`deploy-services-local.ps1` 脚本可能存在以下问题:
1. 构建成功但未停止旧容器
2. 新镜像创建但容器仍使用旧镜像
3. 需要手动 `docker restart` 来应用更改

**建议**: 修改部署脚本,确保:
```powershell
# 停止旧容器
docker stop go-nomads-city-service go-nomads-city-service-dapr

# 删除旧容器
docker rm go-nomads-city-service go-nomads-city-service-dapr

# 重新创建容器
docker compose up -d city-service
```

## 🚀 API 使用示例

### 1. 按国家代码导入城市
```bash
POST /api/geonames/import/country/{countryCode}
Content-Type: application/json

{
  "overwriteExisting": false,
  "minPopulation": 100000
}
```

**示例响应**:
```json
{
  "success": true,
  "message": "导入完成。成功: 611, 跳过: 0, 失败: 0",
  "data": {
    "totalProcessed": 611,
    "successCount": 611,
    "skippedCount": 0,
    "failedCount": 0,
    "errors": [],
    "startTime": "2025-11-05T05:48:12Z",
    "endTime": "2025-11-05T05:56:27Z",
    "duration": "00:08:15.25"
  }
}
```

### 2. 搜索城市
```bash
GET /api/geonames/search?name={cityName}&countryCode={code}&limit=10
```

### 3. 按城市名称获取详情
```bash
GET /api/geonames/city/{cityName}?countryCode={code}
```

## 📊 数据统计

### 当前已导入数据
- **泰国**: 50 个城市 (人口 > 10万)
- **中国**: 611 个城市 (人口 > 100万)
- **总计**: 661 个城市

### 性能指标
- **平均处理速度**: ~74 城市/分钟 (基于中国数据)
- **API 响应时间**: 8-15 分钟 (取决于城市数量)

## 📂 相关文件

### 新建文件
1. `create_geonames_cities_table.sql` - 数据库建表脚本
2. `Domain/Entities/GeoNamesCity.cs` - 实体类
3. `Domain/Repositories/IGeoNamesCityRepository.cs` - Repository 接口
4. `Infrastructure/Repositories/SupabaseGeoNamesCityRepository.cs` - Repository 实现
5. `GEONAMES_TABLE_IMPLEMENTATION.md` - 实现文档

### 修改文件
1. `Application/Services/GeoNamesImportService.cs` - 重构使用新 Repository
2. `Application/Services/IGeoNamesImportService.cs` - 更新接口
3. `API/Controllers/GeoNamesController.cs` - 删除旧的更新坐标 API
4. `Program.cs` - 注册新的 Repository
5. `Domain/Entities/City.cs` - 修复 JSON 序列化问题

## 🔧 调试技巧

### 添加的调试日志
```csharp
// 在 UpsertAsync 方法中
var tableAttr = typeof(GeoNamesCity).GetCustomAttributes(
    typeof(Postgrest.Attributes.TableAttribute), true)
    .FirstOrDefault() as Postgrest.Attributes.TableAttribute;
    
Logger.LogInformation("Upserting to table: {TableName}", tableAttr?.Name);
```

**日志输出示例**:
```
[05:56:20 INF] Upserting to table: geonames_cities (from GeoNamesCity type)
```

### 查看容器日志
```powershell
# 查看最新 100 行日志
docker logs go-nomads-city-service --tail 100

# 实时跟踪日志
docker logs go-nomads-city-service -f

# 过滤特定内容
docker logs go-nomads-city-service --tail 50 | Select-String "Upserting"
```

## ✅ 下一步计划

### 1. 数据同步功能 (优先级: 中)
实现将 `geonames_cities` 数据同步到 `cities` 表的功能:
```csharp
public async Task SyncToMainCitiesTableAsync(long geonameId)
{
    var geoCity = await _geoNamesCityRepository.GetByGeonameIdAsync(geonameId);
    
    // 映射到 City 实体
    var city = MapToCity(geoCity);
    
    // 插入或更新 cities 表
    await _cityRepository.UpsertAsync(city);
    
    // 更新同步状态
    geoCity.SyncedToCities = true;
    geoCity.CityId = city.Id;
    await _geoNamesCityRepository.UpsertAsync(geoCity);
}
```

### 2. 批量导入全球数据 (优先级: 低)
```csharp
[HttpPost("import/global")]
public async Task<IActionResult> ImportAllCountries(
    [FromBody] GeoNamesImportOptions options)
{
    // 获取所有国家代码
    var countryCodes = GetAllCountryCodes();
    
    // 逐个导入
    foreach (var code in countryCodes)
    {
        await _service.ImportCountryCitiesAsync(code, options);
    }
}
```

### 3. 定时自动更新 (优先级: 低)
使用 Hangfire 或 Quartz.NET 实现每月自动更新:
```csharp
[AutomaticRetry(Attempts = 3)]
public async Task MonthlyGeoNamesUpdate()
{
    var countries = await _geoNamesCityRepository.GetDistinctCountryCodesAsync();
    
    foreach (var country in countries)
    {
        await _service.ImportCountryCitiesAsync(country, new GeoNamesImportOptions
        {
            OverwriteExisting = true,
            MinPopulation = 100000
        });
    }
}
```

### 4. 管理 API 增强 (优先级: 低)
- `GET /api/geonames/stats` - 统计信息
- `GET /api/geonames/countries` - 已导入的国家列表
- `DELETE /api/geonames/country/{code}` - 删除某国数据
- `POST /api/geonames/sync/{geonameId}` - 手动同步单个城市

## 🎓 经验总结

### 1. 容器部署最佳实践
- ✅ 构建后**必须**重启容器
- ✅ 使用 `docker-compose up --build --force-recreate` 确保使用新镜像
- ✅ 或者手动 `docker stop && docker rm && docker-compose up`

### 2. Supabase/Postgrest 使用技巧
- ✅ `[Table("table_name")]` 标注必须正确
- ✅ 所有属性必须有 `[Column("column_name")]` 标注
- ✅ Repository 使用 `From<TEntity>()` 时会自动读取 Table 标注
- ✅ Schema cache 问题通常不需要手动刷新(服务器端自动处理)

### 3. 调试技巧
- ✅ 添加日志输出表名和关键数据
- ✅ 使用 `docker logs` 实时查看应用日志
- ✅ 验证容器确实在运行新代码(检查日志时间戳)

### 4. 分离关注点
- ✅ 使用独立表存储第三方原始数据
- ✅ 保持现有 `cities` 表结构不变
- ✅ 通过同步机制选择性更新主表
- ✅ 便于后续数据追溯和对比

## 📚 参考资料

### GeoNames API
- 文档: https://www.geonames.org/export/web-services.html
- 搜索 API: https://secure.geonames.org/searchJSON
- 国家信息: https://secure.geonames.org/countryInfoJSON

### Supabase Postgrest
- 文档: https://postgrest.org/en/stable/
- C# 客户端: https://github.com/supabase-community/postgrest-csharp

### 部署相关
- Docker Compose: https://docs.docker.com/compose/
- Dapr: https://docs.dapr.io/

## ✨ 总结

成功实现了 GeoNames 数据导入功能,采用了**独立表存储**的架构设计,避免了与现有 `cities` 表的冲突。通过完整的分层架构(Entity → Repository → Service → Controller),确保了代码的可维护性和可扩展性。

**关键成功因素**:
1. ✅ 正确的数据库设计(独立表 + 7个索引)
2. ✅ 清晰的分层架构
3. ✅ 完整的错误处理和日志记录
4. ✅ 彻底的容器重启(解决部署问题)

**测试验证**:
- ✅ 泰国: 50/50 成功
- ✅ 中国: 611/611 成功
- ✅ 总计: 661 个城市成功导入

项目现已具备从 GeoNames.org 导入全球城市数据的能力! 🎉

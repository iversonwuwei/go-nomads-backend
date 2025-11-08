# GeoNames 城市数据导入功能完成总结

## 概述
创建了一个独立的 `geonames_cities` 表来存储从 GeoNames.org 导入的完整城市数据,避免与现有 `cities` 表的 schema 冲突。

## 已完成的工作

### 1. 数据库设计
- **文件**: `database/migrations/create_geonames_cities_table.sql`
- **表名**: `geonames_cities`
- **字段**:
  - `id` (UUID, 主键)
  - `geoname_id` (BIGINT, 唯一索引)
  - 基础信息: name, ascii_name, alternate_names
  - 地理坐标: latitude, longitude
  - 分类: feature_class, feature_code
  - 国家: country_code, country_name
  - 行政区划: admin1/2/3/4 code/name
  - 统计: population, elevation, dem
  - 时区: timezone
  - 同步状态: synced_to_cities, city_id
  - 时间戳: imported_at, updated_at

### 2. 实体类
- **文件**: `CityService/Domain/Entities/GeoNamesCity.cs`
- 与数据库表字段完全对应
- 使用 Postgrest 特性标注

### 3. Repository 层
- **接口**: `Domain/Repositories/IGeoNamesCityRepository.cs`
  - `UpsertAsync`: 插入或更新单个城市
  - `UpsertBatchAsync`: 批量插入或更新
  - `GetByGeonameIdAsync`: 根据 GeoNames ID 查询
  - `GetByCountryCodeAsync`: 根据国家代码查询
  - `GetUnsyncedAsync`: 获取未同步的数据
  - `MarkAsSyncedAsync`: 标记已同步
  - `SearchAsync`: 搜索功能
  
- **实现**: `Infrastructure/Repositories/SupabaseGeoNamesCityRepository.cs`
  - 完整实现了所有接口方法
  - 使用 Supabase Postgrest 客户端

### 4. 服务层改造
- **文件**: `Application/Services/GeoNamesImportService.cs`
- **改动**:
  - 构造函数注入 `IGeoNamesCityRepository` (替代 `ICityRepository`)
  - `ProcessSingleCityAsync`: 改为导入到 geonames_cities 表
  - `MapToGeoNamesCityEntity`: 新增映射方法,将 DTO 映射到实体
  - 删除了更新 cities 表坐标的相关代码

### 5. DI 容器注册
- **文件**: `Program.cs`
- 添加: `builder.Services.AddScoped<IGeoNamesCityRepository, SupabaseGeoNamesCityRepository>()`

### 6. City 实体修复
- **文件**: `Domain/Entities/City.cs`
- 在 `AverageCostOfLiving` 属性添加 `[JsonIgnore]` 标记
- 解决了之前导入时数据库列不存在的问题

## 部署步骤

### 1. 创建数据库表
```powershell
# 方法1: 直接在 Supabase 控制台执行 SQL
# 将 database/migrations/create_geonames_cities_table.sql 的内容复制到 Supabase SQL Editor 执行

# 方法2: 使用 Docker (如果有本地数据库容器)
docker cp database/migrations/create_geonames_cities_table.sql <容器名>:/tmp/
docker exec <容器名> psql -U postgres -d postgres -f /tmp/create_geonames_cities_table.sql
```

### 2. 重新构建和部署服务
```powershell
cd e:\Workspaces\WaldenProjects\go-nomads
.\deployment\deploy-services-local.ps1 -ServiceName CityService
```

### 3. 测试导入
```powershell
# 导入泰国城市数据
$body = @{
    overwriteExisting = $false
    minPopulation = 100000
} | ConvertTo-Json

Invoke-WebRequest `
    -Uri "http://localhost:8002/api/geonames/import/country/TH" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

### 4. 查询导入结果
```sql
-- 查看导入的城市数量
SELECT country_code, COUNT(*) as city_count
FROM geonames_cities
GROUP BY country_code
ORDER BY city_count DESC;

-- 查看泰国城市
SELECT id, name, population, latitude, longitude, timezone
FROM geonames_cities
WHERE country_code = 'TH'
ORDER BY population DESC;

-- 查看未同步的城市
SELECT name, country_name, population
FROM geonames_cities
WHERE synced_to_cities = false
ORDER BY population DESC
LIMIT 20;
```

## 后续任务

### 1. 数据同步功能
创建一个服务,将 geonames_cities 表中的数据同步到 cities 表:
- 读取 `synced_to_cities = false` 的记录
- 映射必需字段到 cities 表
- 插入或更新 cities 表
- 标记 `synced_to_cities = true` 并设置 `city_id`

### 2. API 增强
添加更多管理 API:
- `GET /api/geonames/cities`: 列出已导入的城市
- `POST /api/geonames/sync`: 同步到 cities 表
- `DELETE /api/geonames/country/{code}`: 删除指定国家的数据
- `GET /api/geonames/stats`: 统计信息

### 3. 定时任务
使用 Hangfire 或 Quartz.NET 实现:
- 定期从 GeoNames 更新数据
- 自动同步到 cities 表
- 清理过期数据

### 4. 数据质量优化
- 添加数据验证规则
- 处理重复数据
- 补充缺失的地理信息

## 优势

1. **数据完整性**: 保留 GeoNames 的原始数据,不受 cities 表限制
2. **灵活性**: 可以选择性地同步需要的数据到 cities 表
3. **可追溯性**: 通过 `city_id` 关联,可以追溯数据来源
4. **扩展性**: 可以存储更多 GeoNames 提供的字段(alternate_names, admin codes 等)
5. **安全性**: 导入错误不会影响现有 cities 表数据

## 注意事项

1. **API 限制**: GeoNames 免费账户限制为 1000次/小时, 30000次/天
2. **数据量**: 全球城市数据可能达到数十万条,建议分批导入
3. **性能**: 批量导入时注意内存占用和数据库连接数
4. **去重**: Upsert 基于 `geoname_id`,确保不会重复导入

## 相关文件

- 数据库: `database/migrations/create_geonames_cities_table.sql`
- 实体: `CityService/Domain/Entities/GeoNamesCity.cs`
- 接口: `CityService/Domain/Repositories/IGeoNamesCityRepository.cs`
- 实现: `CityService/Infrastructure/Repositories/SupabaseGeoNamesCityRepository.cs`
- 服务: `CityService/Application/Services/GeoNamesImportService.cs`
- DTO: `CityService/Application/DTOs/GeoNamesDtos.cs`
- API: `CityService/API/Controllers/GeoNamesController.cs`

# GeoNames 城市数据导入服务使用指南

## 概述

GeoNames 集成服务用于从 [GeoNames.org](http://www.geonames.org) 导入全球城市数据,替换或更新现有的 cities 表数据。

## 功能特性

- ✅ 批量导入全球城市数据
- ✅ 支持按国家导入
- ✅ 智能避免 API 限制 (每小时 1000 次请求)
- ✅ 支持覆盖/跳过已存在数据
- ✅ 支持仅更新坐标模式
- ✅ 完整的日志和错误追踪
- ✅ 分页处理大量数据避免内存溢出

## 前置要求

### 1. 注册 GeoNames 账户

1. 访问 http://www.geonames.org/login
2. 注册免费账户
3. 激活账户 (检查邮箱)
4. 启用 Web Services: http://www.geonames.org/manageaccount

### 2. 配置 Username

在 `appsettings.json` 或 `appsettings.Development.json` 中配置:

```json
{
  "GeoNames": {
    "Username": "your_actual_username_here"
  }
}
```

⚠️ **重要**: 将 `your_username_here` 替换为您的实际 GeoNames username

### 3. 服务已自动注册

服务已在 `Program.cs` 中注册:

```csharp
builder.Services.AddScoped<IGeoNamesImportService, GeoNamesImportService>();
```

## API 端点

### 1. 搜索预览 (无需认证)

测试 GeoNames API 连接,预览城市数据:

```bash
GET /api/geonames/search?query=Bangkok
```

**响应示例**:
```json
{
  "success": true,
  "data": [
    {
      "geonameId": 1609350,
      "name": "Bangkok",
      "lat": 13.75398,
      "lng": 100.50144,
      "countryCode": "TH",
      "countryName": "Thailand",
      "population": 5104476,
      "timezone": {
        "timeZoneId": "Asia/Bangkok",
        "gmtOffset": 7,
        "dstOffset": 7
      }
    }
  ]
}
```

### 2. 获取城市信息 (无需认证)

获取特定城市的详细信息:

```bash
GET /api/geonames/city/Bangkok?countryCode=TH
```

### 3. 按国家导入 (需要 Admin 权限)

导入特定国家的所有城市 (人口 > 100,000):

```bash
POST /api/geonames/import/country/TH
Content-Type: application/json

{
  "overwriteExisting": true,
  "batchSize": 50
}
```

**国家代码参考**:
- `TH` - 泰国
- `ID` - 印度尼西亚
- `VN` - 越南
- `PT` - 葡萄牙
- `ES` - 西班牙
- `MX` - 墨西哥
- `CO` - 哥伦比亚
- `CR` - 哥斯达黎加
- `GB` - 英国
- `DE` - 德国
- `FR` - 法国
- 更多国家代码: [ISO 3166-1 alpha-2](https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2)

### 4. 完整导入 (需要 Admin 权限)

导入 35 个数字游民热门国家的所有城市:

```bash
POST /api/geonames/import
Content-Type: application/json

{
  "minPopulation": 100000,
  "countryCodes": [],  // 空数组使用默认 35 个国家
  "batchSize": 50,
  "overwriteExisting": true
}
```

**默认国家列表** (35 个):
- 东南亚: TH, ID, VN, MY, PH, SG, KH, LA, MM
- 欧洲: PT, ES, GR, HR, CZ, PL, HU, EE, LV, LT, GB, FR, DE, IT, NL, AT, CH
- 美洲: MX, CO, CR, PA, AR, BR, CL, US, CA
- 其他: AU, NZ, JP, TW

### 5. 更新坐标 (需要 Admin 权限)

仅更新现有城市的经纬度坐标:

```bash
POST /api/geonames/update-coordinates
```

## 导入配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `minPopulation` | int | 100000 | 最小人口数 (过滤小城市) |
| `countryCodes` | string[] | (35 个国家) | 要导入的国家代码列表 |
| `batchSize` | int | 50 | 每批处理数量 |
| `overwriteExisting` | bool | false | 是否覆盖已存在的城市 |

## 使用流程

### 步骤 1: 测试连接

```bash
# 测试 GeoNames API 是否正常
curl "http://localhost:5002/api/geonames/search?query=Bangkok"
```

### 步骤 2: 预览单个城市

```bash
# 查看城市数据结构
curl "http://localhost:5002/api/geonames/city/Bangkok?countryCode=TH"
```

### 步骤 3: 小规模测试导入

```bash
# 先导入一个国家测试
curl -X POST "http://localhost:5002/api/geonames/import/country/TH" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "overwriteExisting": false,
    "batchSize": 10
  }'
```

### 步骤 4: 完整导入

```bash
# 导入所有默认国家
curl -X POST "http://localhost:5002/api/geonames/import" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "minPopulation": 100000,
    "batchSize": 50,
    "overwriteExisting": true
  }'
```

## 响应格式

### 成功响应

```json
{
  "success": true,
  "data": {
    "totalProcessed": 1520,
    "successCount": 1520,
    "skippedCount": 0,
    "failedCount": 0,
    "errors": []
  },
  "message": "Import completed successfully"
}
```

### 错误响应

```json
{
  "success": false,
  "data": null,
  "message": "GeoNames username not configured. Please add GeoNames:Username in appsettings.json",
  "errors": [
    "Configuration error"
  ]
}
```

## API 限制

GeoNames 免费账户限制:
- **每小时**: 1000 次请求
- **每天**: 30,000 次请求

服务已内置限制处理:
- 每次 API 调用后延迟 100-200ms
- 自动批量处理避免超限

## 常见问题

### Q1: Username 配置错误

**错误**: "GeoNames username not configured"

**解决**:
1. 检查 `appsettings.json` 中的 `GeoNames:Username` 配置
2. 确保已注册并激活 GeoNames 账户
3. 确保已启用 Web Services

### Q2: API 调用超限

**错误**: "hourly limit of 1000 credits exceeded"

**解决**:
- 等待 1 小时后重试
- 减小 `batchSize` 参数
- 考虑升级到付费账户

### Q3: 城市已存在

如果 `overwriteExisting: false`:
- 已存在的城市会被跳过
- 查看响应中的 `skippedCount`

如果需要更新:
- 设置 `overwriteExisting: true`
- 或使用 `/update-coordinates` 端点仅更新坐标

### Q4: 导入太慢

优化方法:
- 增加 `batchSize` (最大 100)
- 提高 `minPopulation` 过滤更多小城市
- 按需导入特定国家,而非全部

## 数据映射

| GeoNames 字段 | Cities 表字段 | 说明 |
|---------------|---------------|------|
| `name` | `Name` | 城市名称 (英文) |
| `countryCode` | `Country` | 国家代码 |
| `adminName1` | `Region` | 省份/州 |
| `lat` | `Latitude` | 纬度 |
| `lng` | `Longitude` | 经度 |
| `population` | `Population` | 人口数 |
| `timezone.timeZoneId` | `TimeZone` | 时区 (如 "Asia/Bangkok") |

## 后续优化建议

### 1. 创建后台任务定期同步

```csharp
// 使用 Hangfire 或 Quartz.NET
[RecurringJob("0 0 * * 0")] // 每周日午夜
public async Task WeeklyGeoNamesSync()
{
    await _geoNamesService.ImportCitiesAsync(new GeoNamesImportOptions
    {
        OverwriteExisting = true
    });
}
```

### 2. 添加差异同步

仅同步有变化的城市,减少 API 调用:

```csharp
// 检查最后更新时间
public async Task<GeoNamesImportResult> SyncChangesAsync(DateTime since)
{
    // 实现逻辑
}
```

### 3. 缓存热门城市

对于频繁查询的城市,添加缓存层:

```csharp
builder.Services.AddMemoryCache();
// 在 Service 中使用 IMemoryCache
```

## 完成状态

✅ **已完成**:
- GeoNames DTOs 定义
- GeoNames 服务接口和实现
- GeoNames Controller 创建
- 服务注册到 DI 容器
- 配置文件模板添加

⏳ **待完成**:
- 配置实际的 GeoNames Username
- 测试 API 端点
- 创建后台同步任务 (可选)

## 技术架构

```
Controller (GeoNamesController)
    ↓
Service (GeoNamesImportService)
    ↓
HttpClient → GeoNames API
    ↓
Repository (ICityRepository)
    ↓
Database (cities 表)
```

## 日志

服务使用 `ILogger<GeoNamesImportService>` 记录详细日志:

- **Information**: 导入进度、批次处理
- **Warning**: 跳过的城市、匹配失败
- **Error**: API 调用失败、数据库错误

查看日志:
```bash
# 如果使用 Serilog
tail -f logs/city-service-*.log | grep GeoNames
```

## 相关文件

- `CityService.Application/DTOs/GeoNamesDtos.cs` - 数据模型
- `CityService.Application/Services/IGeoNamesImportService.cs` - 服务接口
- `CityService.Application/Services/GeoNamesImportService.cs` - 服务实现
- `CityService/Controllers/GeoNamesController.cs` - API 控制器
- `CityService/Program.cs` - 服务注册
- `CityService/appsettings.json` - 配置

## 支持

如有问题,请检查:
1. GeoNames 账户是否已激活
2. Web Services 是否已启用
3. Username 配置是否正确
4. 日志中的详细错误信息

---

**最后更新**: 2024-01-XX
**版本**: 1.0.0

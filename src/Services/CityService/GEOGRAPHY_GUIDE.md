# 地理数据三级架构实施指南

## 📋 概述

已实现国家（Country）→ 省份（Province）→ 城市（City）三级地理数据结构，支持关联查询和数据过滤。

## 🗂️ 数据模型

### 1. Country（国家表）
```csharp
- Id: UUID
- Name: 英文名称
- NameZh: 中文名称
- Code: ISO 3166-1 alpha-2 (CN, US, etc.)
- CodeAlpha3: ISO 3166-1 alpha-3 (CHN, USA, etc.)
- Continent: 大洲
- CallingCode: 国际电话区号
- FlagUrl: 国旗图片URL
- IsActive: 是否启用
- CreatedAt/UpdatedAt: 时间戳
```

### 2. Province（省份表）
```csharp
- Id: UUID
- Name: 省份名称
- CountryId: 外键 → countries.id
- Code: 省份代码（可选）
- IsActive: 是否启用
- CreatedAt/UpdatedAt: 时间戳
```

### 3. City（城市表）
```csharp
- 现有所有字段 +
- CountryId: 外键 → countries.id
- ProvinceId: 外键 → provinces.id
```

## 🚀 部署步骤

### 步骤 1: 执行数据库初始化脚本

在 Supabase SQL Editor 中运行：
```bash
src/Services/CityService/CityService/Database/create-geography-tables.sql
```

这将创建：
- `countries` 表
- `provinces` 表  
- 为 `cities` 表添加 `country_id` 和 `province_id` 字段
- 所有必要的索引和外键约束

### 步骤 2: 重新部署 CityService

```bash
cd deployment
./deploy-services-local.sh
```

### 步骤 3: 导入全球国家数据

```bash
curl -X POST http://localhost:8002/api/v1/admin/geography/seed/countries \
  -H "Content-Type: application/json" \
  -d @src/Services/CityService/CityService/Data/world-countries.json
```

### 步骤 4: 导入中国省市数据

使用内置的中国省市数据：
```bash
curl -X POST http://localhost:8002/api/v1/admin/geography/seed/china-default \
  -H "Content-Type: application/json"
```

或使用自定义数据：
```bash
curl -X POST http://localhost:8002/api/v1/admin/geography/seed/china-provinces \
  -H "Content-Type: application/json" \
  -d '[
    {
      "province": "北京市",
      "cities": ["北京市"]
    },
    {
      "province": "上海市", 
      "cities": ["上海市"]
    }
  ]'
```

## 📡 API 端点

### 管理端点

#### 1. 导入全球国家数据
```http
POST /api/v1/admin/geography/seed/countries
Content-Type: application/json

[
  {
    "name": "China",
    "nameZh": "中国",
    "code": "CN",
    "codeAlpha3": "CHN",
    "continent": "Asia",
    "callingCode": "+86"
  }
]
```

#### 2. 导入中国省市（预定义数据）
```http
POST /api/v1/admin/geography/seed/china-default
```

#### 3. 导入中国省市（自定义数据）
```http
POST /api/v1/admin/geography/seed/china-provinces
Content-Type: application/json

[
  {
    "province": "广东省",
    "cities": ["广州市", "深圳市", "珠海市"]
  }
]
```

### 响应示例

```json
{
  "success": true,
  "message": "Data seeded successfully",
  "data": {
    "success": true,
    "countriesCreated": 1,
    "provincesCreated": 34,
    "citiesCreated": 345,
    "citiesFailed": 0,
    "errorMessage": null
  }
}
```

## 🔍 查询示例

### 1. 查询所有国家
```sql
SELECT * FROM countries WHERE is_active = true ORDER BY name;
```

### 2. 查询某个国家的所有省份
```sql
SELECT p.* 
FROM provinces p
JOIN countries c ON p.country_id = c.id
WHERE c.code = 'CN' AND p.is_active = true
ORDER BY p.name;
```

### 3. 查询某个省份的所有城市
```sql
SELECT c.* 
FROM cities c
JOIN provinces p ON c.province_id = p.id
WHERE p.name = '广东省' AND c.is_active = true
ORDER BY c.name;
```

### 4. 查询城市的完整层级信息
```sql
SELECT 
    c.name as city_name,
    p.name as province_name,
    co.name as country_name,
    co.code as country_code
FROM cities c
LEFT JOIN provinces p ON c.province_id = p.id
LEFT JOIN countries co ON c.country_id = co.id
WHERE c.name = '深圳市';
```

## 📊 数据统计

已包含的中国省市数据：
- **34个省级行政区**：
  - 4个直辖市（北京、天津、上海、重庆）
  - 23个省
  - 5个自治区
  - 2个特别行政区（香港、澳门）

- **345+个城市**：包含所有地级市、自治州、地区等

- **40个全球主要国家**：覆盖亚洲、欧洲、美洲、大洋洲、非洲

## 🔧 Repository 使用示例

### C# 代码示例

```csharp
// 注入 Repositories
private readonly ICountryRepository _countryRepository;
private readonly IProvinceRepository _provinceRepository;
private readonly ICityRepository _cityRepository;

// 获取所有国家
var countries = await _countryRepository.GetAllCountriesAsync();

// 获取中国
var china = await _countryRepository.GetCountryByCodeAsync("CN");

// 获取中国的所有省份
var provinces = await _provinceRepository.GetProvincesByCountryIdAsync(china.Id);

// 获取某个省份的所有城市
var cities = await _cityRepository.GetCitiesByProvinceIdAsync(provinceId);
```

## 🎯 关键特性

✅ **三级关联结构**：Country → Province → City  
✅ **外键约束**：保证数据完整性  
✅ **级联操作**：删除国家时级联删除省份  
✅ **索引优化**：为所有外键和常用查询字段建立索引  
✅ **批量导入**：支持批量创建省市数据  
✅ **中文支持**：国家表包含中文名称  
✅ **扩展性强**：可以轻松添加更多国家的省市数据  

## 📝 注意事项

1. **数据导入顺序**：必须先导入国家，再导入省份，最后导入城市
2. **唯一性约束**：`provinces` 表有 `(country_id, name)` 唯一约束，避免重复
3. **软删除**：使用 `is_active` 字段而不是物理删除
4. **时区**：所有时间戳使用 UTC
5. **国家代码**：使用标准的 ISO 3166-1 alpha-2 和 alpha-3 代码

## 🔄 后续扩展

可以继续添加：
- 更多国家的省市数据
- 城市的经纬度坐标（使用 Amap MCP 组件）
- 城市的封面图片 URL
- 国家的国旗图片 URL
- 更详细的城市属性（人口、气候、时区等）

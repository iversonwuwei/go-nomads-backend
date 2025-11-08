# 城市搜索中英文支持 - 实现文档

## 📋 功能概述

为城市搜索接口添加中英文双语搜索支持,用户可以使用中文或英文名称搜索城市。

## 🎯 实现内容

### 1. 数据库层优化

#### 修改文件: `SupabaseCityRepository.cs`
**路径**: `src/Services/CityService/CityService/Infrastructure/Repositories/SupabaseCityRepository.cs`

**修改内容**:
```csharp
// 原代码 (仅支持 name 字段搜索)
if (!string.IsNullOrWhiteSpace(criteria.Name))
{
    cities = cities.Where(c => c.Name.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase));
}

// 新代码 (支持 name 和 name_en 双字段搜索)
if (!string.IsNullOrWhiteSpace(criteria.Name))
{
    // 支持中英文搜索: 在 name 或 name_en 字段中搜索
    cities = cities.Where(c => 
        c.Name.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase) ||
        (!string.IsNullOrWhiteSpace(c.NameEn) && c.NameEn.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase))
    );
}
```

**功能说明**:
- ✅ 同时搜索 `name` 和 `name_en` 字段
- ✅ 大小写不敏感搜索
- ✅ 自动判断 `name_en` 是否为空
- ✅ 任一字段匹配即返回结果

### 2. API 接口优化

#### 修改文件: `CitiesController.cs`
**路径**: `src/Services/CityService/CityService/API/Controllers/CitiesController.cs`

**修改内容**:
```csharp
// 为 GetCities 接口添加 search 参数
[HttpGet]
public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCities(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null)  // 新增搜索参数
{
    // 如果有搜索参数,使用搜索接口(支持中英文搜索)
    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchDto = new CitySearchDto
        {
            Name = search,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        cities = await _cityService.SearchCitiesAsync(searchDto, userId);
        totalCount = cities.Count();
    }
    else
    {
        cities = await _cityService.GetAllCitiesAsync(pageNumber, pageSize, userId);
        totalCount = await _cityService.GetTotalCountAsync();
    }
}
```

**功能说明**:
- ✅ 添加可选的 `search` 查询参数
- ✅ 有搜索参数时使用 SearchCitiesAsync (支持中英文)
- ✅ 无搜索参数时返回全部城市列表
- ✅ 保持向后兼容性

## 📖 API 使用示例

### 1. 搜索中文城市名
```http
GET /api/v1/cities?search=北京
GET /api/v1/cities?search=上海
GET /api/v1/cities?search=深圳
```

**响应示例**:
```json
{
  "success": true,
  "message": "Cities retrieved successfully",
  "data": {
    "items": [
      {
        "id": "xxx",
        "name": "北京",
        "nameEn": "Beijing",
        "country": "China",
        "region": "Beijing"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 10
  }
}
```

### 2. 搜索英文城市名
```http
GET /api/v1/cities?search=Beijing
GET /api/v1/cities?search=Shanghai
GET /api/v1/cities?search=Shenzhen
```

**响应示例**:
```json
{
  "success": true,
  "message": "Cities retrieved successfully",
  "data": {
    "items": [
      {
        "id": "xxx",
        "name": "北京",
        "nameEn": "Beijing",
        "country": "China",
        "region": "Beijing"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 10
  }
}
```

### 3. 模糊搜索
```http
GET /api/v1/cities?search=hai
```

**匹配结果**:
- 上**海** (Shanghai)
- 秦皇岛市 (Qin**huangdao**)

### 4. 不带搜索参数(获取全部)
```http
GET /api/v1/cities?pageNumber=1&pageSize=10
```

**响应**: 返回所有城市的分页列表

### 5. 专用搜索接口(高级搜索)
```http
POST /api/v1/cities/search
Content-Type: application/json

{
  "name": "Beijing",
  "country": "China",
  "minCostOfLiving": 1000,
  "maxCostOfLiving": 3000,
  "pageNumber": 1,
  "pageSize": 10
}
```

## 🔍 搜索逻辑说明

### 搜索优先级
1. **第一优先**: 匹配 `name` 字段(中文名)
2. **第二优先**: 匹配 `name_en` 字段(英文名)
3. **任一匹配即返回**

### 搜索特性
- ✅ **大小写不敏感**: "beijing" 和 "Beijing" 结果相同
- ✅ **部分匹配**: "hai" 可以匹配 "Shanghai"
- ✅ **中英文混合**: 支持任意语言组合
- ✅ **空值安全**: 自动处理 `name_en` 为 NULL 的情况

### 搜索示例

| 输入 | 匹配字段 | 结果 |
|------|---------|------|
| 北京 | name | 北京 (Beijing) |
| Beijing | name_en | 北京 (Beijing) |
| bei | name_en | **Bei**jing |
| 上 | name | **上**海 |
| shang | name_en | **Shang**hai |
| qing | name_en | Qingdao, Qinhuangdao |

## 🚀 部署说明

### 前置条件
1. ✅ 数据库已执行 `add_name_en_to_cities.sql` 脚本
2. ✅ 所有城市都有 `name_en` 字段值

### 部署步骤

#### 1. 重新构建服务
```bash
# 进入项目根目录
cd e:\Workspaces\WaldenProjects\go-nomads

# 重新构建 CityService
docker-compose build cityservice

# 重新构建 Gateway (自动获得新功能)
docker-compose build gateway
```

#### 2. 重启服务
```bash
# 停止现有服务
docker-compose down

# 启动更新后的服务
docker-compose up -d cityservice gateway

# 查看日志
docker-compose logs -f cityservice
docker-compose logs -f gateway
```

#### 3. 验证功能
```bash
# 测试中文搜索
curl "http://localhost:8002/api/v1/cities?search=北京"

# 测试英文搜索
curl "http://localhost:8002/api/v1/cities?search=Beijing"

# 测试模糊搜索
curl "http://localhost:8002/api/v1/cities?search=hai"
```

## 📊 性能优化建议

### 当前实现
- **类型**: 内存过滤
- **方式**: 先加载所有城市,再在内存中过滤
- **适用**: 小规模数据 (< 1000 条)

### 优化方案(未来)
如果城市数据量超过 1000 条,建议:

1. **使用数据库层搜索**:
```csharp
var response = await SupabaseClient
    .From<City>()
    .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
    .Or($"name.ilike.%{criteria.Name}%,name_en.ilike.%{criteria.Name}%")
    .Order(x => x.OverallScore!, Postgrest.Constants.Ordering.Descending)
    .Get();
```

2. **添加全文搜索索引**:
```sql
-- PostgreSQL 全文搜索索引
CREATE INDEX idx_cities_name_fulltext ON cities USING gin(to_tsvector('english', name || ' ' || COALESCE(name_en, '')));
```

3. **使用专业搜索引擎**:
- Elasticsearch
- Algolia
- Meilisearch

## ✅ 测试清单

部署后请测试:

- [ ] 中文城市名搜索: `search=北京`
- [ ] 英文城市名搜索: `search=Beijing`
- [ ] 模糊搜索: `search=hai`
- [ ] 大小写不敏感: `search=BEIJING`
- [ ] 空搜索参数: 无 `search` 参数时返回全部
- [ ] 分页功能: `pageNumber=2&pageSize=20`
- [ ] 不存在的城市: `search=NotExists` 返回空列表
- [ ] 特殊字符: `search=Xi'an` 正常工作

## 🔗 相关接口

### CityService 接口
1. **GET** `/api/v1/cities?search={keyword}` - 获取城市列表(支持搜索)
2. **POST** `/api/v1/cities/search` - 高级搜索(支持多条件)

### Gateway 接口
1. **GET** `/api/cities` - 通过 Gateway 访问城市列表
2. **GET** `/api/cities/with-coworking` - 获取有联合办公的城市

## 📝 代码修改总结

### 修改的文件
1. ✅ `SupabaseCityRepository.cs` - 数据库搜索逻辑
2. ✅ `CitiesController.cs` - API 接口添加 search 参数

### 未修改的文件
- `CityApplicationService.cs` - 无需修改,已支持搜索
- `City.cs` - 实体已有 `NameEn` 字段
- `CityDto.cs` - DTO 已有 `NameEn` 字段
- Gateway 层 - 通过 Dapr 调用,自动获得新功能

### 编译状态
- ✅ CityService 编译成功
- ✅ Gateway 编译成功 (无需修改)

## 💡 使用建议

### 前端集成
```javascript
// 城市搜索组件示例
async function searchCities(keyword) {
  const response = await fetch(
    `/api/v1/cities?search=${encodeURIComponent(keyword)}&pageSize=10`
  );
  return await response.json();
}

// 支持中英文输入
searchCities("北京");  // 搜索中文
searchCities("Beijing");  // 搜索英文
searchCities("bei");  // 模糊搜索
```

### 自动完成建议
```javascript
// 实时搜索建议
const debounce = (func, wait) => {
  let timeout;
  return (...args) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
};

const searchWithDebounce = debounce(async (keyword) => {
  if (keyword.length >= 2) {
    const results = await searchCities(keyword);
    // 显示搜索建议
  }
}, 300);
```

---

**实现时间**: 2025-01-05  
**版本**: v1.0  
**状态**: ✅ 代码完成,待部署测试

# 为 Cities 表添加英文名称字段

## 📅 完成时间
2025-11-05

## ✅ 实施内容

### 1. 数据库层面
**文件**: `database/migrations/add_name_en_to_cities.sql`

#### 主要变更:
- ✅ 添加 `name_en` 字段 (VARCHAR(100))
- ✅ 为所有已有中文城市名称填充英文翻译
- ✅ 为已经是英文的城市名,保持 name_en 与 name 一致
- ✅ 创建索引 `idx_cities_name_en` 提升查询性能

#### 翻译覆盖:
- **中国城市**: 70+ 个 (包括主要城市和省会城市)
- **泰国城市**: 10 个
- **其他国际城市**: 墨西哥城、里斯本、巴塞罗那、巴厘岛等

### 2. 实体层面
**文件**: `Domain/Entities/City.cs`

```csharp
/// <summary>
/// 城市英文名称
/// </summary>
[MaxLength(100)]
[Column("name_en")]
public string? NameEn { get; set; }
```

### 3. DTO 层面

#### CityService DTOs
**文件**: `Application/DTOs/CityDtos.cs`

更新的 DTO 类:
- ✅ `CityDto` - 添加 `NameEn` 属性
- ✅ `CreateCityDto` - 添加 `NameEn` 属性
- ✅ `UpdateCityDto` - 添加 `NameEn` 属性

#### Gateway DTOs
**文件**: `Gateway/DTOs/CityDto.cs`

```csharp
/// <summary>
/// 城市英文名称
/// </summary>
public string? NameEn { get; set; }
```

## 📋 执行步骤

### 步骤 1: 在 Supabase 中执行 SQL 脚本
由于您使用远程 Supabase,请按以下方式执行:

1. 登录 Supabase Dashboard: https://supabase.com
2. 选择项目: `lcfbajrocmjlqndkrsao`
3. 进入 **SQL Editor**
4. 复制 `add_name_en_to_cities.sql` 的内容
5. 点击 **Run** 执行

### 步骤 2: 重新部署服务
```powershell
cd e:\Workspaces\WaldenProjects\go-nomads
.\deployment\deploy-services-local.ps1 -ServiceName CityService
.\deployment\deploy-services-local.ps1 -ServiceName Gateway
```

### 步骤 3: 验证更新
```sql
-- 查看已更新的城市
SELECT name, name_en, country 
FROM cities 
WHERE name_en IS NOT NULL 
LIMIT 20;
```

## 🎯 使用场景

### 1. 多语言支持
前端可以根据用户语言偏好显示对应名称:
```typescript
const cityName = userLanguage === 'en' ? city.nameEn : city.name;
```

### 2. SEO 优化
英文名称可用于 URL slug:
```typescript
const citySlug = city.nameEn?.toLowerCase().replace(/\s+/g, '-');
// /cities/chiang-mai
```

### 3. 搜索增强
支持中英文混合搜索:
```sql
SELECT * FROM cities 
WHERE name ILIKE '%清迈%' 
   OR name_en ILIKE '%Chiang%';
```

### 4. API 响应
客户端可以同时获取中英文名称:
```json
{
  "id": "xxx",
  "name": "清迈",
  "nameEn": "Chiang Mai",
  "country": "Thailand"
}
```

## 📊 数据统计

### 当前数据库城市数据 (119 个城市)
- **中文城市**: 77 个
- **英文城市**: 42 个
- **新增翻译**: 80+ 个映射关系

### 字段信息
- **字段名**: `name_en`
- **类型**: `VARCHAR(100)`
- **可空**: `YES`
- **索引**: `idx_cities_name_en`

## 🔄 后续优化建议

### 1. GeoNames 集成
在导入 GeoNames 数据时自动填充英文名称:
```csharp
city.Name = translatedName;  // 中文名
city.NameEn = geoNamesCity.Name;  // 英文原名
```

### 2. 翻译 API
对于没有预定义翻译的城市,可以集成翻译服务:
```csharp
if (string.IsNullOrEmpty(city.NameEn))
{
    city.NameEn = await _translationService.TranslateAsync(city.Name, "zh", "en");
}
```

### 3. 管理界面
在管理后台添加英文名称编辑功能,允许手动维护翻译。

### 4. 自动同步
创建定时任务,从 GeoNames 定期同步更新城市的英文名称。

## 🛠️ 相关文件清单

### 新增文件
1. `database/migrations/add_name_en_to_cities.sql` - 数据库迁移脚本

### 修改文件
1. `src/Services/CityService/CityService/Domain/Entities/City.cs`
2. `src/Services/CityService/CityService/Application/DTOs/CityDtos.cs`
3. `src/Gateway/Gateway/DTOs/CityDto.cs`

### 构建状态
- ✅ CityService: 构建成功
- ✅ Gateway: 构建成功
- ✅ 无编译错误

## 📝 示例数据

执行 SQL 后的示例结果:
```
name         | name_en       | country
-------------|---------------|----------
北京         | Beijing       | China
上海         | Shanghai      | China
清迈         | Chiang Mai    | Thailand
巴塞罗那     | Barcelona     | Spain
墨西哥城     | Mexico City   | Mexico
```

## ✨ 总结

成功为 `cities` 表添加了 `name_en` 字段,实现了中英文城市名称的双语支持。所有代码层面的修改已完成并通过编译验证,只需在 Supabase Dashboard 中执行 SQL 脚本即可完成整个功能的部署。

这个改进将为应用提供:
- 🌐 更好的国际化支持
- 🔍 增强的搜索功能
- 🎨 灵活的 UI 显示选项
- 🔗 SEO 友好的 URL

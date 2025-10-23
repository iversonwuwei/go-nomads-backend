# SQL 脚本初始化地理数据指南

## 📦 脚本说明

已创建 SQL 脚本来初始化地理数据，无需通过 API 调用。

### 脚本文件

1. **`create-geography-tables.sql`** - 创建表结构（countries, provinces, cities）
2. **`seed-geography-data-complete.sql`** - 完整数据初始化（推荐使用）

## 🚀 执行步骤

### 步骤 1: 创建表结构

登录 Supabase Dashboard → SQL Editor，执行：

```sql
-- 文件位置：
src/Services/CityService/CityService/Database/create-geography-tables.sql
```

这将创建：
- ✅ `countries` 表（国家）
- ✅ `provinces` 表（省份）
- ✅ 为 `cities` 表添加 `country_id` 和 `province_id` 字段
- ✅ 所有必要的索引和外键约束

### 步骤 2: 初始化数据

执行完整数据初始化脚本：

```sql
-- 文件位置：
src/Services/CityService/CityService/Database/seed-geography-data-complete.sql
```

这将插入：
- ✅ **15个全球主要国家**（中国、美国、日本、德国、英国、法国等）
- ✅ **34个中国省级行政区**（4直辖市、23省、5自治区、2特别行政区）
- ✅ **280+个中国主要城市**（所有省会城市和重要地级市）

### 步骤 3: 验证数据

脚本执行完成后会自动显示统计信息：

```sql
📊 数据统计:
- countries: 15
- provinces: 34
- cities: 280+

每个省份的城市数量:
广东省: 21
山东省: 16
河南省: 18
...
```

## 📊 数据规模

| 层级 | 数量 | 说明 |
|-----|------|-----|
| 国家 | 15 | 全球主要国家 |
| 省份 | 34 | 中国所有省级行政区 |
| 城市 | 280+ | 中国主要城市（省会+地级市） |

## ✅ 优势

相比 API 初始化：
1. **更快**：一次性批量插入，不受 API 速率限制
2. **更简单**：直接在数据库执行，无需 HTTP 请求
3. **更可靠**：事务性操作，要么全部成功要么全部回滚
4. **更容易调试**：SQL 语句清晰可见，便于排查问题
5. **可重复执行**：使用 `ON CONFLICT DO NOTHING` 避免重复插入

## 🔍 数据查询示例

### 1. 查询所有国家

```sql
SELECT name, name_zh, code, continent 
FROM countries 
WHERE is_active = true 
ORDER BY name;
```

### 2. 查询中国的所有省份

```sql
SELECT p.name, COUNT(c.id) as city_count
FROM provinces p
LEFT JOIN cities c ON c.province_id = p.id
WHERE p.country_id = (SELECT id FROM countries WHERE code = 'CN')
GROUP BY p.name
ORDER BY city_count DESC;
```

### 3. 查询某个省份的所有城市

```sql
SELECT c.name as city_name
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

## 🔧 自定义扩展

### 添加更多国家

在 `seed-geography-data-complete.sql` 中添加：

```sql
INSERT INTO countries (id, name, name_zh, code, code_alpha3, continent, calling_code, is_active, created_at)
VALUES 
  (gen_random_uuid(), 'New Country', '新国家', 'XX', 'XXX', 'Asia', '+999', true, CURRENT_TIMESTAMP)
ON CONFLICT (code) DO NOTHING;
```

### 添加更多城市

在 `seed-geography-data-complete.sql` 的 `city_data` CTE 中添加：

```sql
('省份名称', ARRAY['城市1', '城市2', '城市3'])
```

## ⚠️ 注意事项

1. **执行顺序**：必须先执行 `create-geography-tables.sql`，再执行 `seed-geography-data-complete.sql`
2. **幂等性**：使用 `ON CONFLICT DO NOTHING` 确保可以重复执行
3. **事务**：所有操作在单个事务中执行，确保数据一致性
4. **性能**：批量插入使用 `unnest` 和 `CTE`，性能优于逐条插入

## 📚 相关文档

- 表结构设计：`src/Services/CityService/GEOGRAPHY_GUIDE.md`
- 实施总结：`GEOGRAPHY_IMPLEMENTATION_SUMMARY.md`
- API 端点文档：`http://localhost:8002/scalar/v1`

## 🎉 完成！

执行完两个 SQL 脚本后，您的系统将拥有完整的三级地理数据结构！

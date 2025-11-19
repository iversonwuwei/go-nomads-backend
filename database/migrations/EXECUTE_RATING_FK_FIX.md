# 修复城市评分外键约束问题

## 问题描述
`city_ratings` 表的 `user_id` 字段有外键约束引用 `auth.users(id)`，导致用户评分时出现：
```
insert or update on table "city_ratings" violates foreign key constraint "city_ratings_user_id_fkey"
Key (user_id)=(xxx) is not present in table "users"
```

## 解决方案
移除外键约束，使评分系统独立于认证系统。

## 执行步骤

### 方法 1: 使用 Supabase Dashboard
1. 登录 Supabase Dashboard
2. 进入 SQL Editor
3. 执行以下 SQL：

```sql
-- 移除 city_ratings 表的 user_id 外键约束
ALTER TABLE city_ratings 
DROP CONSTRAINT IF EXISTS city_ratings_user_id_fkey;

-- 移除 city_rating_categories 的 created_by 外键约束
ALTER TABLE city_rating_categories 
DROP CONSTRAINT IF EXISTS city_rating_categories_created_by_fkey;

-- 验证约束已移除
SELECT
    conname AS constraint_name,
    contype AS constraint_type,
    conrelid::regclass AS table_name
FROM pg_constraint
WHERE conrelid IN ('city_ratings'::regclass, 'city_rating_categories'::regclass)
    AND contype = 'f';
```

### 方法 2: 使用 psql 命令行
```bash
psql "your-database-connection-string" -f remove_city_ratings_fk_constraint.sql
```

### 方法 3: 使用 Docker (如果数据库在容器中)
```bash
docker exec -i your-postgres-container psql -U postgres -d your_database < remove_city_ratings_fk_constraint.sql
```

## 验证
执行后应该看到：
- 0 行结果（表示没有外键约束了）
- 或者显示其他无关的约束

## 重启服务
修复后需要重启 CityService：
```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
docker restart go-nomads-city-service
```

## 注意事项
- 这个修改不会影响现有数据
- 评分功能将正常工作
- 用户ID 将作为普通字段存储，不再验证是否存在于 auth.users 表

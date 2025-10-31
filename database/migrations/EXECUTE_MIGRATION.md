# 数据库迁移执行指南

## 步骤 1: 访问 Supabase SQL Editor

1. 打开浏览器,访问: https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao
2. 登录你的 Supabase 账号
3. 在左侧菜单中点击 "SQL Editor"

## 步骤 2: 执行 SQL 迁移

1. 打开文件: `/Users/walden/Workspaces/WaldenProjects/go-noma/database/migrations/create_user_city_content_tables.sql`
2. 复制整个文件内容 (已修复 city_id 歧义问题)
3. 粘贴到 Supabase SQL Editor 中
4. 点击右下角的 "Run" 按钮执行

## 步骤 3: 验证迁移成功

执行以下查询验证表已创建:

```sql
-- 查看所有新创建的表
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name LIKE 'user_city_%'
ORDER BY table_name;
```

应该返回 3 个表:
- user_city_expenses
- user_city_photos  
- user_city_reviews

## 步骤 4: 验证视图

```sql
-- 查看统计视图
SELECT * FROM user_city_content_stats LIMIT 5;
```

## 步骤 5: 测试 RLS 策略

```sql
-- 查看 RLS 策略
SELECT schemaname, tablename, policyname 
FROM pg_policies 
WHERE tablename LIKE 'user_city_%'
ORDER BY tablename, policyname;
```

应该看到 7 个策略。

## 预期结果

✅ 3 个表创建成功
✅ 13 个索引创建成功  
✅ 7 个 RLS 策略启用
✅ 1 个触发器创建成功
✅ 1 个统计视图创建成功

## 如果遇到错误

如果看到 "already exists" 错误,说明表已经存在,可以先删除旧表:

```sql
-- 谨慎使用!这会删除所有数据
DROP VIEW IF EXISTS user_city_content_stats CASCADE;
DROP TABLE IF EXISTS user_city_reviews CASCADE;
DROP TABLE IF EXISTS user_city_expenses CASCADE;
DROP TABLE IF EXISTS user_city_photos CASCADE;
```

然后重新执行迁移 SQL。

## 修复说明

**已修复的问题**: 在 `user_city_content_stats` 视图中,`city_id` 列引用不明确。

**修复内容**:
- Line 161: `city_id` → `cities.city_id`  
- Line 170: `GROUP BY city_id` → `GROUP BY cities.city_id`

这个修复解决了 "ERROR: 42702: column reference 'city_id' is ambiguous" 错误。

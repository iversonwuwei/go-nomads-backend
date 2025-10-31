# 用户城市内容数据库迁移指南

## ✅ 方式 1: Supabase SQL Editor (推荐)

1. 打开 Supabase 控制台: https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao
2. 进入 SQL Editor
3. 点击 "New Query"
4. 复制粘贴 `create_user_city_content_tables.sql` 的全部内容
5. 点击 "Run" 执行

**SQL 文件位置:**
```
/Users/walden/Workspaces/WaldenProjects/go-noma/database/migrations/create_user_city_content_tables.sql
```

---

## 方式 2: 使用 pgAdmin 或 DBeaver

**连接信息:**
- Host: `db.lcfbajrocmjlqndkrsao.supabase.co`
- Port: `6543`
- Database: `postgres`
- Username: `postgres.lcfbajrocmjlqndkrsao`
- Password: `bwTyaM1eJ1TRIZI3`
- SSL: Require

执行 SQL 文件即可。

---

## 方式 3: 安装 psql 后执行

### macOS 安装 PostgreSQL 客户端:
```bash
brew install postgresql@16
```

### 执行迁移:
```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/database/migrations
./run_user_city_content_migration.sh
```

---

## 创建的表结构

### 1. user_city_photos
```sql
- id (UUID, 主键)
- user_id (UUID, 外键 → auth.users)
- city_id (VARCHAR)
- image_url (TEXT)
- caption (TEXT, 可选)
- location (TEXT, 可选)
- taken_at (TIMESTAMP, 可选)
- created_at (TIMESTAMP)
```

### 2. user_city_expenses
```sql
- id (UUID, 主键)
- user_id (UUID, 外键 → auth.users)
- city_id (VARCHAR)
- category (expense_category_enum)
- amount (DECIMAL)
- currency (VARCHAR, 默认 USD)
- description (TEXT, 可选)
- date (DATE)
- created_at (TIMESTAMP)
```

**费用分类枚举:**
- food
- transport
- accommodation
- activity
- shopping
- other

### 3. user_city_reviews
```sql
- id (UUID, 主键)
- user_id (UUID, 外键 → auth.users)
- city_id (VARCHAR)
- rating (INTEGER, 1-5)
- title (VARCHAR)
- content (TEXT)
- visit_date (DATE, 可选)
- created_at (TIMESTAMP)
- updated_at (TIMESTAMP)
```

**唯一约束:** (user_id, city_id) - 每个用户每个城市只能有一个评论

### 4. user_city_content_stats (视图)
聚合统计视图，包含：
- photo_count
- expense_count
- review_count
- average_rating
- photo_contributors
- expense_contributors
- review_contributors

---

## 安全性 (RLS)

所有表都启用了 Row Level Security:

- ✅ 用户只能 CRUD 自己的照片和费用
- ✅ 评论对所有人可读 (SELECT)
- ✅ 用户只能修改自己的评论

---

## 性能优化

每个表都创建了以下索引:
- `user_id` (单列)
- `city_id` (单列)
- `(user_id, city_id)` (复合)
- `created_at DESC` (排序)

---

## 验证迁移成功

执行以下查询验证:

```sql
-- 检查表是否创建成功
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name LIKE 'user_city_%';

-- 检查 RLS 是否启用
SELECT tablename, rowsecurity 
FROM pg_tables 
WHERE schemaname = 'public' 
  AND tablename LIKE 'user_city_%';

-- 检查索引
SELECT indexname, tablename 
FROM pg_indexes 
WHERE schemaname = 'public' 
  AND tablename LIKE 'user_city_%';
```

---

## 下一步

迁移成功后:

1. ✅ 重新发布并部署 CityService
2. ✅ 测试 API 端点
3. ✅ 开发 Flutter UI 页面

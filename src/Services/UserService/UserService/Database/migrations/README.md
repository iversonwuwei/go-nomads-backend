# 数据库迁移指南

## 概述

本目录包含 UserService 数据库的迁移脚本,用于逐步更新 Supabase 数据库结构。

## 迁移列表

### 001_add_password_and_role.sql

**日期**: 2024-10-21  
**描述**: 添加密码认证和用户角色支持

**变更内容**:

- 添加 `password_hash` 字段 (VARCHAR(255)) - 存储 BCrypt 哈希后的密码
- 添加 `role` 字段 (VARCHAR(50)) - 存储用户角色,默认值为 'user'
- 创建 `role` 字段的索引以提高查询性能
- 为已存在的用户设置默认角色

## 如何执行迁移

### 方法 1: 使用 Supabase Dashboard (推荐)

1. 登录 [Supabase Dashboard](https://app.supabase.com)
2. 选择您的项目
3. 进入 **SQL Editor**
4. 打开 `migrations/001_add_password_and_role.sql` 文件
5. 复制文件内容
6. 粘贴到 SQL Editor 中
7. 点击 **Run** 执行

### 方法 2: 使用 psql 命令行

```bash
# 设置数据库连接信息
export SUPABASE_DB_URL="postgresql://postgres:[YOUR-PASSWORD]@db.lcfbajrocmjlqndkrsao.supabase.co:5432/postgres"

# 执行迁移
psql $SUPABASE_DB_URL -f migrations/001_add_password_and_role.sql
```

### 方法 3: 使用数据库客户端工具

使用 DBeaver, TablePlus, pgAdmin 等工具:

1. 连接到您的 Supabase 数据库
2. 打开 `migrations/001_add_password_and_role.sql` 文件
3. 执行 SQL 脚本

## 验证迁移

执行以下 SQL 查询来验证迁移是否成功:

```sql
-- 检查表结构
SELECT 
    column_name, 
    data_type, 
    column_default,
    is_nullable
FROM 
    information_schema.columns
WHERE 
    table_schema = 'public' 
    AND table_name = 'users'
ORDER BY 
    ordinal_position;

-- 应该看到 password_hash 和 role 字段
```

## 回滚 (如果需要)

如果需要回滚此迁移:

```sql
-- 删除新添加的字段
ALTER TABLE public.users DROP COLUMN IF EXISTS password_hash;
ALTER TABLE public.users DROP COLUMN IF EXISTS role;

-- 删除索引
DROP INDEX IF EXISTS idx_users_role;
```

## 注意事项

⚠️ **重要提醒**:

- 在生产环境执行迁移前,请先在测试环境测试
- 建议在低流量时段执行迁移
- 执行前请备份数据库
- `password_hash` 字段允许 NULL,便于迁移已存在的用户
- 新创建的用户必须提供密码

## 后续步骤

迁移完成后:

1. 重启 UserService 服务
2. 测试用户注册功能 (POST /api/users/register)
3. 测试用户创建功能 (POST /api/users)
4. 测试用户登录功能 (POST /api/users/login)

## 数据库连接信息

当前连接: `db.lcfbajrocmjlqndkrsao.supabase.co:5432`

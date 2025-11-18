# 🔐 添加密码字段 - 快速指南

## 📋 变更内容

已修改的文件:

1. ✅ `Controllers/UsersController.cs` - 添加密码验证和使用密码创建用户
2. ✅ `Database/schema.sql` - 更新基础表结构
3. ✅ `Database/migrations/001_add_password_and_role.sql` - 数据库迁移脚本
4. ✅ `Database/migrations/run-migration.sh` - 自动化迁移工具

## 🚀 执行步骤

### 步骤 1: 在 Supabase 执行迁移

**选项 A: 使用 Supabase Dashboard (最简单)**

1. 打开 [Supabase Dashboard](https://app.supabase.com)
2. 选择您的项目
3. 点击左侧菜单的 **SQL Editor**
4. 点击 **New query**
5. 复制以下 SQL 并执行:

```sql
-- 添加 password_hash 字段
ALTER TABLE public.users 
ADD COLUMN IF NOT EXISTS password_hash VARCHAR(255);

-- 添加 role 字段
ALTER TABLE public.users 
ADD COLUMN IF NOT EXISTS role VARCHAR(50) DEFAULT 'user' NOT NULL;

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_users_role ON public.users(role);

-- 更新现有用户的角色
UPDATE public.users 
SET role = 'user' 
WHERE role IS NULL;
```

6. 点击 **Run** 执行

**选项 B: 使用命令行工具**

```bash
# 1. 设置数据库连接 (替换 [YOUR-PASSWORD])
export SUPABASE_DB_URL="postgresql://postgres:[YOUR-PASSWORD]@db.lcfbajrocmjlqndkrsao.supabase.co:5432/postgres"

# 2. 执行迁移
cd src/Services/UserService/UserService/Database/migrations
./run-migration.sh
```

### 步骤 2: 验证迁移

在 Supabase SQL Editor 中执行:

```sql
-- 查看表结构
SELECT 
    column_name, 
    data_type, 
    column_default
FROM 
    information_schema.columns
WHERE 
    table_name = 'users' 
    AND column_name IN ('password_hash', 'role');
```

应该看到:

- `password_hash` | character varying | (null)
- `role` | character varying | 'user'::character varying

### 步骤 3: 重启 UserService

```bash
# 如果使用 Docker
docker-compose restart userservice

# 或者如果使用脚本部署
cd deployment
./deploy-services-local.sh
```

### 步骤 4: 测试 API

**创建用户 (现在需要密码)**

```bash
curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试用户",
    "email": "test@example.com",
    "password": "password123",
    "phone": "13800138000"
  }'
```

**预期响应:**

```json
{
  "success": true,
  "message": "User created successfully",
  "data": {
    "id": "...",
    "name": "测试用户",
    "email": "test@example.com",
    "phone": "13800138000",
    "createdAt": "2024-10-21T..."
  }
}
```

**注册用户 (也需要密码)**

```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "新用户",
    "email": "newuser@example.com",
    "password": "secure123",
    "phone": "13900139000"
  }'
```

## ⚠️ 重要提醒

1. **密码要求**:
    - 必填字段
    - 最少 6 个字符
    - 使用 BCrypt 自动哈希

2. **已存在的用户**:
    - `password_hash` 字段为 NULL
    - 这些用户需要通过密码重置流程设置密码
    - 或者可以手动更新

3. **安全建议**:
    - 不要在日志中记录密码
    - 密码永远不会在 API 响应中返回
    - 考虑添加密码复杂度要求

## 🔍 故障排查

### 迁移失败

```bash
# 检查数据库连接
psql $SUPABASE_DB_URL -c "SELECT version();"
```

### API 返回 500 错误

```bash
# 检查 UserService 日志
docker logs userservice
```

### 密码验证失败

- 确保密码至少 6 个字符
- 检查请求 JSON 格式是否正确
- 查看 ModelState 错误信息

## 📝 API 文档更新

**POST /api/users** 现在需要:

```json
{
  "name": "string (required)",
  "email": "string (required, valid email)",
  "password": "string (required, min 6 chars)",
  "phone": "string (optional)"
}
```

**验证错误示例:**

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "密码不能为空",
    "密码至少需要6个字符"
  ]
}
```

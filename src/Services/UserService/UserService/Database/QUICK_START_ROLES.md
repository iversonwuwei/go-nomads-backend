# 🔐 角色管理系统 - 快速开始

## ✅ 已完成的工作

### 1. 数据库层面
- ✅ 创建 `roles` 表,包含默认角色: `user` 和 `admin`
- ✅ 在 `users` 表添加 `role_id` 外键字段
- ✅ 创建迁移脚本: `002_create_roles_table.sql`

### 2. 代码层面
- ✅ 创建 `Role` 模型类 (`Shared/Models/Role.cs`)
- ✅ 创建 `RoleRepository` 接口和实现
- ✅ 创建 `RolesController` API 控制器
- ✅ 在 `User` 模型添加 `RoleId` 属性
- ✅ 注册服务到 DI 容器

## 🚀 执行数据库迁移

### 最简单方式: Supabase Dashboard

1. 访问 https://app.supabase.com
2. 选择您的项目
3. 进入 **SQL Editor**
4. 复制以下完整 SQL 并执行:

```sql
-- 1. 创建角色表
CREATE TABLE IF NOT EXISTS public.roles (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. 插入默认角色
INSERT INTO public.roles (id, name, description) VALUES
    ('role_user', 'user', '普通用户角色'),
    ('role_admin', 'admin', '管理员角色')
ON CONFLICT (name) DO NOTHING;

-- 3. 添加 role_id 字段
ALTER TABLE public.users 
ADD COLUMN IF NOT EXISTS role_id VARCHAR(50) DEFAULT 'role_user';

-- 4. 更新现有用户
UPDATE public.users 
SET role_id = CASE 
    WHEN role = 'admin' THEN 'role_admin'
    ELSE 'role_user'
END
WHERE role_id IS NULL;

-- 5. 添加外键约束
ALTER TABLE public.users
ADD CONSTRAINT fk_users_role_id 
FOREIGN KEY (role_id) 
REFERENCES public.roles(id)
ON DELETE SET NULL;

-- 6. 创建索引
CREATE INDEX IF NOT EXISTS idx_users_role_id ON public.users(role_id);

-- 7. 启用 RLS
ALTER TABLE public.roles ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Allow read access to roles" ON public.roles FOR SELECT USING (true);
```

## ✅ 验证迁移

执行以下 SQL 验证:

```sql
-- 查看角色
SELECT * FROM public.roles;
-- 应该看到 role_user 和 role_admin

-- 查看用户的 role_id
SELECT id, name, email, role, role_id FROM public.users LIMIT 5;
```

## 🧪 测试 API

### 1. 重启 UserService

```bash
docker-compose restart userservice
# 或
cd deployment && ./deploy-services-local.sh
```

### 2. 测试角色 API

**获取所有角色:**
```bash
curl http://localhost:5001/api/roles
```

**获取单个角色:**
```bash
curl http://localhost:5001/api/roles/role_admin
```

**创建新角色:**
```bash
curl -X POST http://localhost:5001/api/roles \
  -H "Content-Type: application/json" \
  -d '{"name": "moderator", "description": "内容审核员"}'
```

### 3. 测试用户创建 (会自动分配默认角色)

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

用户会自动获得 `role_id = 'role_user'`

## 📚 API 端点

| 方法 | 路径 | 描述 |
|------|------|------|
| GET | `/api/roles` | 获取所有角色 |
| GET | `/api/roles/{id}` | 获取单个角色 |
| POST | `/api/roles` | 创建新角色 |
| PUT | `/api/roles/{id}` | 更新角色 |
| DELETE | `/api/roles/{id}` | 删除角色 (不能删除默认角色) |

## 📖 更多文档

- 详细 API 文档: `ROLES_API_GUIDE.md`
- 迁移说明: `migrations/README.md`
- 密码字段迁移: `MIGRATION_GUIDE.md`

## 🎯 总结

现在您的系统有:
1. ✅ 密码认证 (`password_hash` 字段)
2. ✅ 角色管理 (`roles` 表 + `role_id` 外键)
3. ✅ 默认角色: `user` 和 `admin`
4. ✅ 完整的 CRUD API

执行迁移 → 重启服务 → 开始使用! 🚀

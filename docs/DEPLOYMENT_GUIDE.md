# Go Nomads 部署指南

## 📦 部署到 Supabase

本指南将帮助您将 Go Nomads 数据库架构部署到 Supabase PostgreSQL。

### 前置条件

- Supabase 账号
- Supabase 项目(免费或付费计划)
- 数据库访问权限

### 方法 1: 使用 Supabase Dashboard (推荐)

这是最简单的方法,适合初次部署或快速测试。

#### 步骤 1: 登录 Supabase

1. 访问 [https://supabase.com](https://supabase.com)
2. 登录您的账号
3. 选择或创建项目

#### 步骤 2: 打开 SQL Editor

1. 在项目 Dashboard 左侧菜单中,点击 **SQL Editor**
2. 点击 **New Query** 按钮创建新查询

#### 步骤 3: 执行架构脚本

1. 打开项目中的 `database/schema.sql` 文件
2. 复制所有内容
3. 粘贴到 SQL Editor 中
4. 点击 **Run** 或按 `Ctrl+Enter` 执行

#### 步骤 4: 验证部署

执行以下查询验证表是否创建成功:

```sql
-- 查看所有表
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- 查看示例城市数据
SELECT * FROM cities;

-- 查看 PostGIS 扩展是否安装
SELECT PostGIS_Version();
```

### 方法 2: 使用 Supabase CLI

适合自动化部署和 CI/CD 流程。

#### 步骤 1: 安装 Supabase CLI

**Windows (使用 Scoop):**
```powershell
scoop bucket add supabase https://github.com/supabase/scoop-bucket.git
scoop install supabase
```

**macOS (使用 Homebrew):**
```bash
brew install supabase/tap/supabase
```

**Linux:**
```bash
brew install supabase/tap/supabase
```

**使用 npm:**
```bash
npm install -g supabase
```

#### 步骤 2: 登录 Supabase

```powershell
supabase login
```

这将打开浏览器窗口,要求您授权 CLI 访问您的账号。

#### 步骤 3: 链接项目

```powershell
# 获取项目 Reference ID (从 Supabase Dashboard 的 Settings > API)
supabase link --project-ref your-project-ref

# 或者交互式选择项目
supabase link
```

#### 步骤 4: 执行迁移

```powershell
# 初始化本地迁移目录(如果还没有)
supabase init

# 创建新迁移文件
supabase migration new initial_schema

# 将 schema.sql 内容复制到新创建的迁移文件
# 文件位置: supabase/migrations/[timestamp]_initial_schema.sql

# 应用迁移到远程数据库
supabase db push
```

#### 步骤 5: 验证

```powershell
# 查看远程数据库状态
supabase db remote status

# 查看已应用的迁移
supabase migration list
```

### 方法 3: 使用 psql 命令行工具

适合高级用户和直接数据库访问。

#### 步骤 1: 获取连接字符串

1. 登录 Supabase Dashboard
2. 进入 **Settings** > **Database**
3. 复制 **Connection string** 中的 **URI**
4. 替换 `[YOUR-PASSWORD]` 为您的数据库密码

示例:
```
postgresql://postgres:your-password@db.abcdefghijk.supabase.co:5432/postgres
```

#### 步骤 2: 连接数据库

```powershell
# Windows (需要安装 PostgreSQL 客户端)
psql "postgresql://postgres:[YOUR-PASSWORD]@db.[YOUR-PROJECT-REF].supabase.co:5432/postgres"

# 或使用环境变量
$env:PGPASSWORD="your-password"
psql -h db.[YOUR-PROJECT-REF].supabase.co -U postgres -d postgres -p 5432
```

#### 步骤 3: 执行脚本

**选项 A: 直接执行文件**
```sql
\i database/schema.sql
```

**选项 B: 复制粘贴内容**
```powershell
# 读取文件并执行
Get-Content database/schema.sql | psql "your-connection-string"
```

#### 步骤 4: 验证

```sql
-- 列出所有表
\dt

-- 查看表结构
\d cities

-- 退出
\q
```

### 方法 4: 使用 GUI 工具 (pgAdmin, DBeaver, TablePlus)

#### 使用 pgAdmin

1. 打开 pgAdmin
2. 右键 **Servers** > **Create** > **Server**
3. **General** 标签页:
   - Name: `Supabase - Go Nomads`
4. **Connection** 标签页:
   - Host: `db.[YOUR-PROJECT-REF].supabase.co`
   - Port: `5432`
   - Database: `postgres`
   - Username: `postgres`
   - Password: `[YOUR-PASSWORD]`
5. 点击 **Save**
6. 展开服务器,右键 `postgres` 数据库
7. 选择 **Query Tool**
8. 粘贴 `database/schema.sql` 内容
9. 点击 **Execute** (F5)

#### 使用 DBeaver

1. 打开 DBeaver
2. 点击 **Database** > **New Database Connection**
3. 选择 **PostgreSQL**
4. 填写连接信息:
   - Host: `db.[YOUR-PROJECT-REF].supabase.co`
   - Port: `5432`
   - Database: `postgres`
   - Username: `postgres`
   - Password: `[YOUR-PASSWORD]`
5. 点击 **Test Connection** 验证
6. 点击 **Finish**
7. 右键数据库 > **SQL Editor** > **New SQL Script**
8. 粘贴 `database/schema.sql` 内容
9. 点击 **Execute SQL Script** (Ctrl+Alt+X)

#### 使用 TablePlus

1. 打开 TablePlus
2. 点击 **Create a new connection**
3. 选择 **PostgreSQL**
4. 填写连接信息:
   - Name: `Supabase - Go Nomads`
   - Host: `db.[YOUR-PROJECT-REF].supabase.co`
   - Port: `5432`
   - User: `postgres`
   - Password: `[YOUR-PASSWORD]`
   - Database: `postgres`
5. 点击 **Connect**
6. 点击工具栏的 **SQL** 按钮
7. 粘贴 `database/schema.sql` 内容
8. 点击 **Run** (Cmd+Enter 或 Ctrl+Enter)

## 🔍 部署后验证

### 1. 检查表创建

```sql
-- 应该看到所有表
SELECT COUNT(*) as table_count 
FROM information_schema.tables 
WHERE table_schema = 'public';
-- 预期结果: 26 个表

-- 列出所有表名
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;
```

### 2. 检查 PostGIS 扩展

```sql
SELECT PostGIS_Version();
-- 预期输出: 3.3 或更高版本

-- 检查地理位置字段
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'cities' AND column_name = 'location';
-- 预期: location | USER-DEFINED (geography)
```

### 3. 检查索引

```sql
SELECT schemaname, tablename, indexname 
FROM pg_indexes 
WHERE schemaname = 'public' 
ORDER BY tablename, indexname;
```

### 4. 检查触发器

```sql
SELECT trigger_name, event_object_table 
FROM information_schema.triggers 
WHERE trigger_schema = 'public';
-- 应该看到所有 update_*_updated_at 触发器
```

### 5. 检查示例数据

```sql
-- 应该有 5 个示例城市
SELECT COUNT(*) FROM cities;

-- 查看城市详情
SELECT name, country, overall_score, currency 
FROM cities 
ORDER BY overall_score DESC;
```

### 6. 测试地理位置查询

```sql
-- 查找清迈附近 100 km 内的城市
SELECT name, country, 
       ST_Distance(
           location, 
           ST_SetSRID(ST_MakePoint(98.9853, 18.7883), 4326)::geography
       ) / 1000 as distance_km
FROM cities
WHERE ST_DWithin(
    location,
    ST_SetSRID(ST_MakePoint(98.9853, 18.7883), 4326)::geography,
    100000  -- 100 km
)
ORDER BY distance_km;
```

### 7. 检查行级安全策略 (RLS)

```sql
-- 查看启用 RLS 的表
SELECT schemaname, tablename, rowsecurity 
FROM pg_tables 
WHERE schemaname = 'public' AND rowsecurity = true;

-- 查看策略
SELECT schemaname, tablename, policyname, permissive, cmd 
FROM pg_policies 
WHERE schemaname = 'public';
```

## ⚠️ 常见问题

### 问题 1: PostGIS 扩展未安装

**错误信息:**
```
ERROR: type "geography" does not exist
```

**解决方案:**
```sql
-- 手动启用 PostGIS 扩展
CREATE EXTENSION IF NOT EXISTS postgis;
```

### 问题 2: UUID 生成函数不存在

**错误信息:**
```
ERROR: function uuid_generate_v4() does not exist
```

**解决方案:**
```sql
-- 启用 UUID 扩展
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
```

### 问题 3: 权限不足

**错误信息:**
```
ERROR: permission denied for schema public
```

**解决方案:**
- 确保使用 `postgres` 用户连接
- 或在 Supabase Dashboard 的 Database Settings 中检查用户权限

### 问题 4: 表已存在

**错误信息:**
```
ERROR: relation "cities" already exists
```

**解决方案:**

如果需要重新部署,可以先删除所有表:

```sql
-- ⚠️ 警告: 这将删除所有数据!
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

-- 然后重新执行 schema.sql
```

### 问题 5: 触发器创建失败

**错误信息:**
```
ERROR: syntax error near "$$"
```

**解决方案:**
- 确保使用支持 PostgreSQL 12+ 的客户端
- 尝试分段执行 SQL 脚本

## 🔐 安全最佳实践

### 1. 使用环境变量存储凭据

**不要在代码中硬编码密码!**

```powershell
# appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.abcdefghijk.supabase.co;Database=postgres;Username=postgres;Password=your-password"
  }
}

# 改为使用环境变量
{
  "ConnectionStrings": {
    "DefaultConnection": "${SUPABASE_CONNECTION_STRING}"
  }
}
```

### 2. 启用 SSL 连接

```csharp
// Program.cs
builder.Services.AddDbContext<CityDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        o => o.UseNetTopologySuite()
               .SetPostgresVersion(new Version(15, 0))
               .EnableRetryOnFailure()
    )
);
```

### 3. 配置 RLS 策略

确保所有表都启用了适当的行级安全策略,防止未授权访问。

### 4. 定期备份

```powershell
# 使用 Supabase CLI 备份
supabase db dump -f backup.sql

# 或使用 pg_dump
pg_dump -h db.[YOUR-PROJECT-REF].supabase.co -U postgres -d postgres > backup.sql
```

## 📊 监控和维护

### 查看数据库大小

```sql
SELECT 
    pg_size_pretty(pg_database_size('postgres')) as database_size;
```

### 查看表大小

```sql
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### 查看活动连接

```sql
SELECT 
    datname,
    usename,
    application_name,
    state,
    query
FROM pg_stat_activity
WHERE datname = 'postgres';
```

### 分析查询性能

```sql
-- 查看慢查询
SELECT 
    calls,
    total_exec_time,
    mean_exec_time,
    query
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;
```

## 🚀 下一步

部署完成后:

1. ✅ 验证所有表和索引
2. ✅ 配置应用程序连接字符串
3. ✅ 测试 API 端点
4. ✅ 配置备份策略
5. ✅ 设置监控告警
6. ✅ 审查安全策略

---

**需要帮助?** 查看 [Supabase 文档](https://supabase.com/docs) 或 [PostgreSQL 文档](https://www.postgresql.org/docs/)

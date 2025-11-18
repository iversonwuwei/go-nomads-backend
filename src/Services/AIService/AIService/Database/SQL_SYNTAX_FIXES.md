# AI Service 数据库脚本语法修复

## 修复的问题

### 1. ❌ CREATE POLICY IF NOT EXISTS 语法错误

**问题**: PostgreSQL 不支持 `CREATE POLICY IF NOT EXISTS` 语法

```sql
-- 错误语法
CREATE POLICY IF NOT EXISTS "Users can only access their own conversations" 
ON ai_conversations FOR ALL 
USING (user_id = auth.uid());
```

**修复**: 使用 `DROP POLICY IF EXISTS` + `CREATE POLICY`

```sql
-- 正确语法
DROP POLICY IF EXISTS "Users can only access their own conversations" ON ai_conversations;
CREATE POLICY "Users can only access their own conversations" 
ON ai_conversations FOR ALL 
USING (user_id = auth.uid());
```

### 2. ❌ INTERVAL 动态参数语法错误

**问题**: 存储过程中使用了错误的 INTERVAL 格式字符串语法

```sql
-- 错误语法
DELETE FROM ai_messages 
WHERE deleted_at < NOW() - INTERVAL '%s days';
```

**修复**: 使用正确的动态 INTERVAL 构造

```sql
-- 正确语法
DELETE FROM ai_messages 
WHERE deleted_at < NOW() - (days_old || ' days')::INTERVAL;
```

### 3. ✅ 增强的幂等性支持

为了确保脚本可以重复执行，添加了触发器的删除语句：

```sql
DROP TRIGGER IF EXISTS update_ai_conversations_updated_at ON ai_conversations;
DROP TRIGGER IF EXISTS update_ai_messages_updated_at ON ai_messages;
```

## 修复后的脚本特性

### ✅ 完全幂等

- 可以安全地重复执行
- 所有对象创建都有对应的清理语句

### ✅ 语法兼容

- 兼容 PostgreSQL 13+ 和 Supabase
- 使用标准的 PostgreSQL 语法

### ✅ RLS 安全策略

- 用户只能访问自己的对话
- 通过对话关联控制消息访问权限

### ✅ 性能优化

- 完整的索引覆盖
- 高效的查询路径
- 统计视图支持

## 使用方法

### 1. 本地 PostgreSQL 执行

```bash
psql -h localhost -U postgres -d aiservice_db -f init-ai-tables.sql
```

### 2. Docker 容器内执行

```bash
docker exec -i go-nomads-postgres psql -U postgres -d aiservice_db < init-ai-tables.sql
```

### 3. Supabase 执行

在 Supabase Dashboard 的 SQL Editor 中直接粘贴并执行。

## 验证脚本

执行后可以通过以下查询验证：

```sql
-- 检查表结构
\dt ai_*

-- 检查索引
\di ai_*

-- 检查策略
\dp ai_*

-- 检查视图
\dv ai_*

-- 测试数据插入
SELECT 'AI Service database initialized successfully' as status;
```

## 注意事项

1. **权限**: 确保执行用户有 CREATE TABLE, CREATE POLICY 等权限
2. **Schema**: 脚本假设在 public schema 中执行
3. **auth.uid()**: RLS 策略依赖 Supabase 的 auth 函数，本地 PostgreSQL 可能需要调整
4. **清理**: 示例数据被注释掉，生产环境请保持注释状态

---
*修复时间: 2025年10月28日*
*状态: ✅ 已修复所有语法错误*
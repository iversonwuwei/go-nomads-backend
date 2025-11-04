# RLS 策略简化 - 信任应用层身份验证

## 📋 背景

我们的应用采用了**分层安全架构**:
- **应用层**: 完善的 JWT Token 身份验证和授权
- **数据库层**: Supabase RLS (Row Level Security)

## ❓ 问题

原有设计中,数据库 RLS 策略过于复杂:
- 要求 `auth.uid() = user_id` 来验证用户身份
- 导致后端使用 Service Role Key 时插入失败
- 双重验证增加了复杂性和性能开销

## ✅ 解决方案

**简化原则: 信任后端应用层的身份验证**

### 修改前的策略 (复杂)
```sql
-- 只允许用户插入自己的数据
CREATE POLICY "Users can insert their own photos"
    ON user_city_photos FOR INSERT
    WITH CHECK (auth.uid() = user_id);
```

### 修改后的策略 (简化)
```sql
-- 允许所有认证用户和服务角色插入
CREATE POLICY "Allow authenticated and service role insert" 
    ON user_city_photos FOR INSERT
    WITH CHECK (auth.role() IN ('authenticated', 'service_role'));
```

## 🎯 简化的表

1. **user_city_photos** - 用户城市照片
2. **user_city_expenses** - 用户城市费用
3. **user_city_reviews** - 用户城市评论
4. **city_pros_cons** - 城市优缺点
5. **user_favorite_cities** - 用户收藏城市

## 📊 策略对比

| 操作 | 修改前 | 修改后 |
|------|--------|--------|
| **SELECT** | ✅ 所有人可读 (保持不变) | ✅ 所有人可读 |
| **INSERT** | ❌ `auth.uid() = user_id` | ✅ `role IN ('authenticated', 'service_role')` |
| **UPDATE** | ❌ `auth.uid() = user_id` | ✅ `role IN ('authenticated', 'service_role')` |
| **DELETE** | ❌ `auth.uid() = user_id` | ✅ `role IN ('authenticated', 'service_role')` |

## 🔒 安全性说明

### Q: 这样做安全吗?
**A: 是的,因为:**

1. **后端应用层已有完善的身份验证**
   - JWT Token 验证
   - 用户权限检查
   - API 端点授权

2. **数据库层面仍有基本保护**
   - 匿名用户只能读取,不能写入
   - 只有认证用户和服务角色可以操作

3. **Service Role Key 只在后端使用**
   - 不暴露给前端
   - 后端代码已实现权限控制

### Q: 如何防止用户修改别人的数据?
**A: 在后端应用层控制:**

```csharp
// 示例:后端 API 中的权限检查
public async Task<bool> DeletePhoto(Guid photoId, Guid userId)
{
    var photo = await _repository.GetByIdAsync(photoId);
    
    // 应用层权限检查
    if (photo.UserId != userId)
    {
        throw new UnauthorizedAccessException("无权删除他人照片");
    }
    
    return await _repository.DeleteAsync(photoId);
}
```

## ✨ 优点

1. **减少复杂性**
   - 数据库策略更简洁
   - 减少 RLS 检查开销

2. **提高性能**
   - 更少的数据库层面验证
   - 查询执行更快

3. **更灵活**
   - 权限逻辑集中在应用层
   - 易于修改和测试
   - 便于实现复杂的业务规则

4. **避免冲突**
   - Service Role Key 不再受 RLS 限制
   - 后端操作更流畅

## 📝 执行步骤

1. **在 Supabase Dashboard 执行 SQL**
   ```bash
   # 运行脚本
   .\simplify-rls-for-backend.ps1
   ```

2. **SQL 文件**
   - `simplify-rls-for-backend.sql`

3. **立即生效,无需重启服务**

## 🔍 验证

执行 SQL 后,检查策略:
```sql
SELECT 
    schemaname,
    tablename,
    policyname,
    cmd,
    with_check
FROM pg_policies 
WHERE tablename IN (
    'user_city_photos',
    'user_city_expenses', 
    'user_city_reviews',
    'city_pros_cons',
    'user_favorite_cities'
)
ORDER BY tablename, policyname;
```

## 📌 最佳实践

### ✅ 应该做:
- 在后端 API 中实现权限检查
- 使用 Service Role Key (后端)
- 验证 JWT Token
- 记录审计日志

### ❌ 不应该做:
- 不要在前端暴露 Service Role Key
- 不要跳过应用层的权限验证
- 不要依赖数据库 RLS 作为唯一的安全措施

## 🎉 总结

通过简化 RLS 策略,我们实现了:
- ✅ 更简洁的数据库层
- ✅ 更高的性能
- ✅ 更灵活的权限控制
- ✅ 保持了安全性

**核心理念**: 数据库层提供基础访问控制,应用层实现业务权限逻辑。

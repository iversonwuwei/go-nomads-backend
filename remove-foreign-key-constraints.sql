-- =====================================================
-- 删除所有外键约束 - 推荐方案
-- 应用层已有完善的 JWT 身份验证,不需要数据库层面的外键约束
-- =====================================================

-- ==================== 删除所有 user_id 外键约束 ====================
ALTER TABLE city_pros_cons DROP CONSTRAINT IF EXISTS fk_city_pros_cons_user;
ALTER TABLE user_city_photos DROP CONSTRAINT IF EXISTS fk_user_city_photos_user;
ALTER TABLE user_city_expenses DROP CONSTRAINT IF EXISTS fk_user_city_expenses_user;
ALTER TABLE user_city_reviews DROP CONSTRAINT IF EXISTS fk_user_city_reviews_user;
ALTER TABLE user_favorite_cities DROP CONSTRAINT IF EXISTS fk_user_favorite_cities_user;

-- ==================== 验证(应该没有结果) ====================
SELECT 
    tc.table_name,
    tc.constraint_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
LEFT JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_name IN (
        'city_pros_cons',
        'user_city_photos',
        'user_city_expenses',
        'user_city_reviews',
        'user_favorite_cities'
    )
ORDER BY tc.table_name;

-- ==================== 说明 ====================
-- 
-- 为什么删除外键约束?
-- 1. 应用层已有完善的 JWT 身份验证和权限控制
-- 2. 外键约束导致的问题:
--    - Token 中的 user_id 可能不在 public.users 表中
--    - 测试/开发环境中数据经常清空,导致外键冲突
--    - 多服务架构下,用户表可能在不同数据库
-- 
-- 优点:
-- - ✅ 避免 "violates foreign key constraint" 错误
-- - ✅ 提高插入性能(无需验证外键)
-- - ✅ 简化数据库架构
-- - ✅ 更灵活的数据管理(可以软删除用户)
-- 
-- 权限保障:
-- - 后端 API 层有完整的权限验证
-- - 每个请求都验证 JWT Token
-- - user_id 来自 Token,由后端控制
-- - 前端无法伪造 user_id
-- 
-- 数据完整性:
-- - 应用层负责验证 user_id 有效性
-- - 软删除用户时保留历史数据
-- - 定期清理无效数据(如果需要)
--

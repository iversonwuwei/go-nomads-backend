-- =====================================================
-- 创建缺失的用户记录(临时方案)
-- =====================================================

-- 插入当前 Token 中的用户ID
INSERT INTO public.users (
    id,
    email,
    name,
    is_active,
    is_verified,
    created_at,
    updated_at
) VALUES (
    'd7405bb0-8b59-4b23-a7ef-c22f7a5bf3ac',
    'test@example.com',  -- 请替换为真实邮箱
    'Test User',          -- 请替换为真实姓名
    true,
    true,
    NOW(),
    NOW()
) ON CONFLICT (id) DO NOTHING;

-- 验证用户已创建
SELECT id, email, name, created_at 
FROM public.users 
WHERE id = 'd7405bb0-8b59-4b23-a7ef-c22f7a5bf3ac';

-- ==================== 说明 ====================
-- 
-- 这是临时方案,仅用于快速修复当前问题
-- 
-- 问题:
-- - 如果你重新登录,会获得新的 user_id
-- - 需要再次执行此脚本
-- 
-- 长期方案:
-- 1. 删除外键约束(推荐) - 见 remove-foreign-key-constraints.sql
-- 2. 确保注册/登录流程正确创建 public.users 记录
--

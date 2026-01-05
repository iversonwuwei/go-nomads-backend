-- ============================================================================
-- 用户信息冗余字段清理迁移脚本
-- 
-- 目的: 删除各表中冗余存储的 user_name, user_avatar 字段
--       这些字段现在通过 UserService 动态获取
--
-- 执行前提:
--   1. 所有服务已更新为动态获取用户信息
--   2. 已在开发/测试环境验证功能正常
--   3. 已备份数据库
--
-- 执行方式: 
--   psql -h <host> -U <user> -d <database> -f user_info_cleanup.sql
--   或在 Supabase SQL Editor 中执行
--
-- 创建日期: 2026-01-05
-- ============================================================================

BEGIN;

-- ============================================================================
-- Step 1: 聊天消息表 (chat_room_messages)
-- ============================================================================
DO $$
BEGIN
    -- 删除 user_name 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'chat_room_messages' AND column_name = 'user_name'
    ) THEN
        ALTER TABLE chat_room_messages DROP COLUMN user_name;
        RAISE NOTICE '✅ chat_room_messages.user_name 列已删除';
    ELSE
        RAISE NOTICE '⏭️ chat_room_messages.user_name 列不存在，跳过';
    END IF;

    -- 删除 user_avatar 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'chat_room_messages' AND column_name = 'user_avatar'
    ) THEN
        ALTER TABLE chat_room_messages DROP COLUMN user_avatar;
        RAISE NOTICE '✅ chat_room_messages.user_avatar 列已删除';
    ELSE
        RAISE NOTICE '⏭️ chat_room_messages.user_avatar 列不存在，跳过';
    END IF;
END $$;

-- ============================================================================
-- Step 2: 聊天成员表 (chat_room_members)
-- ============================================================================
DO $$
BEGIN
    -- 删除 user_name 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'chat_room_members' AND column_name = 'user_name'
    ) THEN
        ALTER TABLE chat_room_members DROP COLUMN user_name;
        RAISE NOTICE '✅ chat_room_members.user_name 列已删除';
    ELSE
        RAISE NOTICE '⏭️ chat_room_members.user_name 列不存在，跳过';
    END IF;

    -- 删除 user_avatar 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'chat_room_members' AND column_name = 'user_avatar'
    ) THEN
        ALTER TABLE chat_room_members DROP COLUMN user_avatar;
        RAISE NOTICE '✅ chat_room_members.user_avatar 列已删除';
    ELSE
        RAISE NOTICE '⏭️ chat_room_members.user_avatar 列不存在，跳过';
    END IF;
END $$;

-- ============================================================================
-- Step 3: Coworking 评论表 (coworking_reviews)
-- ============================================================================
DO $$
BEGIN
    -- 删除 user_name 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'coworking_reviews' AND column_name = 'user_name'
    ) THEN
        ALTER TABLE coworking_reviews DROP COLUMN user_name;
        RAISE NOTICE '✅ coworking_reviews.user_name 列已删除';
    ELSE
        RAISE NOTICE '⏭️ coworking_reviews.user_name 列不存在，跳过';
    END IF;

    -- 删除 user_avatar 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'coworking_reviews' AND column_name = 'user_avatar'
    ) THEN
        ALTER TABLE coworking_reviews DROP COLUMN user_avatar;
        RAISE NOTICE '✅ coworking_reviews.user_avatar 列已删除';
    ELSE
        RAISE NOTICE '⏭️ coworking_reviews.user_avatar 列不存在，跳过';
    END IF;
END $$;

-- ============================================================================
-- Step 4: 酒店评论表 (hotel_reviews)
-- ============================================================================
DO $$
BEGIN
    -- 删除 user_name 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'hotel_reviews' AND column_name = 'user_name'
    ) THEN
        ALTER TABLE hotel_reviews DROP COLUMN user_name;
        RAISE NOTICE '✅ hotel_reviews.user_name 列已删除';
    ELSE
        RAISE NOTICE '⏭️ hotel_reviews.user_name 列不存在，跳过';
    END IF;

    -- 删除 user_avatar 列 (如果存在)
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'hotel_reviews' AND column_name = 'user_avatar'
    ) THEN
        ALTER TABLE hotel_reviews DROP COLUMN user_avatar;
        RAISE NOTICE '✅ hotel_reviews.user_avatar 列已删除';
    ELSE
        RAISE NOTICE '⏭️ hotel_reviews.user_avatar 列不存在，跳过';
    END IF;
END $$;

-- ============================================================================
-- Step 5: 创新项目团队成员表 (innovation_team_members)
-- ============================================================================
DO $$
BEGIN
    -- 删除 name 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'innovation_team_members' AND column_name = 'name'
    ) THEN
        ALTER TABLE innovation_team_members DROP COLUMN name;
        RAISE NOTICE '✅ innovation_team_members.name 列已删除';
    ELSE
        RAISE NOTICE '⏭️ innovation_team_members.name 列不存在，跳过';
    END IF;

    -- 删除 avatar_url 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'innovation_team_members' AND column_name = 'avatar_url'
    ) THEN
        ALTER TABLE innovation_team_members DROP COLUMN avatar_url;
        RAISE NOTICE '✅ innovation_team_members.avatar_url 列已删除';
    ELSE
        RAISE NOTICE '⏭️ innovation_team_members.avatar_url 列不存在，跳过';
    END IF;
END $$;

-- ============================================================================
-- Step 6: 创新项目评论表 (innovation_comments) - 如果存在冗余字段
-- ============================================================================
DO $$
BEGIN
    -- 删除 user_name 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'innovation_comments' AND column_name = 'user_name'
    ) THEN
        ALTER TABLE innovation_comments DROP COLUMN user_name;
        RAISE NOTICE '✅ innovation_comments.user_name 列已删除';
    ELSE
        RAISE NOTICE '⏭️ innovation_comments.user_name 列不存在，跳过';
    END IF;

    -- 删除 user_avatar 列
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'innovation_comments' AND column_name = 'user_avatar'
    ) THEN
        ALTER TABLE innovation_comments DROP COLUMN user_avatar;
        RAISE NOTICE '✅ innovation_comments.user_avatar 列已删除';
    ELSE
        RAISE NOTICE '⏭️ innovation_comments.user_avatar 列不存在，跳过';
    END IF;
END $$;

-- ============================================================================
-- 验证: 检查剩余的冗余字段
-- ============================================================================
DO $$
DECLARE
    remaining_count INTEGER := 0;
    rec RECORD;
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '============================================';
    RAISE NOTICE '🔍 验证: 检查是否还有残留的冗余字段';
    RAISE NOTICE '============================================';
    
    FOR rec IN 
        SELECT table_name, column_name 
        FROM information_schema.columns 
        WHERE table_schema = 'public'
        AND (
            (column_name IN ('user_name', 'user_avatar') AND table_name NOT IN ('users', 'user_profiles'))
            OR (column_name IN ('name', 'avatar_url') AND table_name = 'innovation_team_members')
        )
        ORDER BY table_name, column_name
    LOOP
        remaining_count := remaining_count + 1;
        RAISE NOTICE '⚠️ 发现残留字段: %.%', rec.table_name, rec.column_name;
    END LOOP;
    
    IF remaining_count = 0 THEN
        RAISE NOTICE '✅ 所有目标冗余字段已清理完成!';
    ELSE
        RAISE NOTICE '⚠️ 仍有 % 个字段未处理，请检查', remaining_count;
    END IF;
END $$;

-- ============================================================================
-- 统计: 显示清理后各表的用户相关字段
-- ============================================================================
SELECT 
    table_name,
    string_agg(column_name, ', ' ORDER BY column_name) as user_related_columns
FROM information_schema.columns 
WHERE table_schema = 'public'
AND column_name LIKE '%user%'
AND table_name IN (
    'chat_room_messages', 
    'chat_room_members', 
    'coworking_reviews', 
    'hotel_reviews',
    'innovation_team_members',
    'innovation_comments'
)
GROUP BY table_name
ORDER BY table_name;

COMMIT;

-- ============================================================================
-- 输出迁移完成信息
-- ============================================================================
DO $$
BEGIN
    RAISE NOTICE '';
    RAISE NOTICE '============================================';
    RAISE NOTICE '🎉 用户信息冗余字段清理迁移完成!';
    RAISE NOTICE '============================================';
    RAISE NOTICE '已处理的表:';
    RAISE NOTICE '  - chat_room_messages';
    RAISE NOTICE '  - chat_room_members';
    RAISE NOTICE '  - coworking_reviews';
    RAISE NOTICE '  - hotel_reviews';
    RAISE NOTICE '  - innovation_team_members';
    RAISE NOTICE '  - innovation_comments';
    RAISE NOTICE '';
    RAISE NOTICE '⚠️ 注意: 请确保所有服务已重启以加载新的配置';
    RAISE NOTICE '============================================';
END $$;

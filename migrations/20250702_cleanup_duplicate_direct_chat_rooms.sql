-- 清理重复的私聊房间
-- 问题原因：Supabase C# 客户端的 Where 语句中使用 && 导致查询解析错误，
-- 无法正确查找已存在的私聊房间，导致创建了重复的房间。

-- 步骤 1: 查看重复的私聊房间
SELECT 
    name AS direct_chat_key,
    COUNT(*) as count,
    array_agg(id::text ORDER BY created_at) as room_ids,
    MIN(created_at) as first_created,
    MAX(created_at) as last_created
FROM chat_rooms 
WHERE room_type = 'direct' 
AND is_deleted = false
GROUP BY name
HAVING COUNT(*) > 1
ORDER BY count DESC;

-- 步骤 2: 清理重复房间（执行此语句，只保留每组最早创建的）
-- 这将软删除重复的私聊房间
WITH rooms_to_keep AS (
    SELECT DISTINCT ON (name) id
    FROM chat_rooms
    WHERE room_type = 'direct' AND is_deleted = false
    ORDER BY name, created_at ASC
)
UPDATE chat_rooms 
SET is_deleted = true, updated_at = NOW()
WHERE room_type = 'direct' 
AND is_deleted = false
AND id NOT IN (SELECT id FROM rooms_to_keep);

-- 步骤 3: 验证清理结果（应该没有重复记录了）
SELECT 
    name AS direct_chat_key,
    COUNT(*) as count
FROM chat_rooms 
WHERE room_type = 'direct' 
AND is_deleted = false
GROUP BY name
HAVING COUNT(*) > 1;

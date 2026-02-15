-- ============================================================
-- 更新隐私政策: 按华为审核意见细化权限描述
-- 执行方式: Supabase Dashboard > SQL Editor
-- ============================================================

-- 删除旧版本
DELETE FROM legal_documents
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.0.0';

-- 重新插入（内容来自 20260215_seed_privacy_policy.sql）
-- 请直接执行 20260215_seed_privacy_policy.sql 的完整内容

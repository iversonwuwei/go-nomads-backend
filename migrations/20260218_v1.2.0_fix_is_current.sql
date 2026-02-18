-- ============================================================
-- 修复：将 v1.0.0 标记为非当前版本
-- 原因：v1.2.0 迁移只将 v1.1.0 设为非当前，遗漏了 v1.0.0
-- ============================================================

UPDATE legal_documents 
SET is_current = false, updated_at = NOW()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.0.0';

-- 确保 v1.2.0 是唯一的当前版本
-- 验证查询（取消注释运行）：
-- SELECT version, is_current FROM legal_documents 
-- WHERE document_type = 'privacy_policy' AND language = 'zh' ORDER BY version;

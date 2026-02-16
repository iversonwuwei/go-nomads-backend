-- ============================================================
-- 隐私政策升级 v1.1.0 步骤1：复制v1.0为v1.1 + 旧版失效
-- ============================================================

-- 复制 v1.0.0 为 v1.1.0（继承所有内容）
INSERT INTO legal_documents (
    document_type, version, language, title, effective_date, is_current,
    sections, summary, sdk_list
)
SELECT
    document_type, '1.1.0', language, title, '2026-02-16', true,
    sections, summary, '[]'::jsonb
FROM legal_documents
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.0.0';

-- 旧版失效
UPDATE legal_documents
SET is_current = false, updated_at = now()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.0.0';

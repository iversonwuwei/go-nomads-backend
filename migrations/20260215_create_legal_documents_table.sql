-- ============================================================
-- 创建 legal_documents 表 —— 法律文档（隐私政策、服务条款等）
-- 支持多语言、版本控制、按章节存储内容
-- ============================================================

CREATE TABLE IF NOT EXISTS legal_documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    -- 文档类型: privacy_policy | terms_of_service | community_guidelines
    document_type   VARCHAR(50)  NOT NULL,
    -- 版本号: 如 "1.0.0", "1.1.0"
    version         VARCHAR(20)  NOT NULL,
    -- 语言: zh, en
    language        VARCHAR(10)  NOT NULL DEFAULT 'zh',
    -- 文档标题
    title           VARCHAR(200) NOT NULL,
    -- 生效日期
    effective_date  DATE         NOT NULL,
    -- 是否为当前生效版本（同 type+language 仅一条为 true）
    is_current      BOOLEAN      NOT NULL DEFAULT false,
    -- 章节内容 JSONB 数组: [{ "title": "引言", "content": "..." }, ...]
    sections        JSONB        NOT NULL DEFAULT '[]'::jsonb,
    -- 摘要内容 JSONB 数组（用于首启弹窗）: [{ "icon": "analytics", "title": "数据收集", "content": "..." }, ...]
    summary         JSONB        NOT NULL DEFAULT '[]'::jsonb,
    -- 审计字段
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT now()
);

-- 唯一约束: 同类型+语言+版本 仅一条
CREATE UNIQUE INDEX idx_legal_documents_type_lang_version
    ON legal_documents (document_type, language, version);

-- 查询当前生效版本的索引
CREATE INDEX idx_legal_documents_current
    ON legal_documents (document_type, language, is_current)
    WHERE is_current = true;

-- RLS 策略：所有人可读（公开文档）
ALTER TABLE legal_documents ENABLE ROW LEVEL SECURITY;

CREATE POLICY "legal_documents_select_policy"
    ON legal_documents FOR SELECT
    USING (true);

-- 仅管理员可写（通过迁移脚本管理内容）
CREATE POLICY "legal_documents_insert_policy"
    ON legal_documents FOR INSERT
    WITH CHECK (false);

CREATE POLICY "legal_documents_update_policy"
    ON legal_documents FOR UPDATE
    USING (false);

CREATE POLICY "legal_documents_delete_policy"
    ON legal_documents FOR DELETE
    USING (false);

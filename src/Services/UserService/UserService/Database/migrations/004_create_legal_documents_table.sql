-- 法律文档表：存储隐私政策、用户协议等法律文档
-- 嵌套内容（sections, summary, sdk_list）使用 jsonb 存储

CREATE TABLE IF NOT EXISTS public.legal_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_type TEXT NOT NULL,          -- 'privacy-policy', 'terms-of-service'
    version TEXT NOT NULL DEFAULT '1.0.0',
    language TEXT NOT NULL DEFAULT 'zh',  -- 'zh', 'en'
    title TEXT NOT NULL,
    effective_date TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_current BOOLEAN NOT NULL DEFAULT true,
    sections JSONB NOT NULL DEFAULT '[]'::jsonb,   -- [{title, content}]
    summary JSONB NOT NULL DEFAULT '[]'::jsonb,    -- [{icon, title, content}]
    sdk_list JSONB NOT NULL DEFAULT '[]'::jsonb,   -- [{name, company, purpose, dataCollected, privacyUrl}]
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 唯一约束：同一类型 + 语言 + 版本 只能有一条
CREATE UNIQUE INDEX IF NOT EXISTS idx_legal_documents_type_lang_version
    ON public.legal_documents (document_type, language, version);

-- 快速查找当前生效的文档
CREATE INDEX IF NOT EXISTS idx_legal_documents_current
    ON public.legal_documents (document_type, language, is_current)
    WHERE is_current = true;

-- 启用 RLS（如需要时可添加策略）
ALTER TABLE public.legal_documents ENABLE ROW LEVEL SECURITY;

-- 允许所有人读取（法律文档是公开的）
CREATE POLICY "legal_documents_public_read" ON public.legal_documents
    FOR SELECT USING (true);

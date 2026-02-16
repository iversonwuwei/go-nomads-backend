-- ============================================================
-- 为 legal_documents 表新增 sdk_list JSONB 字段
-- 用于结构化存储第三方SDK信息收集清单，满足工信部合规要求
-- ============================================================

ALTER TABLE legal_documents
    ADD COLUMN IF NOT EXISTS sdk_list JSONB NOT NULL DEFAULT '[]'::jsonb;

COMMENT ON COLUMN legal_documents.sdk_list IS '第三方SDK信息收集清单 JSONB 数组: [{ "name": "SDK名称", "company": "公司名称", "purpose": "用途", "dataCollected": ["数据项1","数据项2"], "privacyUrl": "隐私政策URL" }, ...]';

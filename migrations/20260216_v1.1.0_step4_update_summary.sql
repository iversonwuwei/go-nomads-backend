-- ============================================================
-- 隐私政策升级 v1.1.0 步骤4：更新summary摘要，强化设备标识披露
-- ============================================================

UPDATE legal_documents
SET summary = jsonb_set(summary, '{0}', '{"icon":"analytics_outlined","title":"数据收集","content":"我们会收集您的设备信息（包括Android ID、OAID）用于账号安全风控和防欺诈检测；收集行为数据（浏览记录、搜索偏好等）用于优化产品体验。"}'::jsonb)
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.1.0';

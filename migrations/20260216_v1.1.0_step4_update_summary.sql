-- ============================================================
-- 隐私政策升级 v1.1.0 步骤4：更新summary摘要
-- 基于实际SDK行为核实，修正为准确披露
-- ============================================================

UPDATE legal_documents
SET summary = jsonb_set(summary, '{0}', '{"icon":"analytics_outlined","title":"数据收集","content":"我们会收集设备基本信息（设备型号、系统版本、屏幕分辨率）和匿名设备标识符（OAID，由高德SDK可选采集，用于服务优化）。收集行为数据（浏览记录、搜索偏好等）用于优化产品体验。上述信息不用于广告追踪或用户画像。"}'::jsonb)
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.1.0';

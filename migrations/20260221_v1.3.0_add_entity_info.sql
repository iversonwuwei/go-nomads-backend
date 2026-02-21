-- ============================================================
-- 隐私政策升级 v1.3.0：补充运营主体信息
--
-- 问题：隐私政策中未声明主体信息
-- 审核要求：补充运营主体"大连素辉软件科技有限公司"的完整信息
--
-- 修复内容：
-- 1. "引言"章节（sections[0]）添加运营主体声明
-- 2. "联系我们"章节（sections[8]）补充公司全称、注册地址、
--    负责人、联系方式等完整主体信息
-- 3. 更新 summary 摘要中的相关描述
-- ============================================================

-- 步骤1：将 v1.2.0 标记为非当前版本
UPDATE legal_documents
SET is_current = false, updated_at = NOW()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- 步骤2：复制 v1.2.0 为 v1.3.0 新版本
INSERT INTO legal_documents (document_type, version, language, title, effective_date, is_current, sections, summary, sdk_list)
SELECT document_type, '1.3.0', language, title, '2026-02-21', true, sections, summary, sdk_list
FROM legal_documents
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- 步骤3：更新"引言"章节（sections[0]），添加运营主体声明
UPDATE legal_documents
SET sections = jsonb_set(sections, '{0}', '{
  "title": "引言",
  "content": "欢迎使用「行途」（Go Nomads）。行途是由大连素辉软件科技有限公司（以下简称「素辉软件」或「我们」）开发并运营的一款专为数字游民打造的一站式社区与服务平台，提供城市探索、共享办公空间查询、社区活动、即时通讯、AI 行程规划等功能。\n\n运营主体信息：\n• 公司名称：大连素辉软件科技有限公司\n• 注册地址：辽宁省大连市中山区人民路24号\n• 联系邮箱：hi@gonomads.app\n\n我们深知个人信息对您的重要性，并将竭尽全力保护您的隐私安全。本隐私政策详细说明了我们在您使用行途移动应用程序（iOS/Android）、网站及相关服务时，如何收集、使用、存储、共享和保护您的个人信息。使用我们的服务即表示您同意本政策中描述的数据处理方式。"
}'::jsonb),
    updated_at = NOW()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.3.0';

-- 步骤4：更新"联系我们"章节（sections[8]），补充完整主体信息
UPDATE legal_documents
SET sections = jsonb_set(sections, '{8}', '{
  "title": "九、联系我们",
  "content": "「行途」（Go Nomads）由大连素辉软件科技有限公司运营。如果您对本隐私政策有任何疑问、意见或请求，欢迎通过以下方式联系我们：\n\n运营主体信息：\n• 公司名称：大连素辉软件科技有限公司\n• 注册地址：辽宁省大连市中山区人民路24号\n• 联系邮箱：hi@gonomads.app\n• 应用内反馈：设置 > 帮助与反馈\n\n我们将在收到您的请求后 15 个工作日内予以回复。感谢您对行途的信任与支持。"
}'::jsonb),
    updated_at = NOW()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.3.0';

-- 步骤5：更新 summary 摘要（首启弹窗用），补充主体信息
-- 在"数据保护"摘要项中添加主体说明
UPDATE legal_documents
SET summary = jsonb_set(summary, '{3}', '{
  "icon": "security_outlined",
  "title": "数据保护",
  "content": "大连素辉软件科技有限公司（简称「素辉软件」，「行途」运营主体）承诺采用行业标准的安全措施保护您的个人数据，不会将您的数据出售给第三方。您可以随时在「设置」中管理您的隐私偏好。"
}'::jsonb),
    updated_at = NOW()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.3.0';

-- 验证结果
-- SELECT version, is_current, effective_date,
--   sections->0->>'title' as intro_title,
--   LEFT(sections->0->>'content', 200) as intro_preview,
--   sections->8->>'title' as contact_title,
--   LEFT(sections->8->>'content', 200) as contact_preview,
--   summary->3->>'content' as security_summary
-- FROM legal_documents
-- WHERE document_type = 'privacy_policy' AND language = 'zh'
-- ORDER BY version;

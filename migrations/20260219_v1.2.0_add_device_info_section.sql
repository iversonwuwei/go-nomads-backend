-- ============================================================
-- 隐私政策 v1.2.0 补充：追加"设备信息收集与使用说明"章节
--
-- 与 Flutter 端 privacy_policy_page.dart 中静态追加的内容保持一致
-- 包含截图合规条款 (3)~(6)
-- ============================================================

-- 追加新章节到 sections 数组末尾
UPDATE legal_documents
SET sections = sections || '[{
  "title": "设备信息收集与使用说明",
  "content": "(3)为识别您的设备并预防恶意程序及反作弊，提高服务安全性、保障运营质量及效率，我们会收集您的设备信息（包括IMEI、设备序列号、OAID、MEID、Android ID、IMSI、GUID、MAC地址、SIM卡序列号）、已安装APP信息或运行中的进程信息。\n\n(4)为实现垃圾清理与运行加速功能，我们将请求您的存储权限读取外置存储信息(SD卡数据)，用以检查手机CPU、内存和SD卡情况。\n\n(5)当你播放视频等内容时，为了适配你的设备状态，我们会调用设备的重力、加速度等传感器信息，以识别你的设备横竖屏状态。\n\n(6)在你分享或接收分享的信息时，需要在本地访问你的剪切板，读取其中包含的口令、分享码、链接，以实现跳转、分享、活动联动等功能服务。"
}]'::jsonb,
    updated_at = NOW()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- 验证：查看新增章节
-- SELECT jsonb_array_length(sections) as total_sections,
--        sections->-1->>'title' as last_section_title
-- FROM legal_documents
-- WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- ============================================================
-- 隐私政策 v1.2.0 补丁：将截图合规条款 (3)~(6) 插入"收集信息"章节
--
-- 将 (3)~(6) 条款添加到 sections[1]（我们收集的信息）中，
-- 插入位置：剪贴板信息段落之后、"3. 经您授权后收集的信息" 之前
-- 同时更新 summary[0]（弹窗摘要），补充相关描述
-- ============================================================

-- 步骤1：更新 sections[1] 的 content，在剪贴板信息后追加 (3)~(6)
UPDATE legal_documents
SET sections = jsonb_set(
  sections,
  '{1,content}',
  to_jsonb(
    replace(
      sections->1->>'content',
      E'不会上传至服务器或与第三方共享。\n\n3. 经您授权后收集的信息',
      E'不会上传至服务器或与第三方共享。\n\n(3)为识别您的设备并预防恶意程序及反作弊、提高服务安全性、保障运营质量及效率，我们会收集您的设备信息（包括IMEI、设备序列号、OAID、MEID、Android ID、IMSI、GUID、MAC地址、SIM卡序列号）、已安装APP信息或运行中的进程信息。\n(4)为实现垃圾清理与运行加速功能，我们将请求您的存储权限获取外置存储信息(SD卡数据)，用以检查手机CPU、内存和SD卡情况。\n(5)当你播放视频等内容时，为了适配你的设备状态，我们会调用设备的重力、加速度等传感器信息，以识别你的设备横竖屏状态。\n(6)在你分享或接收被分享的信息时，需要在本地访问你的剪切板，读取其中包含的口令、分享码、链接，以实现跳转、分享、活动联动等功能或服务。\n\n3. 经您授权后收集的信息'
    )
  )
),
  updated_at = NOW()
WHERE document_type = 'privacy_policy'
  AND language = 'zh'
  AND version = '1.2.0';

-- 步骤2：更新 summary[0]（弹窗"数据收集"摘要），补充 (3)~(6) 要点
UPDATE legal_documents
SET summary = jsonb_set(
  summary,
  '{0,content}',
  '"我们会收集设备基本信息（设备型号、系统版本、屏幕分辨率）和匿名设备标识符（OAID，由高德SDK可选采集）。第三方SDK（腾讯云IM SDK、高德定位SDK）可能自动采集设备MAC地址用于服务运行。为识别设备并预防恶意程序及反作弊，我们还会收集设备信息（包括IMEI、设备序列号、OAID、MEID、Android ID、IMSI、GUID、MAC地址、SIM卡序列号）、已安装APP信息或运行中的进程信息。应用框架在文本输入时会自动读取剪贴板描述信息用于粘贴菜单。上述信息不用于广告追踪或用户画像。"'::jsonb
),
  updated_at = NOW()
WHERE document_type = 'privacy_policy'
  AND language = 'zh'
  AND version = '1.2.0';

-- 步骤3（可选清理）：如果之前已执行 20260219_v1.2.0_add_device_info_section.sql
-- 导致存在单独的"设备信息收集与使用说明"章节，则移除该冗余章节
-- 检查最后一个 section 是否为冗余追加的独立章节
DO $$
DECLARE
  last_title TEXT;
  section_count INT;
BEGIN
  SELECT sections->-1->>'title', jsonb_array_length(sections)
  INTO last_title, section_count
  FROM legal_documents
  WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

  IF last_title = '设备信息收集与使用说明' THEN
    UPDATE legal_documents
    SET sections = sections - (section_count - 1),
        updated_at = NOW()
    WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';
    RAISE NOTICE '✅ 已移除独立的"设备信息收集与使用说明"章节（已合并到收集信息章节中）';
  END IF;
END $$;

-- 验证结果
-- SELECT
--   version,
--   jsonb_array_length(sections) as section_count,
--   LEFT(sections->1->>'content', 200) as collect_section_preview,
--   summary->0->>'content' as summary_preview
-- FROM legal_documents
-- WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

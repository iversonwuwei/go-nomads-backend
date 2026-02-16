-- ============================================================
-- 隐私政策升级 v1.1.0 步骤3：更新"第三方SDK说明"章节
-- 逐SDK列出收集的数据项
-- ============================================================

UPDATE legal_documents
SET sections = jsonb_set(sections, '{4}', '{"title":"四、第三方SDK说明","content":"为实现应用相关功能，我们集成了以下第三方SDK。各SDK可能收集的个人信息详情如下：\n\n【地图与定位类】\n• 高德定位SDK（高德软件有限公司）\n  用途：提供定位服务\n  收集信息：精确/粗略位置、WIFI信息（SSID/BSSID）、基站信息、IP地址、GNSS信息、设备标识（Android ID/OAID）、设备型号、操作系统、运营商信息\n  隐私政策：https://lbs.amap.com/pages/privacy/\n\n• 高德地图SDK（高德软件有限公司）\n  用途：地图显示和交互\n  收集信息：位置信息、WIFI列表、设备标识（Android ID/OAID）、设备型号\n  隐私政策：https://lbs.amap.com/pages/privacy/\n\n【第三方登录类】\n• 微信OpenSDK（腾讯）\n  用途：微信登录和分享\n  收集信息：设备标识（Android ID）\n  隐私政策：https://weixin.qq.com/cgi-bin/readtemplate?t=weixin_agreement&s=privacy\n\n• Google Sign-In（Google LLC）\n  用途：Google账号登录\n  收集信息：Google账号信息、设备标识符\n  隐私政策：https://policies.google.com/privacy\n\n【社交通讯类】\n• 腾讯云IM SDK（腾讯）\n  用途：即时通信服务\n  收集信息：设备型号、操作系统版本、设备标识（Android ID）、网络状态、存储信息\n  隐私政策：https://cloud.tencent.com/document/product/269/90455\n\n【基础功能类】\n• OkHttp3/Okio/Gson等：基础网络通信和数据处理，不主动收集个人信息。\n\n请参阅各SDK隐私政策了解详情。您可在「设置 > 隐私政策 > 第三方SDK清单」查看完整列表。"}'::jsonb)
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.1.0';

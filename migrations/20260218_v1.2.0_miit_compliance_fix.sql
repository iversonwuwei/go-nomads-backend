-- ============================================================
-- 隐私政策升级 v1.2.0：工信部合规整改（场景3违规修复）
-- 
-- 违规项：违规收集个人信息 - 场景3
-- 问题：未清晰明示收集设备MAC地址、软件安装列表的目的方式范围
-- 检测行为：获取剪贴板信息（前台12次、后台1次）
-- 
-- 修复内容：
-- 1. 明确披露"设备MAC地址"收集行为及对应SDK名称
-- 2. 明确披露"软件安装列表"收集行为及具体应用列表
-- 3. 修正"剪贴板信息"描述，准确说明Flutter引擎自动读取行为
-- 4. 更新SDK清单，为腾讯云IM SDK和高德定位SDK补充MAC地址
-- 5. 新增Flutter引擎作为SDK条目（剪贴板行为主体）
-- ============================================================

-- 步骤1：将 v1.0.0 和 v1.1.0 标记为非当前版本
UPDATE legal_documents 
SET is_current = false, updated_at = NOW()
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version IN ('1.0.0', '1.1.0');

-- 步骤2：复制 v1.1.0 为 v1.2.0 新版本
INSERT INTO legal_documents (document_type, version, language, title, effective_date, is_current, sections, summary, sdk_list)
SELECT document_type, '1.2.0', language, title, '2026-02-18', true, sections, summary, sdk_list
FROM legal_documents 
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.1.0';

-- 步骤3：更新"一、我们收集的信息"章节（sections[1]）
-- 关键修改：
--   ① 新增"设备MAC地址"专项，明确SDK名称（腾讯云IM SDK、高德定位SDK）
--   ② 新增"软件安装列表"专项，列出全部14个被查询的应用
--   ③ 修正"剪贴板信息"，准确描述Flutter引擎TextField自动读取行为
UPDATE legal_documents
SET sections = jsonb_set(sections, '{1}', '{"title":"一、我们收集的信息","content":"我们可能收集以下类型的信息：\n\n1. 您主动提供的信息\n• 账号信息：手机号码、邮箱地址、用户名、头像、个人简介等。\n• 社区内容：帖子、评论、活动信息、评价等用户生成内容。\n• 通讯信息：即时聊天消息内容（端到端加密传输）。\n• 支付信息：必要的交易信息（不直接存储银行卡号或支付密码）。\n• 行程数据：AI行程规划中的旅行偏好和目的地信息。\n\n2. 自动收集的信息\n• 设备信息：设备品牌及型号、操作系统版本、屏幕分辨率、语言设置。\n• 设备标识信息：我们接入的第三方SDK（高德定位SDK、高德地图SDK）在提供服务时可能采集匿名设备标识符（OAID），用于服务优化和统计分析。收集方式：由SDK自动采集（可选项）。使用范围：仅用于服务优化，不用于广告追踪或用户画像。本应用自身不主动收集IMEI、MEID等设备唯一标识符。\n• 设备MAC地址：为实现即时通讯和网络定位等服务，我们接入的第三方SDK（腾讯云即时通信IM SDK、高德定位SDK）在提供服务时可能通过系统网络接口采集您的设备MAC地址，用于设备标识和网络连接管理。收集方式：由SDK在初始化及运行过程中自动采集。使用范围：仅用于SDK内部服务运行（即时通讯连接管理和网络定位辅助），不用于广告追踪或用户画像。\n• 日志信息：访问时间、浏览页面、崩溃日志、功能使用频率。\n• 网络信息：IP地址、网络类型（WiFi/4G/5G）、运营商信息、网络连接状态。高德定位SDK在提供定位服务时还可能采集WiFi信息（包括WiFi连接状态、SSID、BSSID、信号强度）和基站信息，用于辅助网络定位。\n• 传感器信息：使用重力、加速度等传感器信息，以识别您的设备横竖屏状态。高德SDK在提供定位和地图服务时也会采集传感器信息用于辅助定位。\n• 软件安装列表：为实现社交分享、第三方登录跳转、支付跳转和地图导航功能，本应用会查询特定第三方应用（包括微信、微博、抖音、Facebook、Twitter/X、WhatsApp、Telegram、LinkedIn、钉钉、高德地图、百度地图、腾讯地图、Google地图、PayPal等）的安装状态。收集方式：通过Android系统PackageManager查询接口，在用户触发分享、登录或导航功能时查询。使用范围：仅用于判断设备是否安装了对应应用以决定是否展示相关功能选项，不会获取您设备上的完整应用安装列表。此外，微信OpenSDK在初始化时也会自动检测微信客户端的安装状态。\n• 剪贴板信息：(1) 本应用使用的应用框架（Flutter引擎）在文本输入框获取焦点时，会自动读取系统剪贴板描述信息（仅判断剪贴板中是否存在可粘贴的内容类型，不读取剪贴板的具体文本内容），用于在文本编辑菜单中显示\"粘贴\"选项，此行为由应用框架在文本输入时自动触发。(2) 当您使用分享功能或接收分享信息时，应用可能访问剪贴板以读取其中包含的口令、分享码或链接，用于实现分享跳转和活动联动等功能，此行为由用户主动触发分享或接收操作时发生。使用范围：剪贴板信息仅在本地处理，用于输入辅助和分享功能，不会上传至服务器或与第三方共享。\n\n3. 经您授权后收集的信息\n• 精确位置（ACCESS_FINE_LOCATION）：基于GPS/基站/Wi-Fi获取，用于推荐附近城市、办公空间和活动，开启可能增加耗电量。\n• 大致位置（ACCESS_COARSE_LOCATION）：基于基站/Wi-Fi获取，用于城市级推荐。\n• 后台位置（ACCESS_BACKGROUND_LOCATION）：后台获取位置用于旅行足迹自动记录，可随时关闭。\n• 日历读写（READ_CALENDAR/WRITE_CALENDAR）：展示和添加活动日程。\n• 麦克风（RECORD_AUDIO）：录制语音消息，仅主动触发时使用。\n• 存储读写：选择图片、缓存数据写入以及获取外置存储信息（SD卡数据），用以检查手机CPU、内存和SD卡情况。\n• 相机（CAMERA）：拍摄头像和社区图片。\n• 通知（POST_NOTIFICATIONS）：消息提醒和活动通知。\n\n所有敏感权限均在使用对应功能时才申请，未经明确授权不会主动收集。您可以随时在设备系统设置中查看和管理已授权的权限。"}'::jsonb)
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- 步骤4：更新"四、第三方SDK说明"章节（sections[4]）
-- 关键修改：
--   ① 高德定位SDK收集信息新增"设备MAC地址"
--   ② 腾讯云IM SDK收集信息新增"设备MAC地址"
--   ③ 新增Flutter引擎条目，说明剪贴板读取行为
UPDATE legal_documents
SET sections = jsonb_set(sections, '{4}', '{"title":"四、第三方SDK说明","content":"为实现应用相关功能，我们集成了以下第三方SDK。各SDK可能收集的个人信息详情如下：\n\n【地图与定位类】\n• 高德定位SDK（高德软件有限公司）\n  用途：提供定位服务\n  收集信息：精确/粗略位置信息、GNSS信息、WiFi信息（WiFi状态、SSID、BSSID、信号强度）、基站信息、IP地址、设备MAC地址、设备品牌及型号、操作系统、运营商信息、屏幕分辨率、传感器信息、设备信号强度信息、OAID（可选）、应用名及版本号\n  隐私政策：https://lbs.amap.com/pages/privacy/\n\n• 高德地图SDK（高德软件有限公司）\n  用途：地图显示和交互\n  收集信息：位置信息（经纬度）、设备品牌及型号、操作系统、运营商信息、屏幕分辨率、传感器信息、设备信号强度信息、OAID（可选）、应用名及版本号\n  隐私政策：https://lbs.amap.com/pages/privacy/\n\n【第三方登录类】\n• 微信OpenSDK（深圳市腾讯计算机系统有限公司）\n  用途：微信登录和分享\n  收集信息：用户主动分享的内容、用户授权的微信头像和昵称、微信客户端安装状态（仅Android，用于确认设备是否支持微信登录分享功能）\n  隐私政策：https://support.weixin.qq.com/cgi-bin/mmsupportacctnodeweb-bin/pages/RYiYJkLOrQwu0nb8\n\n• 抖音开放平台（北京抖音科技有限公司）\n  用途：抖音授权登录\n  接入方式：通过OAuth 2.0网页授权流程实现，非原生SDK集成，不在本地采集设备信息\n  收集信息：经用户授权后获取抖音昵称和头像等公开资料\n  隐私政策：https://www.douyin.com/agreements/?id=6773901168964798477\n\n• Google Sign-In（Google LLC）\n  用途：Google账号登录\n  收集信息：经用户授权后获取Google账号信息（邮箱、昵称、头像）\n  隐私政策：https://policies.google.com/privacy\n\n【社交通讯类】\n• 腾讯云即时通信IM SDK（腾讯云计算（北京）有限责任公司）\n  用途：即时通信服务\n  收集信息：设备MAC地址、网络连接状态（WiFi/4G/5G状态）、设备型号、系统版本\n  隐私政策：https://cloud.tencent.com/document/product/269/58094\n\n【基础功能类】\n• Supabase SDK（Supabase Inc.）：后端服务与数据存储，收集账号认证信息和网络请求数据。隐私政策：https://supabase.com/privacy\n• Flutter引擎（Google LLC）：应用运行框架，在文本输入时自动读取剪贴板描述信息用于粘贴菜单判断，不读取或上传剪贴板具体内容。\n• Flutter本地通知插件（开源社区）：本地消息通知，仅使用通知权限状态，不收集个人信息。\n• OkHttp3/Okio/Gson等：基础网络通信和数据处理，不主动收集个人信息。\n\n请参阅各SDK隐私政策了解详情。您可在「设置 > 隐私政策 > 第三方SDK清单」查看完整列表。"}'::jsonb)
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- 步骤5：更新 summary 摘要（首启弹窗用）
-- 更新"数据收集"摘要，明确提及MAC地址、软件安装列表、剪贴板
UPDATE legal_documents
SET summary = jsonb_set(summary, '{0}', '{"icon":"analytics_outlined","title":"数据收集","content":"我们会收集设备基本信息（设备型号、系统版本、屏幕分辨率）和匿名设备标识符（OAID，由高德SDK可选采集）。第三方SDK（腾讯云IM SDK、高德定位SDK）可能自动采集设备MAC地址用于服务运行。应用会查询特定第三方应用的安装状态（软件安装列表）用于分享和登录功能。应用框架在文本输入时会自动读取剪贴板描述信息用于粘贴菜单。上述信息不用于广告追踪或用户画像。"}'::jsonb)
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- 步骤6：更新完整 sdk_list
-- 关键修改：
--   ① 高德定位SDK新增"设备MAC地址"
--   ② 腾讯云IM SDK新增"设备MAC地址"
--   ③ 新增"Flutter引擎"条目（剪贴板描述信息读取）
UPDATE legal_documents
SET sdk_list = '[
  {
    "name": "高德定位SDK",
    "company": "高德软件有限公司",
    "purpose": "提供定位服务",
    "dataCollected": ["精确/粗略位置信息","GNSS信息","WiFi信息（WiFi状态、SSID、BSSID、信号强度）","基站信息","IP地址","设备MAC地址","设备品牌及型号","操作系统","运营商信息","屏幕分辨率","传感器信息","设备信号强度信息","OAID（可选，用于服务优化）","应用名及版本号"],
    "privacyUrl": "https://lbs.amap.com/pages/privacy/"
  },
  {
    "name": "高德地图SDK",
    "company": "高德软件有限公司",
    "purpose": "提供地图显示和交互功能",
    "dataCollected": ["位置信息（经纬度）","设备品牌及型号","操作系统","运营商信息","屏幕分辨率","传感器信息","设备信号强度信息","OAID（可选，用于服务优化）","应用名及版本号"],
    "privacyUrl": "https://lbs.amap.com/pages/privacy/"
  },
  {
    "name": "微信OpenSDK",
    "company": "深圳市腾讯计算机系统有限公司",
    "purpose": "微信登录和分享功能",
    "dataCollected": ["用户主动分享的内容","用户授权的微信头像和昵称","微信客户端安装状态（仅Android）"],
    "privacyUrl": "https://support.weixin.qq.com/cgi-bin/mmsupportacctnodeweb-bin/pages/RYiYJkLOrQwu0nb8"
  },
  {
    "name": "腾讯云即时通信IM SDK",
    "company": "腾讯云计算（北京）有限责任公司",
    "purpose": "即时通信服务",
    "dataCollected": ["设备MAC地址","网络连接状态（WiFi/4G/5G）","设备型号","系统版本"],
    "privacyUrl": "https://cloud.tencent.com/document/product/269/58094"
  },
  {
    "name": "抖音开放平台",
    "company": "北京抖音科技有限公司",
    "purpose": "抖音授权登录（OAuth 2.0网页授权，非原生SDK）",
    "dataCollected": ["经用户授权后获取抖音昵称和头像等公开资料"],
    "privacyUrl": "https://www.douyin.com/agreements/?id=6773901168964798477"
  },
  {
    "name": "Google Sign-In SDK",
    "company": "Google LLC",
    "purpose": "Google账号登录",
    "dataCollected": ["经用户授权后获取Google账号信息（邮箱、昵称、头像）"],
    "privacyUrl": "https://policies.google.com/privacy"
  },
  {
    "name": "Google定位服务",
    "company": "Google LLC",
    "purpose": "海外定位服务（通过系统Google Play Services提供）",
    "dataCollected": ["精确/粗略位置信息","IP地址"],
    "privacyUrl": "https://policies.google.com/privacy"
  },
  {
    "name": "Supabase SDK",
    "company": "Supabase Inc.",
    "purpose": "后端服务、用户认证、数据存储",
    "dataCollected": ["账号认证信息","网络请求数据"],
    "privacyUrl": "https://supabase.com/privacy"
  },
  {
    "name": "Flutter引擎",
    "company": "Google LLC",
    "purpose": "应用运行框架",
    "dataCollected": ["剪贴板描述信息（文本输入时自动读取，仅判断可粘贴内容类型，不读取具体内容）"],
    "privacyUrl": "https://flutter.dev/to/privacy"
  },
  {
    "name": "Flutter本地通知插件",
    "company": "开源社区",
    "purpose": "本地消息通知",
    "dataCollected": ["通知权限状态"],
    "privacyUrl": ""
  }
]'::jsonb
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.2.0';

-- 步骤7：验证更新结果
-- SELECT version, is_current, 
--   sections->1->>'title' as section1_title,
--   jsonb_array_length(sdk_list) as sdk_count
-- FROM legal_documents 
-- WHERE document_type = 'privacy_policy' AND language = 'zh'
-- ORDER BY version;

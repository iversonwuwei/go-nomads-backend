-- ============================================================
-- 隐私政策升级 v1.1.0 步骤5b：追加后4个SDK到sdk_list
-- ============================================================

UPDATE legal_documents
SET sdk_list = sdk_list || '[
  {
    "name": "Google Sign-In SDK",
    "company": "Google LLC",
    "purpose": "Google账号登录",
    "dataCollected": ["Google账号信息","设备标识符"],
    "privacyUrl": "https://policies.google.com/privacy"
  },
  {
    "name": "Google Location Services",
    "company": "Google LLC",
    "purpose": "海外定位服务",
    "dataCollected": ["精确/粗略位置信息","设备标识符","IP地址"],
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
    "name": "Flutter本地通知插件",
    "company": "开源社区",
    "purpose": "本地消息通知推送",
    "dataCollected": ["通知权限状态"],
    "privacyUrl": ""
  }
]'::jsonb
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.1.0';

-- ============================================================
-- 隐私政策升级 v1.1.0 步骤5a：设置sdk_list前4个SDK
-- ============================================================

UPDATE legal_documents
SET sdk_list = '[
  {
    "name": "高德定位SDK",
    "company": "高德软件有限公司",
    "purpose": "提供定位服务",
    "dataCollected": ["精确/粗略位置信息","WIFI信息（SSID、BSSID）","基站信息","IP地址","GNSS信息","设备标识信息（Android ID、OAID）","设备型号","操作系统","运营商信息"],
    "privacyUrl": "https://lbs.amap.com/pages/privacy/"
  },
  {
    "name": "高德地图SDK",
    "company": "高德软件有限公司",
    "purpose": "提供地图显示和交互功能",
    "dataCollected": ["位置信息","WIFI列表","设备标识信息（Android ID、OAID）","设备型号"],
    "privacyUrl": "https://lbs.amap.com/pages/privacy/"
  },
  {
    "name": "微信OpenSDK",
    "company": "深圳市腾讯计算机系统有限公司",
    "purpose": "微信登录和分享功能",
    "dataCollected": ["设备标识信息（Android ID）"],
    "privacyUrl": "https://weixin.qq.com/cgi-bin/readtemplate?t=weixin_agreement&s=privacy"
  },
  {
    "name": "腾讯云即时通信IM SDK",
    "company": "腾讯科技（深圳）有限公司",
    "purpose": "即时通信服务",
    "dataCollected": ["设备型号","操作系统版本","设备标识信息（Android ID）","网络状态","存储信息"],
    "privacyUrl": "https://cloud.tencent.com/document/product/269/90455"
  }
]'::jsonb
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.1.0';

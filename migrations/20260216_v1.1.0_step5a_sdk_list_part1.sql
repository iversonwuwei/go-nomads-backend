-- ============================================================
-- 隐私政策升级 v1.1.0 步骤5a：设置sdk_list（基于官方隐私政策核实）
-- ============================================================

UPDATE legal_documents
SET sdk_list = '[
  {
    "name": "高德定位SDK",
    "company": "高德软件有限公司",
    "purpose": "提供定位服务",
    "dataCollected": ["精确/粗略位置信息","GNSS信息","WiFi信息（WiFi状态、SSID、BSSID、信号强度）","基站信息","IP地址","设备品牌及型号","操作系统","运营商信息","屏幕分辨率","传感器信息","设备信号强度信息","OAID（可选，用于服务优化）","应用名及版本号"],
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
    "dataCollected": ["网络连接状态（WiFi/4G/5G）","设备型号","系统版本"],
    "privacyUrl": "https://cloud.tencent.com/document/product/269/58094"
  },
  {
    "name": "抖音开放平台",
    "company": "北京抖音科技有限公司",
    "purpose": "抖音授权登录（OAuth 2.0网页授权，非原生SDK）",
    "dataCollected": ["经用户授权后获取抖音昵称和头像等公开资料"],
    "privacyUrl": "https://www.douyin.com/agreements/?id=6773901168964798477"
  }
]'::jsonb
WHERE document_type = 'privacy_policy' AND language = 'zh' AND version = '1.1.0';

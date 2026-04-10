# Digital Nomad P0 API Contracts

<!-- markdownlint-disable-file MD024 -->

## Status Snapshot 2026-04-06

- Flutter 客户端已完成 Explore / Land / Community / Inbox / Me 一级入口重构，并已实际消费以下聚合接口：
  - GET /api/v1/migration-workspace
  - GET /api/v1/explore-dashboard/current
  - GET /api/v1/land-hub/current
  - GET /api/v1/budgets/current
  - GET /api/v1/visa/profiles
  - GET /api/v1/inbox/summary
- 当前文档中 /api/v1/migrations 系列为早期草案，已与现行实现不一致。后续契约应以现网实际路径为准，再在此基础上继续演进写接口。
- Land Hub 与 Explore Dashboard 已完成服务端聚合收口；后续切片继续沿用“文档先行 -> backend 实现 -> app 对接 -> 验证”的闭环。
- Profile Snapshot 与 Community Snapshot v2 已完成文档驱动闭环；Community 首屏的 field notes、questions 与 recommendations 已切到真实后端聚合数据，meetups 聚合接口已就位。
- 当前最新完成切片: City Nomad Summary v1，已把 City Detail 决策面板的 budget 与 decision signals 从客户端主推导链路迁回后端聚合。
- 下一自然切片: Community Q&A 写模型与互动持久化；Community 首页 meetup preview 已切到 Community Snapshot。

## 1. Requirement Frame

### 目标

- 为 go-nomads-app 的 P0 升级提供最小可实施 API 契约草案。
- 优先支撑 Explore Dashboard、Migration Workspace、Budget Center、Visa Center、Inbox Summary、City Decision View。
- 保持现有接口兼容，不与现有城市、通知、聊天、旅行计划能力发生破坏性冲突。

### 调用方

- go-nomads-app Flutter 客户端
- 后续可复用到 go-nomads-web / go-nomads-admin

### 范围

- 仅定义 P0 契约边界、DTO 草案、错误语义、兼容策略、观测点。
- 本文档不直接要求一次性完成所有后端实现。

### 服务边界建议

- CityService: 继续作为城市基础信息 source of truth。
- SearchService 或聚合层: 承担城市数字游民摘要聚合。
- AIService: 仅提供上下文化辅助，不作为 P0 唯一数据源。
- 新增 Migration / Budget / Visa 聚合 API 时，优先以独立 Application 模块或独立服务承接，避免把复杂状态塞回 TravelPlan 旧接口。

## 2. Contract Principles

- 新增字段默认可空，保证旧客户端不崩溃。
- 新接口采用聚合查询优先，避免客户端在首页发起过多瀑布请求。
- 所有写接口需要明确资源归属、状态流转、非法状态错误。
- DTO 命名优先清晰表达“迁移”“预算”“签证”语义，不复用旧 TravelPlan DTO 名字造成歧义。

## 3. API Overview

### P0 需要的最小接口集合

- GET /api/v1/app/config
- GET /api/v1/inbox/summary
- GET /api/v1/cities/{cityId}/nomad-summary
- GET /api/v1/migration-workspace
- GET /api/v1/budgets/current
- GET /api/v1/visa/profiles
- GET /api/v1/explore-dashboard/current
- GET /api/v1/land-hub/current
- GET /api/v1/profile-snapshot/current
- GET /api/v1/community-snapshot/current
- POST /api/v1/migration-workspace/plans/{planId}/state
- POST /api/v1/budgets/plans/{planId}
- POST /api/v1/visa/profiles/{planId}
- POST /api/v1/community/questions
- POST /api/v1/community/questions/{questionId}/answers
- POST /api/v1/community/questions/{questionId}/upvote
- POST /api/v1/community/answers/{answerId}/upvote

## 4. App Config API

### GET /api/v1/app/config

#### 用途

- 为 Flutter / Harmony 等客户端提供可发布的静态配置读取入口。
- 当前最小落地范围: 社区准则正文、首次法律文档同意版本号、首次启动隐私弹窗的外围与交互文案、forgot-password 登录前找回密码流程文案、登录/注册页法律包装文案、登录前品牌/备案壳层文案、位置/日历/通知权限用途说明弹窗文案，以及位置权限请求弹窗/状态卡片文案。
- 该接口允许匿名 GET 读取，适用于首次启动前或未登录状态下的静态内容展示。

#### Response 200

```json
{
  "version": 3,
  "publishedAt": "2026-04-09T08:00:00Z",
  "staticTexts": {
    "legal.community_guidelines.sections_json": "[{\"title\":\"1. 尊重与友善\",\"content\":\"请尊重他人观点与文化差异。\"}]",
    "legal.first_launch.dialog.title": "服务协议与隐私政策",
    "legal.first_launch.dialog.intro": "欢迎使用行途（Go-Nomads）！为了继续使用应用，请您阅读并同意《隐私政策》和《用户协议》。",
    "legal.first_launch.dialog.privacy_checkbox_prefix": "我已阅读并同意",
    "legal.first_launch.dialog.terms_checkbox_prefix": "我已阅读并同意",
    "legal.first_launch.dialog.decline_tip_prefix": "如果您不同意上述法律文档，将无法继续使用本应用。您可以随时在设置中查看完整的",
    "legal.first_launch.dialog.sdk_link_label": "第三方SDK清单",
    "legal.first_launch.dialog.agree_button": "同意并继续",
    "legal.first_launch.dialog.reject_button": "不同意并退出",
    "legal.first_launch.dialog.summary_fallback_title": "法律文档说明",
    "legal.first_launch.dialog.summary_fallback_content": "我们重视您的隐私与使用权益。请查看完整隐私政策、用户协议和第三方 SDK 清单。",
    "legal.first_launch.dialog.unchecked_toast_title": "需要同意条款",
    "legal.first_launch.dialog.unchecked_toast_message": "请先同意隐私政策和用户协议",
    "legal.first_launch.dialog.decline_confirm_title": "温馨提示",
    "legal.first_launch.dialog.decline_confirm_message": "如果您不同意隐私政策和用户协议，将无法使用本应用的相关功能。\n\n我们非常重视您的隐私安全，收集的信息仅用于为您提供更好的服务。\n\n您确定不同意吗？",
    "legal.first_launch.dialog.decline_confirm_cancel": "再想想",
    "legal.first_launch.dialog.decline_confirm_exit": "确认退出",
    "auth.forgot_password.step.account.title": "找回密码",
    "auth.forgot_password.step.account.description": "请输入您的邮箱或手机号\n我们将发送验证码帮助您重置密码",
    "auth.forgot_password.step.account.input_label": "邮箱或手机号",
    "auth.forgot_password.step.account.send_code_button": "发送验证码",
    "auth.forgot_password.step.verify.title": "验证身份",
    "auth.forgot_password.step.verify.description_template": "验证码已发送至\n{target}",
    "auth.forgot_password.step.verify.code_label": "验证码",
    "auth.forgot_password.step.verify.resend_countdown_template": "{seconds}s 后重新发送",
    "auth.forgot_password.step.verify.resend_button": "重新发送验证码",
    "auth.forgot_password.step.verify.next_button": "下一步",
    "auth.forgot_password.step.reset.title": "设置新密码",
    "auth.forgot_password.step.reset.description": "请设置您的新密码",
    "auth.forgot_password.step.reset.new_password_label": "新密码",
    "auth.forgot_password.step.reset.confirm_password_label": "确认密码",
    "auth.forgot_password.step.reset.submit_button": "重置密码",
    "auth.forgot_password.toast.account_required": "请输入邮箱或手机号",
    "auth.forgot_password.toast.code_sent_email": "验证码已发送到邮箱",
    "auth.forgot_password.toast.code_sent_phone": "验证码已发送到手机",
    "auth.forgot_password.toast.send_failed_fallback": "发送验证码失败，请稍后重试",
    "auth.forgot_password.toast.code_required": "请输入验证码",
    "auth.forgot_password.toast.code_incomplete": "请输入完整的验证码",
    "auth.forgot_password.toast.new_password_required": "请输入新密码",
    "auth.forgot_password.toast.password_min_length": "密码至少需要6个字符",
    "auth.forgot_password.toast.confirm_password_required": "请确认新密码",
    "auth.forgot_password.toast.password_mismatch": "两次输入的密码不一致",
    "auth.forgot_password.toast.reset_success": "密码重置成功，请使用新密码登录",
    "auth.forgot_password.toast.reset_failed_fallback": "重置密码失败，请稍后重试",
    "auth.login.terms.prefix": "我已阅读并同意 ",
    "auth.login.terms.connector": " 和 ",
    "auth.login.terms.suffix": "。",
    "auth.register.terms.prefix": "我已阅读并同意 ",
    "auth.register.terms.connector": " 和 ",
    "auth.register.terms.community_prefix": "，并遵守 ",
    "auth.register.terms.suffix": "。",
    "auth.legal_links.prefix": "继续使用即表示您同意 ",
    "auth.legal_links.connector": " 与 ",
    "auth.legal_links.suffix": "。",
    "auth.login.header.title": "欢迎",
    "auth.login.header.subtitle": "登录",
    "auth.login.link.register_prefix": "Let's Go",
    "auth.login.community.title": "加入 38,000+ 游牧者",
    "auth.login.community.subtitle": "在全球各地生活和工作",
    "auth.login.community.badge.meetups": "363 场聚会/年",
    "auth.login.community.badge.messages": "15k+ 消息",
    "auth.login.community.badge.cities": "100+ 城市",
    "auth.register.header.title": "成为数字游民",
    "auth.register.header.subtitle": "加入全球远程工作者社区",
    "auth.register.link.login_prefix": "已有账号?",
    "auth.register.highlights.title": "加入 38,000+ 会员并获得:",
    "auth.register.highlights.meetups.title": "参加 363 场聚会/年",
    "auth.register.highlights.meetups.subtitle": "在全球 100+ 城市",
    "auth.register.highlights.people.title": "结识新朋友",
    "auth.register.highlights.people.subtitle": "用于约会和交友",
    "auth.register.highlights.destinations.title": "研究目的地",
    "auth.register.highlights.destinations.subtitle": "找到最适合您的居住地",
    "auth.register.highlights.chat.title": "加入专属聊天",
    "auth.register.highlights.chat.subtitle": "本月发送了 15,000+ 条消息",
    "auth.register.highlights.travels.title": "记录您的旅行",
    "auth.register.highlights.travels.subtitle": "分享您的旅程",
    "legal.first_launch.dialog.decline_tip_link_separator": "、",
    "legal.first_launch.dialog.decline_tip_link_final_connector": "和",
    "legal.first_launch.dialog.decline_tip_suffix": "。",
    "auth.login.form.tab.email": "邮箱登录",
    "auth.login.form.tab.phone": "手机登录",
    "auth.login.form.email.label": "邮箱",
    "auth.login.form.email.hint": "邮箱",
    "auth.login.form.password.label": "密码",
    "auth.login.form.password.hint": "密码",
    "auth.login.form.remember_me": "记住我",
    "auth.login.form.forgot_password": "忘记密码?",
    "auth.login.form.submit_email_button": "点击登录/注册",
    "auth.login.form.phone.label": "手机号",
    "auth.login.form.phone.hint": "请输入手机号",
    "auth.login.form.sms_code.label": "验证码",
    "auth.login.form.sms_code.hint": "请输入验证码",
    "auth.login.form.sms_code.send_button": "获取验证码",
    "auth.login.form.sms_code.countdown_template": "{seconds}s",
    "auth.login.form.submit_phone_button": "点击登录/注册",
    "auth.login.form.error.email_required": "请输入邮箱",
    "auth.login.form.error.email_invalid": "邮箱格式不正确",
    "auth.login.form.error.password_required": "请输入密码",
    "auth.login.form.error.phone_required": "请输入手机号",
    "auth.login.form.error.phone_invalid": "请输入正确的手机号",
    "auth.login.form.error.sms_code_required": "请输入验证码",
    "auth.login.feedback.terms_required_title": "需要同意条款",
    "auth.login.feedback.terms_required_message": "请先同意服务条款与隐私政策",
    "auth.login.feedback.phone_required": "请输入手机号",
    "auth.login.feedback.phone_invalid": "请输入有效的中国大陆手机号",
    "auth.login.feedback.sms_code_sent": "验证码已发送",
    "auth.login.feedback.send_failed": "发送失败，请稍后重试",
    "auth.login.feedback.send_sms_failed": "发送验证码失败，请稍后重试",
    "auth.login.feedback.welcome_back": "欢迎回来",
    "auth.login.feedback.login_success_title": "登录成功",
    "auth.login.feedback.invalid_email_or_password": "邮箱或密码错误",
    "auth.login.feedback.login_failed_title": "登录失败",
    "auth.login.feedback.unknown_error_retry": "登录失败，请重试",
    "auth.login.feedback.login_failed_retry": "登录失败，请重试",
    "auth.login.feedback.sms_code_invalid_or_expired": "验证码无效或已过期",
    "auth.login.feedback.social_loading_title_template": "正在使用 {platform} 登录",
    "auth.login.feedback.please_wait": "请稍候...",
    "auth.login.feedback.social_failed_template": "{platform} 登录失败，请稍后重试",
    "auth.login.social.divider": "或使用以下方式继续",
    "auth.login.social.label.wechat": "微信",
    "auth.login.social.label.qq": "QQ",
    "auth.login.social.label.apple": "Apple",
    "auth.login.social.label.google": "Google",
    "auth.login.social.label.twitter": "Twitter",
    "auth.login.social.label.facebook": "Facebook",
    "auth.login.social.facebook_unavailable_title": "使用 Facebook 继续",
    "auth.login.social.facebook_unavailable_message": "该登录方式即将开放",
    "auth.register.form.username.label": "用户名",
    "auth.register.form.username.hint": "选择您的用户名",
    "auth.register.form.email.label": "邮箱",
    "auth.register.form.email.hint": "邮箱",
    "auth.register.form.verification_code.label": "验证码",
    "auth.register.form.verification_code.hint": "请输入验证码",
    "auth.register.form.verification_code.send_button": "获取验证码",
    "auth.register.form.verification_code.countdown_template": "{seconds}s",
    "auth.register.form.verification_code.resend_button": "重新发送",
    "auth.register.form.password.label": "密码",
    "auth.register.form.password.hint": "创建密码",
    "auth.register.form.confirm_password.label": "确认密码",
    "auth.register.form.confirm_password.hint": "重新输入密码",
    "auth.register.form.submit_button": "加入行途",
    "auth.register.form.toast.terms_required_title": "需要同意条款",
    "auth.register.form.toast.terms_required_message": "请同意服务条款和社区准则",
    "auth.register.form.toast.welcome_message": "欢迎加入 Nomads 社区!",
    "auth.register.form.toast.success_title": "成功",
    "auth.register.form.error.username_required": "请输入用户名",
    "auth.register.form.error.username_min_length": "用户名至少需要3个字符",
    "auth.register.form.error.email_required": "请输入邮箱",
    "auth.register.form.error.email_invalid": "邮箱格式不正确",
    "auth.register.form.error.verification_code_required": "请输入验证码",
    "auth.register.form.error.verification_code_length": "验证码必须为6位",
    "auth.register.form.error.password_required": "请输入密码",
    "auth.register.form.error.password_min_length": "密码至少6位",
    "auth.register.form.error.confirm_password_required": "请确认您的密码",
    "auth.register.form.error.passwords_not_match": "密码不匹配",
    "auth.register.feedback.code_sent_to_email": "验证码已发送到邮箱",
    "auth.register.feedback.send_failed": "发送失败，请稍后重试",
    "auth.register.feedback.send_code_failed_retry": "验证码发送失败，请稍后重试",
    "auth.register.feedback.register_failed_check_input": "注册失败，请检查输入信息",
    "auth.register.feedback.register_failed_title": "注册失败",
    "auth.register.feedback.register_failed_process_error": "注册失败，请稍后重试",
    "brand.loading.title": "行途 Go Nomads",
    "brand.loading.tagline": "Explore cities, workspaces and community",
    "brand.footer.copyright": "© 大连素辉软件科技有限公司 All Rights Reserved",
    "brand.footer.icp_record": "辽ICP备2026001591号",
    "permission.location.purpose_dialog_json": "{\"title\":\"需要使用您的位置信息\",\"description\":\"行途需要获取您的位置权限，用于以下功能：\",\"purposes\":[\"为您推荐附近的城市和目的地\",\"查找您附近的活动和聚会\",\"发现附近的共享办公空间\",\"提供地图导航和位置选择功能\"],\"note\":\"我们仅在您使用相关功能时获取位置信息，不会在后台持续追踪您的位置。您可以随时在系统设置中关闭位置权限。\",\"confirmText\":\"继续\"}",
    "permission.calendar.purpose_dialog_json": "{\"title\":\"需要访问您的日历\",\"description\":\"行途需要获取日历权限，用于以下功能：\",\"purposes\":[\"将活动和聚会添加到您的日历中\",\"设置活动提醒，避免错过精彩活动\"],\"note\":\"我们仅在您主动点击\\\"添加到日历\\\"时访问日历，不会读取您的其他日历信息。\",\"confirmText\":\"继续\"}",
    "permission.notification.purpose_dialog_json": "{\"title\":\"需要发送通知\",\"description\":\"行途需要通知权限，用于以下功能：\",\"purposes\":[\"旅行指南生成完成通知\",\"新消息和互动提醒\",\"活动开始前提醒\"],\"note\":\"您可以随时在应用设置或系统设置中关闭通知。\",\"confirmText\":\"继续\"}",
    "permission.location.dialog.title": "需要位置权限",
    "permission.location.dialog.description": "我们需要访问您的位置信息,以便为您推荐附近的城市和提供基于位置的服务",
    "permission.location.dialog.cancel_button": "取消",
    "permission.location.dialog.confirm_button": "授予权限",
    "permission.location.status.loading": "正在获取位置...",
    "permission.location.status.disabled": "位置未启用",
    "permission.location.status.enable_action": "启用"
  },
  "optionGroups": {},
  "systemSettings": {
    "legal_documents": {
      "privacy_policy_version": {
        "label": "Privacy Policy Version",
        "valueType": "string",
        "value": "2026.04.09",
        "defaultValue": null,
        "description": "Current accepted version for first-launch privacy consent"
      },
      "terms_of_service_version": {
        "label": "Terms of Service Version",
        "valueType": "string",
        "value": "2026.04.09",
        "defaultValue": null,
        "description": "Current accepted version for first-launch terms consent"
      }
    }
  }
}
```

#### 说明

- `staticTexts` 为 locale 过滤后的最终结果；客户端不需要再处理多语言字典，只按 key 读取即可。
- `legal.community_guidelines.sections_json` 由客户端按 JSON 数组解析，数组元素包含 `title` 与 `content`。
- `legal.first_launch.dialog.*` 用于首次启动隐私弹窗的外围与交互文案，包括摘要兜底、未勾选提示、拒绝确认弹窗按钮以及 decline tip 中三个文档链接之间的连接符/结尾标点；法律文档摘要卡片仍继续读取 `/api/v1/users/legal/privacy-policy` 返回的 `summary`，隐私政策与用户协议链接标题优先读取对应 legal document 的 `title`。
- `auth.forgot_password.*` 用于登录前找回密码三步流的标题、步骤说明、按钮和 toast 文案；`auth.forgot_password.step.verify.description_template` 必须包含 `{target}` 占位符，`auth.forgot_password.step.verify.resend_countdown_template` 必须包含 `{seconds}` 占位符。
- `auth.login.terms.*`、`auth.register.terms.*`、`auth.legal_links.*` 用于登录/注册页法律链接外围包装文案；terms / privacy / community 三个链接标题本身不在本轮下沉，继续保持现有来源与跳转。
- `auth.login.header.*`、`auth.login.link.register_prefix`、`auth.login.community.*`、`auth.register.header.*`、`auth.register.link.login_prefix`、`auth.register.highlights.*` 用于登录/注册第一页的欢迎 header、跨页跳转提示和 marketing/highlight 文案；仅迁移外层展示 copy，不改变登录方式、注册提交流程、图标或布局。
- `auth.login.form.*`、`auth.register.form.*` 用于登录/注册表单入口中的 tab、字段标题、placeholder、字段级错误提示、验证码发送按钮、倒计时模板、主 CTA 和注册成功前置 toast 文案；不迁移校验规则、接口错误信息或实际提交逻辑。倒计时模板必须包含 `{seconds}` 占位符。
- `auth.login.feedback.*`、`auth.register.feedback.*` 用于登录/注册入口动作后的公开反馈 copy，包括协议未勾选提醒、验证码发送结果、登录成功/失败 toast、社交登录 loading 文案和注册失败提示；服务端真实报错若已返回 message，客户端仍优先展示接口 message，只在缺省时回退这些静态文案。
- `auth.login.social.*` 用于登录页社交登录区域中的分隔线、平台按钮标签和暂未开放入口提示；只迁移展示 copy，不改变 provider 可用性、平台分流、图标品牌或真实社交登录调用逻辑。
- `brand.loading.*`、`brand.footer.*` 用于 loading 页品牌标题/副标题与页面底部版权/备案展示；仅承载公开展示文案，不包含任何内部运营或敏感配置。
- `permission.location.purpose_dialog_json`、`permission.calendar.purpose_dialog_json`、`permission.notification.purpose_dialog_json` 用于权限申请前用途说明弹窗；客户端按 JSON 解析 `title`、`description`、`purposes[]`、`note`、`confirmText`，图标和配色仍由客户端本地决定。
- `permission.location.dialog.*`、`permission.location.status.*` 用于位置权限请求弹窗与位置状态卡片的标题、描述、按钮和状态提示；仅迁移文案 ownership，不改变定位申请、刷新或布局行为。
- `systemSettings.legal_documents.privacy_policy_version` 与 `terms_of_service_version` 用于客户端本地 consent cache 的版本判断；法律文档正文本身仍以 `/api/v1/users/legal/*` 为 source of truth。
- ConfigService 启动时会自检并补齐最小必需 key：`legal.community_guidelines.sections_json`、`legal.first_launch.dialog.*`、`auth.forgot_password.*`、`auth.login.terms.*`、`auth.register.terms.*`、`auth.legal_links.*`、`auth.login.header.*`、`auth.login.link.register_prefix`、`auth.login.community.*`、`auth.login.form.*`、`auth.login.feedback.*`、`auth.login.social.*`、`auth.register.header.*`、`auth.register.link.login_prefix`、`auth.register.highlights.*`、`auth.register.form.*`、`auth.register.feedback.*`、`brand.loading.*`、`brand.footer.*`、`permission.*.purpose_dialog_json`、`permission.location.dialog.*`、`permission.location.status.*`（均覆盖 `zh-CN`/`en-US`）以及 `legal_documents.privacy_policy_version`、`legal_documents.terms_of_service_version`；若当前已发布快照缺失或与库内当前值不一致，会自动重新发布。对于仍由 bootstrap 持有的默认静态文本，若数据库值与代码种子发生漂移，也会自动纠正；管理员已改写过的文案不会被回写覆盖。
- Admin 侧修改这些静态文本时，鉴权链路以 Gateway 的 `/api/v1/admin/*` admin 校验为准；ConfigService 仅消费 Gateway 透传的 `X-User-Id` / `X-User-Email` / `X-User-Role` 做权限判断，避免服务内旧 JWT 配置与 Supabase access token 冲突导致后台治理入口失效。

#### 错误语义

- 404: 尚未发布任何配置，客户端应回退到本地默认值。
- 500: ConfigService 读取失败，客户端应回退到本地默认值。

#### 发布安全与回退

- 新增 key 必须遵循“先发布配置，再上线消费代码”或“消费代码带本地 fallback”的原则，避免发布顺序导致客户端白屏。
- 对匿名可读的配置项，只允许包含可公开展示的文案和非敏感系统设置；任何 secret 或管理员内部配置不得通过该接口返回。

## 5. Inbox Summary API

### GET /api/v1/inbox/summary

#### 用途

- 为 P0 新的 Inbox 主入口和首页摘要卡提供聚合未读与待处理数据。

#### Response 200

```json
{
  "unreadMessages": 5,
  "unreadNotifications": 3,
  "budgetAlerts": 1,
  "visaAlerts": 2,
  "pendingTasks": 4,
  "lastUpdatedAt": "2026-04-04T09:30:00Z"
}
```

#### 说明

- unreadMessages: 当前 IM 会话未读总数
- unreadNotifications: 通知中心未读总数
- budgetAlerts: 预算超限或预算提醒数量
- visaAlerts: 签证/停留提醒数量
- pendingTasks: 迁移工作台未完成关键任务数

#### 错误语义

- 401: 未登录或 token 无效
- 503: 聚合依赖不可用，但建议局部降级返回已知字段，不要轻易整体失败

## 5. City Nomad Summary API

### GET /api/v1/cities/{cityId}/nomad-summary

#### 用途

- 支撑城市详情从内容页升级为数字游民决策页。
- 为 City Detail 决策面板提供单次聚合读取能力，统一返回 budget range、decision signals 和 top resource previews，减少页面对 CityDetail + AI guide + cost summary + meetup/coworking/hotel 多源推导的依赖。

#### Response 200

```json
{
  "cityId": "bangkok",
  "cityName": "Bangkok",
  "country": "Thailand",
  "timezone": "Asia/Bangkok",
  "monthlyBudgetRange": {
    "currency": "USD",
    "min": 900,
    "max": 1800
  },
  "decisionSignals": {
    "networkQualityScore": 88,
    "videoCallFriendlinessScore": 84,
    "visaFriendlinessScore": 78,
    "timezoneOverlapScore": 72,
    "communityActivityScore": 81,
    "climateStabilityScore": 66,
    "safetyScore": 74
  },
  "recommendedCoworkings": [
    {
      "id": "cwk_1",
      "name": "The Work Loft",
      "rating": 4.7,
      "dayPassPrice": 12,
      "currency": "USD"
    }
  ],
  "recommendedStays": [
    {
      "id": "stay_1",
      "name": "Nomad Base Bangkok",
      "rating": 4.5,
      "pricePerNight": 68,
      "currency": "USD"
    }
  ],
  "upcomingMeetups": [
    {
      "id": "meet_1",
      "title": "Friday Nomad Mixer",
      "startTime": "2026-04-06T11:00:00Z"
    }
  ],
  "lastUpdatedAt": "2026-04-06T10:00:00Z"
}
```

#### 数据来源建议

- 城市基础数据来自 CityService
- 预算范围来自城市成本统计聚合
- 推荐办公 / 住宿来自 Coworking / Hotel 数据
- upcomingMeetups 来自 MeetupService

#### Scope Notes

- 本切片优先服务 City Detail 决策面板，不要求同步重写城市详情页的 guide、weather、reviews、hotels、coworking 各 tab。
- `decisionSignals` 为后端统一计算的决策信号，Flutter 不再把这些值完全建立在 `City` 与 `DigitalNomadGuide` 的客户端推导上。
- `recommendedCoworkings`、`recommendedStays`、`upcomingMeetups` 先返回顶部预览数据，供 City Detail 决策上下文使用；相关列表页仍沿用现有独立接口。

#### 服务边界建议

- CityService 作为该切片的主聚合层，负责输出 City Nomad Summary。
- 城市基础信息、community / climate / safety 基线优先复用现有 CityService 城市详情与用户内容统计。
- budget range 复用 `GET /api/v1/cities/{cityId}/user-content/cost-summary` 的现有费用聚合语义，不额外发明第二套预算存储。
- coworking / hotel / meetup 通过服务调用获取预览数据，不增加跨服务项目引用。
- visaFriendlinessScore 可基于现有 Digital Nomad Guide 或城市签证语义推导；若 guide 缺失，允许返回 conservative fallback，而不是整体失败。

#### 兼容策略

- 如果暂时没有某项分值，`decisionSignals` 内对应字段可返回 null
- 推荐列表可为空数组，不要省略字段

#### 风险与降级

- 若 CoworkingService / AccommodationService / EventService 任一失败，可只返回空数组，不应影响 decision signals 主体。
- 若 cost summary 不可用，monthlyBudgetRange 允许回退为 `City.averageCost` 的近似区间或 null，但不要导致整个接口失败。
- 若 Digital Nomad Guide 缺失，visaFriendlinessScore 允许降级到 conservative fallback，Flutter 可继续保留现有 visa 文案卡。

#### 实现状态

- 已完成: CityService 已新增 `GET /api/v1/cities/{cityId}/nomad-summary`，返回 budget range、decision signals 与 top resource previews。
- 已完成: Flutter City Detail 决策面板已切换为优先消费该接口，guide 的说明性内容与其他 tabs 继续维持现有实现。
- 已验证: `dotnet build src/Services/CityService/CityService/CityService.csproj` 成功。
- 已验证: `flutter analyze` 覆盖 City Nomad Summary Flutter 接入文件，无静态问题。

## 6. Migration Workspace API

### GET /api/v1/migration-workspace

#### 用途

- 返回当前用户的迁移工作台聚合数据，用于 Migration Workspace 页和 Explore Dashboard 摘要。

#### Response 200

```json
{
  "totalPlans": 2,
  "activePlans": 1,
  "draftPlans": 1,
  "upcomingDepartures": 1,
  "recommendedAction": "lock-departure-window",
  "lastUpdatedAt": "2026-04-06T10:00:00Z",
  "latestPlan": {
    "id": "plan_1",
    "cityId": "bangkok",
    "cityName": "Bangkok",
    "budget": "medium",
    "travelStyle": "culture",
    "status": "planning",
    "departureDate": "2026-05-01T00:00:00Z"
  },
  "plans": []
}
```

#### 说明

- 当前实现继续以 AI Travel Plan 为聚合根，但阶段流转、待办和时间轴状态统一收口到 `ai_travel_plans.plan_data.migrationWorkspace`。
- 这样可以在不破坏既有 Travel Plan 读取链路的前提下，先把 Migration Workspace 的真实状态源补齐；后续如需独立资源再从 `plan_data` 中抽离。

### POST /api/v1/migration-workspace/plans/{planId}/state

#### 用途

- 为 Migration Workspace 写入阶段、焦点备注、待办清单和时间轴。
- 写入后直接返回最新 Migration Workspace 聚合结果，供 Flutter 页面刷新。

#### Request

```json
{
  "stage": "visa_ready",
  "focusNote": "Need to lock lease and collect visa docs before booking flight.",
  "checklist": [
    {
      "id": "lease",
      "title": "Lock first-month lease",
      "isCompleted": true
    },
    {
      "id": "visa_docs",
      "title": "Prepare visa documents",
      "isCompleted": false
    }
  ],
  "timeline": [
    {
      "id": "visa_submission",
      "title": "Submit visa application",
      "status": "pending",
      "targetDate": "2026-04-18T00:00:00Z"
    }
  ]
}
```

#### 状态语义

- `stage` 建议值: `researching`, `budgeting`, `visa_ready`, `booking`, `landing`, `settled`
- `checklist` 用于 Migration Workspace 的待办闭环，不替代旧 Travel Plan 文本内容。
- `timeline` 用于关键里程碑，不要求一次覆盖完整项目管理功能。

## 7. Budget API

### GET /api/v1/budgets/current

#### 用途

- 返回当前用户预算中心首页所需的聚合摘要。

#### Response 200

```json
{
  "currency": "USD",
  "monthlyBudget": 1800,
  "estimatedSpent": 920,
  "remainingBudget": 880,
  "forecastEndOfMonth": 1540,
  "status": "on_track",
  "categories": [
    {
      "category": "accommodation",
      "budget": 800,
      "spent": 620
    }
  ]
}
```

#### 实现状态

- 已在 AIService 中实现并供 Flutter Budget Center 使用。
- 预算基线、模板、提醒阈值统一收口到 `ai_travel_plans.plan_data.budgetWorkspace`。

### POST /api/v1/budgets/plans/{planId}

#### 用途

- 为某个迁移计划保存首月预算基线、预算模板和超支提醒阈值。

#### Request

```json
{
  "templateName": "lean-landing",
  "monthlyBudgetTargetUsd": 2200,
  "forecastMonthlyCostUsd": 2360,
  "alertThresholdPercent": 8,
  "overrunAlertEnabled": true,
  "categories": [
    {
      "category": "accommodation",
      "budgetUsd": 950
    },
    {
      "category": "coworking",
      "budgetUsd": 180
    }
  ]
}
```

#### 说明

- `templateName` 用于标识预算模板，如 `lean-landing`、`balanced`、`comfort`。
- `alertThresholdPercent` 表示当 forecast 高于 target 的百分比时触发预算提醒。
- 返回值为最新 `BudgetCenterResponse`，客户端不需要再单独拼预算快照。

### POST /api/v1/budgets/monthly

#### Request

```json
{
  "currency": "USD",
  "monthlyBudget": 1800,
  "categories": [
    {
      "category": "accommodation",
      "budget": 800
    },
    {
      "category": "coworking",
      "budget": 150
    }
  ]
}
```

### GET /api/v1/budgets/forecast?cityId=bangkok&stage=evaluating

#### Response 200

```json
{
  "cityId": "bangkok",
  "stage": "evaluating",
  "currency": "USD",
  "forecastMonthlyCost": 1450,
  "suggestedBudget": 1600,
  "breakdown": {
    "accommodation": 680,
    "coworking": 120,
    "food": 300,
    "transport": 80,
    "visa": 60,
    "buffer": 210
  }
}
```

#### 兼容策略

- 预算系统初期可只存用户聚合预算，不要求取代现有城市成本内容数据。

## 8. Visa API

### GET /api/v1/visa/profiles

#### 用途

- 返回用户签证档案列表。

#### Response 200

```json
[
  {
    "id": "visa_1",
    "countryCode": "TH",
    "countryName": "Thailand",
    "visaType": "tourist",
    "entryDate": "2026-04-01",
    "expiryDate": "2026-05-30",
    "daysRemaining": 56,
    "status": "active"
  }
]
```

### POST /api/v1/visa/profiles

#### Request

```json
{
  "countryCode": "TH",
  "visaType": "tourist",
  "entryDate": "2026-04-01",
  "expiryDate": "2026-05-30",
  "notes": "May extend once"
}
```

#### 实现状态

- 已在 AIService 中实现并供 Flutter Visa Center 使用。
- 签证类型、停留区间、材料清单和提醒时间统一收口到 `ai_travel_plans.plan_data.visaWorkspace`。

### POST /api/v1/visa/profiles/{planId}

#### 用途

- 为某个迁移计划创建或编辑签证/停留档案。

#### Request

```json
{
  "visaType": "digital_nomad",
  "stayDurationDays": 90,
  "entryDate": "2026-05-01T00:00:00Z",
  "expiryDate": "2026-07-30T00:00:00Z",
  "estimatedCostUsd": 160,
  "requirementsSummary": "Passport, proof of remote income, lease address.",
  "processSummary": "Collect docs, submit online, attend verification if requested.",
  "requiredDocuments": [
    "Passport",
    "Remote income proof",
    "Accommodation confirmation"
  ],
  "reminderDates": [
    "2026-04-15T00:00:00Z",
    "2026-07-20T00:00:00Z"
  ]
}
```

#### 说明

- `requiredDocuments` 为最小材料清单闭环。
- `reminderDates` 为服务端保存的提醒建议时间，Flutter 仍可继续调用 OpenClaw 做本地提醒下发。

## 8.1 Community Q&A Write Model

### POST /api/v1/community/questions

#### 用途

- 创建真实持久化的问题线程，不再依赖 field notes 映射的伪线程。

#### Request

```json
{
  "city": "Bangkok",
  "title": "How fast is Bangkok immigration for a digital nomad visa renewal?",
  "content": "I want to understand practical queue times and whether weekday mornings are better.",
  "tags": ["visa", "renewal", "bangkok"]
}
```

### POST /api/v1/community/questions/{questionId}/answers

#### 用途

- 为真实问题发布回答，并在 question detail 中持久化展示。

#### Request

```json
{
  "content": "Morning slots are usually faster. Bring printed income proof even if the checklist says digital copies are accepted."
}
```

### POST /api/v1/community/questions/{questionId}/upvote

### POST /api/v1/community/answers/{answerId}/upvote

#### 说明

- upvote 接口为 toggle 语义，再次调用代表取消点赞。
- Community Snapshot 的 `questions[].answers[]` 现在应优先返回真实 Q&A 持久化结果；若没有真实数据，允许为空数组，不再回退为 system-generated 假答案。

## 9. Explore Dashboard API

### GET /api/v1/explore-dashboard/current

#### 用途

- 为 Explore 首页提供单次聚合读取能力。
- 统一返回 Migration Workspace、Budget Center、Visa Center 与 Inbox Summary，减少首页四次并发请求。

#### Response 200

```json
{
  "migrationWorkspace": {
    "totalPlans": 2,
    "activePlans": 1,
    "draftPlans": 1,
    "upcomingDepartures": 1,
    "recommendedAction": "review-upcoming-departure",
    "lastUpdatedAt": "2026-04-06T10:00:00Z",
    "latestPlan": {
      "id": "plan_1",
      "cityId": "bangkok",
      "cityName": "Bangkok"
    },
    "plans": []
  },
  "budgetCenter": {
    "monthlyBudgetTargetUsd": 1800,
    "forecastMonthlyCostUsd": 1650,
    "deltaUsd": 150,
    "activePlanCount": 1,
    "trackedCityCount": 1,
    "budgetHealth": "on_track",
    "recommendedAction": "finalize-budget-baseline",
    "lastUpdatedAt": "2026-04-06T10:00:00Z",
    "focusPlan": {},
    "plans": []
  },
  "visaCenter": {
    "activeProfileCount": 1,
    "attentionRequiredCount": 0,
    "reminderReadyCount": 1,
    "recommendedAction": "review-latest-visa",
    "lastUpdatedAt": "2026-04-06T10:00:00Z",
    "focusProfile": {},
    "profiles": []
  },
  "inboxSummary": {
    "unreadNotifications": 3,
    "totalNotifications": 8,
    "actionRequiredCount": 2,
    "latestNotificationAt": "2026-04-06T09:40:00Z",
    "recentNotifications": []
  },
  "lastUpdatedAt": "2026-04-06T10:00:00Z"
}
```

#### 契约要求

- migrationWorkspace / budgetCenter / visaCenter 直接复用现有 AIService DTO。
- inboxSummary 直接复用 MessageService 的 Inbox Summary 契约，不在 AIService 内部重命名字段。
- AIService 通过内部服务调用向 MessageService 请求 inbox summary，禁止增加跨服务项目引用。
- 若 inbox summary 下游暂时失败，允许返回 null，但 migration / budget / visa 不应因此整体失败。

#### 实现约束

- 聚合逻辑保持在 Application 层，Controller 仅负责鉴权与返回 ApiResponse。
- 下游调用优先使用 ServiceInvocationClient，并显式透传 X-User-Id，确保 MessageService 可以解析当前用户。
- 本切片只聚合现有四个摘要，不下沉首页 priority queue 排序逻辑。

#### 实现状态

- 已完成: AIService 新增 ExploreDashboardController、ExploreDashboardResponse 与 Application 聚合逻辑。
- 已完成: AIService 通过 ServiceInvocationClient 调用 MessageService `/api/v1/inbox/summary`，并透传 X-User-Id。
- 已完成: Flutter 首页改为单次读取 `/api/v1/explore-dashboard/current`。
- 已验证: `dotnet build src/Services/AIService/AIService/AIService.csproj` 成功。
- 已验证: `dotnet test tests/AIService.Tests/AIService.Tests.csproj --filter FullyQualifiedName~AIChatApplicationServiceExploreDashboardTests` 成功，覆盖下游成功与 message-service 失败时的部分成功响应。

## 10. Land Hub API

### GET /api/v1/land-hub/current

#### 用途

- 由服务端一次返回 Land Hub 所需的迁移、预算、签证与焦点 Travel Plan 明细。
- 替代客户端当前的多接口并发拼装，减少 Land Hub 的聚合职责。

#### Response 200

```json
{
  "migrationWorkspace": {
    "totalPlans": 2,
    "activePlans": 1,
    "draftPlans": 1,
    "upcomingDepartures": 1,
    "recommendedAction": "lock-departure-window",
    "lastUpdatedAt": "2026-04-06T10:00:00Z",
    "latestPlan": {
      "id": "plan_1",
      "cityId": "bangkok",
      "cityName": "Bangkok"
    },
    "plans": []
  },
  "budgetCenter": {
    "monthlyBudgetTargetUsd": 1800,
    "forecastMonthlyCostUsd": 1650,
    "deltaUsd": 150,
    "activePlanCount": 1,
    "trackedCityCount": 1,
    "budgetHealth": "on_track",
    "recommendedAction": "finalize-budget-baseline",
    "lastUpdatedAt": "2026-04-06T10:00:00Z",
    "focusPlan": {},
    "plans": []
  },
  "visaCenter": {
    "activeProfileCount": 1,
    "attentionRequiredCount": 0,
    "reminderReadyCount": 1,
    "recommendedAction": "review-latest-visa",
    "lastUpdatedAt": "2026-04-06T10:00:00Z",
    "focusProfile": {},
    "profiles": []
  },
  "focusTravelPlan": {
    "id": "plan_1",
    "cityId": "bangkok",
    "cityName": "Bangkok",
    "transportation": {},
    "accommodation": {}
  },
  "lastUpdatedAt": "2026-04-06T10:00:00Z"
}
```

#### 契约要求

- migrationWorkspace / budgetCenter / visaCenter 字段结构直接复用现有 Flutter 已消费 DTO。
- focusTravelPlan 复用 Travel Plan detail 返回结构，避免再定义第二套明细模型。
- 新增字段保持可空，客户端允许 migrationWorkspace / budgetCenter / visaCenter / focusTravelPlan 单项为空。

### GET /api/v1/visa/rules/{countryCode}

#### Response 200

```json
{
  "countryCode": "TH",
  "countryName": "Thailand",
  "lastUpdatedAt": "2026-04-01T12:00:00Z",
  "supportedVisaTypes": [
    {
      "type": "tourist",
      "maxStayDays": 60,
      "extendable": true,
      "summary": "Tourist visa allows up to 60 days stay"
    }
  ],
  "checklist": [
    "Passport valid for 6 months",
    "Proof of onward travel"
  ],
  "disclaimer": "Policy may change. Verify with official source before departure."
}
```

#### 错误语义

- 404: 该国家暂无规则数据
- 502/503: 上游政策源不可用

## 11. Profile Snapshot API

### GET /api/v1/profile-snapshot/current

#### 用途

- 为 Me/Profile 首屏提供单次聚合读取能力。
- 统一返回用户资料、会员快照、Nomad 统计、收藏城市、最新旅行计划和下一站城市摘要，减少页面首屏与路由恢复时的多源请求。

#### 当前客户端拼装来源

- UserService: GET /api/v1/users/me
- UserService: GET /api/v1/users/me/stats
- CityService: GET /api/v1/user-favorite-cities/ids
- AIService: GET /api/v1/ai/chat/travel-plans?page=1&pageSize=1
- CityService: 基于 latestTravelPlan.cityId 获取城市详情

#### 服务边界建议

- UserService 作为该切片的主聚合层，负责输出 Profile Snapshot。
- 用户资料与会员快照优先复用现有 UserDto 的字段结构，避免新增第二套用户模型。
- UserService 通过内部服务调用获取 favoriteCityIds 与 nextDestinationCity，避免客户端为此再发额外请求。
- UserService 通过内部服务调用 AIService 获取 latestTravelPlan 摘要，不直接跨项目引用 AIService DTO。

#### Response 200

```json
{
  "user": {
    "id": "user_1",
    "name": "Walden",
    "avatarUrl": "https://example.com/avatar.png",
    "bio": "Remote product builder",
    "currentCity": "Bangkok",
    "skills": [],
    "interests": [],
    "socialLinks": {},
    "membership": {
      "level": 1,
      "levelName": "basic",
      "expiryDate": "2026-12-31T00:00:00Z",
      "isActive": true,
      "aiUsageThisMonth": 4,
      "aiUsageLimit": 30,
      "remainingDays": 180,
      "canUseAI": true,
      "canApplyModerator": false
    }
  },
  "nomadStats": {
    "countriesVisited": 6,
    "citiesLived": 4,
    "daysNomading": 280,
    "tripsCompleted": 9,
    "meetupsCreated": 2,
    "meetupsJoined": 5,
    "favoriteCitiesCount": 8
  },
  "favoriteCityIds": [
    "bangkok",
    "lisbon"
  ],
  "latestTravelPlan": {
    "id": "plan_1",
    "cityId": "bangkok",
    "cityName": "Bangkok",
    "duration": 30,
    "budgetLevel": "medium",
    "travelStyle": "culture",
    "status": "planning",
    "departureDate": "2026-05-01T00:00:00Z"
  },
  "nextDestinationCity": {
    "id": "bangkok",
    "name": "Bangkok",
    "country": "Thailand",
    "timezone": "Asia/Bangkok"
  },
  "lastUpdatedAt": "2026-04-06T10:00:00Z"
}
```

#### 契约要求

- user 字段优先复用现有 GET /api/v1/users/me 的返回结构，membership 继续内嵌在 user 下。
- nomadStats 优先复用现有 GET /api/v1/users/me/stats 的字段命名，避免客户端为同一统计信息维护两套解析逻辑。
- favoriteCityIds 即使为空也返回空数组，不省略字段。
- latestTravelPlan 仅返回摘要字段；Profile 首屏不需要完整 Travel Plan detail。
- nextDestinationCity 允许为 null；当用户没有旅行计划或 CityService 下游失败时，不应导致整个 Profile Snapshot 失败。

#### 风险与降级

- 若 AIService latestTravelPlan 下游失败，可返回 latestTravelPlan = null 与 nextDestinationCity = null，同时保留 user / nomadStats / favoriteCityIds。
- 若 CityService favorite ids 下游失败，允许返回空数组并在日志中标记 degradedSources。
- 本切片只收口首屏数据来源，不下沉 Profile 页面现有 focus route 推导和文案拼装逻辑。

#### 实现状态

- 已完成: UserService 新增 ProfileSnapshotController 与 ProfileSnapshotService，暴露 GET /api/v1/profile-snapshot/current。
- 已完成: 聚合层复用现有 UserDto，并在 Application 层组合 user stats、favorite city ids、latest travel plan 与 next destination city。
- 已完成: 对 AIService 与 CityService 下游调用显式透传 X-User-Id，并在 latestTravelPlan / nextDestinationCity 缺失时返回部分成功响应。
- 已完成: Flutter Me 页面改为通过 ProfileSnapshotRepository 单次读取该接口，Profile 首屏不再并发拉 users/me、users/me/stats、favorite city ids 与 latest travel plan。
- 已验证: `dotnet build src/Services/UserService/UserService/UserService.csproj` 成功。
- 已验证: `dotnet test tests/UserService.Tests/UserService.Tests.csproj --filter FullyQualifiedName~ProfileSnapshotServiceTests` 成功，覆盖完整聚合与 AIService 失败降级场景。
- 已验证: `flutter analyze` 覆盖 Profile Snapshot Flutter 接入文件，无静态问题。

## 12. Community Snapshot API

### GET /api/v1/community-snapshot/current

#### 用途

- 为 Community 首屏提供单次聚合读取能力。
- 统一返回当前焦点城市、upcoming meetups、field notes、questions 与 recommendations，使 Community 首页的 intelligence feed 不再依赖客户端 mock，也不必在页面层手工拼 EventService 与 CityService 数据。

#### 当前客户端拼装来源

- EventService: GET /api/v1/events?status=upcoming&page=1&pageSize=3
- UserService: GET /api/v1/users/me
- AIService: GET /api/v1/ai/chat/travel-plans?page=1&pageSize=1
- CityService: 基于 focus city 调用 GET /api/v1/cities/{cityId}/user-content/reviews?page=1&pageSize=3

#### 服务边界建议

- AIService 作为该切片的主聚合层，负责输出 Community Snapshot。
- focusCity 与 nextCoordinationCity 优先复用当前用户 currentCity 与 latestTravelPlan 的现有语义，不额外发明新的“社区上下文”模型。
- upcomingMeetups 继续复用 EventService EventResponse，聚合层只裁剪 Community 首屏需要的字段。
- fieldNotes 复用 CityService 的 UserCityReviewDto，并在 Application 层映射为 Community 页面使用的 trip report 摘要模型。
- questions 继续留在 AIService 聚合层实现，优先由 CityService 的 reviews 与 pros-cons 映射为 Community intelligence threads，并把 answers 作为 question payload 的内嵌聚合结果返回。
- recommendations 继续留在 AIService 聚合层实现，优先由 CityService 的 pros-cons 与 field notes 映射生成，不新增独立 recommendation 域。
- 本切片不新增独立 question/answer 写模型；问题创建、回答发布、投票持久化仍留待后续专门切片处理。

#### Response 200

```json
{
  "focusCity": "Bangkok",
  "nextCoordinationCity": "Bangkok",
  "upcomingMeetups": [
    {
      "id": "meetup_1",
      "title": "Friday Cowork Sprint",
      "cityId": "bangkok",
      "cityName": "Bangkok",
      "venue": "The Work Loft",
      "startTime": "2026-04-08T09:00:00Z",
      "participantCount": 12,
      "maxParticipants": 20,
      "isJoined": true
    }
  ],
  "fieldNotes": [
    {
      "id": "review_1",
      "userId": "user_2",
      "userName": "Nomad Ada",
      "userAvatar": "https://example.com/avatar.png",
      "city": "Bangkok",
      "country": "Thailand",
      "title": "One month in Ari",
      "content": "Quiet mornings, reliable Wi-Fi, easy BTS access.",
      "overallRating": 4.0,
      "ratings": {
        "internet": 5.0,
        "safety": 4.0,
        "cost": 4.0,
        "community": 4.0,
        "weather": 3.0
      },
      "photos": [],
      "likes": 0,
      "comments": 0,
      "createdAt": "2026-04-05T10:00:00Z"
    }
  ],
  "questions": [
    {
      "id": "question-review-1",
      "userId": "user_2",
      "userName": "Nomad Ada",
      "userAvatar": "https://example.com/avatar.png",
      "city": "Bangkok",
      "title": "One month in Ari",
      "content": "Quiet mornings, reliable Wi-Fi, easy BTS access.",
      "tags": ["internet", "safety", "field-note"],
      "upvotes": 0,
      "answerCount": 3,
      "hasAcceptedAnswer": true,
      "createdAt": "2026-04-05T10:00:00Z",
      "isUpvoted": false,
      "answers": [
        {
          "id": "question-review-1-rating-internet",
          "questionId": "question-review-1",
          "userId": "system-community-signal",
          "userName": "Community signal",
          "content": "This review highlights internet at 5/5 for Bangkok.",
          "upvotes": 1,
          "isAccepted": true,
          "createdAt": "2026-04-05T10:00:00Z",
          "isUpvoted": false
        }
      ]
    }
  ],
  "recommendations": [
    {
      "id": "recommendation-signal-1",
      "city": "Bangkok",
      "name": "Ari has enough cowork-friendly cafes to keep a full work week...",
      "category": "Activity",
      "description": "Ari has enough cowork-friendly cafes to keep a full work week moving without changing neighborhoods.",
      "rating": 4.0,
      "reviewCount": 9,
      "priceRange": null,
      "address": null,
      "photos": [],
      "website": null,
      "tags": ["community-signal", "local-tip"],
      "userId": "system-signal-1",
      "userName": "Community signal",
      "userAvatar": null
    }
  ],
  "lastUpdatedAt": "2026-04-06T10:00:00Z"
}
```

#### 契约要求

- focusCity 必须稳定返回字符串；当当前用户与最新旅行计划都缺失城市上下文时，可回退到最近一个 upcoming meetup 的城市，再次缺失时返回空字符串。
- nextCoordinationCity 允许为 null；Community 首屏可以回退到 focusCity 或客户端现有文案。
- upcomingMeetups、fieldNotes、questions 与 recommendations 即使为空也返回空数组，不省略字段。
- fieldNotes 中的 ratings 字段允许为空键缺失，但 overallRating、title、content、createdAt 必须始终可解析。
- `questions[].answers` 允许为空数组，但 question detail 不应依赖第二个 answers endpoint。
- system-generated answers 允许使用 `system-*` userId 返回社区信号，调用方应避免把这类 answer author 暴露成可私聊对象。

#### 风险与降级

- 若 EventService 下游失败，可返回 upcomingMeetups = []，同时保留 fieldNotes。
- 若 CityService review 下游失败，可返回 fieldNotes = []，同时保留 upcomingMeetups。
- 若 CityService pros-cons 下游失败，可继续返回 fieldNotes 与基于 ratings 的 questions，recommendations 允许为空数组。
- 若 latestTravelPlan / users/me 获取失败，focusCity 允许降级为 meetup city 或空字符串，但不应导致整个接口失败。

#### 实现状态

- 已完成: AIService 新增 `GET /api/v1/community-snapshot/current`，聚合 EventService upcoming meetups 与 CityService field notes，并支持下游部分失败降级。
- 已完成: Flutter Community 页面已使用该接口填充 field notes，移除 trip reports 的 mock 数据依赖。
- 已完成: AIService 继续在同一接口中聚合 CityService pros-cons，并映射返回 `questions`、内嵌 `answers` 与 `recommendations`，不新增独立 Q&A 写模型。
- 已完成: Flutter Community 页面已使用该接口填充 questions、recommendations 与 question detail answers，移除 intelligence feed 的 mock 数据依赖。
- 已完成: Community 首页 meetup preview 已切到 Community Snapshot，同一聚合口现已覆盖首页 meetup / field notes / questions / recommendations 四类数据。
- 保持不变: 独立 Meetup 列表、详情页、SignalR 与 RSVP 主链路仍沿用现有 `MeetupStateController` / MeetupRepository。
- 已验证: `dotnet build src/Services/AIService/AIService/AIService.csproj` 成功。
- 已验证: `dotnet test tests/AIService.Tests/AIService.Tests.csproj --filter AIChatApplicationServiceCommunitySnapshotTests` 成功，2 个测试通过，覆盖完整聚合、pros-cons 映射与 EventService 失败降级场景。
- 已验证: `flutter analyze lib/features/community/infrastructure/repositories/community_repository.dart lib/pages/community_page.dart` 无静态问题。

## 13. Shared DTO Notes

### 命名建议

- MigrationSummaryDto
- MigrationWorkspaceDto
- BudgetSnapshotDto
- BudgetForecastDto
- VisaProfileDto
- VisaRuleSummaryDto
- InboxSummaryDto
- CityNomadSummaryDto

### 审计字段

所有 P0 新资源建议至少包含：

- createdAt
- updatedAt
- createdBy 或 accountId

### 鉴权要求

- 所有用户级资源必须从当前认证上下文推导 accountId，禁止信任客户端透传 userId 作为唯一归属依据。

## 14. Observability

### 日志建议

- Migration Workspace 读写：accountId、migrationId、stage、cityId、result
- Budget 写入：accountId、budgetId、currency、monthlyBudget、result
- Visa 写入：accountId、visaProfileId、countryCode、expiryDate、result
- Inbox Summary 聚合：accountId、dependencyLatency、degradedSources
- Explore Dashboard 聚合：accountId、messageServiceLatency、downstreamFailures、partialResponseFields
- Profile Snapshot 聚合：accountId、aiServiceLatency、cityServiceLatency、downstreamFailures、partialResponseFields
- Community Snapshot 聚合：accountId、focusCitySource、eventServiceLatency、cityReviewLatency、cityProsConsLatency、downstreamFailures、partialResponseFields

### 最小诊断能力

- 聚合接口建议在日志里标记哪些下游成功、哪些下游降级。
- 预算和签证接口失败时应能通过日志快速定位 accountId 与请求参数。

## 15. Validation

### 最小验证要求

- DTO 序列化/反序列化单测
- Controller / Endpoint 合同测试
- 404 / 409 / 422 语义测试
- 兼容旧客户端时的可空字段验证
- Explore Dashboard 已完成 message-service 下游失败时的部分成功响应验证
- Community Snapshot 至少需要覆盖 EventService 失败、CityService review 失败与 CityService pros-cons 失败时的部分成功响应验证

### 未验证风险

- Migration Workspace 若直接复用旧 TravelPlan 数据表，可能引入语义污染。
- Visa 规则数据源若未稳定，会先影响规则接口可信度。
- Budget Forecast 若依赖城市成本聚合质量，首版结果可能波动较大。
- Profile Snapshot 若直接复用多个现有 DTO，需额外确认 user / stats / travel plan 的字段可空策略在 Flutter 端完全兼容。
- Community Snapshot 若同时承接 meetup、field notes 与 community signals，需额外确认 focusCity 的降级来源不会造成 Community 首页上下文跳变。

## 16. Rollback Strategy

- 所有新增接口都为增量接口，不替换现有核心接口。
- 新字段默认 optional，客户端按缺失字段降级。
- 若某个聚合接口不稳定，可先在客户端回退为多接口拉取，不影响既有城市、通知、聊天主流程。

## 17. Delivery Summary

- 本文档定义了 go-nomads-app P0 升级所需的最小后端接口边界。
- Explore Dashboard、Land Hub、Profile Snapshot、Community Snapshot v2 与 City Nomad Summary v1 已完成当前阶段的文档驱动闭环。
- 推荐后续实现顺序：Community Q&A 写模型与互动持久化 -> 独立 Meetup 模块与 Community Snapshot 的参与态统一。
- Community Snapshot v2 已消除 Community field notes、questions 与 recommendations 的 mock 数据依赖；meetups 聚合接口已具备，但仍需在参与态语义稳定后再切客户端数据源。
- City Nomad Summary v1 已将 City Detail 决策面板的 budget 与 decision signals 收口到后端聚合；城市详情页其余 guide / tabs / 独立列表页仍保留现有数据链路。

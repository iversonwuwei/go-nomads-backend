BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS public.legal_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_type TEXT NOT NULL,
    version TEXT NOT NULL DEFAULT '1.0.0',
    language TEXT NOT NULL DEFAULT 'zh',
    title TEXT NOT NULL,
    effective_date TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_current BOOLEAN NOT NULL DEFAULT true,
    sections JSONB NOT NULL DEFAULT '[]'::jsonb,
    summary JSONB NOT NULL DEFAULT '[]'::jsonb,
    sdk_list JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_legal_documents_type_lang_version
    ON public.legal_documents (document_type, language, version);

CREATE INDEX IF NOT EXISTS idx_legal_documents_current
    ON public.legal_documents (document_type, language, is_current)
    WHERE is_current = true;

ALTER TABLE public.legal_documents ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_policies
        WHERE schemaname = 'public'
          AND tablename = 'legal_documents'
          AND policyname = 'legal_documents_public_read'
    ) THEN
        CREATE POLICY legal_documents_public_read ON public.legal_documents
            FOR SELECT USING (true);
    END IF;
END $$;

ALTER TABLE public.user_preferences
    ADD COLUMN IF NOT EXISTS privacy_policy_accepted BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS privacy_policy_accepted_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS terms_of_service_accepted BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS terms_of_service_accepted_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS privacy_policy_accepted_version TEXT NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS terms_of_service_accepted_version TEXT NOT NULL DEFAULT '';

UPDATE public.legal_documents
SET is_current = false,
    updated_at = now()
WHERE document_type IN ('privacy-policy', 'terms-of-service')
  AND language IN ('zh', 'en');

INSERT INTO public.legal_documents (
    document_type,
    version,
    language,
    title,
    effective_date,
    is_current,
    sections,
    summary,
    sdk_list,
    created_at,
    updated_at
) VALUES (
    'privacy-policy',
    '1.0.0',
    'zh',
    '隐私政策',
    '2026-03-23T00:00:00Z',
    true,
    $$[
      {
        "title": "一、我们收集的信息",
        "content": "为了向您提供城市探索、共享办公、住宿、活动报名、社交互动与账户安全等服务，我们可能收集您主动提供的信息，包括手机号、邮箱、昵称、头像、登录凭证、常驻城市、偏好设置，以及您在平台中发布、报名、收藏、评论和聊天时提交的内容。"
      },
      {
        "title": "二、我们如何使用信息",
        "content": "我们将您的信息用于账户注册与登录、身份校验、活动与 meetup 报名、订单履约、消息通知、推荐更相关的城市与空间内容、优化产品体验、处理投诉与风控，以及依法履行合规义务。未经您的同意，我们不会将您的个人信息用于与本政策未说明的用途。"
      },
      {
        "title": "三、设备权限与敏感信息",
        "content": "在获得您授权后，我们可能访问位置、相册、相机、通知、存储等权限，用于定位附近地点、上传头像或活动图片、保存海报、接收通知等功能。您可以在系统设置中关闭相关权限，但这可能导致部分功能无法正常使用。"
      },
      {
        "title": "四、信息共享与第三方服务",
        "content": "为了实现登录、地图、即时通讯、支付或推送等功能，我们可能接入第三方 SDK 或服务。我们仅会在实现业务功能所必需的范围内共享必要信息，并要求合作方依法采取保护措施。第三方对信息的处理受其自身隐私政策约束。"
      },
      {
        "title": "五、信息存储与保护",
        "content": "我们会在实现本政策所述目的所需的最短期限内保存您的信息，并通过访问控制、加密传输、日志审计、最小权限等措施保护数据安全。尽管如此，互联网传输并非绝对安全，请您妥善保管账号与验证码信息。"
      },
      {
        "title": "六、您的权利与联系我们",
        "content": "您可以依法访问、更正、删除个人信息，撤回授权，注销账户，或就隐私保护问题与我们联系。若您对本政策有疑问、意见或投诉，可通过应用内反馈、客服渠道或官方邮箱联系 Go Nomads（行途）团队。"
      }
    ]$$::jsonb,
    $$[
      {
        "icon": "shield",
        "title": "账号与安全",
        "content": "用于完成注册登录、身份识别和账户安全保护。"
      },
      {
        "icon": "location_on",
        "title": "位置与推荐",
        "content": "用于展示附近城市、共享办公空间、活动和本地化内容。"
      },
      {
        "icon": "forum",
        "title": "社交与消息",
        "content": "用于 meetup 报名、聊天互动、通知提醒和社区运营。"
      },
      {
        "icon": "settings",
        "title": "服务优化",
        "content": "用于故障排查、性能分析、风控治理和体验改进。"
      }
    ]$$::jsonb,
    $$[
      {
        "name": "Supabase",
        "company": "Supabase, Inc.",
        "purpose": "用户认证、数据库存储与文件服务",
        "dataCollected": ["账号标识", "会话信息", "业务数据"],
        "privacyUrl": "https://supabase.com/privacy"
      },
      {
        "name": "Google Maps Platform",
        "company": "Google LLC",
        "purpose": "地图展示、地点搜索与地理编码",
        "dataCollected": ["位置信息", "设备信息", "搜索关键词"],
        "privacyUrl": "https://policies.google.com/privacy"
      },
      {
        "name": "Tencent Cloud Chat",
        "company": "深圳市腾讯计算机系统有限公司",
        "purpose": "即时通讯与消息能力",
        "dataCollected": ["账号标识", "设备信息", "消息元数据"],
        "privacyUrl": "https://privacy.qq.com/"
      }
    ]$$::jsonb,
    now(),
    now()
) ON CONFLICT (document_type, language, version)
DO UPDATE SET
    title = EXCLUDED.title,
    effective_date = EXCLUDED.effective_date,
    is_current = EXCLUDED.is_current,
    sections = EXCLUDED.sections,
    summary = EXCLUDED.summary,
    sdk_list = EXCLUDED.sdk_list,
    updated_at = now();

INSERT INTO public.legal_documents (
    document_type,
    version,
    language,
    title,
    effective_date,
    is_current,
    sections,
    summary,
    sdk_list,
    created_at,
    updated_at
) VALUES (
    'privacy-policy',
    '1.0.0',
    'en',
    'Privacy Policy',
    '2026-03-23T00:00:00Z',
    true,
    $$[
      {
        "title": "1. Information We Collect",
        "content": "To provide city discovery, coworking, accommodation, meetup participation, social interaction, and account security features, we may collect information you provide directly, including your phone number, email, nickname, avatar, login credentials, preferred city, settings, and the content you submit through posts, registrations, bookmarks, comments, and chats."
      },
      {
        "title": "2. How We Use Information",
        "content": "We use your information to register and authenticate your account, verify identity, process meetup registrations and orders, send notifications, recommend relevant cities and spaces, improve product experience, handle complaints and fraud prevention, and comply with legal obligations. We will not use your personal information for purposes not described in this policy without your consent."
      },
      {
        "title": "3. Permissions and Sensitive Data",
        "content": "With your authorization, we may access location, photos, camera, notifications, and storage permissions to support nearby recommendations, image uploads, poster saving, and message alerts. You can disable these permissions in your device settings, but some features may become unavailable."
      },
      {
        "title": "4. Sharing and Third-Party Services",
        "content": "To support login, maps, instant messaging, payments, or push notifications, we may integrate third-party SDKs or services. We only share the minimum necessary information required for the relevant function and require partners to protect data in accordance with applicable laws. Their handling of information is governed by their own privacy policies."
      },
      {
        "title": "5. Storage and Protection",
        "content": "We retain your information only for the minimum period necessary to fulfill the purposes described in this policy and protect it with measures such as access control, encrypted transmission, audit logs, and least-privilege access. However, no internet transmission is absolutely secure, so please keep your account credentials and verification codes safe."
      },
      {
        "title": "6. Your Rights and Contact",
        "content": "You may access, correct, or delete your personal information, withdraw consent, close your account, or contact us regarding privacy questions in accordance with applicable laws. If you have any questions, suggestions, or complaints, please contact the Go Nomads team through in-app feedback, customer support channels, or our official email."
      }
    ]$$::jsonb,
    $$[
      {
        "icon": "shield",
        "title": "Account and Security",
        "content": "Used for registration, authentication, and account protection."
      },
      {
        "icon": "location_on",
        "title": "Location and Recommendations",
        "content": "Used to show nearby cities, coworking spaces, events, and localized content."
      },
      {
        "icon": "forum",
        "title": "Social and Messaging",
        "content": "Used for meetup participation, chat interaction, notifications, and community operations."
      },
      {
        "icon": "settings",
        "title": "Service Improvement",
        "content": "Used for troubleshooting, analytics, fraud prevention, and product optimization."
      }
    ]$$::jsonb,
    $$[
      {
        "name": "Supabase",
        "company": "Supabase, Inc.",
        "purpose": "Authentication, database storage, and file services",
        "dataCollected": ["account identifiers", "session information", "business data"],
        "privacyUrl": "https://supabase.com/privacy"
      },
      {
        "name": "Google Maps Platform",
        "company": "Google LLC",
        "purpose": "Map rendering, place search, and geocoding",
        "dataCollected": ["location information", "device information", "search keywords"],
        "privacyUrl": "https://policies.google.com/privacy"
      },
      {
        "name": "Tencent Cloud Chat",
        "company": "Tencent",
        "purpose": "Instant messaging capabilities",
        "dataCollected": ["account identifiers", "device information", "message metadata"],
        "privacyUrl": "https://privacy.qq.com/"
      }
    ]$$::jsonb,
    now(),
    now()
) ON CONFLICT (document_type, language, version)
DO UPDATE SET
    title = EXCLUDED.title,
    effective_date = EXCLUDED.effective_date,
    is_current = EXCLUDED.is_current,
    sections = EXCLUDED.sections,
    summary = EXCLUDED.summary,
    sdk_list = EXCLUDED.sdk_list,
    updated_at = now();

INSERT INTO public.legal_documents (
    document_type,
    version,
    language,
    title,
    effective_date,
    is_current,
    sections,
    summary,
    sdk_list,
    created_at,
    updated_at
) VALUES (
    'terms-of-service',
    '1.0.0',
    'zh',
    '用户协议',
    '2026-03-23T00:00:00Z',
    true,
    $$[
      {
        "title": "1. 接受条款",
        "content": "使用 Go Nomads（行途）即表示您已阅读、理解并同意遵守本服务条款及平台规则；如您不同意，请立即停止使用本应用。"
      },
      {
        "title": "2. 账号与安全",
        "content": "您需提供真实、准确、完整的注册信息，并妥善保管账号与登录凭证。因您保管不善导致的风险由您自行承担；如发现异常登录或疑似盗用，请及时联系平台。"
      },
      {
        "title": "3. 社区内容与发布规范",
        "content": "您在城市、共享办公、创新项目、活动、评论、聊天等模块发布的内容应合法、真实、文明，不得侵犯他人权益或违反法律法规。平台有权对违规内容采取删除、限制展示、封禁等处理措施。"
      },
      {
        "title": "4. 功能与服务范围",
        "content": "平台为数字游民提供城市信息、空间与住宿发现、活动组织、社交互动、旅行规划等服务。具体功能可能因版本、地区、政策或运营安排而调整。"
      },
      {
        "title": "5. 付费与会员服务",
        "content": "如您购买会员或其他付费服务，应按页面提示完成支付并遵守对应规则。具体权益、价格、有效期、退款条件以购买页面或订单说明为准。"
      },
      {
        "title": "6. 安全与风险提示",
        "content": "线下活动、住宿、共享办公等场景可能存在人身与财产风险。请您独立判断并注意安全。对于第三方服务主体的履约行为，平台在法律允许范围内提供协助但不承担超出法定范围的责任。"
      },
      {
        "title": "7. 知识产权",
        "content": "平台软件、界面设计、商标、标识、文案与素材（用户依法享有权利的内容除外）受法律保护。未经授权，不得复制、改编、传播或用于商业用途。"
      },
      {
        "title": "8. 账号管理与终止",
        "content": "若您违反本条款或平台规范，平台有权视情节采取警告、限制功能、暂停服务或终止账号等措施。您也可依据平台流程申请注销账号。"
      },
      {
        "title": "9. 条款更新",
        "content": "平台可能根据业务发展或法律法规变化更新本条款，并通过应用内公告、页面提示等方式告知。更新后您继续使用服务的，视为接受更新内容。"
      },
      {
        "title": "10. 联系我们",
        "content": "如您对本条款有任何疑问、建议或投诉，可通过应用内反馈、客服渠道或官方邮箱与我们联系。"
      }
    ]$$::jsonb,
    $$[
      {
        "icon": "gavel",
        "title": "规则接受",
        "content": "注册、登录或继续使用服务，即表示您接受本协议及平台规则。"
      },
      {
        "icon": "person",
        "title": "账号责任",
        "content": "您需提供真实信息并妥善保管账号，异常情况应及时通知平台。"
      },
      {
        "icon": "groups",
        "title": "社区与线下行为",
        "content": "发布内容、参加活动和线下互动时，应遵守法律法规及社区规范。"
      },
      {
        "icon": "warning",
        "title": "责任边界",
        "content": "第三方服务履约及不可抗力风险，按照法律规定和协议约定处理。"
      }
    ]$$::jsonb,
    '[]'::jsonb,
    now(),
    now()
) ON CONFLICT (document_type, language, version)
DO UPDATE SET
    title = EXCLUDED.title,
    effective_date = EXCLUDED.effective_date,
    is_current = EXCLUDED.is_current,
    sections = EXCLUDED.sections,
    summary = EXCLUDED.summary,
    sdk_list = EXCLUDED.sdk_list,
    updated_at = now();

INSERT INTO public.legal_documents (
    document_type,
    version,
    language,
    title,
    effective_date,
    is_current,
    sections,
    summary,
    sdk_list,
    created_at,
    updated_at
) VALUES (
    'terms-of-service',
    '1.0.0',
    'en',
    'Terms of Service',
    '2026-03-23T00:00:00Z',
    true,
    $$[
      {
        "title": "1. Acceptance of Terms",
        "content": "By using Go Nomads, you confirm that you have read, understood, and agreed to these Terms and related platform rules. If you do not agree, please stop using the app."
      },
      {
        "title": "2. Account and Security",
        "content": "You must provide accurate and complete registration information and keep your account credentials secure. You are responsible for risks caused by improper account protection, and should contact us promptly if abnormal access is detected."
      },
      {
        "title": "3. Community Content and Publishing Rules",
        "content": "Content posted in city, coworking, innovation, event, comment, and chat modules must be lawful, truthful, and respectful. The platform may remove or restrict violating content and take account-level actions when necessary."
      },
      {
        "title": "4. Scope of Services",
        "content": "The platform provides services including city information, coworking and accommodation discovery, event organization, social interaction, and travel planning. Specific features may vary by version, region, policy, or operational needs."
      },
      {
        "title": "5. Paid Services and Membership",
        "content": "If you purchase membership or paid services, payment and usage must follow the on-screen rules. Benefits, pricing, validity, and refund conditions are subject to the product and order details shown in the app."
      },
      {
        "title": "6. Safety and Risk Notice",
        "content": "Offline activities, accommodation, and coworking scenarios involve potential personal and property risks. Please make independent judgments and take appropriate precautions. For third-party service performance, the platform provides reasonable assistance within legal limits."
      },
      {
        "title": "7. Intellectual Property",
        "content": "The platform software, interface design, trademarks, identifiers, copy, and media assets (excluding user-owned lawful content) are legally protected. Unauthorized copying, adaptation, distribution, or commercial use is prohibited."
      },
      {
        "title": "8. Account Restriction and Termination",
        "content": "If you violate these Terms or platform rules, we may issue warnings, limit features, suspend services, or terminate your account depending on the severity of the violation. You may also request account closure through platform procedures."
      },
      {
        "title": "9. Updates to Terms",
        "content": "These Terms may be updated due to business or regulatory changes. We will notify users through in-app announcements or page notices. Continued use after updates means acceptance of the revised Terms."
      },
      {
        "title": "10. Contact Us",
        "content": "For questions, suggestions, or complaints regarding these Terms, please contact us via in-app feedback, customer support channels, or our official email."
      }
    ]$$::jsonb,
    $$[
      {
        "icon": "gavel",
        "title": "Acceptance of Rules",
        "content": "By registering, signing in, or continuing to use the service, you accept these Terms and platform rules."
      },
      {
        "icon": "person",
        "title": "Account Responsibility",
        "content": "You must provide accurate information and keep your account secure, and promptly notify the platform of anomalies."
      },
      {
        "icon": "groups",
        "title": "Community and Offline Conduct",
        "content": "You must comply with law and community standards when posting content, joining events, and participating offline."
      },
      {
        "icon": "warning",
        "title": "Liability Boundary",
        "content": "Third-party performance and force majeure risks are handled according to law and these Terms."
      }
    ]$$::jsonb,
    '[]'::jsonb,
    now(),
    now()
) ON CONFLICT (document_type, language, version)
DO UPDATE SET
    title = EXCLUDED.title,
    effective_date = EXCLUDED.effective_date,
    is_current = EXCLUDED.is_current,
    sections = EXCLUDED.sections,
    summary = EXCLUDED.summary,
    sdk_list = EXCLUDED.sdk_list,
    updated_at = now();

COMMIT;

-- 验证查询
-- select document_type, language, version, is_current, effective_date
-- from public.legal_documents
-- order by document_type, language, effective_date desc;
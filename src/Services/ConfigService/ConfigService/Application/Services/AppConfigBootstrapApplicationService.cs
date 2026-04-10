using System.Text.Json;
using ConfigService.Application.DTOs;
using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;

namespace ConfigService.Application.Services;

public class AppConfigBootstrapApplicationService : IAppConfigBootstrapService
{
    private static readonly Guid BootstrapUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly IStaticTextRepository _staticTextRepository;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IConfigSnapshotRepository _configSnapshotRepository;
    private readonly IConfigPublishService _configPublishService;
    private readonly ILogger<AppConfigBootstrapApplicationService> _logger;

    public AppConfigBootstrapApplicationService(
        IStaticTextRepository staticTextRepository,
        ISystemSettingRepository systemSettingRepository,
        IConfigSnapshotRepository configSnapshotRepository,
        IConfigPublishService configPublishService,
        ILogger<AppConfigBootstrapApplicationService> logger)
    {
        _staticTextRepository = staticTextRepository;
        _systemSettingRepository = systemSettingRepository;
        _configSnapshotRepository = configSnapshotRepository;
        _configPublishService = configPublishService;
        _logger = logger;
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        var currentStaticTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var currentSettingValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hasDataChanges = false;

        foreach (var definition in GetStaticTextSeeds())
        {
            var result = await EnsureStaticTextAsync(definition);
            currentStaticTexts[$"{definition.Locale}:{definition.TextKey}"] = result.CurrentValue;
            hasDataChanges |= result.Changed;
        }

        foreach (var definition in GetSystemSettingSeeds())
        {
            var result = await EnsureSystemSettingAsync(definition, cancellationToken);
            currentSettingValues[$"{definition.Section}:{definition.SettingKey}"] = result.CurrentValue;
            hasDataChanges |= result.Changed;
        }

        if (!await ShouldPublishAsync(currentStaticTexts, currentSettingValues, hasDataChanges))
        {
            _logger.LogInformation("app/config bootstrap 已检查完成，当前快照已包含所需 legal 配置，无需重新发布");
            return;
        }

        var snapshot = await _configPublishService.PublishAsync(BootstrapUserId);
        _logger.LogInformation("app/config bootstrap 已发布快照 v{Version}", snapshot.Version);
    }

    private async Task<(bool Changed, string CurrentValue)> EnsureStaticTextAsync(AppStaticTextSeed definition)
    {
        var existing = await _staticTextRepository.GetByKeyAndLocaleAsync(definition.TextKey, definition.Locale);
        if (existing == null)
        {
            var entity = new StaticText
            {
                TextKey = definition.TextKey,
                Locale = definition.Locale,
                TextValue = definition.TextValue,
                Category = definition.Category,
                Description = definition.Description,
                IsActive = true,
                Version = 1,
                CreatedBy = BootstrapUserId,
                UpdatedBy = BootstrapUserId
            };

            await _staticTextRepository.CreateAsync(entity);
            _logger.LogInformation("补齐 app static text: {Key} [{Locale}]", definition.TextKey, definition.Locale);
            return (true, definition.TextValue);
        }

        var changed = false;
        var textValueChanged = false;

        if (!existing.IsActive)
        {
            existing.IsActive = true;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(existing.TextValue))
        {
            existing.TextValue = definition.TextValue;
            changed = true;
            textValueChanged = true;
        }
        else if (IsBootstrapManagedStaticText(existing) &&
                 !string.Equals(existing.TextValue, definition.TextValue, StringComparison.Ordinal))
        {
            existing.TextValue = definition.TextValue;
            changed = true;
            textValueChanged = true;
        }

        if (!string.Equals(existing.Category, definition.Category, StringComparison.Ordinal))
        {
            existing.Category = definition.Category;
            changed = true;
        }

        if (!string.Equals(existing.Description, definition.Description, StringComparison.Ordinal))
        {
            existing.Description = definition.Description;
            changed = true;
        }

        if (!changed)
        {
            return (false, existing.TextValue);
        }

        existing.Version = Math.Max(existing.Version, 1) + 1;
        existing.UpdatedBy = BootstrapUserId;
        await _staticTextRepository.UpdateAsync(existing);
        _logger.LogInformation(
            textValueChanged
                ? "修复 app static text 默认值与元数据: {Key} [{Locale}]"
                : "修复 app static text 元数据: {Key} [{Locale}]",
            definition.TextKey,
            definition.Locale);
        return (true, existing.TextValue);
    }

    private static bool IsBootstrapManagedStaticText(StaticText existing)
    {
        if (existing.CreatedBy != BootstrapUserId)
        {
            return false;
        }

        return existing.UpdatedBy == null || existing.UpdatedBy == BootstrapUserId;
    }

    private async Task<(bool Changed, string CurrentValue)> EnsureSystemSettingAsync(
        AppSystemSettingSeed definition,
        CancellationToken cancellationToken)
    {
        var existing = await _systemSettingRepository.GetByKeyAsync(definition.Section, definition.SettingKey, cancellationToken);
        if (existing == null)
        {
            var entity = new SystemSetting
            {
                Section = definition.Section,
                SettingKey = definition.SettingKey,
                Label = definition.Label,
                Description = definition.Description,
                ValueType = definition.ValueType,
                Value = definition.Value,
                DefaultValue = definition.DefaultValue,
                IsActive = true,
                IsSecret = false,
                SortOrder = definition.SortOrder,
                CreatedBy = BootstrapUserId,
                UpdatedBy = BootstrapUserId
            };

            await _systemSettingRepository.CreateAsync(entity, cancellationToken);
            _logger.LogInformation("补齐 app system setting: {Section}.{Key}", definition.Section, definition.SettingKey);
            return (true, definition.Value);
        }

        var changed = false;

        if (!existing.IsActive)
        {
            existing.IsActive = true;
            changed = true;
        }

        if (existing.IsSecret)
        {
            existing.IsSecret = false;
            changed = true;
        }

        if (!string.Equals(existing.ValueType, definition.ValueType, StringComparison.OrdinalIgnoreCase))
        {
            existing.ValueType = definition.ValueType;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(existing.Value))
        {
            existing.Value = definition.Value;
            changed = true;
        }

        if (!string.Equals(existing.DefaultValue, definition.DefaultValue, StringComparison.Ordinal))
        {
            existing.DefaultValue = definition.DefaultValue;
            changed = true;
        }

        if (!string.Equals(existing.Label, definition.Label, StringComparison.Ordinal))
        {
            existing.Label = definition.Label;
            changed = true;
        }

        if (!string.Equals(existing.Description, definition.Description, StringComparison.Ordinal))
        {
            existing.Description = definition.Description;
            changed = true;
        }

        if (existing.SortOrder != definition.SortOrder)
        {
            existing.SortOrder = definition.SortOrder;
            changed = true;
        }

        if (!changed)
        {
            return (false, existing.Value);
        }

        existing.UpdatedBy = BootstrapUserId;
        await _systemSettingRepository.UpdateAsync(existing, cancellationToken);
        _logger.LogInformation("修复 app system setting 元数据: {Section}.{Key}", definition.Section, definition.SettingKey);
        return (true, existing.Value);
    }

    private async Task<bool> ShouldPublishAsync(
        IReadOnlyDictionary<string, string> currentStaticTexts,
        IReadOnlyDictionary<string, string> currentSettingValues,
        bool hasDataChanges)
    {
        var publishedSnapshot = await _configSnapshotRepository.GetPublishedAsync();
        if (publishedSnapshot == null)
        {
            _logger.LogInformation("尚无已发布 app/config 快照，bootstrap 将发布首个版本");
            return true;
        }

        var publishedTexts = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(publishedSnapshot.StaticTexts)
            ?? new Dictionary<string, Dictionary<string, string>>();
        var publishedSettings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, AppSystemSettingDto>>>(publishedSnapshot.SystemSettings)
            ?? new Dictionary<string, Dictionary<string, AppSystemSettingDto>>();

        foreach (var entry in currentStaticTexts)
        {
            var parts = entry.Key.Split(':', 2);
            var locale = parts[0];
            var textKey = parts[1];

            if (!publishedTexts.TryGetValue(locale, out var localeTexts) ||
                !localeTexts.TryGetValue(textKey, out var publishedValue) ||
                !string.Equals(publishedValue, entry.Value, StringComparison.Ordinal))
            {
                _logger.LogInformation("已发布快照缺少或未同步 static text: {Key} [{Locale}]", textKey, locale);
                return true;
            }
        }

        foreach (var entry in currentSettingValues)
        {
            var parts = entry.Key.Split(':', 2);
            var section = parts[0];
            var settingKey = parts[1];

            if (!publishedSettings.TryGetValue(section, out var sectionSettings) ||
                !sectionSettings.TryGetValue(settingKey, out var publishedSetting) ||
                string.IsNullOrWhiteSpace(publishedSetting.Value) ||
                !string.Equals(publishedSetting.Value, entry.Value, StringComparison.Ordinal))
            {
                _logger.LogInformation("已发布快照缺少或未同步 system setting: {Section}.{Key}", section, settingKey);
                return true;
            }
        }

        return hasDataChanges;
    }

    private static IReadOnlyList<AppStaticTextSeed> GetStaticTextSeeds()
    {
        return new List<AppStaticTextSeed>
        {
            new(
                TextKey: "legal.community_guidelines.sections_json",
                Locale: "zh-CN",
                TextValue: JsonSerializer.Serialize(new object[]
                {
                    new { title = "1. 尊重与友善", content = "请尊重他人观点与文化差异，避免人身攻击、歧视、骚扰或仇恨言论。" },
                    new { title = "2. 真实与可信", content = "请发布真实信息与经历，避免虚假宣传、刷评、诱导或误导性内容。" },
                    new { title = "3. 合法与安全", content = "禁止发布违法、诈骗、侵权、色情、暴力或其他有害内容。" },
                    new { title = "4. 隐私保护", content = "未经允许不得发布他人隐私信息或私人联系方式。" },
                    new { title = "5. 友好互动与建设性反馈", content = "鼓励分享有价值的经验、建议与改进意见，避免灌水或恶意攻击。" },
                    new { title = "6. 线下活动礼仪", content = "参加线下活动请守时、守约并注意安全，如遇问题及时联系组织者或平台。" },
                    new { title = "7. 举报与处理", content = "如发现违规内容，请使用举报功能。平台将依据准则进行处理。" },
                    new { title = "8. 准则更新", content = "我们可能不定期更新社区准则，更新后继续使用视为同意。" }
                }),
                Category: "legal",
                Description: "App 社区准则正文 JSON，供未登录或首次启动场景读取"),
            new(
                TextKey: "legal.community_guidelines.sections_json",
                Locale: "en-US",
                TextValue: JsonSerializer.Serialize(new object[]
                {
                    new { title = "1. Respect And Kindness", content = "Respect different viewpoints and cultural backgrounds. Personal attacks, discrimination, harassment, and hate speech are prohibited." },
                    new { title = "2. Authentic And Trustworthy", content = "Share genuine information and real experiences. Misleading promotions, fake reviews, and deceptive content are not allowed." },
                    new { title = "3. Legal And Safe", content = "Do not post illegal, fraudulent, infringing, explicit, violent, or otherwise harmful content." },
                    new { title = "4. Privacy Protection", content = "Do not disclose another person's private information or contact details without permission." },
                    new { title = "5. Constructive Participation", content = "We encourage helpful experiences, practical suggestions, and constructive feedback instead of spam or hostile behavior." },
                    new { title = "6. Offline Event Etiquette", content = "Be punctual, honor your commitments, and stay aware of personal safety when attending offline events." },
                    new { title = "7. Reporting And Enforcement", content = "Use the reporting tools when you see violations. The platform will review and handle cases according to these guidelines." },
                    new { title = "8. Guideline Updates", content = "We may update these community guidelines from time to time. Continued use of the app means you accept the updated version." }
                }),
                Category: "legal",
                Description: "App community guidelines JSON for anonymous app/config reads"),
            new(
                TextKey: "legal.first_launch.dialog.title",
                Locale: "zh-CN",
                TextValue: "服务协议与隐私政策",
                Category: "legal",
                Description: "首启隐私弹窗标题"),
            new(
                TextKey: "legal.first_launch.dialog.title",
                Locale: "en-US",
                TextValue: "Terms Of Service And Privacy Policy",
                Category: "legal",
                Description: "First-launch privacy dialog title"),
            new(
                TextKey: "legal.first_launch.dialog.intro",
                Locale: "zh-CN",
                TextValue: "欢迎使用行途（Go-Nomads）！为了继续使用应用，请您阅读并同意《隐私政策》和《用户协议》。",
                Category: "legal",
                Description: "首启隐私弹窗说明文案"),
            new(
                TextKey: "legal.first_launch.dialog.intro",
                Locale: "en-US",
                TextValue: "Welcome to Go-Nomads. To continue using the app, please review and accept the Privacy Policy and Terms of Service.",
                Category: "legal",
                Description: "First-launch privacy dialog introduction copy"),
            new(
                TextKey: "legal.first_launch.dialog.privacy_checkbox_prefix",
                Locale: "zh-CN",
                TextValue: "我已阅读并同意",
                Category: "legal",
                Description: "首启隐私弹窗隐私政策勾选前缀"),
            new(
                TextKey: "legal.first_launch.dialog.privacy_checkbox_prefix",
                Locale: "en-US",
                TextValue: "I have read and agree to the ",
                Category: "legal",
                Description: "First-launch privacy dialog privacy checkbox prefix"),
            new(
                TextKey: "legal.first_launch.dialog.terms_checkbox_prefix",
                Locale: "zh-CN",
                TextValue: "我已阅读并同意",
                Category: "legal",
                Description: "首启隐私弹窗用户协议勾选前缀"),
            new(
                TextKey: "legal.first_launch.dialog.terms_checkbox_prefix",
                Locale: "en-US",
                TextValue: "I have read and agree to the ",
                Category: "legal",
                Description: "First-launch privacy dialog terms checkbox prefix"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_prefix",
                Locale: "zh-CN",
                TextValue: "如果您不同意上述法律文档，将无法继续使用本应用。您可以随时在设置中查看完整的",
                Category: "legal",
                Description: "首启隐私弹窗底部提示前缀"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_prefix",
                Locale: "en-US",
                TextValue: "If you do not accept the legal documents above, you will not be able to continue using the app. You can review the full ",
                Category: "legal",
                Description: "First-launch privacy dialog footnote prefix"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_link_separator",
                Locale: "zh-CN",
                TextValue: "、",
                Category: "legal",
                Description: "首启隐私弹窗 decline tip 链接间分隔符"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_link_separator",
                Locale: "en-US",
                TextValue: ", ",
                Category: "legal",
                Description: "First-launch privacy dialog decline tip link separator"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_link_final_connector",
                Locale: "zh-CN",
                TextValue: "和",
                Category: "legal",
                Description: "首启隐私弹窗 decline tip 最后一个链接连接词"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_link_final_connector",
                Locale: "en-US",
                TextValue: ", and ",
                Category: "legal",
                Description: "First-launch privacy dialog decline tip final link connector"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_suffix",
                Locale: "zh-CN",
                TextValue: "。",
                Category: "legal",
                Description: "首启隐私弹窗 decline tip 结尾标点"),
            new(
                TextKey: "legal.first_launch.dialog.decline_tip_suffix",
                Locale: "en-US",
                TextValue: ".",
                Category: "legal",
                Description: "First-launch privacy dialog decline tip suffix"),
            new(
                TextKey: "legal.first_launch.dialog.sdk_link_label",
                Locale: "zh-CN",
                TextValue: "第三方SDK清单",
                Category: "legal",
                Description: "首启隐私弹窗 SDK 链接标题"),
            new(
                TextKey: "legal.first_launch.dialog.sdk_link_label",
                Locale: "en-US",
                TextValue: "Third-Party SDK List",
                Category: "legal",
                Description: "First-launch privacy dialog SDK link label"),
            new(
                TextKey: "legal.first_launch.dialog.agree_button",
                Locale: "zh-CN",
                TextValue: "同意并继续",
                Category: "legal",
                Description: "首启隐私弹窗同意按钮文案"),
            new(
                TextKey: "legal.first_launch.dialog.agree_button",
                Locale: "en-US",
                TextValue: "Agree And Continue",
                Category: "legal",
                Description: "First-launch privacy dialog agree button label"),
            new(
                TextKey: "legal.first_launch.dialog.reject_button",
                Locale: "zh-CN",
                TextValue: "不同意并退出",
                Category: "legal",
                Description: "首启隐私弹窗拒绝按钮文案"),
            new(
                TextKey: "legal.first_launch.dialog.reject_button",
                Locale: "en-US",
                TextValue: "Disagree And Exit",
                Category: "legal",
                Description: "First-launch privacy dialog reject button label"),
            new(
                TextKey: "legal.first_launch.dialog.summary_fallback_title",
                Locale: "zh-CN",
                TextValue: "法律文档说明",
                Category: "legal",
                Description: "首启隐私弹窗摘要兜底标题"),
            new(
                TextKey: "legal.first_launch.dialog.summary_fallback_title",
                Locale: "en-US",
                TextValue: "Legal Document Notice",
                Category: "legal",
                Description: "First-launch privacy dialog fallback summary title"),
            new(
                TextKey: "legal.first_launch.dialog.summary_fallback_content",
                Locale: "zh-CN",
                TextValue: "我们重视您的隐私与使用权益。请查看完整隐私政策、用户协议和第三方 SDK 清单。",
                Category: "legal",
                Description: "首启隐私弹窗摘要兜底内容"),
            new(
                TextKey: "legal.first_launch.dialog.summary_fallback_content",
                Locale: "en-US",
                TextValue: "We value your privacy and usage rights. Please review the full Privacy Policy, Terms of Service, and third-party SDK list.",
                Category: "legal",
                Description: "First-launch privacy dialog fallback summary content"),
            new(
                TextKey: "legal.first_launch.dialog.unchecked_toast_title",
                Locale: "zh-CN",
                TextValue: "需要同意条款",
                Category: "legal",
                Description: "首启隐私弹窗未勾选提示标题"),
            new(
                TextKey: "legal.first_launch.dialog.unchecked_toast_title",
                Locale: "en-US",
                TextValue: "Consent Required",
                Category: "legal",
                Description: "First-launch privacy dialog unchecked toast title"),
            new(
                TextKey: "legal.first_launch.dialog.unchecked_toast_message",
                Locale: "zh-CN",
                TextValue: "请先同意隐私政策和用户协议",
                Category: "legal",
                Description: "首启隐私弹窗未勾选提示内容"),
            new(
                TextKey: "legal.first_launch.dialog.unchecked_toast_message",
                Locale: "en-US",
                TextValue: "Please agree to the Privacy Policy and Terms of Service first.",
                Category: "legal",
                Description: "First-launch privacy dialog unchecked toast message"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_title",
                Locale: "zh-CN",
                TextValue: "温馨提示",
                Category: "legal",
                Description: "首启隐私弹窗拒绝确认标题"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_title",
                Locale: "en-US",
                TextValue: "Please Confirm",
                Category: "legal",
                Description: "First-launch privacy dialog decline confirm title"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_message",
                Locale: "zh-CN",
                TextValue: "如果您不同意隐私政策和用户协议，将无法使用本应用的相关功能。\n\n我们非常重视您的隐私安全，收集的信息仅用于为您提供更好的服务。\n\n您确定不同意吗？",
                Category: "legal",
                Description: "首启隐私弹窗拒绝确认内容"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_message",
                Locale: "en-US",
                TextValue: "If you do not agree to the Privacy Policy and Terms of Service, you will not be able to use the related features of this app.\n\nWe take your privacy seriously, and any information collected is only used to provide better service.\n\nAre you sure you want to decline?",
                Category: "legal",
                Description: "First-launch privacy dialog decline confirm message"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_cancel",
                Locale: "zh-CN",
                TextValue: "再想想",
                Category: "legal",
                Description: "首启隐私弹窗拒绝确认取消按钮"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_cancel",
                Locale: "en-US",
                TextValue: "Go Back",
                Category: "legal",
                Description: "First-launch privacy dialog decline confirm cancel label"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_exit",
                Locale: "zh-CN",
                TextValue: "确认退出",
                Category: "legal",
                Description: "首启隐私弹窗拒绝确认退出按钮"),
            new(
                TextKey: "legal.first_launch.dialog.decline_confirm_exit",
                Locale: "en-US",
                TextValue: "Exit App",
                Category: "legal",
                Description: "First-launch privacy dialog decline confirm exit label"),
            new(
                TextKey: "auth.forgot_password.step.account.title",
                Locale: "zh-CN",
                TextValue: "找回密码",
                Category: "auth",
                Description: "找回密码步骤一标题"),
            new(
                TextKey: "auth.forgot_password.step.account.title",
                Locale: "en-US",
                TextValue: "Reset Password",
                Category: "auth",
                Description: "Forgot-password step 1 title"),
            new(
                TextKey: "auth.forgot_password.step.account.description",
                Locale: "zh-CN",
                TextValue: "请输入您的邮箱或手机号\n我们将发送验证码帮助您重置密码",
                Category: "auth",
                Description: "找回密码步骤一说明文案"),
            new(
                TextKey: "auth.forgot_password.step.account.description",
                Locale: "en-US",
                TextValue: "Enter your email or phone number.\nWe will send a verification code to help you reset your password.",
                Category: "auth",
                Description: "Forgot-password step 1 description"),
            new(
                TextKey: "auth.forgot_password.step.account.input_label",
                Locale: "zh-CN",
                TextValue: "邮箱或手机号",
                Category: "auth",
                Description: "找回密码步骤一输入框标签"),
            new(
                TextKey: "auth.forgot_password.step.account.input_label",
                Locale: "en-US",
                TextValue: "Email Or Phone Number",
                Category: "auth",
                Description: "Forgot-password step 1 input label"),
            new(
                TextKey: "auth.forgot_password.step.account.send_code_button",
                Locale: "zh-CN",
                TextValue: "发送验证码",
                Category: "auth",
                Description: "找回密码步骤一发送验证码按钮"),
            new(
                TextKey: "auth.forgot_password.step.account.send_code_button",
                Locale: "en-US",
                TextValue: "Send Verification Code",
                Category: "auth",
                Description: "Forgot-password step 1 send code button"),
            new(
                TextKey: "auth.forgot_password.step.verify.title",
                Locale: "zh-CN",
                TextValue: "验证身份",
                Category: "auth",
                Description: "找回密码步骤二标题"),
            new(
                TextKey: "auth.forgot_password.step.verify.title",
                Locale: "en-US",
                TextValue: "Verify Identity",
                Category: "auth",
                Description: "Forgot-password step 2 title"),
            new(
                TextKey: "auth.forgot_password.step.verify.description_template",
                Locale: "zh-CN",
                TextValue: "验证码已发送至\n{target}",
                Category: "auth",
                Description: "找回密码步骤二说明模板，包含 {target} 占位符"),
            new(
                TextKey: "auth.forgot_password.step.verify.description_template",
                Locale: "en-US",
                TextValue: "A verification code was sent to\n{target}",
                Category: "auth",
                Description: "Forgot-password step 2 description template with {target}"),
            new(
                TextKey: "auth.forgot_password.step.verify.code_label",
                Locale: "zh-CN",
                TextValue: "验证码",
                Category: "auth",
                Description: "找回密码步骤二验证码标签"),
            new(
                TextKey: "auth.forgot_password.step.verify.code_label",
                Locale: "en-US",
                TextValue: "Verification Code",
                Category: "auth",
                Description: "Forgot-password step 2 code label"),
            new(
                TextKey: "auth.forgot_password.step.verify.resend_countdown_template",
                Locale: "zh-CN",
                TextValue: "{seconds}s 后重新发送",
                Category: "auth",
                Description: "找回密码步骤二重发倒计时模板，包含 {seconds} 占位符"),
            new(
                TextKey: "auth.forgot_password.step.verify.resend_countdown_template",
                Locale: "en-US",
                TextValue: "Resend in {seconds}s",
                Category: "auth",
                Description: "Forgot-password step 2 resend countdown template with {seconds}"),
            new(
                TextKey: "auth.forgot_password.step.verify.resend_button",
                Locale: "zh-CN",
                TextValue: "重新发送验证码",
                Category: "auth",
                Description: "找回密码步骤二重新发送按钮"),
            new(
                TextKey: "auth.forgot_password.step.verify.resend_button",
                Locale: "en-US",
                TextValue: "Resend Code",
                Category: "auth",
                Description: "Forgot-password step 2 resend button"),
            new(
                TextKey: "auth.forgot_password.step.verify.next_button",
                Locale: "zh-CN",
                TextValue: "下一步",
                Category: "auth",
                Description: "找回密码步骤二下一步按钮"),
            new(
                TextKey: "auth.forgot_password.step.verify.next_button",
                Locale: "en-US",
                TextValue: "Next",
                Category: "auth",
                Description: "Forgot-password step 2 next button"),
            new(
                TextKey: "auth.forgot_password.step.reset.title",
                Locale: "zh-CN",
                TextValue: "设置新密码",
                Category: "auth",
                Description: "找回密码步骤三标题"),
            new(
                TextKey: "auth.forgot_password.step.reset.title",
                Locale: "en-US",
                TextValue: "Set New Password",
                Category: "auth",
                Description: "Forgot-password step 3 title"),
            new(
                TextKey: "auth.forgot_password.step.reset.description",
                Locale: "zh-CN",
                TextValue: "请设置您的新密码",
                Category: "auth",
                Description: "找回密码步骤三说明文案"),
            new(
                TextKey: "auth.forgot_password.step.reset.description",
                Locale: "en-US",
                TextValue: "Please set your new password.",
                Category: "auth",
                Description: "Forgot-password step 3 description"),
            new(
                TextKey: "auth.forgot_password.step.reset.new_password_label",
                Locale: "zh-CN",
                TextValue: "新密码",
                Category: "auth",
                Description: "找回密码步骤三新密码标签"),
            new(
                TextKey: "auth.forgot_password.step.reset.new_password_label",
                Locale: "en-US",
                TextValue: "New Password",
                Category: "auth",
                Description: "Forgot-password step 3 new password label"),
            new(
                TextKey: "auth.forgot_password.step.reset.confirm_password_label",
                Locale: "zh-CN",
                TextValue: "确认密码",
                Category: "auth",
                Description: "找回密码步骤三确认密码标签"),
            new(
                TextKey: "auth.forgot_password.step.reset.confirm_password_label",
                Locale: "en-US",
                TextValue: "Confirm Password",
                Category: "auth",
                Description: "Forgot-password step 3 confirm password label"),
            new(
                TextKey: "auth.forgot_password.step.reset.submit_button",
                Locale: "zh-CN",
                TextValue: "重置密码",
                Category: "auth",
                Description: "找回密码步骤三提交按钮"),
            new(
                TextKey: "auth.forgot_password.step.reset.submit_button",
                Locale: "en-US",
                TextValue: "Reset Password",
                Category: "auth",
                Description: "Forgot-password step 3 submit button"),
            new(
                TextKey: "auth.forgot_password.toast.account_required",
                Locale: "zh-CN",
                TextValue: "请输入邮箱或手机号",
                Category: "auth",
                Description: "找回密码账号为空提示"),
            new(
                TextKey: "auth.forgot_password.toast.account_required",
                Locale: "en-US",
                TextValue: "Please enter your email or phone number.",
                Category: "auth",
                Description: "Forgot-password account required toast"),
            new(
                TextKey: "auth.forgot_password.toast.code_sent_email",
                Locale: "zh-CN",
                TextValue: "验证码已发送到邮箱",
                Category: "auth",
                Description: "找回密码邮箱验证码发送成功提示"),
            new(
                TextKey: "auth.forgot_password.toast.code_sent_email",
                Locale: "en-US",
                TextValue: "Verification code sent to your email.",
                Category: "auth",
                Description: "Forgot-password email code sent toast"),
            new(
                TextKey: "auth.forgot_password.toast.code_sent_phone",
                Locale: "zh-CN",
                TextValue: "验证码已发送到手机",
                Category: "auth",
                Description: "找回密码手机验证码发送成功提示"),
            new(
                TextKey: "auth.forgot_password.toast.code_sent_phone",
                Locale: "en-US",
                TextValue: "Verification code sent to your phone.",
                Category: "auth",
                Description: "Forgot-password phone code sent toast"),
            new(
                TextKey: "auth.forgot_password.toast.send_failed_fallback",
                Locale: "zh-CN",
                TextValue: "发送验证码失败，请稍后重试",
                Category: "auth",
                Description: "找回密码发送验证码兜底失败提示"),
            new(
                TextKey: "auth.forgot_password.toast.send_failed_fallback",
                Locale: "en-US",
                TextValue: "Failed to send the verification code. Please try again later.",
                Category: "auth",
                Description: "Forgot-password send code fallback error toast"),
            new(
                TextKey: "auth.forgot_password.toast.code_required",
                Locale: "zh-CN",
                TextValue: "请输入验证码",
                Category: "auth",
                Description: "找回密码验证码为空提示"),
            new(
                TextKey: "auth.forgot_password.toast.code_required",
                Locale: "en-US",
                TextValue: "Please enter the verification code.",
                Category: "auth",
                Description: "Forgot-password code required toast"),
            new(
                TextKey: "auth.forgot_password.toast.code_incomplete",
                Locale: "zh-CN",
                TextValue: "请输入完整的验证码",
                Category: "auth",
                Description: "找回密码验证码不完整提示"),
            new(
                TextKey: "auth.forgot_password.toast.code_incomplete",
                Locale: "en-US",
                TextValue: "Please enter the full verification code.",
                Category: "auth",
                Description: "Forgot-password incomplete code toast"),
            new(
                TextKey: "auth.forgot_password.toast.new_password_required",
                Locale: "zh-CN",
                TextValue: "请输入新密码",
                Category: "auth",
                Description: "找回密码新密码为空提示"),
            new(
                TextKey: "auth.forgot_password.toast.new_password_required",
                Locale: "en-US",
                TextValue: "Please enter a new password.",
                Category: "auth",
                Description: "Forgot-password new password required toast"),
            new(
                TextKey: "auth.forgot_password.toast.password_min_length",
                Locale: "zh-CN",
                TextValue: "密码至少需要6个字符",
                Category: "auth",
                Description: "找回密码密码长度不足提示"),
            new(
                TextKey: "auth.forgot_password.toast.password_min_length",
                Locale: "en-US",
                TextValue: "Password must be at least 6 characters long.",
                Category: "auth",
                Description: "Forgot-password minimum password length toast"),
            new(
                TextKey: "auth.forgot_password.toast.confirm_password_required",
                Locale: "zh-CN",
                TextValue: "请确认新密码",
                Category: "auth",
                Description: "找回密码确认密码为空提示"),
            new(
                TextKey: "auth.forgot_password.toast.confirm_password_required",
                Locale: "en-US",
                TextValue: "Please confirm your new password.",
                Category: "auth",
                Description: "Forgot-password confirm password required toast"),
            new(
                TextKey: "auth.forgot_password.toast.password_mismatch",
                Locale: "zh-CN",
                TextValue: "两次输入的密码不一致",
                Category: "auth",
                Description: "找回密码密码不一致提示"),
            new(
                TextKey: "auth.forgot_password.toast.password_mismatch",
                Locale: "en-US",
                TextValue: "The passwords you entered do not match.",
                Category: "auth",
                Description: "Forgot-password password mismatch toast"),
            new(
                TextKey: "auth.forgot_password.toast.reset_success",
                Locale: "zh-CN",
                TextValue: "密码重置成功，请使用新密码登录",
                Category: "auth",
                Description: "找回密码重置成功提示"),
            new(
                TextKey: "auth.forgot_password.toast.reset_success",
                Locale: "en-US",
                TextValue: "Password reset successfully. Please sign in with your new password.",
                Category: "auth",
                Description: "Forgot-password reset success toast"),
            new(
                TextKey: "auth.forgot_password.toast.reset_failed_fallback",
                Locale: "zh-CN",
                TextValue: "重置密码失败，请稍后重试",
                Category: "auth",
                Description: "找回密码重置失败兜底提示"),
            new(
                TextKey: "auth.forgot_password.toast.reset_failed_fallback",
                Locale: "en-US",
                TextValue: "Failed to reset the password. Please try again later.",
                Category: "auth",
                Description: "Forgot-password reset failure fallback toast"),
            new(
                TextKey: "auth.login.terms.prefix",
                Locale: "zh-CN",
                TextValue: "我已阅读并同意 ",
                Category: "auth",
                Description: "登录页协议勾选框前缀"),
            new(
                TextKey: "auth.login.terms.prefix",
                Locale: "en-US",
                TextValue: "I have read and agree to the ",
                Category: "auth",
                Description: "Login terms wrapper prefix"),
            new(
                TextKey: "auth.login.terms.connector",
                Locale: "zh-CN",
                TextValue: " 和 ",
                Category: "auth",
                Description: "登录页协议勾选框连接词"),
            new(
                TextKey: "auth.login.terms.connector",
                Locale: "en-US",
                TextValue: " and ",
                Category: "auth",
                Description: "Login terms wrapper connector"),
            new(
                TextKey: "auth.login.terms.suffix",
                Locale: "zh-CN",
                TextValue: "。",
                Category: "auth",
                Description: "登录页协议勾选框结尾标点"),
            new(
                TextKey: "auth.login.terms.suffix",
                Locale: "en-US",
                TextValue: ".",
                Category: "auth",
                Description: "Login terms wrapper suffix"),
            new(
                TextKey: "auth.register.terms.prefix",
                Locale: "zh-CN",
                TextValue: "我已阅读并同意 ",
                Category: "auth",
                Description: "注册页协议勾选框前缀"),
            new(
                TextKey: "auth.register.terms.prefix",
                Locale: "en-US",
                TextValue: "I have read and agree to the ",
                Category: "auth",
                Description: "Register terms wrapper prefix"),
            new(
                TextKey: "auth.register.terms.connector",
                Locale: "zh-CN",
                TextValue: " 和 ",
                Category: "auth",
                Description: "注册页协议勾选框连接词"),
            new(
                TextKey: "auth.register.terms.connector",
                Locale: "en-US",
                TextValue: " and ",
                Category: "auth",
                Description: "Register terms wrapper connector"),
            new(
                TextKey: "auth.register.terms.community_prefix",
                Locale: "zh-CN",
                TextValue: "，并遵守 ",
                Category: "auth",
                Description: "注册页社区准则连接前缀"),
            new(
                TextKey: "auth.register.terms.community_prefix",
                Locale: "en-US",
                TextValue: ", and follow the ",
                Category: "auth",
                Description: "Register community guidelines prefix"),
            new(
                TextKey: "auth.register.terms.suffix",
                Locale: "zh-CN",
                TextValue: "。",
                Category: "auth",
                Description: "注册页协议勾选框结尾标点"),
            new(
                TextKey: "auth.register.terms.suffix",
                Locale: "en-US",
                TextValue: ".",
                Category: "auth",
                Description: "Register terms wrapper suffix"),
            new(
                TextKey: "auth.legal_links.prefix",
                Locale: "zh-CN",
                TextValue: "继续使用即表示您同意 ",
                Category: "auth",
                Description: "底部法律链接前缀"),
            new(
                TextKey: "auth.legal_links.prefix",
                Locale: "en-US",
                TextValue: "By continuing, you agree to the ",
                Category: "auth",
                Description: "Bottom legal links prefix"),
            new(
                TextKey: "auth.legal_links.connector",
                Locale: "zh-CN",
                TextValue: " 与 ",
                Category: "auth",
                Description: "底部法律链接连接词"),
            new(
                TextKey: "auth.legal_links.connector",
                Locale: "en-US",
                TextValue: " and ",
                Category: "auth",
                Description: "Bottom legal links connector"),
            new(
                TextKey: "auth.legal_links.suffix",
                Locale: "zh-CN",
                TextValue: "。",
                Category: "auth",
                Description: "底部法律链接结尾标点"),
            new(
                TextKey: "auth.legal_links.suffix",
                Locale: "en-US",
                TextValue: ".",
                Category: "auth",
                Description: "Bottom legal links suffix"),
            new(
                TextKey: "auth.login.header.title",
                Locale: "zh-CN",
                TextValue: "欢迎",
                Category: "auth",
                Description: "登录页头部标题"),
            new(
                TextKey: "auth.login.header.title",
                Locale: "en-US",
                TextValue: "Welcome",
                Category: "auth",
                Description: "Login page header title"),
            new(
                TextKey: "auth.login.header.subtitle",
                Locale: "zh-CN",
                TextValue: "登录",
                Category: "auth",
                Description: "登录页头部副标题"),
            new(
                TextKey: "auth.login.header.subtitle",
                Locale: "en-US",
                TextValue: "Login",
                Category: "auth",
                Description: "Login page header subtitle"),
            new(
                TextKey: "auth.login.link.register_prefix",
                Locale: "zh-CN",
                TextValue: "Let's Go",
                Category: "auth",
                Description: "登录页跳转注册前缀"),
            new(
                TextKey: "auth.login.link.register_prefix",
                Locale: "en-US",
                TextValue: "Let's Go",
                Category: "auth",
                Description: "Login page register link prefix"),
            new(
                TextKey: "auth.login.community.title",
                Locale: "zh-CN",
                TextValue: "加入 38,000+ 游牧者",
                Category: "auth",
                Description: "登录页社区亮点标题"),
            new(
                TextKey: "auth.login.community.title",
                Locale: "en-US",
                TextValue: "Join 38,000+ nomads",
                Category: "auth",
                Description: "Login page community highlight title"),
            new(
                TextKey: "auth.login.community.subtitle",
                Locale: "zh-CN",
                TextValue: "在全球各地生活和工作",
                Category: "auth",
                Description: "登录页社区亮点副标题"),
            new(
                TextKey: "auth.login.community.subtitle",
                Locale: "en-US",
                TextValue: "Living and working around the world",
                Category: "auth",
                Description: "Login page community highlight subtitle"),
            new(
                TextKey: "auth.login.community.badge.meetups",
                Locale: "zh-CN",
                TextValue: "363 场聚会/年",
                Category: "auth",
                Description: "登录页社区亮点聚会徽章"),
            new(
                TextKey: "auth.login.community.badge.meetups",
                Locale: "en-US",
                TextValue: "363 meetups/year",
                Category: "auth",
                Description: "Login page community highlight meetups badge"),
            new(
                TextKey: "auth.login.community.badge.messages",
                Locale: "zh-CN",
                TextValue: "15k+ 消息",
                Category: "auth",
                Description: "登录页社区亮点消息徽章"),
            new(
                TextKey: "auth.login.community.badge.messages",
                Locale: "en-US",
                TextValue: "15k+ messages",
                Category: "auth",
                Description: "Login page community highlight messages badge"),
            new(
                TextKey: "auth.login.community.badge.cities",
                Locale: "zh-CN",
                TextValue: "100+ 城市",
                Category: "auth",
                Description: "登录页社区亮点城市徽章"),
            new(
                TextKey: "auth.login.community.badge.cities",
                Locale: "en-US",
                TextValue: "100+ cities",
                Category: "auth",
                Description: "Login page community highlight cities badge"),
            new(
                TextKey: "auth.register.header.title",
                Locale: "zh-CN",
                TextValue: "成为数字游民",
                Category: "auth",
                Description: "注册页头部标题"),
            new(
                TextKey: "auth.register.header.title",
                Locale: "en-US",
                TextValue: "Go nomad",
                Category: "auth",
                Description: "Register page header title"),
            new(
                TextKey: "auth.register.header.subtitle",
                Locale: "zh-CN",
                TextValue: "加入全球远程工作者社区",
                Category: "auth",
                Description: "注册页头部副标题"),
            new(
                TextKey: "auth.register.header.subtitle",
                Locale: "en-US",
                TextValue: "Join a global community of remote workers",
                Category: "auth",
                Description: "Register page header subtitle"),
            new(
                TextKey: "auth.register.link.login_prefix",
                Locale: "zh-CN",
                TextValue: "已有账号?",
                Category: "auth",
                Description: "注册页跳转登录前缀"),
            new(
                TextKey: "auth.register.link.login_prefix",
                Locale: "en-US",
                TextValue: "Already have an account?",
                Category: "auth",
                Description: "Register page login link prefix"),
            new(
                TextKey: "auth.register.highlights.title",
                Locale: "zh-CN",
                TextValue: "加入 38,000+ 会员并获得:",
                Category: "auth",
                Description: "注册页亮点卡标题"),
            new(
                TextKey: "auth.register.highlights.title",
                Locale: "en-US",
                TextValue: "Join 38,000+ members and get:",
                Category: "auth",
                Description: "Register page highlights title"),
            new(
                TextKey: "auth.register.highlights.meetups.title",
                Locale: "zh-CN",
                TextValue: "参加 363 场聚会/年",
                Category: "auth",
                Description: "注册页亮点聚会标题"),
            new(
                TextKey: "auth.register.highlights.meetups.title",
                Locale: "en-US",
                TextValue: "Attend 363 meetups/year",
                Category: "auth",
                Description: "Register page meetups highlight title"),
            new(
                TextKey: "auth.register.highlights.meetups.subtitle",
                Locale: "zh-CN",
                TextValue: "在全球 100+ 城市",
                Category: "auth",
                Description: "注册页亮点聚会副标题"),
            new(
                TextKey: "auth.register.highlights.meetups.subtitle",
                Locale: "en-US",
                TextValue: "in 100+ cities worldwide",
                Category: "auth",
                Description: "Register page meetups highlight subtitle"),
            new(
                TextKey: "auth.register.highlights.people.title",
                Locale: "zh-CN",
                TextValue: "结识新朋友",
                Category: "auth",
                Description: "注册页亮点社交标题"),
            new(
                TextKey: "auth.register.highlights.people.title",
                Locale: "en-US",
                TextValue: "Meet new people",
                Category: "auth",
                Description: "Register page people highlight title"),
            new(
                TextKey: "auth.register.highlights.people.subtitle",
                Locale: "zh-CN",
                TextValue: "用于约会和交友",
                Category: "auth",
                Description: "注册页亮点社交副标题"),
            new(
                TextKey: "auth.register.highlights.people.subtitle",
                Locale: "en-US",
                TextValue: "for dating and friends",
                Category: "auth",
                Description: "Register page people highlight subtitle"),
            new(
                TextKey: "auth.register.highlights.destinations.title",
                Locale: "zh-CN",
                TextValue: "研究目的地",
                Category: "auth",
                Description: "注册页亮点目的地标题"),
            new(
                TextKey: "auth.register.highlights.destinations.title",
                Locale: "en-US",
                TextValue: "Research destinations",
                Category: "auth",
                Description: "Register page destinations highlight title"),
            new(
                TextKey: "auth.register.highlights.destinations.subtitle",
                Locale: "zh-CN",
                TextValue: "找到最适合您的居住地",
                Category: "auth",
                Description: "注册页亮点目的地副标题"),
            new(
                TextKey: "auth.register.highlights.destinations.subtitle",
                Locale: "en-US",
                TextValue: "and find your best place to live",
                Category: "auth",
                Description: "Register page destinations highlight subtitle"),
            new(
                TextKey: "auth.register.highlights.chat.title",
                Locale: "zh-CN",
                TextValue: "加入专属聊天",
                Category: "auth",
                Description: "注册页亮点聊天标题"),
            new(
                TextKey: "auth.register.highlights.chat.title",
                Locale: "en-US",
                TextValue: "Join exclusive chat",
                Category: "auth",
                Description: "Register page chat highlight title"),
            new(
                TextKey: "auth.register.highlights.chat.subtitle",
                Locale: "zh-CN",
                TextValue: "本月发送了 15,000+ 条消息",
                Category: "auth",
                Description: "注册页亮点聊天副标题"),
            new(
                TextKey: "auth.register.highlights.chat.subtitle",
                Locale: "en-US",
                TextValue: "15,000+ messages sent this month",
                Category: "auth",
                Description: "Register page chat highlight subtitle"),
            new(
                TextKey: "auth.register.highlights.travels.title",
                Locale: "zh-CN",
                TextValue: "记录您的旅行",
                Category: "auth",
                Description: "注册页亮点旅行标题"),
            new(
                TextKey: "auth.register.highlights.travels.title",
                Locale: "en-US",
                TextValue: "Track your travels",
                Category: "auth",
                Description: "Register page travels highlight title"),
            new(
                TextKey: "auth.register.highlights.travels.subtitle",
                Locale: "zh-CN",
                TextValue: "分享您的旅程",
                Category: "auth",
                Description: "注册页亮点旅行副标题"),
            new(
                TextKey: "auth.register.highlights.travels.subtitle",
                Locale: "en-US",
                TextValue: "and share your journey",
                Category: "auth",
                Description: "Register page travels highlight subtitle"),
            new(
                TextKey: "auth.login.form.tab.email",
                Locale: "zh-CN",
                TextValue: "邮箱登录",
                Category: "auth",
                Description: "登录表单邮箱 tab 文案"),
            new(
                TextKey: "auth.login.form.tab.email",
                Locale: "en-US",
                TextValue: "Email Login",
                Category: "auth",
                Description: "Login form email tab label"),
            new(
                TextKey: "auth.login.form.tab.phone",
                Locale: "zh-CN",
                TextValue: "手机登录",
                Category: "auth",
                Description: "登录表单手机 tab 文案"),
            new(
                TextKey: "auth.login.form.tab.phone",
                Locale: "en-US",
                TextValue: "Phone Login",
                Category: "auth",
                Description: "Login form phone tab label"),
            new(
                TextKey: "auth.login.form.email.label",
                Locale: "zh-CN",
                TextValue: "邮箱",
                Category: "auth",
                Description: "登录表单邮箱字段标题"),
            new(
                TextKey: "auth.login.form.email.label",
                Locale: "en-US",
                TextValue: "Email",
                Category: "auth",
                Description: "Login form email field label"),
            new(
                TextKey: "auth.login.form.email.hint",
                Locale: "zh-CN",
                TextValue: "邮箱",
                Category: "auth",
                Description: "登录表单邮箱字段占位"),
            new(
                TextKey: "auth.login.form.email.hint",
                Locale: "en-US",
                TextValue: "Email",
                Category: "auth",
                Description: "Login form email field hint"),
            new(
                TextKey: "auth.login.form.password.label",
                Locale: "zh-CN",
                TextValue: "密码",
                Category: "auth",
                Description: "登录表单密码字段标题"),
            new(
                TextKey: "auth.login.form.password.label",
                Locale: "en-US",
                TextValue: "Password",
                Category: "auth",
                Description: "Login form password field label"),
            new(
                TextKey: "auth.login.form.password.hint",
                Locale: "zh-CN",
                TextValue: "密码",
                Category: "auth",
                Description: "登录表单密码字段占位"),
            new(
                TextKey: "auth.login.form.password.hint",
                Locale: "en-US",
                TextValue: "Password",
                Category: "auth",
                Description: "Login form password field hint"),
            new(
                TextKey: "auth.login.form.remember_me",
                Locale: "zh-CN",
                TextValue: "记住我",
                Category: "auth",
                Description: "登录表单记住我文案"),
            new(
                TextKey: "auth.login.form.remember_me",
                Locale: "en-US",
                TextValue: "Remember Me",
                Category: "auth",
                Description: "Login form remember me label"),
            new(
                TextKey: "auth.login.form.forgot_password",
                Locale: "zh-CN",
                TextValue: "忘记密码?",
                Category: "auth",
                Description: "登录表单忘记密码文案"),
            new(
                TextKey: "auth.login.form.forgot_password",
                Locale: "en-US",
                TextValue: "Forgot Password?",
                Category: "auth",
                Description: "Login form forgot password label"),
            new(
                TextKey: "auth.login.form.submit_email_button",
                Locale: "zh-CN",
                TextValue: "点击登录/注册",
                Category: "auth",
                Description: "登录表单邮箱登录提交按钮"),
            new(
                TextKey: "auth.login.form.submit_email_button",
                Locale: "en-US",
                TextValue: "Login / Register",
                Category: "auth",
                Description: "Login form email submit button"),
            new(
                TextKey: "auth.login.form.phone.label",
                Locale: "zh-CN",
                TextValue: "手机号",
                Category: "auth",
                Description: "登录表单手机号字段标题"),
            new(
                TextKey: "auth.login.form.phone.label",
                Locale: "en-US",
                TextValue: "Phone Number",
                Category: "auth",
                Description: "Login form phone field label"),
            new(
                TextKey: "auth.login.form.phone.hint",
                Locale: "zh-CN",
                TextValue: "请输入手机号",
                Category: "auth",
                Description: "登录表单手机号字段占位"),
            new(
                TextKey: "auth.login.form.phone.hint",
                Locale: "en-US",
                TextValue: "Enter your phone number",
                Category: "auth",
                Description: "Login form phone field hint"),
            new(
                TextKey: "auth.login.form.sms_code.label",
                Locale: "zh-CN",
                TextValue: "验证码",
                Category: "auth",
                Description: "登录表单验证码字段标题"),
            new(
                TextKey: "auth.login.form.sms_code.label",
                Locale: "en-US",
                TextValue: "Verification Code",
                Category: "auth",
                Description: "Login form sms code field label"),
            new(
                TextKey: "auth.login.form.sms_code.hint",
                Locale: "zh-CN",
                TextValue: "请输入验证码",
                Category: "auth",
                Description: "登录表单验证码字段占位"),
            new(
                TextKey: "auth.login.form.sms_code.hint",
                Locale: "en-US",
                TextValue: "Enter verification code",
                Category: "auth",
                Description: "Login form sms code field hint"),
            new(
                TextKey: "auth.login.form.sms_code.send_button",
                Locale: "zh-CN",
                TextValue: "获取验证码",
                Category: "auth",
                Description: "登录表单发送验证码按钮"),
            new(
                TextKey: "auth.login.form.sms_code.send_button",
                Locale: "en-US",
                TextValue: "Send Code",
                Category: "auth",
                Description: "Login form sms code send button"),
            new(
                TextKey: "auth.login.form.sms_code.countdown_template",
                Locale: "zh-CN",
                TextValue: "{seconds}s",
                Category: "auth",
                Description: "登录表单短信验证码倒计时模板"),
            new(
                TextKey: "auth.login.form.sms_code.countdown_template",
                Locale: "en-US",
                TextValue: "{seconds}s",
                Category: "auth",
                Description: "Login form sms code countdown template"),
            new(
                TextKey: "auth.login.form.submit_phone_button",
                Locale: "zh-CN",
                TextValue: "点击登录/注册",
                Category: "auth",
                Description: "登录表单手机登录提交按钮"),
            new(
                TextKey: "auth.login.form.submit_phone_button",
                Locale: "en-US",
                TextValue: "Login / Register",
                Category: "auth",
                Description: "Login form phone submit button"),
            new(
                TextKey: "auth.login.form.error.email_required",
                Locale: "zh-CN",
                TextValue: "请输入邮箱",
                Category: "auth",
                Description: "登录表单邮箱必填错误提示"),
            new(
                TextKey: "auth.login.form.error.email_required",
                Locale: "en-US",
                TextValue: "Please enter your email",
                Category: "auth",
                Description: "Login form email required validation"),
            new(
                TextKey: "auth.login.form.error.email_invalid",
                Locale: "zh-CN",
                TextValue: "请输入有效的邮箱地址",
                Category: "auth",
                Description: "登录表单邮箱格式错误提示"),
            new(
                TextKey: "auth.login.form.error.email_invalid",
                Locale: "en-US",
                TextValue: "Please enter a valid email address",
                Category: "auth",
                Description: "Login form email invalid validation"),
            new(
                TextKey: "auth.login.form.error.password_required",
                Locale: "zh-CN",
                TextValue: "请输入密码",
                Category: "auth",
                Description: "登录表单密码必填错误提示"),
            new(
                TextKey: "auth.login.form.error.password_required",
                Locale: "en-US",
                TextValue: "Please enter your password",
                Category: "auth",
                Description: "Login form password required validation"),
            new(
                TextKey: "auth.login.form.error.phone_required",
                Locale: "zh-CN",
                TextValue: "请输入手机号",
                Category: "auth",
                Description: "登录表单手机号必填错误提示"),
            new(
                TextKey: "auth.login.form.error.phone_required",
                Locale: "en-US",
                TextValue: "Please enter your phone number",
                Category: "auth",
                Description: "Login form phone required validation"),
            new(
                TextKey: "auth.login.form.error.phone_invalid",
                Locale: "zh-CN",
                TextValue: "请输入有效的手机号",
                Category: "auth",
                Description: "登录表单手机号格式错误提示"),
            new(
                TextKey: "auth.login.form.error.phone_invalid",
                Locale: "en-US",
                TextValue: "Please enter a valid phone number",
                Category: "auth",
                Description: "Login form phone invalid validation"),
            new(
                TextKey: "auth.login.form.error.sms_code_required",
                Locale: "zh-CN",
                TextValue: "请输入验证码",
                Category: "auth",
                Description: "登录表单验证码必填错误提示"),
            new(
                TextKey: "auth.login.form.error.sms_code_required",
                Locale: "en-US",
                TextValue: "Please enter the verification code",
                Category: "auth",
                Description: "Login form sms code required validation"),
            new(
                TextKey: "auth.register.form.username.label",
                Locale: "zh-CN",
                TextValue: "用户名",
                Category: "auth",
                Description: "注册表单用户名字段标题"),
            new(
                TextKey: "auth.register.form.username.label",
                Locale: "en-US",
                TextValue: "Username",
                Category: "auth",
                Description: "Register form username field label"),
            new(
                TextKey: "auth.register.form.username.hint",
                Locale: "zh-CN",
                TextValue: "选择您的用户名",
                Category: "auth",
                Description: "注册表单用户名字段占位"),
            new(
                TextKey: "auth.register.form.username.hint",
                Locale: "en-US",
                TextValue: "Choose your username",
                Category: "auth",
                Description: "Register form username field hint"),
            new(
                TextKey: "auth.register.form.email.label",
                Locale: "zh-CN",
                TextValue: "邮箱",
                Category: "auth",
                Description: "注册表单邮箱字段标题"),
            new(
                TextKey: "auth.register.form.email.label",
                Locale: "en-US",
                TextValue: "Email",
                Category: "auth",
                Description: "Register form email field label"),
            new(
                TextKey: "auth.register.form.email.hint",
                Locale: "zh-CN",
                TextValue: "邮箱",
                Category: "auth",
                Description: "注册表单邮箱字段占位"),
            new(
                TextKey: "auth.register.form.email.hint",
                Locale: "en-US",
                TextValue: "Email",
                Category: "auth",
                Description: "Register form email field hint"),
            new(
                TextKey: "auth.register.form.verification_code.label",
                Locale: "zh-CN",
                TextValue: "验证码",
                Category: "auth",
                Description: "注册表单验证码字段标题"),
            new(
                TextKey: "auth.register.form.verification_code.label",
                Locale: "en-US",
                TextValue: "Verification Code",
                Category: "auth",
                Description: "Register form verification code label"),
            new(
                TextKey: "auth.register.form.verification_code.hint",
                Locale: "zh-CN",
                TextValue: "请输入验证码",
                Category: "auth",
                Description: "注册表单验证码字段占位"),
            new(
                TextKey: "auth.register.form.verification_code.hint",
                Locale: "en-US",
                TextValue: "Enter verification code",
                Category: "auth",
                Description: "Register form verification code hint"),
            new(
                TextKey: "auth.register.form.verification_code.send_button",
                Locale: "zh-CN",
                TextValue: "获取验证码",
                Category: "auth",
                Description: "注册表单发送验证码按钮"),
            new(
                TextKey: "auth.register.form.verification_code.send_button",
                Locale: "en-US",
                TextValue: "Send Code",
                Category: "auth",
                Description: "Register form verification code send button"),
            new(
                TextKey: "auth.register.form.verification_code.countdown_template",
                Locale: "zh-CN",
                TextValue: "{seconds}s",
                Category: "auth",
                Description: "注册表单验证码倒计时模板"),
            new(
                TextKey: "auth.register.form.verification_code.countdown_template",
                Locale: "en-US",
                TextValue: "{seconds}s",
                Category: "auth",
                Description: "Register form verification code countdown template"),
            new(
                TextKey: "auth.register.form.verification_code.resend_button",
                Locale: "zh-CN",
                TextValue: "重新发送",
                Category: "auth",
                Description: "注册表单重发验证码按钮"),
            new(
                TextKey: "auth.register.form.verification_code.resend_button",
                Locale: "en-US",
                TextValue: "Resend",
                Category: "auth",
                Description: "Register form verification code resend button"),
            new(
                TextKey: "auth.register.form.password.label",
                Locale: "zh-CN",
                TextValue: "密码",
                Category: "auth",
                Description: "注册表单密码字段标题"),
            new(
                TextKey: "auth.register.form.password.label",
                Locale: "en-US",
                TextValue: "Password",
                Category: "auth",
                Description: "Register form password field label"),
            new(
                TextKey: "auth.register.form.password.hint",
                Locale: "zh-CN",
                TextValue: "创建密码",
                Category: "auth",
                Description: "注册表单密码字段占位"),
            new(
                TextKey: "auth.register.form.password.hint",
                Locale: "en-US",
                TextValue: "Create a password",
                Category: "auth",
                Description: "Register form password field hint"),
            new(
                TextKey: "auth.register.form.confirm_password.label",
                Locale: "zh-CN",
                TextValue: "确认密码",
                Category: "auth",
                Description: "注册表单确认密码字段标题"),
            new(
                TextKey: "auth.register.form.confirm_password.label",
                Locale: "en-US",
                TextValue: "Confirm Password",
                Category: "auth",
                Description: "Register form confirm password label"),
            new(
                TextKey: "auth.register.form.confirm_password.hint",
                Locale: "zh-CN",
                TextValue: "重新输入密码",
                Category: "auth",
                Description: "注册表单确认密码字段占位"),
            new(
                TextKey: "auth.register.form.confirm_password.hint",
                Locale: "en-US",
                TextValue: "Re-enter your password",
                Category: "auth",
                Description: "Register form confirm password hint"),
            new(
                TextKey: "auth.register.form.submit_button",
                Locale: "zh-CN",
                TextValue: "加入行途",
                Category: "auth",
                Description: "注册表单主提交按钮"),
            new(
                TextKey: "auth.register.form.submit_button",
                Locale: "en-US",
                TextValue: "Join Go-Nomads",
                Category: "auth",
                Description: "Register form submit button"),
            new(
                TextKey: "auth.register.form.toast.terms_required_title",
                Locale: "zh-CN",
                TextValue: "需要同意条款",
                Category: "auth",
                Description: "注册表单未勾选条款提示标题"),
            new(
                TextKey: "auth.register.form.toast.terms_required_title",
                Locale: "en-US",
                TextValue: "Terms Required",
                Category: "auth",
                Description: "Register form terms required toast title"),
            new(
                TextKey: "auth.register.form.toast.terms_required_message",
                Locale: "zh-CN",
                TextValue: "请同意服务条款和社区准则",
                Category: "auth",
                Description: "注册表单未勾选条款提示内容"),
            new(
                TextKey: "auth.register.form.toast.terms_required_message",
                Locale: "en-US",
                TextValue: "Please agree to the Terms of Service and Community Guidelines",
                Category: "auth",
                Description: "Register form terms required toast message"),
            new(
                TextKey: "auth.register.form.toast.welcome_message",
                Locale: "zh-CN",
                TextValue: "欢迎加入 Nomads 社区!",
                Category: "auth",
                Description: "注册成功欢迎提示内容"),
            new(
                TextKey: "auth.register.form.toast.welcome_message",
                Locale: "en-US",
                TextValue: "Welcome to the Nomads community!",
                Category: "auth",
                Description: "Register success welcome toast message"),
            new(
                TextKey: "auth.register.form.toast.success_title",
                Locale: "zh-CN",
                TextValue: "成功",
                Category: "auth",
                Description: "注册成功提示标题"),
            new(
                TextKey: "auth.register.form.toast.success_title",
                Locale: "en-US",
                TextValue: "Success",
                Category: "auth",
                Description: "Register success toast title"),
            new(
                TextKey: "auth.register.form.error.username_required",
                Locale: "zh-CN",
                TextValue: "请输入用户名",
                Category: "auth",
                Description: "注册表单用户名必填错误提示"),
            new(
                TextKey: "auth.register.form.error.username_required",
                Locale: "en-US",
                TextValue: "Please enter a username",
                Category: "auth",
                Description: "Register form username required validation"),
            new(
                TextKey: "auth.register.form.error.username_min_length",
                Locale: "zh-CN",
                TextValue: "用户名至少需要 2 个字符",
                Category: "auth",
                Description: "注册表单用户名长度错误提示"),
            new(
                TextKey: "auth.register.form.error.username_min_length",
                Locale: "en-US",
                TextValue: "Username must be at least 2 characters",
                Category: "auth",
                Description: "Register form username min length validation"),
            new(
                TextKey: "auth.register.form.error.email_required",
                Locale: "zh-CN",
                TextValue: "请输入邮箱",
                Category: "auth",
                Description: "注册表单邮箱必填错误提示"),
            new(
                TextKey: "auth.register.form.error.email_required",
                Locale: "en-US",
                TextValue: "Please enter your email",
                Category: "auth",
                Description: "Register form email required validation"),
            new(
                TextKey: "auth.register.form.error.email_invalid",
                Locale: "zh-CN",
                TextValue: "请输入有效的邮箱地址",
                Category: "auth",
                Description: "注册表单邮箱格式错误提示"),
            new(
                TextKey: "auth.register.form.error.email_invalid",
                Locale: "en-US",
                TextValue: "Please enter a valid email address",
                Category: "auth",
                Description: "Register form email invalid validation"),
            new(
                TextKey: "auth.register.form.error.verification_code_required",
                Locale: "zh-CN",
                TextValue: "请输入验证码",
                Category: "auth",
                Description: "注册表单验证码必填错误提示"),
            new(
                TextKey: "auth.register.form.error.verification_code_required",
                Locale: "en-US",
                TextValue: "Please enter the verification code",
                Category: "auth",
                Description: "Register form verification code required validation"),
            new(
                TextKey: "auth.register.form.error.verification_code_length",
                Locale: "zh-CN",
                TextValue: "验证码必须为 6 位",
                Category: "auth",
                Description: "注册表单验证码长度错误提示"),
            new(
                TextKey: "auth.register.form.error.verification_code_length",
                Locale: "en-US",
                TextValue: "Verification code must be 6 digits",
                Category: "auth",
                Description: "Register form verification code length validation"),
            new(
                TextKey: "auth.register.form.error.password_required",
                Locale: "zh-CN",
                TextValue: "请输入密码",
                Category: "auth",
                Description: "注册表单密码必填错误提示"),
            new(
                TextKey: "auth.register.form.error.password_required",
                Locale: "en-US",
                TextValue: "Please enter a password",
                Category: "auth",
                Description: "Register form password required validation"),
            new(
                TextKey: "auth.register.form.error.password_min_length",
                Locale: "zh-CN",
                TextValue: "密码至少需要 6 个字符",
                Category: "auth",
                Description: "注册表单密码长度错误提示"),
            new(
                TextKey: "auth.register.form.error.password_min_length",
                Locale: "en-US",
                TextValue: "Password must be at least 6 characters",
                Category: "auth",
                Description: "Register form password min length validation"),
            new(
                TextKey: "auth.register.form.error.confirm_password_required",
                Locale: "zh-CN",
                TextValue: "请确认密码",
                Category: "auth",
                Description: "注册表单确认密码必填错误提示"),
            new(
                TextKey: "auth.register.form.error.confirm_password_required",
                Locale: "en-US",
                TextValue: "Please confirm your password",
                Category: "auth",
                Description: "Register form confirm password required validation"),
            new(
                TextKey: "auth.register.form.error.passwords_not_match",
                Locale: "zh-CN",
                TextValue: "两次输入的密码不一致",
                Category: "auth",
                Description: "注册表单两次密码不一致错误提示"),
            new(
                TextKey: "auth.register.form.error.passwords_not_match",
                Locale: "en-US",
                TextValue: "Passwords do not match",
                Category: "auth",
                Description: "Register form passwords mismatch validation"),
            new(
                TextKey: "auth.login.feedback.terms_required_title",
                Locale: "zh-CN",
                TextValue: "需要同意条款",
                Category: "auth",
                Description: "登录页未勾选条款提示标题"),
            new(
                TextKey: "auth.login.feedback.terms_required_title",
                Locale: "en-US",
                TextValue: "Terms Required",
                Category: "auth",
                Description: "Login terms required toast title"),
            new(
                TextKey: "auth.login.feedback.terms_required_message",
                Locale: "zh-CN",
                TextValue: "请先同意服务条款与隐私政策",
                Category: "auth",
                Description: "登录页未勾选条款提示内容"),
            new(
                TextKey: "auth.login.feedback.terms_required_message",
                Locale: "en-US",
                TextValue: "Please agree to the Terms of Service and Privacy Policy first",
                Category: "auth",
                Description: "Login terms required toast message"),
            new(
                TextKey: "auth.login.feedback.phone_required",
                Locale: "zh-CN",
                TextValue: "请输入手机号",
                Category: "auth",
                Description: "登录页手机号为空提示"),
            new(
                TextKey: "auth.login.feedback.phone_required",
                Locale: "en-US",
                TextValue: "Please enter your phone number",
                Category: "auth",
                Description: "Login phone required message"),
            new(
                TextKey: "auth.login.feedback.phone_invalid",
                Locale: "zh-CN",
                TextValue: "请输入有效的中国大陆手机号",
                Category: "auth",
                Description: "登录页手机号格式错误提示"),
            new(
                TextKey: "auth.login.feedback.phone_invalid",
                Locale: "en-US",
                TextValue: "Please enter a valid mainland China phone number",
                Category: "auth",
                Description: "Login phone invalid message"),
            new(
                TextKey: "auth.login.feedback.sms_code_sent",
                Locale: "zh-CN",
                TextValue: "验证码已发送",
                Category: "auth",
                Description: "登录页短信验证码发送成功提示"),
            new(
                TextKey: "auth.login.feedback.sms_code_sent",
                Locale: "en-US",
                TextValue: "Verification code sent",
                Category: "auth",
                Description: "Login sms code sent message"),
            new(
                TextKey: "auth.login.feedback.send_failed",
                Locale: "zh-CN",
                TextValue: "发送失败，请稍后重试",
                Category: "auth",
                Description: "登录页验证码发送失败兜底提示"),
            new(
                TextKey: "auth.login.feedback.send_failed",
                Locale: "en-US",
                TextValue: "Send failed, please try again later",
                Category: "auth",
                Description: "Login send failed fallback message"),
            new(
                TextKey: "auth.login.feedback.send_sms_failed",
                Locale: "zh-CN",
                TextValue: "发送验证码失败，请稍后重试",
                Category: "auth",
                Description: "登录页短信发送异常提示"),
            new(
                TextKey: "auth.login.feedback.send_sms_failed",
                Locale: "en-US",
                TextValue: "Failed to send verification code, please try again later",
                Category: "auth",
                Description: "Login send sms failed message"),
            new(
                TextKey: "auth.login.feedback.welcome_back",
                Locale: "zh-CN",
                TextValue: "欢迎回来",
                Category: "auth",
                Description: "登录成功欢迎语"),
            new(
                TextKey: "auth.login.feedback.welcome_back",
                Locale: "en-US",
                TextValue: "Welcome back",
                Category: "auth",
                Description: "Login welcome back message"),
            new(
                TextKey: "auth.login.feedback.login_success_title",
                Locale: "zh-CN",
                TextValue: "登录成功",
                Category: "auth",
                Description: "登录成功提示标题"),
            new(
                TextKey: "auth.login.feedback.login_success_title",
                Locale: "en-US",
                TextValue: "Login Successful",
                Category: "auth",
                Description: "Login success toast title"),
            new(
                TextKey: "auth.login.feedback.invalid_email_or_password",
                Locale: "zh-CN",
                TextValue: "邮箱或密码错误",
                Category: "auth",
                Description: "登录页邮箱密码错误提示"),
            new(
                TextKey: "auth.login.feedback.invalid_email_or_password",
                Locale: "en-US",
                TextValue: "Invalid email or password",
                Category: "auth",
                Description: "Login invalid email or password message"),
            new(
                TextKey: "auth.login.feedback.login_failed_title",
                Locale: "zh-CN",
                TextValue: "登录失败",
                Category: "auth",
                Description: "登录失败提示标题"),
            new(
                TextKey: "auth.login.feedback.login_failed_title",
                Locale: "en-US",
                TextValue: "Login Failed",
                Category: "auth",
                Description: "Login failed toast title"),
            new(
                TextKey: "auth.login.feedback.unknown_error_retry",
                Locale: "zh-CN",
                TextValue: "登录失败，请重试",
                Category: "auth",
                Description: "登录未知异常兜底提示"),
            new(
                TextKey: "auth.login.feedback.unknown_error_retry",
                Locale: "en-US",
                TextValue: "Login failed, please try again",
                Category: "auth",
                Description: "Login unknown error retry message"),
            new(
                TextKey: "auth.login.feedback.login_failed_retry",
                Locale: "zh-CN",
                TextValue: "登录失败，请重试",
                Category: "auth",
                Description: "登录失败重试提示"),
            new(
                TextKey: "auth.login.feedback.login_failed_retry",
                Locale: "en-US",
                TextValue: "Login failed, please try again",
                Category: "auth",
                Description: "Login failed retry message"),
            new(
                TextKey: "auth.login.feedback.sms_code_invalid_or_expired",
                Locale: "zh-CN",
                TextValue: "验证码无效或已过期",
                Category: "auth",
                Description: "登录页短信验证码无效提示"),
            new(
                TextKey: "auth.login.feedback.sms_code_invalid_or_expired",
                Locale: "en-US",
                TextValue: "The verification code is invalid or expired",
                Category: "auth",
                Description: "Login sms code invalid or expired message"),
            new(
                TextKey: "auth.login.feedback.social_loading_title_template",
                Locale: "zh-CN",
                TextValue: "正在使用 {platform} 登录",
                Category: "auth",
                Description: "社交登录 loading 标题模板"),
            new(
                TextKey: "auth.login.feedback.social_loading_title_template",
                Locale: "en-US",
                TextValue: "Signing in with {platform}",
                Category: "auth",
                Description: "Social login loading title template"),
            new(
                TextKey: "auth.login.feedback.please_wait",
                Locale: "zh-CN",
                TextValue: "请稍候...",
                Category: "auth",
                Description: "登录反馈等待提示"),
            new(
                TextKey: "auth.login.feedback.please_wait",
                Locale: "en-US",
                TextValue: "Please wait...",
                Category: "auth",
                Description: "Login feedback please wait message"),
            new(
                TextKey: "auth.login.feedback.social_failed_template",
                Locale: "zh-CN",
                TextValue: "{platform} 登录失败，请稍后重试",
                Category: "auth",
                Description: "社交登录失败提示模板"),
            new(
                TextKey: "auth.login.feedback.social_failed_template",
                Locale: "en-US",
                TextValue: "{platform} login failed, please try again later",
                Category: "auth",
                Description: "Social login failed message template"),
            new(
                TextKey: "auth.login.social.divider",
                Locale: "zh-CN",
                TextValue: "或使用以下方式继续",
                Category: "auth",
                Description: "登录页社交登录分隔线文案"),
            new(
                TextKey: "auth.login.social.divider",
                Locale: "en-US",
                TextValue: "Or continue with",
                Category: "auth",
                Description: "Login social divider label"),
            new(
                TextKey: "auth.login.social.label.wechat",
                Locale: "zh-CN",
                TextValue: "微信",
                Category: "auth",
                Description: "登录页微信按钮标签"),
            new(
                TextKey: "auth.login.social.label.wechat",
                Locale: "en-US",
                TextValue: "WeChat",
                Category: "auth",
                Description: "Login social WeChat label"),
            new(
                TextKey: "auth.login.social.label.qq",
                Locale: "zh-CN",
                TextValue: "QQ",
                Category: "auth",
                Description: "登录页 QQ 按钮标签"),
            new(
                TextKey: "auth.login.social.label.qq",
                Locale: "en-US",
                TextValue: "QQ",
                Category: "auth",
                Description: "Login social QQ label"),
            new(
                TextKey: "auth.login.social.label.apple",
                Locale: "zh-CN",
                TextValue: "Apple",
                Category: "auth",
                Description: "登录页 Apple 按钮标签"),
            new(
                TextKey: "auth.login.social.label.apple",
                Locale: "en-US",
                TextValue: "Apple",
                Category: "auth",
                Description: "Login social Apple label"),
            new(
                TextKey: "auth.login.social.label.google",
                Locale: "zh-CN",
                TextValue: "Google",
                Category: "auth",
                Description: "登录页 Google 按钮标签"),
            new(
                TextKey: "auth.login.social.label.google",
                Locale: "en-US",
                TextValue: "Google",
                Category: "auth",
                Description: "Login social Google label"),
            new(
                TextKey: "auth.login.social.label.twitter",
                Locale: "zh-CN",
                TextValue: "Twitter",
                Category: "auth",
                Description: "登录页 Twitter 按钮标签"),
            new(
                TextKey: "auth.login.social.label.twitter",
                Locale: "en-US",
                TextValue: "Twitter",
                Category: "auth",
                Description: "Login social Twitter label"),
            new(
                TextKey: "auth.login.social.label.facebook",
                Locale: "zh-CN",
                TextValue: "Facebook",
                Category: "auth",
                Description: "登录页 Facebook 按钮标签"),
            new(
                TextKey: "auth.login.social.label.facebook",
                Locale: "en-US",
                TextValue: "Facebook",
                Category: "auth",
                Description: "Login social Facebook label"),
            new(
                TextKey: "auth.login.social.facebook_unavailable_title",
                Locale: "zh-CN",
                TextValue: "使用 Facebook 继续",
                Category: "auth",
                Description: "登录页 Facebook 未开放提示标题"),
            new(
                TextKey: "auth.login.social.facebook_unavailable_title",
                Locale: "en-US",
                TextValue: "Continue with Facebook",
                Category: "auth",
                Description: "Login social Facebook unavailable title"),
            new(
                TextKey: "auth.login.social.facebook_unavailable_message",
                Locale: "zh-CN",
                TextValue: "该登录方式即将开放",
                Category: "auth",
                Description: "登录页 Facebook 未开放提示内容"),
            new(
                TextKey: "auth.login.social.facebook_unavailable_message",
                Locale: "en-US",
                TextValue: "This login option is coming soon",
                Category: "auth",
                Description: "Login social Facebook unavailable message"),
            new(
                TextKey: "auth.register.feedback.code_sent_to_email",
                Locale: "zh-CN",
                TextValue: "验证码已发送到邮箱",
                Category: "auth",
                Description: "注册页邮箱验证码发送成功提示"),
            new(
                TextKey: "auth.register.feedback.code_sent_to_email",
                Locale: "en-US",
                TextValue: "Verification code sent to your email",
                Category: "auth",
                Description: "Register code sent to email message"),
            new(
                TextKey: "auth.register.feedback.send_failed",
                Locale: "zh-CN",
                TextValue: "发送失败，请稍后重试",
                Category: "auth",
                Description: "注册页验证码发送失败兜底提示"),
            new(
                TextKey: "auth.register.feedback.send_failed",
                Locale: "en-US",
                TextValue: "Send failed, please try again later",
                Category: "auth",
                Description: "Register send failed fallback message"),
            new(
                TextKey: "auth.register.feedback.send_code_failed_retry",
                Locale: "zh-CN",
                TextValue: "验证码发送失败，请稍后重试",
                Category: "auth",
                Description: "注册页验证码发送异常提示"),
            new(
                TextKey: "auth.register.feedback.send_code_failed_retry",
                Locale: "en-US",
                TextValue: "Failed to send verification code, please try again later",
                Category: "auth",
                Description: "Register send code failed retry message"),
            new(
                TextKey: "auth.register.feedback.register_failed_check_input",
                Locale: "zh-CN",
                TextValue: "注册失败，请检查输入信息",
                Category: "auth",
                Description: "注册失败检查输入提示"),
            new(
                TextKey: "auth.register.feedback.register_failed_check_input",
                Locale: "en-US",
                TextValue: "Registration failed, please check your input",
                Category: "auth",
                Description: "Register failed check input message"),
            new(
                TextKey: "auth.register.feedback.register_failed_title",
                Locale: "zh-CN",
                TextValue: "注册失败",
                Category: "auth",
                Description: "注册失败提示标题"),
            new(
                TextKey: "auth.register.feedback.register_failed_title",
                Locale: "en-US",
                TextValue: "Registration Failed",
                Category: "auth",
                Description: "Register failed toast title"),
            new(
                TextKey: "auth.register.feedback.register_failed_process_error",
                Locale: "zh-CN",
                TextValue: "注册失败，请稍后重试",
                Category: "auth",
                Description: "注册失败流程异常兜底提示"),
            new(
                TextKey: "auth.register.feedback.register_failed_process_error",
                Locale: "en-US",
                TextValue: "Registration failed, please try again later",
                Category: "auth",
                Description: "Register failed process error message"),
            new(
                TextKey: "brand.loading.title",
                Locale: "zh-CN",
                TextValue: "行途 Go Nomads",
                Category: "branding",
                Description: "全局 loading 品牌标题"),
            new(
                TextKey: "brand.loading.title",
                Locale: "en-US",
                TextValue: "行途 Go Nomads",
                Category: "branding",
                Description: "Global loading brand title"),
            new(
                TextKey: "brand.loading.tagline",
                Locale: "zh-CN",
                TextValue: "Explore cities, workspaces and community",
                Category: "branding",
                Description: "全局 loading 品牌副标题"),
            new(
                TextKey: "brand.loading.tagline",
                Locale: "en-US",
                TextValue: "Explore cities, workspaces and community",
                Category: "branding",
                Description: "Global loading brand tagline"),
            new(
                TextKey: "brand.footer.copyright",
                Locale: "zh-CN",
                TextValue: "© 大连素辉软件科技有限公司 All Rights Reserved",
                Category: "branding",
                Description: "登录前与法律页底部版权文案"),
            new(
                TextKey: "brand.footer.copyright",
                Locale: "en-US",
                TextValue: "© 大连素辉软件科技有限公司 All Rights Reserved",
                Category: "branding",
                Description: "Footer copyright copy for pre-auth pages"),
            new(
                TextKey: "brand.footer.icp_record",
                Locale: "zh-CN",
                TextValue: "辽ICP备2026001591号",
                Category: "branding",
                Description: "登录前与法律页底部备案号"),
            new(
                TextKey: "brand.footer.icp_record",
                Locale: "en-US",
                TextValue: "辽ICP备2026001591号",
                Category: "branding",
                Description: "Footer ICP filing number for pre-auth pages"),
            new(
                TextKey: "permission.location.purpose_dialog_json",
                Locale: "zh-CN",
                TextValue: JsonSerializer.Serialize(new
                {
                    title = "需要使用您的位置信息",
                    description = "行途需要获取您的位置权限，用于以下功能：",
                    purposes = new[]
                    {
                        "为您推荐附近的城市和目的地",
                        "查找您附近的活动和聚会",
                        "发现附近的共享办公空间",
                        "提供地图导航和位置选择功能"
                    },
                    note = "我们仅在您使用相关功能时获取位置信息，不会在后台持续追踪您的位置。您可以随时在系统设置中关闭位置权限。",
                    confirmText = "继续"
                }),
                Category: "permission",
                Description: "位置权限用途说明弹窗 JSON"),
            new(
                TextKey: "permission.location.purpose_dialog_json",
                Locale: "en-US",
                TextValue: JsonSerializer.Serialize(new
                {
                    title = "Location Permission Required",
                    description = "Go Nomads needs your location permission for the following features:",
                    purposes = new[]
                    {
                        "Recommend nearby cities and destinations for you.",
                        "Find events and meetups around you.",
                        "Discover nearby coworking spaces.",
                        "Provide map navigation and location selection."
                    },
                    note = "We only access your location when you use related features and do not continuously track you in the background. You can turn off location permission anytime in system settings.",
                    confirmText = "Continue"
                }),
                Category: "permission",
                Description: "Location permission purpose dialog JSON"),
            new(
                TextKey: "permission.calendar.purpose_dialog_json",
                Locale: "zh-CN",
                TextValue: JsonSerializer.Serialize(new
                {
                    title = "需要访问您的日历",
                    description = "行途需要获取日历权限，用于以下功能：",
                    purposes = new[]
                    {
                        "将活动和聚会添加到您的日历中",
                        "设置活动提醒，避免错过精彩活动"
                    },
                    note = "我们仅在您主动点击\"添加到日历\"时访问日历，不会读取您的其他日历信息。",
                    confirmText = "继续"
                }),
                Category: "permission",
                Description: "日历权限用途说明弹窗 JSON"),
            new(
                TextKey: "permission.calendar.purpose_dialog_json",
                Locale: "en-US",
                TextValue: JsonSerializer.Serialize(new
                {
                    title = "Calendar Access Required",
                    description = "Go Nomads needs calendar permission for the following features:",
                    purposes = new[]
                    {
                        "Add events and meetups to your calendar.",
                        "Set reminders so you do not miss important events."
                    },
                    note = "We only access your calendar when you explicitly choose \"Add to Calendar\" and do not read your other calendar data.",
                    confirmText = "Continue"
                }),
                Category: "permission",
                Description: "Calendar permission purpose dialog JSON"),
            new(
                TextKey: "permission.notification.purpose_dialog_json",
                Locale: "zh-CN",
                TextValue: JsonSerializer.Serialize(new
                {
                    title = "需要发送通知",
                    description = "行途需要通知权限，用于以下功能：",
                    purposes = new[]
                    {
                        "旅行指南生成完成通知",
                        "新消息和互动提醒",
                        "活动开始前提醒"
                    },
                    note = "您可以随时在应用设置或系统设置中关闭通知。",
                    confirmText = "继续"
                }),
                Category: "permission",
                Description: "通知权限用途说明弹窗 JSON"),
            new(
                TextKey: "permission.notification.purpose_dialog_json",
                Locale: "en-US",
                TextValue: JsonSerializer.Serialize(new
                {
                    title = "Notification Permission Required",
                    description = "Go Nomads uses notification permission for the following features:",
                    purposes = new[]
                    {
                        "Notify you when a travel guide is ready.",
                        "Alert you about new messages and interactions.",
                        "Remind you before an event starts."
                    },
                    note = "You can turn off notifications anytime in the app settings or system settings.",
                    confirmText = "Continue"
                }),
                Category: "permission",
                Description: "Notification permission purpose dialog JSON"),
            new(
                TextKey: "permission.location.dialog.title",
                Locale: "zh-CN",
                TextValue: "需要位置权限",
                Category: "permission",
                Description: "位置权限请求弹窗标题"),
            new(
                TextKey: "permission.location.dialog.title",
                Locale: "en-US",
                TextValue: "Location Permission Required",
                Category: "permission",
                Description: "Location permission request dialog title"),
            new(
                TextKey: "permission.location.dialog.description",
                Locale: "zh-CN",
                TextValue: "我们需要访问您的位置信息,以便为您推荐附近的城市和提供基于位置的服务",
                Category: "permission",
                Description: "位置权限请求弹窗说明"),
            new(
                TextKey: "permission.location.dialog.description",
                Locale: "en-US",
                TextValue: "We need access to your location so we can recommend nearby cities and provide location-based services.",
                Category: "permission",
                Description: "Location permission request dialog description"),
            new(
                TextKey: "permission.location.dialog.cancel_button",
                Locale: "zh-CN",
                TextValue: "取消",
                Category: "permission",
                Description: "位置权限请求弹窗取消按钮"),
            new(
                TextKey: "permission.location.dialog.cancel_button",
                Locale: "en-US",
                TextValue: "Cancel",
                Category: "permission",
                Description: "Location permission request dialog cancel button"),
            new(
                TextKey: "permission.location.dialog.confirm_button",
                Locale: "zh-CN",
                TextValue: "授予权限",
                Category: "permission",
                Description: "位置权限请求弹窗确认按钮"),
            new(
                TextKey: "permission.location.dialog.confirm_button",
                Locale: "en-US",
                TextValue: "Allow Access",
                Category: "permission",
                Description: "Location permission request dialog confirm button"),
            new(
                TextKey: "permission.location.status.loading",
                Locale: "zh-CN",
                TextValue: "正在获取位置...",
                Category: "permission",
                Description: "位置状态卡片加载提示"),
            new(
                TextKey: "permission.location.status.loading",
                Locale: "en-US",
                TextValue: "Fetching your location...",
                Category: "permission",
                Description: "Location status card loading text"),
            new(
                TextKey: "permission.location.status.disabled",
                Locale: "zh-CN",
                TextValue: "位置未启用",
                Category: "permission",
                Description: "位置状态卡片未启用提示"),
            new(
                TextKey: "permission.location.status.disabled",
                Locale: "en-US",
                TextValue: "Location is disabled",
                Category: "permission",
                Description: "Location status card disabled text"),
            new(
                TextKey: "permission.location.status.enable_action",
                Locale: "zh-CN",
                TextValue: "启用",
                Category: "permission",
                Description: "位置状态卡片启用按钮"),
            new(
                TextKey: "permission.location.status.enable_action",
                Locale: "en-US",
                TextValue: "Enable",
                Category: "permission",
                Description: "Location status card enable action")
        };
    }

    private static IReadOnlyList<AppSystemSettingSeed> GetSystemSettingSeeds()
    {
        return new List<AppSystemSettingSeed>
        {
            new(
                Section: "legal_documents",
                SettingKey: "privacy_policy_version",
                Label: "Privacy Policy Version",
                Description: "Current accepted version for first-launch privacy consent",
                ValueType: "string",
                Value: "1.0.0",
                DefaultValue: "1.0.0",
                SortOrder: 10),
            new(
                Section: "legal_documents",
                SettingKey: "terms_of_service_version",
                Label: "Terms of Service Version",
                Description: "Current accepted version for first-launch terms consent",
                ValueType: "string",
                Value: "1.0.0",
                DefaultValue: "1.0.0",
                SortOrder: 20)
        };
    }

    private sealed record AppStaticTextSeed(
        string TextKey,
        string Locale,
        string TextValue,
        string Category,
        string Description);

    private sealed record AppSystemSettingSeed(
        string Section,
        string SettingKey,
        string Label,
        string Description,
        string ValueType,
        string Value,
        string DefaultValue,
        int SortOrder);
}
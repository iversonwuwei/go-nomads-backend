using System.Security.Cryptography;
using System.Text;
using AlibabaCloud.SDK.Dysmsapi20170525;
using AlibabaCloud.SDK.Dysmsapi20170525.Models;
using AlibabaCloud.TeaUtil.Models;
using Microsoft.Extensions.Options;
using Tea;
using UserService.Application.Services;
using UserService.Infrastructure.Configuration;

namespace UserService.Infrastructure.Services;

/// <summary>
///     阿里云短信服务实现
///     使用阿里云官方 SDK 发送短信验证码
/// </summary>
public class AliyunSmsService : IAliyunSmsService
{
    private readonly Client _client;
    private readonly ILogger<AliyunSmsService> _logger;
    private readonly AliyunSmsSettings _settings;

    public AliyunSmsService(
        IOptions<AliyunSmsSettings> settings,
        ILogger<AliyunSmsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // 初始化阿里云 SDK 客户端
        var config = new AlibabaCloud.OpenApiClient.Models.Config
        {
            AccessKeyId = _settings.AccessKeyId,
            AccessKeySecret = _settings.AccessKeySecret,
            Endpoint = _settings.Endpoint
        };
        _client = new Client(config);
    }

    /// <summary>
    ///     发送验证码短信
    /// </summary>
    public async Task<SmsResult> SendVerificationCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📱 准备发送验证码到: {Phone}", MaskPhoneNumber(phoneNumber));

            // 处理手机号（移除 + 号，保留国家区号）
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            // 构建请求
            var sendSmsRequest = new SendSmsRequest
            {
                PhoneNumbers = normalizedPhone,
                SignName = _settings.SignName,
                TemplateCode = _settings.LoginTemplateCode,
                TemplateParam = $"{{\"code\":\"{code}\"}}"
            };

            var runtime = new RuntimeOptions();

            _logger.LogDebug("📤 发送 SMS 请求到阿里云: Phone={Phone}, SignName={SignName}, TemplateCode={TemplateCode}",
                MaskPhoneNumber(phoneNumber), _settings.SignName, _settings.LoginTemplateCode);

            // 发送请求
            var response = await _client.SendSmsWithOptionsAsync(sendSmsRequest, runtime);

            _logger.LogDebug("📥 阿里云响应: Code={Code}, Message={Message}, RequestId={RequestId}",
                response.Body.Code, response.Body.Message, response.Body.RequestId);

            if (response.Body.Code == "OK")
            {
                _logger.LogInformation("✅ 验证码发送成功到 {Phone}, RequestId: {RequestId}, BizId: {BizId}",
                    MaskPhoneNumber(phoneNumber), response.Body.RequestId, response.Body.BizId);

                return SmsResult.Ok("发送成功", response.Body.RequestId, response.Body.BizId);
            }

            _logger.LogWarning("⚠️ 验证码发送失败到 {Phone}: {Code} - {Message}",
                MaskPhoneNumber(phoneNumber), response.Body.Code, response.Body.Message);

            return SmsResult.Fail(response.Body.Message ?? "发送失败", response.Body.Code, response.Body.RequestId);
        }
        catch (TeaException ex)
        {
            _logger.LogError(ex, "❌ 阿里云 SDK 异常: {Phone}, Code={Code}, Message={Message}",
                MaskPhoneNumber(phoneNumber), ex.Code, ex.Message);
            return SmsResult.Fail($"发送短信失败: {ex.Message}", ex.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送验证码异常: {Phone}", MaskPhoneNumber(phoneNumber));
            return SmsResult.Fail($"发送短信失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     生成验证码
    /// </summary>
    public string GenerateVerificationCode(int length = 6)
    {
        var random = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        random.GetBytes(bytes);

        var code = new StringBuilder();
        foreach (var b in bytes) code.Append(b % 10);

        return code.ToString();
    }

    /// <summary>
    ///     规范化手机号
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        // 移除所有非数字字符（保留 +）
        var normalized = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // 移除开头的 + 号
        if (normalized.StartsWith('+')) normalized = normalized[1..];

        return normalized;
    }

    /// <summary>
    ///     脱敏手机号
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 7)
            return "***";

        return phoneNumber[..3] + "****" + phoneNumber[^4..];
    }
}

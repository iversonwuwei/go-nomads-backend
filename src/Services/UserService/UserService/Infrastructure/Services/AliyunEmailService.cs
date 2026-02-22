using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using UserService.Application.Services;
using UserService.Infrastructure.Configuration;

namespace UserService.Infrastructure.Services;

/// <summary>
///     阿里云邮件推送服务实现
///     使用 MailKit + SMTP 连接阿里云 DirectMail 发送邮件
/// </summary>
public class AliyunEmailService : IEmailService
{
    private readonly AliyunEmailSettings _settings;
    private readonly ILogger<AliyunEmailService> _logger;

    public AliyunEmailService(
        IOptions<AliyunEmailSettings> settings,
        ILogger<AliyunEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    ///     发送验证码邮件
    /// </summary>
    public async Task<EmailResult> SendVerificationCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📧 准备发送验证码到: {Email}", MaskEmail(toEmail));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderAddress));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = $"【行途 Go Nomads】密码重置验证码: {code}";

            // 构建 HTML 邮件内容
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = BuildVerificationEmailHtml(code),
                TextBody = $"您的密码重置验证码是: {code}，有效期 {_settings.CodeExpirationMinutes} 分钟。如非本人操作，请忽略此邮件。"
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // 连接 SMTP 服务器
            var secureSocketOptions = _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                secureSocketOptions,
                cancellationToken);

            // 认证
            await client.AuthenticateAsync(
                _settings.SenderAddress,
                _settings.SenderPassword,
                cancellationToken);

            // 发送
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("✅ 验证码邮件发送成功到 {Email}", MaskEmail(toEmail));

            return EmailResult.Ok("验证码已发送到邮箱");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送验证码邮件失败: {Email}", MaskEmail(toEmail));
            return EmailResult.Fail($"发送邮件失败: {ex.Message}");
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
    ///     构建验证码邮件 HTML 内容
    /// </summary>
    private string BuildVerificationEmailHtml(string code)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5f5; margin: 0; padding: 20px; }
                    .container { max-width: 480px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 40px 32px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }
                    .logo { text-align: center; margin-bottom: 24px; font-size: 24px; font-weight: bold; color: #2563eb; }
                    .title { text-align: center; font-size: 18px; color: #333; margin-bottom: 8px; }
                    .desc { text-align: center; font-size: 14px; color: #888; margin-bottom: 32px; }
                    .code-box { text-align: center; background: #f0f7ff; border-radius: 8px; padding: 20px; margin-bottom: 24px; }
                    .code { font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #2563eb; }
                    .expire { text-align: center; font-size: 13px; color: #999; margin-bottom: 24px; }
                    .warning { font-size: 12px; color: #bbb; text-align: center; border-top: 1px solid #eee; padding-top: 16px; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="logo">🌍 行途 Go Nomads</div>
                    <div class="title">密码重置验证码</div>
                    <div class="desc">您正在重置账户密码，请使用以下验证码完成操作</div>
                    <div class="code-box">
                        <div class="code">{{code}}</div>
                    </div>
                    <div class="expire">验证码有效期 <strong>{{_settings.CodeExpirationMinutes}} 分钟</strong>，请尽快使用</div>
                    <div class="warning">如果这不是您的操作，请忽略此邮件。请勿将验证码分享给他人。</div>
                </div>
            </body>
            </html>
            """;
    }

    /// <summary>
    ///     脱敏邮箱
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "***";
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        var name = parts[0];
        var masked = name.Length <= 2
            ? name[..1] + "***"
            : name[..2] + "***" + name[^1..];
        return $"{masked}@{parts[1]}";
    }
}

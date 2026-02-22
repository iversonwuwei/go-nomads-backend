namespace UserService.Infrastructure.Configuration;

/// <summary>
///     阿里云邮件推送服务配置（DirectMail SMTP）
/// </summary>
public class AliyunEmailSettings
{
    public const string SectionName = "AliyunEmail";

    /// <summary>
    ///     SMTP 服务器地址
    /// </summary>
    public string SmtpHost { get; set; } = "smtpdm.aliyun.com";

    /// <summary>
    ///     SMTP 端口（465 = SSL, 25/80 = 非加密）
    /// </summary>
    public int SmtpPort { get; set; } = 465;

    /// <summary>
    ///     是否使用 SSL
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    ///     发信地址（在阿里云 DirectMail 控制台配置）
    /// </summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>
    ///     发信密码（SMTP 密码，在 DirectMail 控制台设置）
    /// </summary>
    public string SenderPassword { get; set; } = string.Empty;

    /// <summary>
    ///     发信人名称
    /// </summary>
    public string SenderName { get; set; } = "行途 Go Nomads";

    /// <summary>
    ///     验证码有效期（分钟）
    /// </summary>
    public int CodeExpirationMinutes { get; set; } = 10;

    /// <summary>
    ///     验证码长度
    /// </summary>
    public int CodeLength { get; set; } = 6;

    /// <summary>
    ///     是否允许测试验证码（123456）
    ///     生产环境应设置为 false
    /// </summary>
    public bool AllowTestCode { get; set; } = false;
}

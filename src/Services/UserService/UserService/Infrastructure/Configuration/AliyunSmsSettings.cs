namespace UserService.Infrastructure.Configuration;

/// <summary>
///     阿里云短信服务配置
/// </summary>
public class AliyunSmsSettings
{
    public const string SectionName = "AliyunSms";

    /// <summary>
    ///     阿里云 AccessKey ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    ///     阿里云 AccessKey Secret
    /// </summary>
    public string AccessKeySecret { get; set; } = string.Empty;

    /// <summary>
    ///     短信签名
    /// </summary>
    public string SignName { get; set; } = string.Empty;

    /// <summary>
    ///     登录验证码模板 Code
    /// </summary>
    public string LoginTemplateCode { get; set; } = "SMS_148695703";

    /// <summary>
    ///     区域 ID
    /// </summary>
    public string RegionId { get; set; } = "cn-hangzhou";

    /// <summary>
    ///     短信 API 端点
    /// </summary>
    public string Endpoint { get; set; } = "dysmsapi.aliyuncs.com";

    /// <summary>
    ///     验证码有效期（分钟）
    /// </summary>
    public int CodeExpirationMinutes { get; set; } = 5;

    /// <summary>
    ///     验证码长度
    /// </summary>
    public int CodeLength { get; set; } = 6;
}

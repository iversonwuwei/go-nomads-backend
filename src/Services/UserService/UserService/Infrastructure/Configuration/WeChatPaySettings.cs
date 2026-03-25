namespace UserService.Infrastructure.Configuration;

/// <summary>
/// 微信支付 V3 配置
/// </summary>
public class WeChatPaySettings
{
    public const string SectionName = "WeChatPay";

    /// <summary>
    /// 微信开放平台 AppId
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 商户号
    /// </summary>
    public string MchId { get; set; } = string.Empty;

    /// <summary>
    /// APIv3 密钥（用于解密回调通知）
    /// </summary>
    public string ApiV3Key { get; set; } = string.Empty;

    /// <summary>
    /// 商户 API 证书序列号
    /// </summary>
    public string CertificateSerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 商户 API 私钥文件路径（PEM 格式，用于签名请求）
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// 支付结果通知回调地址
    /// </summary>
    public string NotifyUrl { get; set; } = string.Empty;
}

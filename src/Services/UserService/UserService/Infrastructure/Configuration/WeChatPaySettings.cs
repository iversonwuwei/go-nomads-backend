namespace UserService.Infrastructure.Configuration;

/// <summary>
///     微信支付 V3 配置
/// </summary>
public class WeChatPaySettings
{
    public const string SectionName = "WeChatPay";

    /// <summary>
    ///     微信开放平台 AppId（与 App 中 fluwx 注册使用的 AppId 一致）
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    ///     商户号 (MchId)
    /// </summary>
    public string MchId { get; set; } = string.Empty;

    /// <summary>
    ///     API v3 密钥（32字节，在商户平台设置）
    /// </summary>
    public string ApiV3Key { get; set; } = string.Empty;

    /// <summary>
    ///     商户 API 证书序列号
    /// </summary>
    public string CertificateSerialNumber { get; set; } = string.Empty;

    /// <summary>
    ///     商户 API 证书私钥 (PEM 格式内容)
    ///     可以直接放 PEM 内容，或者填写文件路径（以 file: 开头）
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    ///     支付结果通知 URL
    /// </summary>
    public string NotifyUrl { get; set; } = string.Empty;

    /// <summary>
    ///     获取私钥内容（支持直接 PEM 内容或 file: 开头的文件路径）
    /// </summary>
    public string GetPrivateKeyContent()
    {
        if (string.IsNullOrEmpty(PrivateKey))
            return string.Empty;

        if (PrivateKey.StartsWith("file:"))
        {
            var filePath = PrivateKey[5..].Trim();
            return File.ReadAllText(filePath);
        }

        return PrivateKey;
    }
}

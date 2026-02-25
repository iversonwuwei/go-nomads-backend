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
    ///     商户 API 证书私钥
    ///     支持三种格式：
    ///     1. Base64 编码的 PEM 内容（推荐，用于 Docker 环境变量注入）
    ///     2. 直接 PEM 内容（以 -----BEGIN 开头）
    ///     3. 文件路径（以 file: 开头）
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    ///     支付结果通知 URL
    /// </summary>
    public string NotifyUrl { get; set; } = string.Empty;

    /// <summary>
    ///     获取私钥 PEM 内容（自动识别 Base64 编码 / 直接 PEM / 文件路径）
    /// </summary>
    public string GetPrivateKeyContent()
    {
        if (string.IsNullOrEmpty(PrivateKey))
            return string.Empty;

        var key = PrivateKey.Trim();

        // 文件路径：以 file: 开头
        if (key.StartsWith("file:"))
        {
            var filePath = key[5..].Trim();
            return File.ReadAllText(filePath);
        }

        // 直接 PEM 内容
        if (key.StartsWith("-----BEGIN"))
            return key;

        // Base64 编码的 PEM 内容（Docker 环境变量注入场景）
        try
        {
            var sanitizedBase64 = new string(key.Where(c =>
                    char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '=')
                .ToArray());

            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(sanitizedBase64));
            if (decoded.StartsWith("-----BEGIN"))
                return decoded;
        }
        catch (FormatException)
        {
            // 不是合法 Base64，原样返回
        }

        return key;
    }
}

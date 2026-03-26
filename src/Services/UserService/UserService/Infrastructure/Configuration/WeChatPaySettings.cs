using System.Security.Cryptography;

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
    ///     商户 API 证书私钥
    ///     支持三种格式：
    ///     1. Base64 编码的 PEM 内容（推荐，用于 Docker 环境变量注入）
    ///     2. 直接 PEM 内容（以 -----BEGIN 开头）
    ///     3. 文件路径（以 file: 开头）
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    ///     微信支付回调地址
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
            return ReadPrivateKeyFromFile(filePath);
        }

        // 兼容旧部署：直接传入裸文件路径（例如 /root/docker-compose/certs/*.pem）
        if (LooksLikeFilePath(key))
            return ReadPrivateKeyFromFile(key);

        // 直接 PEM 内容
        if (key.StartsWith("-----BEGIN"))
            return ConvertPkcs1ToPkcs8(key);

        // Base64 编码的 PEM 内容（Docker 环境变量注入场景）
        var decodedPem = TryDecodeBase64Pem(key);
        if (!string.IsNullOrEmpty(decodedPem))
            return ConvertPkcs1ToPkcs8(decodedPem);

        return key;
    }

    private static bool LooksLikeFilePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (Path.IsPathRooted(value))
            return true;

        if (value.StartsWith("./") || value.StartsWith("../"))
            return true;

        return value.EndsWith(".pem", StringComparison.OrdinalIgnoreCase)
               || value.EndsWith(".key", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadPrivateKeyFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new InvalidOperationException($"微信支付私钥文件不存在: {filePath}");

        try
        {
            return ConvertPkcs1ToPkcs8(File.ReadAllText(filePath));
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"读取微信支付私钥文件失败: {filePath}", ex);
        }
    }

    private static string? TryDecodeBase64Pem(string key)
    {
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
            // 不是合法 Base64，忽略并交由后续逻辑处理
        }

        return null;
    }

    /// <summary>
    ///     将 PKCS#1 (RSA PRIVATE KEY) 格式转为 PKCS#8 (PRIVATE KEY) 格式
    ///     SKIT SDK 仅支持 PKCS#8
    /// </summary>
    private static string ConvertPkcs1ToPkcs8(string pem)
    {
        if (!pem.Contains("-----BEGIN RSA PRIVATE KEY-----"))
            return pem;

        var base64 = pem
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
            .Replace("-----END RSA PRIVATE KEY-----", "")
            .Replace("\r", "").Replace("\n", "").Trim();

        var rsaKeyBytes = Convert.FromBase64String(base64);
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(rsaKeyBytes, out _);

        var pkcs8Bytes = rsa.ExportPkcs8PrivateKey();
        var pkcs8Base64 = Convert.ToBase64String(pkcs8Bytes, Base64FormattingOptions.InsertLineBreaks);
        return $"-----BEGIN PRIVATE KEY-----\n{pkcs8Base64}\n-----END PRIVATE KEY-----";
    }
}

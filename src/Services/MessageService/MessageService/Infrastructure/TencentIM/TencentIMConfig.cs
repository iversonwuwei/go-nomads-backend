namespace MessageService.Infrastructure.TencentIM;

/// <summary>
/// 腾讯云IM配置
/// </summary>
public class TencentIMConfig
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "TencentIM";

    /// <summary>
    /// 腾讯云IM应用ID
    /// </summary>
    public long SdkAppId { get; set; }

    /// <summary>
    /// 密钥（用于生成UserSig）
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 管理员账号（用于调用REST API）
    /// </summary>
    public string AdminUserId { get; set; } = "administrator";

    /// <summary>
    /// UserSig有效期（秒），默认7天
    /// </summary>
    public int UserSigExpireSeconds { get; set; } = 604800;

    /// <summary>
    /// REST API基础URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://console.tim.qq.com";
}

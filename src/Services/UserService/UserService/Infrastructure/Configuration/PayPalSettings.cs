namespace UserService.Infrastructure.Configuration;

/// <summary>
///     PayPal 配置
/// </summary>
public class PayPalSettings
{
    public const string SectionName = "PayPal";

    /// <summary>
    ///     Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    ///     Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    ///     是否使用沙箱环境
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    ///     PayPal API 基础 URL
    /// </summary>
    public string BaseUrl => UseSandbox
        ? "https://api-m.sandbox.paypal.com"
        : "https://api-m.paypal.com";

    /// <summary>
    ///     支付成功后的返回 URL
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    ///     支付取消后的返回 URL
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Webhook ID (用于验证 webhook 签名)
    /// </summary>
    public string WebhookId { get; set; } = string.Empty;
}

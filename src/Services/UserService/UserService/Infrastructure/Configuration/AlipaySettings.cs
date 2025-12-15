namespace UserService.Infrastructure.Configuration;

/// <summary>
/// 支付宝配置
/// </summary>
public class AlipaySettings
{
    public string AppId { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string AlipayPublicKey { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public bool UseSandbox { get; set; } = true;
    
    /// <summary>
    /// 合作伙伴 ID (PID)，用于授权登录
    /// </summary>
    public string? PartnerId { get; set; }
    
    /// <summary>
    /// 获取网关地址
    /// </summary>
    public string GatewayUrl => UseSandbox 
        ? "https://openapi-sandbox.dl.alipaydev.com/gateway.do"
        : "https://openapi.alipay.com/gateway.do";
}

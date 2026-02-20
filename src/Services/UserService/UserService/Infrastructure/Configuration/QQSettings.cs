namespace UserService.Infrastructure.Configuration;

/// <summary>
/// QQ 互联开放平台配置
/// </summary>
public class QQSettings
{
    public const string SectionName = "QQ";
    
    /// <summary>
    /// QQ 互联 APP ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;
    
    /// <summary>
    /// QQ 互联 APP Key
    /// </summary>
    public string AppKey { get; set; } = string.Empty;
    
    /// <summary>
    /// QQ OAuth Token 端点
    /// </summary>
    public string TokenUrl { get; set; } = "https://graph.qq.com/oauth2.0/token";
    
    /// <summary>
    /// QQ 获取 OpenID 端点
    /// </summary>
    public string OpenIdUrl { get; set; } = "https://graph.qq.com/oauth2.0/me";
    
    /// <summary>
    /// QQ 用户信息端点
    /// </summary>
    public string UserInfoUrl { get; set; } = "https://graph.qq.com/user/get_user_info";
    
    /// <summary>
    /// QQ OAuth 回调地址
    /// </summary>
    public string RedirectUri { get; set; } = "gonomads://qq-callback";
}

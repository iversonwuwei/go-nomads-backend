namespace UserService.Infrastructure.Configuration;

/// <summary>
/// 抖音开放平台配置
/// </summary>
public class DouyinSettings
{
    public const string SectionName = "Douyin";
    
    /// <summary>
    /// 抖音开放平台 Client Key
    /// </summary>
    public string ClientKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 抖音开放平台 Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// 抖音 OAuth Token 端点
    /// </summary>
    public string TokenUrl { get; set; } = "https://open.douyin.com/oauth/access_token/";
    
    /// <summary>
    /// 抖音用户信息端点
    /// </summary>
    public string UserInfoUrl { get; set; } = "https://open.douyin.com/oauth/userinfo/";
}

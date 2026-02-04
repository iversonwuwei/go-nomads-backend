using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure.Services;

/// <summary>
///     微信 OAuth 服务
///     用于处理微信登录的 code 换取 access_token 和用户信息
/// </summary>
public interface IWeChatOAuthService
{
    /// <summary>
    ///     通过授权码获取微信用户信息
    /// </summary>
    /// <param name="code">微信授权码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>微信用户信息</returns>
    Task<WeChatUserInfo?> GetUserInfoByCodeAsync(string code, CancellationToken cancellationToken = default);
}

/// <summary>
///     微信用户信息
/// </summary>
public class WeChatUserInfo
{
    /// <summary>
    ///     用户唯一标识（同一应用下唯一）
    /// </summary>
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    ///     用户统一标识（同一开放平台下的多个应用唯一）
    /// </summary>
    public string? UnionId { get; set; }

    /// <summary>
    ///     用户昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    ///     用户头像 URL
    /// </summary>
    public string? HeadImgUrl { get; set; }

    /// <summary>
    ///     用户性别（1: 男, 2: 女, 0: 未知）
    /// </summary>
    public int Sex { get; set; }

    /// <summary>
    ///     用户所在省份
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    ///     用户所在城市
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    ///     用户所在国家
    /// </summary>
    public string? Country { get; set; }
}

/// <summary>
///     微信 OAuth 服务实现
/// </summary>
public class WeChatOAuthService : IWeChatOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeChatOAuthService> _logger;

    // 微信 API 地址
    private const string AccessTokenUrl = "https://api.weixin.qq.com/sns/oauth2/access_token";
    private const string UserInfoUrl = "https://api.weixin.qq.com/sns/userinfo";

    public WeChatOAuthService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<WeChatOAuthService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    ///     通过授权码获取微信用户信息
    /// </summary>
    public async Task<WeChatUserInfo?> GetUserInfoByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 获取 AppId 和 AppSecret
            var appId = _configuration["WeChat:AppId"];
            var appSecret = _configuration["WeChat:AppSecret"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
            {
                _logger.LogError("❌ 微信 AppId 或 AppSecret 未配置");
                throw new InvalidOperationException("微信登录配置错误");
            }

            // 2. 用 code 换取 access_token
            var tokenUrl = $"{AccessTokenUrl}?appid={appId}&secret={appSecret}&code={code}&grant_type=authorization_code";
            
            _logger.LogInformation("🔐 微信登录: 正在用 code 换取 access_token...");
            
            var tokenResponse = await _httpClient.GetAsync(tokenUrl, cancellationToken);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("微信 access_token 响应: {Response}", tokenContent);

            var tokenResult = JsonSerializer.Deserialize<WeChatTokenResponse>(tokenContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResult == null || !string.IsNullOrEmpty(tokenResult.ErrCode?.ToString()) && tokenResult.ErrCode != 0)
            {
                _logger.LogError("❌ 微信获取 access_token 失败: errcode={ErrCode}, errmsg={ErrMsg}",
                    tokenResult?.ErrCode, tokenResult?.ErrMsg);
                throw new InvalidOperationException($"微信授权失败: {tokenResult?.ErrMsg ?? "未知错误"}");
            }

            if (string.IsNullOrEmpty(tokenResult.AccessToken) || string.IsNullOrEmpty(tokenResult.OpenId))
            {
                _logger.LogError("❌ 微信返回的 access_token 或 openid 为空");
                throw new InvalidOperationException("微信授权失败: 返回数据不完整");
            }

            _logger.LogInformation("✅ 微信 access_token 获取成功: openid={OpenId}", tokenResult.OpenId);

            // 3. 用 access_token 获取用户信息
            var userInfoUrl = $"{UserInfoUrl}?access_token={tokenResult.AccessToken}&openid={tokenResult.OpenId}&lang=zh_CN";
            
            var userInfoResponse = await _httpClient.GetAsync(userInfoUrl, cancellationToken);
            var userInfoContent = await userInfoResponse.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogDebug("微信用户信息响应: {Response}", userInfoContent);

            var userInfoResult = JsonSerializer.Deserialize<WeChatUserInfoResponse>(userInfoContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfoResult == null || !string.IsNullOrEmpty(userInfoResult.ErrCode?.ToString()) && userInfoResult.ErrCode != 0)
            {
                _logger.LogWarning("⚠️ 微信获取用户信息失败: errcode={ErrCode}, errmsg={ErrMsg}",
                    userInfoResult?.ErrCode, userInfoResult?.ErrMsg);
                
                // 即使获取用户信息失败，也返回基本信息（openid）
                return new WeChatUserInfo
                {
                    OpenId = tokenResult.OpenId,
                    UnionId = tokenResult.UnionId
                };
            }

            _logger.LogInformation("✅ 微信用户信息获取成功: nickname={Nickname}", userInfoResult.Nickname);

            return new WeChatUserInfo
            {
                OpenId = userInfoResult.OpenId ?? tokenResult.OpenId,
                UnionId = userInfoResult.UnionId ?? tokenResult.UnionId,
                Nickname = userInfoResult.Nickname,
                HeadImgUrl = userInfoResult.HeadImgUrl,
                Sex = userInfoResult.Sex,
                Province = userInfoResult.Province,
                City = userInfoResult.City,
                Country = userInfoResult.Country
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 微信登录异常");
            throw new InvalidOperationException("微信登录失败，请稍后重试");
        }
    }
}

/// <summary>
///     微信 access_token 响应
/// </summary>
internal class WeChatTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
    
    [JsonPropertyName("openid")]
    public string? OpenId { get; set; }
    
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
    
    [JsonPropertyName("unionid")]
    public string? UnionId { get; set; }
    
    [JsonPropertyName("errcode")]
    public int? ErrCode { get; set; }
    
    [JsonPropertyName("errmsg")]
    public string? ErrMsg { get; set; }
}

/// <summary>
///     微信用户信息响应
/// </summary>
internal class WeChatUserInfoResponse
{
    [JsonPropertyName("openid")]
    public string? OpenId { get; set; }
    
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }
    
    [JsonPropertyName("sex")]
    public int Sex { get; set; }
    
    [JsonPropertyName("province")]
    public string? Province { get; set; }
    
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    
    [JsonPropertyName("headimgurl")]
    public string? HeadImgUrl { get; set; }
    
    [JsonPropertyName("privilege")]
    public string[]? Privilege { get; set; }
    
    [JsonPropertyName("unionid")]
    public string? UnionId { get; set; }
    
    [JsonPropertyName("errcode")]
    public int? ErrCode { get; set; }
    
    [JsonPropertyName("errmsg")]
    public string? ErrMsg { get; set; }
}

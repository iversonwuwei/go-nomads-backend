using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure.Services;

/// <summary>
///     Twitter OAuth 服务接口
///     用于验证 Twitter OAuth 2.0 授权码并获取用户信息
/// </summary>
public interface ITwitterOAuthService
{
    /// <summary>
    ///     通过 OAuth 2.0 授权码和 PKCE code_verifier 获取用户信息
    /// </summary>
    /// <param name="authorizationCode">Twitter 授权码</param>
    /// <param name="codeVerifier">PKCE code_verifier</param>
    /// <param name="redirectUri">回调 URI</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Twitter 用户信息，失败返回 null</returns>
    Task<TwitterUserInfo?> AuthenticateAsync(
        string authorizationCode,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Twitter 用户信息
/// </summary>
public class TwitterUserInfo
{
    /// <summary>
    ///     Twitter 用户 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     用户显示名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     用户名（@handle）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     用户头像 URL
    /// </summary>
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
///     Twitter OAuth 2.0 服务实现
///     使用 OAuth 2.0 Authorization Code with PKCE 流程
/// </summary>
public class TwitterOAuthService : ITwitterOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TwitterOAuthService> _logger;

    /// <summary>
    ///     Twitter OAuth 2.0 Token 端点
    /// </summary>
    private const string TokenUrl = "https://api.twitter.com/2/oauth2/token";

    /// <summary>
    ///     Twitter API v2 用户信息端点
    /// </summary>
    private const string UserInfoUrl = "https://api.twitter.com/2/users/me";

    public TwitterOAuthService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<TwitterOAuthService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    ///     通过 OAuth 2.0 授权码换取 access token，再获取用户信息
    /// </summary>
    public async Task<TwitterUserInfo?> AuthenticateAsync(
        string authorizationCode,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🐦 开始 Twitter OAuth 2.0 认证");

            // Step 1: 用授权码换取 access token
            var accessToken = await ExchangeCodeForTokenAsync(
                authorizationCode, codeVerifier, redirectUri, cancellationToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("❌ Twitter 授权码换取 access token 失败");
                return null;
            }

            // Step 2: 用 access token 获取用户信息
            var userInfo = await GetUserInfoAsync(accessToken, cancellationToken);
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Twitter OAuth 认证异常");
            return null;
        }
    }

    /// <summary>
    ///     用授权码换取 access token (OAuth 2.0 PKCE)
    /// </summary>
    private async Task<string?> ExchangeCodeForTokenAsync(
        string authorizationCode,
        string codeVerifier,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId = _configuration["Twitter:ConsumerKey"];
            var clientSecret = _configuration["Twitter:SecretKey"];

            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogError("❌ Twitter ConsumerKey 未配置");
                return null;
            }

            // 构建 token 请求
            var tokenRequestParams = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = authorizationCode,
                ["redirect_uri"] = redirectUri,
                ["code_verifier"] = codeVerifier,
                ["client_id"] = clientId,
            };

            var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = new FormUrlEncodedContent(tokenRequestParams)
            };

            // Twitter OAuth 2.0 需要 Basic Auth (client_id:client_secret)
            if (!string.IsNullOrEmpty(clientSecret))
            {
                var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("❌ Twitter token 交换失败: StatusCode={StatusCode}, Body={Body}",
                    response.StatusCode, json);
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize<TwitterTokenResponse>(json);
            if (tokenResponse?.AccessToken == null)
            {
                _logger.LogWarning("❌ Twitter token 响应解析失败");
                return null;
            }

            _logger.LogInformation("✅ Twitter access token 获取成功");
            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Twitter token 交换异常");
            return null;
        }
    }

    /// <summary>
    ///     通过 access token 获取 Twitter 用户信息
    /// </summary>
    private async Task<TwitterUserInfo?> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{UserInfoUrl}?user.fields=profile_image_url");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("❌ Twitter 用户信息获取失败: StatusCode={StatusCode}, Body={Body}",
                    response.StatusCode, json);
                return null;
            }

            var userResponse = JsonSerializer.Deserialize<TwitterUserResponse>(json);
            if (userResponse?.Data == null)
            {
                _logger.LogWarning("❌ Twitter 用户信息解析失败");
                return null;
            }

            var userInfo = new TwitterUserInfo
            {
                Id = userResponse.Data.Id ?? string.Empty,
                Name = userResponse.Data.Name,
                Username = userResponse.Data.Username,
                ProfileImageUrl = userResponse.Data.ProfileImageUrl,
            };

            _logger.LogInformation("✅ Twitter 用户信息获取成功: Id={Id}, Name={Name}, Username={Username}",
                userInfo.Id, userInfo.Name, userInfo.Username);

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Twitter 用户信息获取异常");
            return null;
        }
    }
}

/// <summary>
///     Twitter OAuth 2.0 Token 响应
/// </summary>
internal class TwitterTokenResponse
{
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}

/// <summary>
///     Twitter API v2 用户信息响应
/// </summary>
internal class TwitterUserResponse
{
    [JsonPropertyName("data")]
    public TwitterUserData? Data { get; set; }
}

internal class TwitterUserData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; set; }
}

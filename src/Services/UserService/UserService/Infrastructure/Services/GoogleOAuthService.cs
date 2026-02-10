using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure.Services;

/// <summary>
///     Google OAuth 服务接口
///     用于验证 Google ID Token 并获取用户信息
/// </summary>
public interface IGoogleOAuthService
{
    /// <summary>
    ///     通过 Google ID Token 验证并获取用户信息
    /// </summary>
    /// <param name="idToken">Google 登录返回的 ID Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Google 用户信息，验证失败返回 null</returns>
    Task<GoogleUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}

/// <summary>
///     Google 用户信息
/// </summary>
public class GoogleUserInfo
{
    /// <summary>
    ///     Google 用户唯一标识（sub claim）
    /// </summary>
    public string Sub { get; set; } = string.Empty;

    /// <summary>
    ///     用户邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     邮箱是否已验证
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    ///     用户全名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     用户头像 URL
    /// </summary>
    public string? Picture { get; set; }

    /// <summary>
    ///     名（given name）
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    ///     姓（family name）
    /// </summary>
    public string? FamilyName { get; set; }
}

/// <summary>
///     Google OAuth 服务实现
///     通过 Google tokeninfo 端点验证 ID Token
/// </summary>
public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleOAuthService> _logger;

    /// <summary>
    ///     Google Token 验证端点
    /// </summary>
    private const string TokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo";

    public GoogleOAuthService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<GoogleOAuthService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    ///     通过 Google ID Token 验证并获取用户信息
    ///     使用 Google 的 tokeninfo 端点进行服务端验证
    /// </summary>
    public async Task<GoogleUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔑 开始验证 Google ID Token");

            // 调用 Google tokeninfo 端点验证 ID Token
            var response = await _httpClient.GetAsync(
                $"{TokenInfoUrl}?id_token={idToken}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("❌ Google ID Token 验证失败: StatusCode={StatusCode}, Error={Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfoResponse>(json);

            if (tokenInfo == null)
            {
                _logger.LogWarning("❌ Google ID Token 响应解析失败");
                return null;
            }

            // 验证 audience（确保 token 是颁发给我们的应用）
            var expectedClientIds = GetExpectedClientIds();
            if (string.IsNullOrEmpty(tokenInfo.Aud) || !expectedClientIds.Contains(tokenInfo.Aud))
            {
                _logger.LogWarning("❌ Google ID Token audience 不匹配: Expected={Expected}, Got={Got}",
                    string.Join(", ", expectedClientIds), tokenInfo.Aud);
                return null;
            }

            // 验证 token 是否过期
            if (tokenInfo.Exp.HasValue)
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(tokenInfo.Exp.Value);
                if (expirationTime < DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("❌ Google ID Token 已过期: Exp={Exp}", expirationTime);
                    return null;
                }
            }

            var userInfo = new GoogleUserInfo
            {
                Sub = tokenInfo.Sub ?? string.Empty,
                Email = tokenInfo.Email,
                EmailVerified = tokenInfo.EmailVerified == "true",
                Name = tokenInfo.Name,
                Picture = tokenInfo.Picture,
                GivenName = tokenInfo.GivenName,
                FamilyName = tokenInfo.FamilyName,
            };

            _logger.LogInformation("✅ Google ID Token 验证成功: Sub={Sub}, Email={Email}, Name={Name}",
                userInfo.Sub, userInfo.Email, userInfo.Name);

            return userInfo;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ 调用 Google tokeninfo 端点失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Google ID Token 验证异常");
            return null;
        }
    }

    /// <summary>
    ///     获取预期的 Google Client ID 列表（iOS + Web/Server）
    /// </summary>
    private List<string> GetExpectedClientIds()
    {
        var clientIds = new List<string>();

        var webClientId = _configuration["Google:WebClientId"];
        if (!string.IsNullOrEmpty(webClientId))
            clientIds.Add(webClientId);

        var iosClientId = _configuration["Google:IosClientId"];
        if (!string.IsNullOrEmpty(iosClientId))
            clientIds.Add(iosClientId);

        var androidClientId = _configuration["Google:AndroidClientId"];
        if (!string.IsNullOrEmpty(androidClientId))
            clientIds.Add(androidClientId);

        return clientIds;
    }
}

/// <summary>
///     Google tokeninfo 端点响应
/// </summary>
internal class GoogleTokenInfoResponse
{
    [JsonPropertyName("iss")]
    public string? Iss { get; set; }

    [JsonPropertyName("azp")]
    public string? Azp { get; set; }

    [JsonPropertyName("aud")]
    public string? Aud { get; set; }

    [JsonPropertyName("sub")]
    public string? Sub { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public string? EmailVerified { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("exp")]
    public long? Exp { get; set; }

    [JsonPropertyName("iat")]
    public long? Iat { get; set; }
}

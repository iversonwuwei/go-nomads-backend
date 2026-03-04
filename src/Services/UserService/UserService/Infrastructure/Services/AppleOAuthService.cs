using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace UserService.Infrastructure.Services;

/// <summary>
///     Apple OAuth 服务接口
///     用于验证 Apple Identity Token (JWT) 并获取用户信息
/// </summary>
public interface IAppleOAuthService
{
    /// <summary>
    ///     验证 Apple Identity Token 并获取用户信息
    /// </summary>
    /// <param name="identityToken">Apple 登录返回的 Identity Token (JWT)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Apple 用户信息，验证失败返回 null</returns>
    Task<AppleUserInfo?> VerifyIdentityTokenAsync(string identityToken, CancellationToken cancellationToken = default);
}

/// <summary>
///     Apple 用户信息
/// </summary>
public class AppleUserInfo
{
    /// <summary>
    ///     Apple 用户唯一标识（sub claim）
    /// </summary>
    public string Sub { get; set; } = string.Empty;

    /// <summary>
    ///     用户邮箱（可能为 Apple 中继邮箱）
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     邮箱是否已验证
    /// </summary>
    public bool EmailVerified { get; set; }
}

/// <summary>
///     Apple OAuth 服务实现
///     通过 Apple 公钥 (JWKS) 验证 Identity Token (JWT)
///     参考: https://developer.apple.com/documentation/sign_in_with_apple/sign_in_with_apple_rest_api/verifying_a_user
/// </summary>
public class AppleOAuthService : IAppleOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppleOAuthService> _logger;

    /// <summary>
    ///     Apple JWKS 端点（用于获取公钥验证 JWT 签名）
    /// </summary>
    private const string AppleJwksUrl = "https://appleid.apple.com/auth/keys";

    /// <summary>
    ///     Apple JWT 签发者
    /// </summary>
    private const string AppleIssuer = "https://appleid.apple.com";

    /// <summary>
    ///     缓存的 Apple JWKS（公钥不常变化，缓存减少请求）
    /// </summary>
    private static JsonWebKeySet? _cachedJwks;
    private static DateTime _jwksCacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan JwksCacheDuration = TimeSpan.FromHours(24);

    public AppleOAuthService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<AppleOAuthService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    ///     验证 Apple Identity Token
    ///     1. 从 Apple JWKS 端点获取公钥
    ///     2. 用公钥验证 JWT 签名
    ///     3. 验证 issuer、audience、过期时间
    ///     4. 提取用户信息 (sub, email)
    /// </summary>
    public async Task<AppleUserInfo?> VerifyIdentityTokenAsync(
        string identityToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🍎 开始验证 Apple Identity Token");

            // 1. 获取 Apple JWKS 公钥
            var jwks = await GetAppleJwksAsync(cancellationToken);
            if (jwks == null)
            {
                _logger.LogWarning("❌ 无法获取 Apple JWKS 公钥");
                return null;
            }

            // 2. 配置 JWT 验证参数
            var expectedAudiences = GetExpectedAudiences();
            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = AppleIssuer,
                ValidAudiences = expectedAudiences,
                IssuerSigningKeys = jwks.GetSigningKeys(),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5),
            };

            // 3. 验证 JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(identityToken, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("❌ Apple Identity Token 不是有效的 JWT");
                return null;
            }

            // 4. 验证 algorithm（Apple 使用 RS256）
            if (!jwtToken.Header.Alg.Equals("RS256", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("❌ Apple Identity Token 使用了不支持的算法: {Alg}", jwtToken.Header.Alg);
                return null;
            }

            // 5. 提取用户信息
            var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                _logger.LogWarning("❌ Apple Identity Token 缺少 sub claim");
                return null;
            }

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var emailVerified = jwtToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;

            var userInfo = new AppleUserInfo
            {
                Sub = sub,
                Email = email,
                EmailVerified = emailVerified?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
            };

            _logger.LogInformation("✅ Apple Identity Token 验证成功: Sub={Sub}, Email={Email}",
                userInfo.Sub, userInfo.Email);

            return userInfo;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "❌ Apple Identity Token 已过期");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "❌ Apple Identity Token 签名无效");
            return null;
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning(ex, "❌ Apple Identity Token 验证失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Apple Identity Token 验证异常");
            return null;
        }
    }

    /// <summary>
    ///     获取 Apple JWKS 公钥（带缓存）
    /// </summary>
    private async Task<JsonWebKeySet?> GetAppleJwksAsync(CancellationToken cancellationToken)
    {
        // 检查缓存是否有效
        if (_cachedJwks != null && DateTime.UtcNow < _jwksCacheExpiry)
        {
            return _cachedJwks;
        }

        try
        {
            _logger.LogInformation("🔑 正在获取 Apple JWKS 公钥: {Url}", AppleJwksUrl);

            var response = await _httpClient.GetAsync(AppleJwksUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("❌ 获取 Apple JWKS 失败: StatusCode={StatusCode}, Error={Error}",
                    response.StatusCode, errorContent);
                return _cachedJwks; // 返回旧缓存（如果有）
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var jwks = new JsonWebKeySet(json);

            // 更新缓存
            _cachedJwks = jwks;
            _jwksCacheExpiry = DateTime.UtcNow.Add(JwksCacheDuration);

            _logger.LogInformation("✅ Apple JWKS 公钥获取成功，共 {Count} 个密钥", jwks.Keys.Count);

            return jwks;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ 调用 Apple JWKS 端点失败");
            return _cachedJwks; // 返回旧缓存（如果有）
        }
    }

    /// <summary>
    ///     获取预期的 Audience 列表（iOS Bundle ID）
    ///     Apple Identity Token 的 aud claim 应该匹配你的 App Bundle ID
    /// </summary>
    private List<string> GetExpectedAudiences()
    {
        var audiences = new List<string>();

        var bundleId = _configuration["Apple:BundleId"];
        if (!string.IsNullOrEmpty(bundleId))
            audiences.Add(bundleId);

        var serviceId = _configuration["Apple:ServiceId"];
        if (!string.IsNullOrEmpty(serviceId))
            audiences.Add(serviceId);

        // 如果没有配置，使用默认的 Bundle ID
        if (audiences.Count == 0)
        {
            audiences.Add("com.gonomads.GoNomadsApp");
        }

        return audiences;
    }
}

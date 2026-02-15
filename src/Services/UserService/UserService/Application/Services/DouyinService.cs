using System.Text.Json;
using Microsoft.Extensions.Options;
using UserService.Infrastructure.Configuration;

namespace UserService.Application.Services;

/// <summary>
/// 抖音开放平台服务接口
/// </summary>
public interface IDouyinService
{
    /// <summary>
    /// 使用授权码换取 access_token 和 open_id
    /// </summary>
    Task<DouyinTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取抖音用户信息
    /// </summary>
    Task<DouyinUserInfo?> GetUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 抖音开放平台服务实现
/// </summary>
public class DouyinService : IDouyinService
{
    private readonly DouyinSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DouyinService> _logger;

    public DouyinService(
        HttpClient httpClient,
        IOptions<DouyinSettings> settings,
        ILogger<DouyinService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 使用授权码换取 access_token 和 open_id
    /// </summary>
    public async Task<DouyinTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔑 抖音授权码换取 token: code={Code}", code[..Math.Min(8, code.Length)] + "...");

        try
        {
            var requestUrl = $"{_settings.TokenUrl}?client_key={_settings.ClientKey}&client_secret={_settings.ClientSecret}&code={code}&grant_type=authorization_code";

            var response = await _httpClient.PostAsync(requestUrl, null, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("📦 抖音 token 响应: {Content}", content);

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                var errorCode = data.TryGetProperty("error_code", out var ec) ? ec.GetInt32() : -1;
                
                if (errorCode == 0)
                {
                    var accessToken = data.GetProperty("access_token").GetString() ?? "";
                    var openId = data.GetProperty("open_id").GetString() ?? "";
                    var expiresIn = data.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : 0;
                    var refreshToken = data.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

                    _logger.LogInformation("✅ 抖音 token 获取成功: openId={OpenId}", openId);

                    return new DouyinTokenResult
                    {
                        Success = true,
                        AccessToken = accessToken,
                        OpenId = openId,
                        ExpiresIn = expiresIn,
                        RefreshToken = refreshToken
                    };
                }
                else
                {
                    var description = data.TryGetProperty("description", out var desc) ? desc.GetString() : "未知错误";
                    _logger.LogWarning("⚠️ 抖音 token 获取失败: errorCode={ErrorCode}, description={Description}", errorCode, description);

                    return new DouyinTokenResult
                    {
                        Success = false,
                        ErrorMessage = description ?? $"错误码: {errorCode}"
                    };
                }
            }

            return new DouyinTokenResult
            {
                Success = false,
                ErrorMessage = "抖音返回数据格式异常"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 抖音授权码换取 token 异常");
            return new DouyinTokenResult
            {
                Success = false,
                ErrorMessage = $"请求异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取抖音用户信息
    /// </summary>
    public async Task<DouyinUserInfo?> GetUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📱 获取抖音用户信息: openId={OpenId}", openId);

        try
        {
            var requestUrl = $"{_settings.UserInfoUrl}?access_token={accessToken}&open_id={openId}";

            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                var errorCode = data.TryGetProperty("error_code", out var ec) ? ec.GetInt32() : -1;
                
                if (errorCode == 0)
                {
                    var nickname = data.TryGetProperty("nickname", out var nn) ? nn.GetString() : null;
                    var avatar = data.TryGetProperty("avatar", out var av) ? av.GetString() : null;
                    var unionId = data.TryGetProperty("union_id", out var ui) ? ui.GetString() : null;

                    _logger.LogInformation("✅ 获取抖音用户信息成功: nickname={Nickname}", nickname);

                    return new DouyinUserInfo
                    {
                        OpenId = openId,
                        UnionId = unionId,
                        Nickname = nickname,
                        AvatarUrl = avatar
                    };
                }
            }

            _logger.LogWarning("⚠️ 获取抖音用户信息失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取抖音用户信息异常");
            return null;
        }
    }
}

/// <summary>
/// 抖音 Token 换取结果
/// </summary>
public class DouyinTokenResult
{
    public bool Success { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string OpenId { get; set; } = string.Empty;
    public long ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 抖音用户信息
/// </summary>
public class DouyinUserInfo
{
    public string OpenId { get; set; } = string.Empty;
    public string? UnionId { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
}

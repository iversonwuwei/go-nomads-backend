using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using UserService.Infrastructure.Configuration;

namespace UserService.Application.Services;

/// <summary>
/// QQ 互联开放平台服务接口
/// </summary>
public interface IQQService
{
    /// <summary>
    /// 使用授权码换取 access_token 和 openid
    /// </summary>
    Task<QQTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取 QQ 用户信息
    /// </summary>
    Task<QQUserInfo?> GetUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default);
}

/// <summary>
/// QQ 互联开放平台服务实现
/// </summary>
public class QQService : IQQService
{
    private readonly QQSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<QQService> _logger;

    public QQService(
        HttpClient httpClient,
        IOptions<QQSettings> settings,
        ILogger<QQService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 使用授权码换取 access_token
    /// </summary>
    public async Task<QQTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔑 QQ 授权码换取 token: code={Code}", code[..Math.Min(8, code.Length)] + "...");

        try
        {
            // 1. 用 code 换取 access_token
            var tokenUrl = $"{_settings.TokenUrl}?grant_type=authorization_code&client_id={_settings.AppId}&client_secret={_settings.AppKey}&code={code}&redirect_uri={Uri.EscapeDataString(_settings.RedirectUri)}&fmt=json";

            var tokenResponse = await _httpClient.GetAsync(tokenUrl, cancellationToken);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("📦 QQ token 响应: {Content}", tokenContent);

            using var tokenDoc = JsonDocument.Parse(tokenContent);
            var tokenRoot = tokenDoc.RootElement;

            if (tokenRoot.TryGetProperty("error", out var errorProp))
            {
                var errorCode = errorProp.GetInt32();
                var errorDesc = tokenRoot.TryGetProperty("error_description", out var descProp) ? descProp.GetString() : "未知错误";
                _logger.LogWarning("⚠️ QQ token 获取失败: error={ErrorCode}, description={Description}", errorCode, errorDesc);
                return new QQTokenResult
                {
                    Success = false,
                    ErrorMessage = errorDesc ?? $"错误码: {errorCode}"
                };
            }

            var accessToken = tokenRoot.GetProperty("access_token").GetString() ?? "";
            var expiresIn = tokenRoot.TryGetProperty("expires_in", out var ei) ? ei.GetInt64() : 0;
            var refreshToken = tokenRoot.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

            // 2. 用 access_token 获取 openid
            var openIdUrl = $"{_settings.OpenIdUrl}?access_token={accessToken}&fmt=json";
            var openIdResponse = await _httpClient.GetAsync(openIdUrl, cancellationToken);
            var openIdContent = await openIdResponse.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("📦 QQ openid 响应: {Content}", openIdContent);

            using var openIdDoc = JsonDocument.Parse(openIdContent);
            var openIdRoot = openIdDoc.RootElement;

            if (openIdRoot.TryGetProperty("error", out var openIdError))
            {
                var errorDesc = openIdRoot.TryGetProperty("error_description", out var descProp2) ? descProp2.GetString() : "未知错误";
                return new QQTokenResult
                {
                    Success = false,
                    ErrorMessage = $"获取 OpenID 失败: {errorDesc}"
                };
            }

            var openId = openIdRoot.GetProperty("openid").GetString() ?? "";

            _logger.LogInformation("✅ QQ token 和 openid 获取成功: openId={OpenId}", openId);

            return new QQTokenResult
            {
                Success = true,
                AccessToken = accessToken,
                OpenId = openId,
                ExpiresIn = expiresIn,
                RefreshToken = refreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ QQ 授权码换取 token 异常");
            return new QQTokenResult
            {
                Success = false,
                ErrorMessage = $"请求异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 获取 QQ 用户信息
    /// </summary>
    public async Task<QQUserInfo?> GetUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📱 获取 QQ 用户信息: openId={OpenId}", openId);

        try
        {
            var requestUrl = $"{_settings.UserInfoUrl}?access_token={accessToken}&oauth_consumer_key={_settings.AppId}&openid={openId}";

            var response = await _httpClient.GetAsync(requestUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var ret = root.TryGetProperty("ret", out var retProp) ? retProp.GetInt32() : -1;
            
            if (ret == 0)
            {
                var nickname = root.TryGetProperty("nickname", out var nn) ? nn.GetString() : null;
                var avatar = root.TryGetProperty("figureurl_qq_2", out var av) ? av.GetString() : null;
                avatar ??= root.TryGetProperty("figureurl_qq_1", out var av1) ? av1.GetString() : null;

                _logger.LogInformation("✅ 获取 QQ 用户信息成功: nickname={Nickname}", nickname);

                return new QQUserInfo
                {
                    OpenId = openId,
                    Nickname = nickname,
                    AvatarUrl = avatar
                };
            }

            var msg = root.TryGetProperty("msg", out var msgProp) ? msgProp.GetString() : "未知错误";
            _logger.LogWarning("⚠️ 获取 QQ 用户信息失败: ret={Ret}, msg={Msg}", ret, msg);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 QQ 用户信息异常");
            return null;
        }
    }
}

/// <summary>
/// QQ Token 换取结果
/// </summary>
public class QQTokenResult
{
    public bool Success { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string OpenId { get; set; } = string.Empty;
    public long ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// QQ 用户信息
/// </summary>
public class QQUserInfo
{
    public string OpenId { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
}

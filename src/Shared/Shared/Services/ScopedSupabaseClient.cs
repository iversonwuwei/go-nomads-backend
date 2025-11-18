using GoNomads.Shared.Middleware;
using Supabase;

namespace Shared.Services;

/// <summary>
///     作用域的 Supabase 客户端 - 提供当前用户的 JWT token
/// </summary>
public class ScopedSupabaseClient
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ScopedSupabaseClient> _logger;
    private readonly string? _userToken;

    public ScopedSupabaseClient(
        Client client,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ScopedSupabaseClient> logger)
    {
        Client = client;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        // 从当前 HTTP 上下文提取用户 token
        _userToken = ExtractUserToken();
    }

    /// <summary>
    ///     获取 Supabase 客户端实例
    /// </summary>
    public Client Client { get; }

    /// <summary>
    ///     获取当前用户的 Authorization header (包含 "Bearer " 前缀)
    ///     如果没有用户 token，返回 null (将使用 service key)
    /// </summary>
    public string? UserAuthorizationHeader => _userToken != null ? $"Bearer {_userToken}" : null;

    /// <summary>
    ///     从当前 HTTP 上下文提取用户 token
    /// </summary>
    private string? ExtractUserToken()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogDebug("No HTTP context available, will use service key");
            return null;
        }

        // 从 UserContext 获取 JWT token
        var userContext = UserContextMiddleware.GetUserContext(httpContext);
        if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.AuthorizationHeader))
        {
            // 提取 Bearer token
            var token = userContext.AuthorizationHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            _logger.LogInformation("✅ Extracted user token for UserId: {UserId}", userContext.UserId);
            return token;
        }

        _logger.LogDebug("No authenticated user context, will use service key");
        return null;
    }
}
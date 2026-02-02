using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GoNomads.Shared.Models;

namespace GoNomads.Shared.Middleware;

/// <summary>
///     用户上下文中间件 - 从请求头中提取 Gateway 传递的用户信息
/// </summary>
public class UserContextMiddleware
{
    // HTTP 上下文访问器的键
    private const string UserContextKey = "UserContext";
    private readonly ILogger<UserContextMiddleware> _logger;
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userContext = new UserContext();

        // 从请求头中提取 Gateway 传递的用户信息
        if (context.Request.Headers.TryGetValue("X-User-Id", out var userId))
            userContext.UserId = userId.FirstOrDefault() ?? string.Empty;

        if (context.Request.Headers.TryGetValue("X-User-Email", out var email))
            userContext.Email = email.FirstOrDefault() ?? string.Empty;

        if (context.Request.Headers.TryGetValue("X-User-Role", out var role))
            userContext.Role = role.FirstOrDefault() ?? string.Empty;

        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            userContext.AuthorizationHeader = authHeader.FirstOrDefault() ?? string.Empty;

        // 如果网关未透传用户信息，但携带了 Bearer Token，则尝试从 JWT 中解析用户信息
        if (string.IsNullOrWhiteSpace(userContext.UserId) && !string.IsNullOrWhiteSpace(userContext.AuthorizationHeader))
            TryPopulateFromJwt(userContext);

        // 将用户上下文存储到 HttpContext.Items 中
        context.Items[UserContextKey] = userContext;

        if (userContext.IsAuthenticated)
            _logger.LogInformation(
                "用户上下文已设置 - UserId: {UserId}, Email: {Email}, Role: {Role}",
                userContext.UserId,
                userContext.Email,
                userContext.Role
            );
        else
            _logger.LogDebug("未认证的请求");

        await _next(context);
    }

    /// <summary>
    ///     从 HttpContext 中获取用户上下文
    /// </summary>
    public static UserContext? GetUserContext(HttpContext context)
    {
        return context.Items.TryGetValue(UserContextKey, out var userContext)
            ? userContext as UserContext
            : null;
    }

    /// <summary>
    ///     从 JWT 中解析用户标识，作为没有网关透传时的兜底方案
    /// </summary>
    private void TryPopulateFromJwt(UserContext userContext)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = userContext.AuthorizationHeader!.Replace("Bearer", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

            if (!handler.CanReadToken(token)) return;

            var jwt = handler.ReadJwtToken(token);

            userContext.UserId = GetClaim(jwt, ClaimTypes.NameIdentifier, "sub", "userId", "oid");
            userContext.Email = GetClaim(jwt, ClaimTypes.Email, "email", "preferred_username", "upn");
            userContext.Role = GetClaim(jwt, ClaimTypes.Role, "role", "roles");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法从 JWT 解析用户信息，可能缺少网关透传的用户头");
        }
    }

    private static string? GetClaim(JwtSecurityToken jwt, params string[] claimTypes)
    {
        foreach (var type in claimTypes)
        {
            var value = jwt.Claims.FirstOrDefault(c => c.Type == type)?.Value;
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }
}
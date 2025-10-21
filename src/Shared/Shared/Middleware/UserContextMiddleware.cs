using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GoNomads.Shared.Middleware;

/// <summary>
/// 用户上下文中间件 - 从请求头中提取 Gateway 传递的用户信息
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    // HTTP 上下文访问器的键
    private const string UserContextKey = "UserContext";

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
        {
            userContext.UserId = userId.ToString();
        }

        if (context.Request.Headers.TryGetValue("X-User-Email", out var email))
        {
            userContext.Email = email.ToString();
        }

        if (context.Request.Headers.TryGetValue("X-User-Role", out var role))
        {
            userContext.Role = role.ToString();
        }

        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            userContext.AuthorizationHeader = authHeader.ToString();
        }

        // 将用户上下文存储到 HttpContext.Items 中
        context.Items[UserContextKey] = userContext;

        if (userContext.IsAuthenticated)
        {
            _logger.LogInformation(
                "用户上下文已设置 - UserId: {UserId}, Email: {Email}, Role: {Role}",
                userContext.UserId,
                userContext.Email,
                userContext.Role
            );
        }
        else
        {
            _logger.LogDebug("未认证的请求");
        }

        await _next(context);
    }

    /// <summary>
    /// 从 HttpContext 中获取用户上下文
    /// </summary>
    public static UserContext? GetUserContext(HttpContext context)
    {
        return context.Items.TryGetValue(UserContextKey, out var userContext)
            ? userContext as UserContext
            : null;
    }
}

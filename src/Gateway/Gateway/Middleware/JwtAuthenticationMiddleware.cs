using Gateway.Services;

namespace Gateway.Middleware;

/// <summary>
///     JWT 认证中间件 - 拦截请求并验证 JWT 令牌
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly RequestDelegate _next;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 检查是否需要认证
        if (RouteAuthorizationConfig.RequiresAuthentication(path))
        {
            // 检查用户是否已认证
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning(
                    "Unauthorized access attempt to protected route: {Path} from {IP}",
                    path,
                    context.Connection.RemoteIpAddress);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "Unauthorized. Please provide a valid JWT token.",
                    error = "Missing or invalid Authorization header"
                };

                await context.Response.WriteAsJsonAsync(response);
                return;
            }

            // 检查是否需要管理员权限
            if (RouteAuthorizationConfig.RequiresAdmin(path))
            {
                var role = context.User.FindFirst("role")?.Value;

                if (role != "admin")
                {
                    _logger.LogWarning(
                        "Forbidden access attempt to admin route: {Path} by user {UserId} with role {Role}",
                        path,
                        context.User.FindFirst("sub")?.Value,
                        role);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        success = false,
                        message = "Forbidden. Admin access required.",
                        error = "Insufficient permissions"
                    };

                    await context.Response.WriteAsJsonAsync(response);
                    return;
                }
            }

            _logger.LogDebug(
                "Authenticated request: {Path} by user {UserId}",
                path,
                context.User.FindFirst("sub")?.Value);
        }

        await _next(context);
    }
}

/// <summary>
///     中间件扩展方法
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationMiddleware>();
    }
}
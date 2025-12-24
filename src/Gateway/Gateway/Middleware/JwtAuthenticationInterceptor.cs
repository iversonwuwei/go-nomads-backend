using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Gateway.Middleware;

/// <summary>
///     JWT 认证拦截中间件
///     在请求到达 YARP 反向代理之前验证 JWT token
/// </summary>
public class JwtAuthenticationInterceptor
{
    private readonly ILogger<JwtAuthenticationInterceptor> _logger;
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _publicPaths;
    private readonly HashSet<string> _publicGetPaths;

    public JwtAuthenticationInterceptor(
        RequestDelegate next,
        ILogger<JwtAuthenticationInterceptor> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        // 从配置读取公开路径白名单（完全公开，任何方法都不需要认证）
        var publicPaths = configuration.GetSection("Authentication:PublicPaths").Get<string[]>() ??
                          Array.Empty<string>();
        _publicPaths = new HashSet<string>(publicPaths, StringComparer.OrdinalIgnoreCase);

        // 从配置读取 GET 请求公开路径（只有 GET 请求不需要认证）
        var publicGetPaths = configuration.GetSection("Authentication:PublicGetPaths").Get<string[]>() ??
                             Array.Empty<string>();
        _publicGetPaths = new HashSet<string>(publicGetPaths, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("🔓 Public paths configured: {Paths}", string.Join(", ", _publicPaths));
        _logger.LogInformation("🔓 Public GET paths configured: {Paths}", string.Join(", ", _publicGetPaths));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        _logger.LogInformation("🔍 JWT Interceptor - {Method} {Path}", method, path);

        // 检查是否是公开路径（完全公开或 GET 请求公开）
        if (IsPublicPath(path, method))
        {
            _logger.LogInformation("⚪ Public path: {Method} {Path} - Skipping authentication", method, path);
            await _next(context);
            return;
        }

        _logger.LogInformation("🔒 Protected path: {Method} {Path} - Validating JWT", method, path);

        // 检查是否有 Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogWarning("❌ Missing Authorization header for path: {Method} {Path}", method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Missing Authorization header",
                error = "Unauthorized"
            });
            return;
        }

        var token = authHeader.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("❌ Empty Authorization header for path: {Method} {Path}", method, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Empty Authorization header",
                error = "Unauthorized"
            });
            return;
        }

        _logger.LogInformation("🔑 Found Authorization header, validating token...");

        // 移除 "Bearer " 前缀
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) token = token.Substring(7);

        // 验证 token (通过 ASP.NET Core Authentication)
        var authenticateResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("❌ JWT validation failed for path: {Path} - Error: {Error}",
                path,
                authenticateResult.Failure?.Message ?? "Unknown error");

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Invalid or expired token",
                error = "Unauthorized",
                details = authenticateResult.Failure?.Message
            });
            return;
        }

        _logger.LogInformation("✅ JWT validation succeeded");

        // Token 验证成功,提取用户信息并添加到请求头
        var userId = authenticateResult.Principal?.FindFirst("sub")?.Value;
        var email = authenticateResult.Principal?.FindFirst("email")?.Value;
        var role = authenticateResult.Principal?.FindFirst("role")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            context.Request.Headers["X-User-Id"] = userId;
            _logger.LogInformation("   Added X-User-Id: {UserId}", userId);
        }

        if (!string.IsNullOrEmpty(email))
        {
            context.Request.Headers["X-User-Email"] = email;
            _logger.LogInformation("   Added X-User-Email: {Email}", email);
        }

        if (!string.IsNullOrEmpty(role))
        {
            context.Request.Headers["X-User-Role"] = role;
            _logger.LogInformation("   Added X-User-Role: {Role}", role);
        }

        _logger.LogInformation("✅ JWT validated - UserId: {UserId}, Email: {Email}, Role: {Role}, Path: {Path}",
            userId, email, role, path);

        // 继续处理请求
        await _next(context);
    }

    private bool IsPublicPath(string path, string method)
    {
        // 检查完全公开路径（任何方法都不需要认证）
        if (_publicPaths.Contains(path)) return true;
        foreach (var publicPath in _publicPaths)
            if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
                return true;

        // 检查 GET 请求公开路径
        if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            if (_publicGetPaths.Contains(path)) return true;
            foreach (var publicGetPath in _publicGetPaths)
                if (path.StartsWith(publicGetPath, StringComparison.OrdinalIgnoreCase))
                    return true;
        }

        return false;
    }
}

/// <summary>
///     JWT 认证拦截中间件扩展方法
/// </summary>
public static class JwtAuthenticationInterceptorExtensions
{
    public static IApplicationBuilder UseJwtAuthenticationInterceptor(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationInterceptor>();
    }
}
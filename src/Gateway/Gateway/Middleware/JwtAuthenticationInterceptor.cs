using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;

namespace Gateway.Middleware;

/// <summary>
/// JWT 认证拦截中间件
/// 在请求到达 YARP 反向代理之前验证 JWT token
/// </summary>
public class JwtAuthenticationInterceptor
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationInterceptor> _logger;
    private readonly HashSet<string> _publicPaths;

    public JwtAuthenticationInterceptor(
        RequestDelegate next, 
        ILogger<JwtAuthenticationInterceptor> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        // 从配置读取公开路径白名单
        var publicPaths = configuration.GetSection("Authentication:PublicPaths").Get<string[]>() ?? Array.Empty<string>();
        _publicPaths = new HashSet<string>(publicPaths, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("🔓 Public paths configured: {Paths}", string.Join(", ", _publicPaths));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 检查是否是公开路径
        if (IsPublicPath(path))
        {
            _logger.LogDebug("⚪ Public path: {Path} - Skipping authentication", path);
            await _next(context);
            return;
        }

        // 检查是否有 Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogWarning("❌ Missing Authorization header for path: {Path}", path);
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
            _logger.LogWarning("❌ Empty Authorization header for path: {Path}", path);
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

        // 移除 "Bearer " 前缀
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token.Substring(7);
        }

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

        // Token 验证成功,提取用户信息并添加到请求头
        var userId = authenticateResult.Principal?.FindFirst("sub")?.Value;
        var email = authenticateResult.Principal?.FindFirst("email")?.Value;
        var role = authenticateResult.Principal?.FindFirst("role")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            context.Request.Headers["X-User-Id"] = userId;
        }
        if (!string.IsNullOrEmpty(email))
        {
            context.Request.Headers["X-User-Email"] = email;
        }
        if (!string.IsNullOrEmpty(role))
        {
            context.Request.Headers["X-User-Role"] = role;
        }

        _logger.LogInformation("✅ JWT validated - UserId: {UserId}, Email: {Email}, Role: {Role}, Path: {Path}", 
            userId, email, role, path);

        // 继续处理请求
        await _next(context);
    }

    private bool IsPublicPath(string path)
    {
        // 精确匹配
        if (_publicPaths.Contains(path))
        {
            return true;
        }

        // 前缀匹配
        foreach (var publicPath in _publicPaths)
        {
            if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// JWT 认证拦截中间件扩展方法
/// </summary>
public static class JwtAuthenticationInterceptorExtensions
{
    public static IApplicationBuilder UseJwtAuthenticationInterceptor(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthenticationInterceptor>();
    }
}

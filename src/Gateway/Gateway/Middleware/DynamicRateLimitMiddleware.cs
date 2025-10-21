using System.Threading.RateLimiting;
using Gateway.Services;

namespace Gateway.Middleware;

/// <summary>
/// 动态速率限制中间件 - 根据路由应用不同的限流策略
/// </summary>
public class DynamicRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DynamicRateLimitMiddleware> _logger;

    public DynamicRateLimitMiddleware(
        RequestDelegate next,
        ILogger<DynamicRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // 根据路径设置限流策略
        string? policyName = DeterminePolicyName(path);

        if (!string.IsNullOrEmpty(policyName))
        {
            // 设置策略名称到 HttpContext，供 RateLimiter 使用
            context.Request.Headers["X-Rate-Limit-Policy"] = policyName;
            
            _logger.LogDebug("Applying rate limit policy '{Policy}' to path '{Path}'", policyName, path);
        }

        await _next(context);
    }

    /// <summary>
    /// 根据路径确定限流策略
    /// </summary>
    private string? DeterminePolicyName(string path)
    {
        // 登录端点 - 严格限流
        if (path.Contains("/api/users/login"))
        {
            return RateLimitConfig.LoginPolicy;
        }

        // 注册端点 - 严格限流
        if (path.Contains("/api/users/register"))
        {
            return RateLimitConfig.RegisterPolicy;
        }

        // 敏感操作 - 严格限流
        if (path.Contains("/api/users/admin") ||
            path.Contains("/api/users/delete") ||
            path.Contains("/api/users/reset-password"))
        {
            return RateLimitConfig.StrictPolicy;
        }

        // 其他 API 端点 - 常规限流
        if (path.StartsWith("/api/"))
        {
            return RateLimitConfig.ApiPolicy;
        }

        // 其他端点不限流（由全局限流器处理）
        return null;
    }
}

/// <summary>
/// 中间件扩展方法
/// </summary>
public static class DynamicRateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseDynamicRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DynamicRateLimitMiddleware>();
    }
}

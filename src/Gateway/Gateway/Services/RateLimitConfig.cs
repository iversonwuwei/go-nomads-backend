using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.Services;

/// <summary>
/// API 限流配置
/// </summary>
public static class RateLimitConfig
{
    /// <summary>
    /// 登录限流策略名称
    /// </summary>
    public const string LoginPolicy = "login";

    /// <summary>
    /// 注册限流策略名称
    /// </summary>
    public const string RegisterPolicy = "register";

    /// <summary>
    /// API 限流策略名称
    /// </summary>
    public const string ApiPolicy = "api";

    /// <summary>
    /// 严格限流策略名称（用于敏感操作）
    /// </summary>
    public const string StrictPolicy = "strict";

    /// <summary>
    /// 配置速率限制策略
    /// </summary>
    public static void ConfigureRateLimiter(RateLimiterOptions options)
    {
        // 1. 登录限流 - 固定窗口算法
        // 每个 IP 每分钟最多 5 次登录尝试
        options.AddFixedWindowLimiter(LoginPolicy, opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 5;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 2; // 排队数量
        });

        // 2. 注册限流 - 固定窗口算法
        // 每个 IP 每小时最多 3 次注册尝试
        options.AddFixedWindowLimiter(RegisterPolicy, opt =>
        {
            opt.Window = TimeSpan.FromHours(1);
            opt.PermitLimit = 3;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0; // 不排队
        });

        // 3. API 限流 - 滑动窗口算法
        // 每个 IP 每分钟最多 100 个请求
        options.AddSlidingWindowLimiter(ApiPolicy, opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 100;
            opt.SegmentsPerWindow = 6; // 将窗口分为 6 段，每段 10 秒
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 10;
        });

        // 4. 严格限流 - 令牌桶算法
        // 用于敏感操作（如密码重置、删除用户等）
        options.AddTokenBucketLimiter(StrictPolicy, opt =>
        {
            opt.TokenLimit = 10; // 令牌桶容量
            opt.TokensPerPeriod = 2; // 每个周期补充的令牌数
            opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1); // 补充周期
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0; // 不排队
        });

        // 5. 全局限流 - 并发限制
        // 所有请求的并发数限制
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            // 健康检查和监控端点不限流
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/metrics"))
            {
                return RateLimitPartition.GetNoLimiter("unlimited");
            }

            // 其他请求按 IP 限流
            var clientIp = GetClientIpAddress(context);
            
            return RateLimitPartition.GetConcurrencyLimiter(clientIp, _ =>
                new ConcurrencyLimiterOptions
                {
                    PermitLimit = 50, // 每个 IP 最多 50 个并发请求
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                });
        });

        // 限流被拒绝时的响应
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                ? retryAfterValue.TotalSeconds
                : 60;

            var response = new
            {
                success = false,
                message = "请求过于频繁，请稍后再试",
                error = "Too Many Requests",
                retryAfter = retryAfter,
                timestamp = DateTime.UtcNow
            };

            await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        };
    }

    /// <summary>
    /// 获取客户端 IP 地址
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        // 优先从 X-Forwarded-For 头获取（支持反向代理）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // 其次从 X-Real-IP 头获取
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 最后使用连接的远程 IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

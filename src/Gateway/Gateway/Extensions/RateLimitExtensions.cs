namespace Gateway.Extensions;

/// <summary>
///     速率限制扩展方法
/// </summary>
public static class RateLimitExtensions
{
    /// <summary>
    ///     为端点添加速率限制策略
    /// </summary>
    public static IEndpointConventionBuilder RequireRateLimit(
        this IEndpointConventionBuilder builder,
        string policyName)
    {
        return builder.RequireRateLimiting(policyName);
    }

    /// <summary>
    ///     禁用端点的速率限制
    /// </summary>
    public static IEndpointConventionBuilder DisableRateLimit(
        this IEndpointConventionBuilder builder)
    {
        return builder.DisableRateLimiting();
    }
}
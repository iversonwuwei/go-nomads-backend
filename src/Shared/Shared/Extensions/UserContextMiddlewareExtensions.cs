using GoNomads.Shared.Middleware;

namespace GoNomads.Shared.Extensions;

/// <summary>
///     用户上下文中间件扩展
/// </summary>
public static class UserContextMiddlewareExtensions
{
    /// <summary>
    ///     使用用户上下文中间件
    /// </summary>
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserContextMiddleware>();
    }
}
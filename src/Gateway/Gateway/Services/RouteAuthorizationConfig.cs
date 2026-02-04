namespace Gateway.Services;

/// <summary>
///     路由认证配置 - 定义哪些路由需要认证
/// </summary>
public class RouteAuthorizationConfig
{
    /// <summary>
    ///     公开路由（不需要认证）- 这些路由完全公开，任何方法都不需要认证
    /// </summary>
    public static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Authentication endpoints (v1 API)
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/auth/refresh",
        "/api/v1/auth/logout",
        "/api/v1/auth/social-login",
        "/api/v1/auth/alipay/auth-info",
        "/api/v1/auth/sms/send",
        "/api/v1/auth/sms/login",

        // Legacy routes (for backward compatibility - remove after migration)
        "/api/users/login",
        "/api/users/register",
        "/api/users/refresh",

        // System endpoints
        "/api/test", // 测试端点
        "/health",
        "/metrics",
        "/scalar/v1"
    };

    /// <summary>
    ///     GET 请求公开的路由前缀（允许匿名浏览）
    ///     POST/PUT/DELETE 仍然需要认证
    /// </summary>
    public static readonly HashSet<string> PublicGetRoutePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        // 数据浏览类接口 - 允许匿名用户浏览列表和详情
        "/api/v1/cities",        // 城市列表和详情
        "/api/v1/hotels",        // 酒店列表和详情
        "/api/v1/coworking",     // 共享办公空间列表和详情
        "/api/v1/products"       // 产品列表和详情
    };

    /// <summary>
    ///     需要管理员权限的路由
    /// </summary>
    public static readonly HashSet<string> AdminRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/users/admin"
        // 可以添加更多管理员路由
    };

    /// <summary>
    ///     检查路由是否需要认证（考虑 HTTP Method）
    /// </summary>
    public static bool RequiresAuthentication(string path, string? method = null)
    {
        // 检查是否是完全公开路由（任何方法都不需要认证）
        if (PublicRoutes.Any(route =>
                path.Equals(route, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(route, StringComparison.OrdinalIgnoreCase)))
            return false;

        // 检查 GET 请求是否在公开浏览列表中
        if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            if (PublicGetRoutePrefixes.Any(prefix =>
                    path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        // 默认所有 /api/* 路由都需要认证
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     检查路由是否需要认证（向后兼容，不考虑 Method）
    /// </summary>
    public static bool RequiresAuthentication(string path)
    {
        return RequiresAuthentication(path, null);
    }

    /// <summary>
    ///     检查路由是否需要管理员权限
    /// </summary>
    public static bool RequiresAdmin(string path)
    {
        return AdminRoutes.Any(route =>
            path.Equals(route, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(route, StringComparison.OrdinalIgnoreCase));
    }
}
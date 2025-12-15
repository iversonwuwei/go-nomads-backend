namespace Gateway.Services;

/// <summary>
///     路由认证配置 - 定义哪些路由需要认证
/// </summary>
public class RouteAuthorizationConfig
{
    /// <summary>
    ///     公开路由（不需要认证）
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
    ///     需要管理员权限的路由
    /// </summary>
    public static readonly HashSet<string> AdminRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/users/admin"
        // 可以添加更多管理员路由
    };

    /// <summary>
    ///     检查路由是否需要认证
    /// </summary>
    public static bool RequiresAuthentication(string path)
    {
        // 检查是否是公开路由
        if (PublicRoutes.Any(route =>
                path.Equals(route, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(route, StringComparison.OrdinalIgnoreCase)))
            return false;

        // 默认所有 /api/* 路由都需要认证
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
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
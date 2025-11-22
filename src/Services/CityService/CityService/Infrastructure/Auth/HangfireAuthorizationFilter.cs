using Hangfire.Dashboard;

namespace CityService.Infrastructure.Auth;

/// <summary>
///     Hangfire Dashboard 授权过滤器（开发环境允许所有访问）
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // 开发环境允许所有访问
        // 生产环境应该添加身份验证逻辑
        return true;
    }
}

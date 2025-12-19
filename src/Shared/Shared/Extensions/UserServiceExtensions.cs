using GoNomads.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GoNomads.Shared.Extensions;

/// <summary>
/// 用户服务依赖注入扩展
/// </summary>
public static class UserServiceExtensions
{
    /// <summary>
    /// 注册当前用户服务
    /// </summary>
    public static IServiceCollection AddCurrentUserService(this IServiceCollection services)
    {
        // 确保 HttpContextAccessor 已注册
        services.AddHttpContextAccessor();
        
        // 注册 CurrentUserService 为 Scoped（每个请求一个实例）
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        
        return services;
    }
}

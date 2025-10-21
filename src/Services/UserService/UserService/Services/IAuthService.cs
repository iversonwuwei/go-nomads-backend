using UserService.DTOs;

namespace UserService.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="email">用户邮箱</param>
    /// <param name="password">用户密码</param>
    /// <returns>认证响应</returns>
    Task<AuthResponseDto> LoginAsync(string email, string password);

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>新的认证响应</returns>
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <returns></returns>
    Task SignOutAsync();
}

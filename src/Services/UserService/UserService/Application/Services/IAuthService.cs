using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
/// 认证应用服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户注册
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户登录
    /// </summary>
    Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task SignOutAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task ChangePasswordAsync(string userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}

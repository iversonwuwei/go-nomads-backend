using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
///     认证应用服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    ///     用户注册
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     用户登录
    /// </summary>
    Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     刷新访问令牌
    /// </summary>
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     用户登出
    /// </summary>
    Task SignOutAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     修改密码
    /// </summary>
    Task ChangePasswordAsync(string userId, string oldPassword, string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送短信验证码
    /// </summary>
    Task<SendSmsCodeResponse> SendSmsCodeAsync(SendSmsCodeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     手机号验证码登录
    /// </summary>
    Task<AuthResponseDto> LoginWithPhoneAsync(PhoneLoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     社交登录（微信/抖音等）
    ///     用户不存在时自动创建
    /// </summary>
    Task<AuthResponseDto> SocialLoginAsync(SocialLoginRequest request,
        CancellationToken cancellationToken = default);
}
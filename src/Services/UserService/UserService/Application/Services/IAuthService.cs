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
    ///     设置密码（用于未设置密码的用户，如手机号/社交登录用户）
    /// </summary>
    Task SetPasswordAsync(string userId, string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查用户是否已设置密码
    /// </summary>
    Task<bool> HasPasswordAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查邮箱是否可用（未被其他用户占用）
    /// </summary>
    Task<bool> CheckEmailAvailabilityAsync(string email, string currentUserId,
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

    /// <summary>
    ///     发送找回密码验证码（支持邮箱和手机号）
    /// </summary>
    Task<SendResetCodeResponse> SendResetPasswordCodeAsync(SendResetCodeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     验证验证码并重置密码
    /// </summary>
    Task ResetPasswordWithCodeAsync(ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     发送找回密码验证码请求
/// </summary>
public class SendResetCodeRequest
{
    /// <summary>
    ///     邮箱或手机号
    /// </summary>
    public string EmailOrPhone { get; set; } = string.Empty;
}

/// <summary>
///     发送找回密码验证码响应
/// </summary>
public class SendResetCodeResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     找回方式（email / sms）
    /// </summary>
    public string RecoveryMethod { get; set; } = string.Empty;

    /// <summary>
    ///     脱敏后的目标（如 wa***n@gmail.com 或 138****1234）
    /// </summary>
    public string MaskedTarget { get; set; } = string.Empty;

    /// <summary>
    ///     验证码有效期（秒）
    /// </summary>
    public int ExpiresInSeconds { get; set; }
}

/// <summary>
///     重置密码请求
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    ///     邮箱或手机号（与发送验证码时一致）
    /// </summary>
    public string EmailOrPhone { get; set; } = string.Empty;

    /// <summary>
    ///     验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    ///     新密码
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
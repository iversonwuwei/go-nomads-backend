using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

/// <summary>
///     发送短信验证码请求
/// </summary>
public class SendSmsCodeRequest
{
    /// <summary>
    ///     手机号（包含国际区号，如 +8613800138000）
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    ///     验证码用途（login: 登录, register: 注册, reset_password: 重置密码）
    /// </summary>
    public string Purpose { get; set; } = "login";
}

/// <summary>
///     发送短信验证码响应
/// </summary>
public class SendSmsCodeResponse
{
    /// <summary>
    ///     是否发送成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     验证码有效期（秒）
    /// </summary>
    public int ExpiresInSeconds { get; set; }

    /// <summary>
    ///     请求 ID（用于排查问题）
    /// </summary>
    public string? RequestId { get; set; }
}

/// <summary>
///     手机号登录请求
/// </summary>
public class PhoneLoginRequest
{
    /// <summary>
    ///     手机号（包含国际区号，如 +8613800138000）
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    ///     验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    [StringLength(6, MinimumLength = 4, ErrorMessage = "验证码长度为 4-6 位")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
///     社交登录请求
/// </summary>
public class SocialLoginRequest
{
    /// <summary>
    ///     社交平台（wechat, qq, alipay, apple, google）
    /// </summary>
    [Required(ErrorMessage = "社交平台不能为空")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    ///     授权码（OAuth code）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     访问令牌（某些平台直接返回）
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    ///     用户唯一标识（OpenID）
    /// </summary>
    public string? OpenId { get; set; }
}

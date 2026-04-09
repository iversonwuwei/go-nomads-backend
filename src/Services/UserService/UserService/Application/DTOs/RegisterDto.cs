using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

/// <summary>
///     用户注册 DTO
/// </summary>
public class RegisterDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "用户名长度必须在2-100之间")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100之间")]
    public string Password { get; set; } = string.Empty;

    [StringLength(6, MinimumLength = 4, ErrorMessage = "验证码长度为 4-6 位")]
    public string? VerificationCode { get; set; }

    [Phone(ErrorMessage = "手机号格式不正确")] public string? Phone { get; set; }
}
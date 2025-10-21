using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

/// <summary>
/// 登录请求 DTO
/// </summary>
public class LoginDto
{
    /// <summary>
    /// 用户邮箱
    /// </summary>
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 用户密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(6, ErrorMessage = "密码长度至少为6位")]
    public string Password { get; set; } = string.Empty;
}

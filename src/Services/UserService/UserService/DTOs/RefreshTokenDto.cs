using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

/// <summary>
/// 刷新令牌请求 DTO
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}

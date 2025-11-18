namespace UserService.Application.DTOs;

/// <summary>
///     认证响应 DTO
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    ///     访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    ///     刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     令牌类型 (通常是 "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    ///     令牌过期时间 (秒)
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    ///     用户信息
    /// </summary>
    public UserDto? User { get; set; }
}
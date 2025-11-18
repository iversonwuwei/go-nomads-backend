namespace GoNomads.Shared.Models;

/// <summary>
///     用户上下文信息 - 从 Gateway 传递过来的用户信息
/// </summary>
public class UserContext
{
    /// <summary>
    ///     用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    ///     用户邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     用户角色
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    ///     是否已认证
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    /// <summary>
    ///     原始 Authorization 头
    /// </summary>
    public string? AuthorizationHeader { get; set; }
}
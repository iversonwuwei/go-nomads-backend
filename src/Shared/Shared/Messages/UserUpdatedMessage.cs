namespace Shared.Messages;

/// <summary>
///     用户信息更新消息
///     当用户修改名称、头像等信息时发布此消息
///     订阅者可以更新自己服务中的冗余数据
/// </summary>
public class UserUpdatedMessage
{
    /// <summary>
    ///     用户 ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    ///     用户名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     用户头像 URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    ///     用户邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     更新的字段列表（可选，用于增量更新）
    /// </summary>
    public List<string>? UpdatedFields { get; set; }
}

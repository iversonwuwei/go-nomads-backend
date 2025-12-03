namespace Shared.Messages;

/// <summary>
///     聊天室在线状态变更消息（用于实时在线人数推送）
/// </summary>
public class ChatRoomOnlineStatusMessage
{
    /// <summary>
    ///     聊天室 ID
    /// </summary>
    public required string RoomId { get; set; }

    /// <summary>
    ///     用户 ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    ///     用户名
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    ///     用户头像
    /// </summary>
    public string? UserAvatar { get; set; }

    /// <summary>
    ///     用户角色: owner, admin, member
    /// </summary>
    public string Role { get; set; } = "member";

    /// <summary>
    ///     事件类型: joined, left
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    ///     当前在线人数
    /// </summary>
    public int OnlineCount { get; set; }

    /// <summary>
    ///     在线用户列表（可选，仅在需要完整列表时填充）
    /// </summary>
    public List<OnlineUserInfo>? OnlineUsers { get; set; }

    /// <summary>
    ///     时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     在线用户信息
/// </summary>
public class OnlineUserInfo
{
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public string? UserAvatar { get; set; }
    public string Role { get; set; } = "member";
    public bool IsOnline { get; set; } = true;
}

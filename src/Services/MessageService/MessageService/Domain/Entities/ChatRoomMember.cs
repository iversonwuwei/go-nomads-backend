namespace MessageService.Domain.Entities;

/// <summary>
///     聊天室成员实体
/// </summary>
public class ChatRoomMember
{
    public Guid Id { get; set; }
    
    /// <summary>
    ///     聊天室 ID
    /// </summary>
    public string RoomId { get; set; } = string.Empty;
    
    /// <summary>
    ///     用户 ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    ///     用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    ///     用户头像
    /// </summary>
    public string? UserAvatar { get; set; }
    
    /// <summary>
    ///     成员角色：member, admin, owner
    /// </summary>
    public string Role { get; set; } = "member";
    
    /// <summary>
    ///     加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; }
    
    /// <summary>
    ///     最后活跃时间
    /// </summary>
    public DateTime? LastSeenAt { get; set; }
    
    /// <summary>
    ///     是否被禁言
    /// </summary>
    public bool IsMuted { get; set; }
    
    /// <summary>
    ///     禁言到期时间
    /// </summary>
    public DateTime? MutedUntil { get; set; }
    
    /// <summary>
    ///     是否已离开
    /// </summary>
    public bool HasLeft { get; set; }
    
    /// <summary>
    ///     离开时间
    /// </summary>
    public DateTime? LeftAt { get; set; }
}

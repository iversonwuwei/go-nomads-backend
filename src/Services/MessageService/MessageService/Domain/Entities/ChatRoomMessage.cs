namespace MessageService.Domain.Entities;

/// <summary>
///     聊天室消息实体
/// </summary>
public class ChatRoomMessage
{
    public Guid Id { get; set; }
    
    /// <summary>
    ///     聊天室 ID
    /// </summary>
    public string RoomId { get; set; } = string.Empty;
    
    /// <summary>
    ///     发送者用户 ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    ///     发送者用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    ///     发送者头像
    /// </summary>
    public string? UserAvatar { get; set; }
    
    /// <summary>
    ///     消息内容
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    ///     消息类型：text, image, file, location, voice, video
    /// </summary>
    public string MessageType { get; set; } = "text";
    
    /// <summary>
    ///     回复的消息 ID
    /// </summary>
    public Guid? ReplyToId { get; set; }
    
    /// <summary>
    ///     回复的消息（导航属性）
    /// </summary>
    public ChatRoomMessage? ReplyTo { get; set; }
    
    /// <summary>
    ///     @提及的用户 ID 列表 (JSON)
    /// </summary>
    public string? MentionsJson { get; set; }
    
    /// <summary>
    ///     附件信息 (JSON)
    /// </summary>
    public string? AttachmentJson { get; set; }
    
    /// <summary>
    ///     发送时间
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    ///     是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    ///     删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}

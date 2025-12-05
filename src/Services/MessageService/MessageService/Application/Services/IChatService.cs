using MessageService.Domain.Entities;

namespace MessageService.Application.Services;

/// <summary>
///     聊天服务接口
/// </summary>
public interface IChatService
{
    // ==================== 聊天室管理 ====================
    
    /// <summary>
    ///     获取公开聊天室列表
    /// </summary>
    Task<List<ChatRoomDto>> GetPublicRoomsAsync(int page = 1, int pageSize = 20);
    
    /// <summary>
    ///     获取聊天室详情
    /// </summary>
    Task<ChatRoomDto?> GetRoomByIdAsync(string roomId);
    
    /// <summary>
    ///     根据 Meetup ID 获取或创建聊天室
    /// </summary>
    /// <param name="meetupId">Meetup ID</param>
    /// <param name="meetupTitle">Meetup 标题</param>
    /// <param name="meetupType">Meetup 类型</param>
    /// <param name="organizerId">组织者用户ID（可选，创建时自动加入群聊）</param>
    /// <param name="organizerName">组织者用户名（可选）</param>
    /// <param name="organizerAvatar">组织者头像（可选）</param>
    Task<ChatRoomDto> GetOrCreateMeetupRoomAsync(
        Guid meetupId, 
        string meetupTitle, 
        string? meetupType,
        string? organizerId = null,
        string? organizerName = null,
        string? organizerAvatar = null);
    
    /// <summary>
    ///     获取用户加入的聊天室
    /// </summary>
    Task<List<ChatRoomDto>> GetUserRoomsAsync(string userId, int page = 1, int pageSize = 20);
    
    /// <summary>
    ///     加入聊天室
    /// </summary>
    Task<bool> JoinRoomAsync(string roomId, string userId, string userName, string? userAvatar);
    
    /// <summary>
    ///     离开聊天室
    /// </summary>
    Task<bool> LeaveRoomAsync(string roomId, string userId);
    
    /// <summary>
    ///     获取或创建一对一私聊房间
    /// </summary>
    /// <param name="userId1">用户1的ID</param>
    /// <param name="userId2">用户2的ID</param>
    /// <param name="userName1">用户1的名称</param>
    /// <param name="userName2">用户2的名称</param>
    /// <param name="userAvatar1">用户1的头像</param>
    /// <param name="userAvatar2">用户2的头像</param>
    Task<ChatRoomDto> GetOrCreateDirectChatAsync(
        string userId1, 
        string userId2, 
        string userName1, 
        string userName2,
        string? userAvatar1 = null,
        string? userAvatar2 = null);
    
    // ==================== 消息管理 ====================
    
    /// <summary>
    ///     保存消息
    /// </summary>
    Task<SavedMessageDto> SaveMessageAsync(SaveMessageDto dto);
    
    /// <summary>
    ///     获取消息列表
    /// </summary>
    Task<List<ChatMessageDto>> GetMessagesAsync(string roomId, int page = 1, int pageSize = 50);
    
    /// <summary>
    ///     删除消息
    /// </summary>
    Task<bool> DeleteMessageAsync(string messageId, string userId);
    
    // ==================== 成员管理 ====================
    
    /// <summary>
    ///     获取聊天室成员
    /// </summary>
    Task<List<MemberDto>> GetMembersAsync(string roomId, int page = 1, int pageSize = 20);
    
    /// <summary>
    ///     获取在线成员
    /// </summary>
    Task<List<MemberDto>> GetOnlineMembersAsync(string roomId);
}

#region DTOs

public class ChatRoomDto
{
    public string Id { get; set; } = string.Empty;
    public string RoomType { get; set; } = "city";
    public string? MeetupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ImageUrl { get; set; }
    public int TotalMembers { get; set; }
    public int OnlineUsers { get; set; }
    public ChatMessageDto? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public AuthorDto Author { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public ReplyDto? ReplyTo { get; set; }
    public List<string> Mentions { get; set; } = new();
    public MessageAttachmentDto? Attachment { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuthorDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
}

public class ReplyDto
{
    public string MessageId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class MemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Role { get; set; } = "member";
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public class MessageAttachmentDto
{
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public int? Duration { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

public class SaveMessageDto
{
    public string RoomId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public string? ReplyToId { get; set; }
    public List<string>? Mentions { get; set; }
    public MessageAttachmentDto? Attachment { get; set; }
}

public class SavedMessageDto
{
    public string Id { get; set; } = string.Empty;
    public ReplyDto? ReplyTo { get; set; }
    public DateTime Timestamp { get; set; }
}

#endregion

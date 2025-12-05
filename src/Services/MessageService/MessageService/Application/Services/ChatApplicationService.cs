using System.Text.Json;
using MessageService.Domain.Entities;
using MessageService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MessageService.Application.Services;

/// <summary>
///     聊天服务实现
/// </summary>
public class ChatApplicationService : IChatService
{
    private readonly IChatRoomRepository _roomRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatMemberRepository _memberRepository;
    private readonly ILogger<ChatApplicationService> _logger;

    public ChatApplicationService(
        IChatRoomRepository roomRepository,
        IChatMessageRepository messageRepository,
        IChatMemberRepository memberRepository,
        ILogger<ChatApplicationService> logger)
    {
        _roomRepository = roomRepository;
        _messageRepository = messageRepository;
        _memberRepository = memberRepository;
        _logger = logger;
    }

    #region 聊天室管理

    public async Task<List<ChatRoomDto>> GetPublicRoomsAsync(int page = 1, int pageSize = 20)
    {
        var rooms = await _roomRepository.GetPublicRoomsAsync(page, pageSize);
        var result = new List<ChatRoomDto>();

        foreach (var room in rooms)
        {
            var dto = MapToDto(room);
            dto.OnlineUsers = 0; // TODO: 从 Redis 获取在线人数
            result.Add(dto);
        }

        return result;
    }

    public async Task<ChatRoomDto?> GetRoomByIdAsync(string roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return null;

        var dto = MapToDto(room);
        dto.OnlineUsers = 0; // TODO: 从 Redis 获取在线人数
        return dto;
    }

    public async Task<ChatRoomDto> GetOrCreateMeetupRoomAsync(
        Guid meetupId, 
        string meetupTitle, 
        string? meetupType,
        string? organizerId = null,
        string? organizerName = null,
        string? organizerAvatar = null)
    {
        // 先查找是否已存在
        var existingRoom = await _roomRepository.GetRoomByMeetupIdAsync(meetupId);
        if (existingRoom != null)
        {
            return MapToDto(existingRoom);
        }

        // 创建新聊天室
        var newRoom = new ChatRoom
        {
            Id = Guid.NewGuid(),
            RoomType = "meetup",
            MeetupId = meetupId,
            Name = meetupTitle,
            Description = $"{meetupType} Meetup Chat",
            IsPublic = false, // Meetup 聊天室只对参与者可见
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdRoom = await _roomRepository.CreateAsync(newRoom);
        _logger.LogInformation("创建 Meetup 聊天室: {RoomId} for Meetup: {MeetupId}", createdRoom.Id, meetupId);

        // 如果提供了组织者信息，自动将组织者加入群聊（角色为 owner）
        if (!string.IsNullOrEmpty(organizerId))
        {
            var userName = organizerName ?? "Organizer";
            await JoinRoomAsOwnerAsync(createdRoom.Id.ToString(), organizerId, userName, organizerAvatar);
            _logger.LogInformation("组织者 {OrganizerId} 已自动加入 Meetup 聊天室 {RoomId} (角色: owner)", organizerId, createdRoom.Id);
        }

        return MapToDto(createdRoom);
    }

    public async Task<List<ChatRoomDto>> GetUserRoomsAsync(string userId, int page = 1, int pageSize = 20)
    {
        var rooms = await _roomRepository.GetUserRoomsAsync(userId, page, pageSize);
        return rooms.Select(MapToDto).ToList();
    }

    public async Task<bool> JoinRoomAsync(string roomId, string userId, string userName, string? userAvatar)
    {
        return await JoinRoomWithRoleAsync(roomId, userId, userName, userAvatar, "member");
    }

    /// <summary>
    /// 以 owner 角色加入聊天室
    /// </summary>
    public async Task<bool> JoinRoomAsOwnerAsync(string roomId, string userId, string userName, string? userAvatar)
    {
        return await JoinRoomWithRoleAsync(roomId, userId, userName, userAvatar, "owner");
    }

    /// <summary>
    /// 以指定角色加入聊天室
    /// </summary>
    private async Task<bool> JoinRoomWithRoleAsync(string roomId, string userId, string userName, string? userAvatar, string role)
    {
        // 检查是否已是成员
        var isMember = await _memberRepository.IsMemberAsync(roomId, userId);
        if (isMember)
        {
            // 更新最后活跃时间
            await _memberRepository.UpdateLastSeenAsync(roomId, userId);
            return true;
        }

        // 添加为新成员
        var member = new ChatRoomMember
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            UserId = userId,
            UserName = userName,
            UserAvatar = userAvatar,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        await _memberRepository.AddMemberAsync(member);
        _logger.LogInformation("用户 {UserId} 以 {Role} 角色加入聊天室 {RoomId}", userId, role, roomId);

        return true;
    }

    public async Task<bool> LeaveRoomAsync(string roomId, string userId)
    {
        await _memberRepository.RemoveMemberAsync(roomId, userId);
        _logger.LogInformation("用户 {UserId} 离开聊天室 {RoomId}", userId, roomId);
        return true;
    }

    /// <summary>
    /// 获取或创建一对一私聊房间
    /// </summary>
    public async Task<ChatRoomDto> GetOrCreateDirectChatAsync(
        string userId1, 
        string userId2, 
        string userName1, 
        string userName2,
        string? userAvatar1 = null,
        string? userAvatar2 = null)
    {
        // 生成唯一的私聊房间标识（按字典序排列，确保两个用户无论谁发起都是同一个房间）
        var sortedIds = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        var directChatKey = $"direct_{sortedIds[0]}_{sortedIds[1]}";
        
        // 先查找是否已存在私聊房间
        var existingRoom = await _roomRepository.GetRoomByDirectChatKeyAsync(directChatKey);
        if (existingRoom != null)
        {
            _logger.LogInformation("找到已存在的私聊房间: {RoomId}", existingRoom.Id);
            return MapToDto(existingRoom);
        }

        // 创建新的私聊房间
        var newRoom = new ChatRoom
        {
            Id = Guid.NewGuid(),
            RoomType = "direct",
            Name = directChatKey, // 使用唯一标识作为名称
            Description = $"Private chat between {userName1} and {userName2}",
            IsPublic = false,
            CreatedBy = userId1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdRoom = await _roomRepository.CreateAsync(newRoom);
        _logger.LogInformation("创建私聊房间: {RoomId} between {User1} and {User2}", 
            createdRoom.Id, userId1, userId2);

        // 将两个用户都加入房间
        await JoinRoomWithRoleAsync(createdRoom.Id.ToString(), userId1, userName1, userAvatar1, "member");
        await JoinRoomWithRoleAsync(createdRoom.Id.ToString(), userId2, userName2, userAvatar2, "member");

        return MapToDto(createdRoom);
    }

    #endregion

    #region 消息管理

    public async Task<SavedMessageDto> SaveMessageAsync(SaveMessageDto dto)
    {
        var message = new ChatRoomMessage
        {
            Id = Guid.NewGuid(),
            RoomId = dto.RoomId,
            UserId = dto.UserId,
            UserName = dto.UserName,
            UserAvatar = dto.UserAvatar,
            Message = dto.Message,
            MessageType = dto.MessageType,
            Timestamp = DateTime.UtcNow
        };

        // 处理回复
        ReplyDto? replyDto = null;
        if (!string.IsNullOrEmpty(dto.ReplyToId) && Guid.TryParse(dto.ReplyToId, out var replyToGuid))
        {
            message.ReplyToId = replyToGuid;
            
            // 获取被回复的消息
            var replyToMessage = await _messageRepository.GetByIdAsync(replyToGuid);
            if (replyToMessage != null)
            {
                replyDto = new ReplyDto
                {
                    MessageId = replyToMessage.Id.ToString(),
                    Message = replyToMessage.Message.Length > 100 
                        ? replyToMessage.Message.Substring(0, 100) + "..." 
                        : replyToMessage.Message,
                    UserName = replyToMessage.UserName
                };
            }
        }

        // 处理提及
        if (dto.Mentions != null && dto.Mentions.Any())
        {
            message.MentionsJson = JsonSerializer.Serialize(dto.Mentions);
        }

        // 处理附件
        if (dto.Attachment != null)
        {
            message.AttachmentJson = JsonSerializer.Serialize(dto.Attachment);
        }

        var savedMessage = await _messageRepository.SaveAsync(message);

        return new SavedMessageDto
        {
            Id = savedMessage.Id.ToString(),
            ReplyTo = replyDto,
            Timestamp = savedMessage.Timestamp
        };
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(string roomId, int page = 1, int pageSize = 50)
    {
        var messages = await _messageRepository.GetMessagesAsync(roomId, page, pageSize);
        return messages.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteMessageAsync(string messageId, string userId)
    {
        if (!Guid.TryParse(messageId, out var msgGuid))
        {
            return false;
        }

        return await _messageRepository.DeleteAsync(msgGuid, userId);
    }

    #endregion

    #region 成员管理

    public async Task<List<MemberDto>> GetMembersAsync(string roomId, int page = 1, int pageSize = 20)
    {
        var members = await _memberRepository.GetMembersAsync(roomId, page, pageSize);
        return members.Select(m => new MemberDto
        {
            UserId = m.UserId,
            UserName = m.UserName,
            UserAvatar = m.UserAvatar,
            Role = m.Role,
            IsOnline = false, // TODO: 从 Redis 获取在线状态
            LastSeenAt = m.LastSeenAt
        }).ToList();
    }

    public async Task<List<MemberDto>> GetOnlineMembersAsync(string roomId)
    {
        // TODO: 从 Redis 获取实时在线用户
        var members = await _memberRepository.GetOnlineMembersAsync(roomId);
        return members.Select(m => new MemberDto
        {
            UserId = m.UserId,
            UserName = m.UserName,
            UserAvatar = m.UserAvatar,
            Role = m.Role,
            IsOnline = true,
            LastSeenAt = DateTime.UtcNow
        }).ToList();
    }

    #endregion

    #region 映射方法

    private ChatRoomDto MapToDto(ChatRoom room)
    {
        return new ChatRoomDto
        {
            Id = room.Id.ToString(),
            RoomType = room.RoomType,
            MeetupId = room.MeetupId?.ToString(),
            Name = room.Name,
            Description = room.Description,
            City = room.City,
            Country = room.Country,
            ImageUrl = room.ImageUrl,
            TotalMembers = room.TotalMembers,
            CreatedAt = room.CreatedAt
        };
    }

    private ChatMessageDto MapToDto(ChatRoomMessage message)
    {
        var dto = new ChatMessageDto
        {
            Id = message.Id.ToString(),
            Author = new AuthorDto
            {
                UserId = message.UserId,
                UserName = message.UserName,
                UserAvatar = message.UserAvatar
            },
            Message = message.Message,
            MessageType = message.MessageType,
            Timestamp = message.Timestamp
        };

        // 解析提及
        if (!string.IsNullOrEmpty(message.MentionsJson))
        {
            try
            {
                dto.Mentions = JsonSerializer.Deserialize<List<string>>(message.MentionsJson) ?? new List<string>();
            }
            catch
            {
                dto.Mentions = new List<string>();
            }
        }

        // 解析附件
        if (!string.IsNullOrEmpty(message.AttachmentJson))
        {
            try
            {
                dto.Attachment = JsonSerializer.Deserialize<MessageAttachmentDto>(message.AttachmentJson);
            }
            catch
            {
                // 忽略解析错误
            }
        }

        // 解析回复
        if (message.ReplyTo != null)
        {
            dto.ReplyTo = new ReplyDto
            {
                MessageId = message.ReplyTo.Id.ToString(),
                Message = message.ReplyTo.Message.Length > 100 
                    ? message.ReplyTo.Message.Substring(0, 100) + "..." 
                    : message.ReplyTo.Message,
                UserName = message.ReplyTo.UserName
            };
        }

        return dto;
    }

    #endregion
}

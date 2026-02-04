using MessageService.Domain.Entities;
using MessageService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Postgrest;
using Postgrest.Attributes;
using Client = Supabase.Client;

namespace MessageService.Infrastructure.Repositories;

/// <summary>
///     聊天消息仓储实现 - Supabase
/// </summary>
public class SupabaseChatMessageRepository : IChatMessageRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseChatMessageRepository> _logger;

    public SupabaseChatMessageRepository(Client supabaseClient, ILogger<SupabaseChatMessageRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<ChatRoomMessage>> GetMessagesAsync(string roomId, int page = 1, int pageSize = 50)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var response = await _supabaseClient
                .From<ChatRoomMessageModel>()
                .Where(m => m.RoomId == roomId)
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Order("timestamp", Constants.Ordering.Descending)
                .Range(skip, skip + pageSize - 1)
                .Get();

            return response.Models.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天消息失败: RoomId={RoomId}", roomId);
            return new List<ChatRoomMessage>();
        }
    }

    public async Task<ChatRoomMessage?> GetByIdAsync(Guid messageId)
    {
        try
        {
            var response = await _supabaseClient
                .From<ChatRoomMessageModel>()
                .Where(m => m.Id == messageId)
                .Single();

            return response != null ? MapToDomain(response) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取消息失败: MessageId={MessageId}", messageId);
            return null;
        }
    }

    public async Task<ChatRoomMessage> SaveAsync(ChatRoomMessage message)
    {
        try
        {
            var model = MapToModel(message);
            var response = await _supabaseClient
                .From<ChatRoomMessageModel>()
                .Insert(model, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("保存消息失败");
            }

            _logger.LogInformation("保存消息成功: Id={Id}, RoomId={RoomId}", created.Id, created.RoomId);
            return MapToDomain(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存消息失败: RoomId={RoomId}", message.RoomId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid messageId, string userId)
    {
        try
        {
            // 获取消息，验证权限
            var message = await GetByIdAsync(messageId);
            if (message == null)
            {
                return false;
            }

            // 只允许消息发送者删除
            if (message.UserId != userId)
            {
                _logger.LogWarning("用户无权删除消息: MessageId={MessageId}, UserId={UserId}", messageId, userId);
                return false;
            }

            // 软删除
            var model = new ChatRoomMessageModel
            {
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            };

            await _supabaseClient
                .From<ChatRoomMessageModel>()
                .Where(m => m.Id == messageId)
                .Update(model);

            _logger.LogInformation("删除消息成功: Id={Id}", messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除消息失败: MessageId={MessageId}", messageId);
            return false;
        }
    }

    public async Task<int> GetMessageCountAsync(string roomId)
    {
        try
        {
            var response = await _supabaseClient
                .From<ChatRoomMessageModel>()
                .Where(m => m.RoomId == roomId)
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Get();

            return response.Models.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取消息数量失败: RoomId={RoomId}", roomId);
            return 0;
        }
    }

    public async Task<List<ChatRoomMessage>> SearchMessagesAsync(string roomId, string keyword, int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;

            // Supabase 使用 ilike 进行模糊搜索
            var response = await _supabaseClient
                .From<ChatRoomMessageModel>()
                .Filter("room_id", Constants.Operator.Equals, roomId)
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Filter("message", Constants.Operator.ILike, $"%{keyword}%")
                .Order("timestamp", Constants.Ordering.Descending)
                .Range(skip, skip + pageSize - 1)
                .Get();

            return response.Models.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索消息失败: RoomId={RoomId}, Keyword={Keyword}", roomId, keyword);
            return new List<ChatRoomMessage>();
        }
    }

    #region 映射方法

    private ChatRoomMessage MapToDomain(ChatRoomMessageModel model)
    {
        return new ChatRoomMessage
        {
            Id = model.Id,
            RoomId = model.RoomId,
            UserId = model.UserId,
            // UserName 和 UserAvatar 从 UserService 获取
            UserName = string.Empty,
            UserAvatar = null,
            Message = model.Message,
            MessageType = model.MessageType,
            ReplyToId = model.ReplyToId,
            MentionsJson = model.MentionsJson,
            AttachmentJson = model.AttachmentJson,
            Timestamp = model.Timestamp,
            IsDeleted = model.IsDeleted,
            DeletedAt = model.DeletedAt
        };
    }

    private ChatRoomMessageModel MapToModel(ChatRoomMessage message)
    {
        return new ChatRoomMessageModel
        {
            Id = message.Id,
            RoomId = message.RoomId,
            UserId = message.UserId,
            // UserName 和 UserAvatar 不再存储到数据库
            Message = message.Message,
            MessageType = message.MessageType,
            ReplyToId = message.ReplyToId,
            MentionsJson = message.MentionsJson,
            AttachmentJson = message.AttachmentJson,
            Timestamp = message.Timestamp,
            IsDeleted = message.IsDeleted,
            DeletedAt = message.DeletedAt
        };
    }

    #endregion
}

#region Supabase Models

[Table("chat_room_messages")]
public class ChatRoomMessageModel : Postgrest.Models.BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("room_id")]
    public string RoomId { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    // user_name 和 user_avatar 已从数据库表中删除
    // 用户信息现在通过 UserService 动态获取

    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("message_type")]
    public string MessageType { get; set; } = "text";

    [Column("reply_to_id")]
    public Guid? ReplyToId { get; set; }

    [Column("mentions_json")]
    public string? MentionsJson { get; set; }

    [Column("attachment_json")]
    public string? AttachmentJson { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

#endregion

using MessageService.Domain.Entities;
using MessageService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Postgrest;
using Postgrest.Attributes;
using Client = Supabase.Client;

namespace MessageService.Infrastructure.Repositories;

/// <summary>
///     聊天室仓储实现 - Supabase
/// </summary>
public class SupabaseChatRoomRepository : IChatRoomRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseChatRoomRepository> _logger;

    public SupabaseChatRoomRepository(Client supabaseClient, ILogger<SupabaseChatRoomRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<ChatRoom>> GetPublicRoomsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var response = await _supabaseClient
                .From<ChatRoomModel>()
                .Where(r => r.IsPublic == true && r.IsDeleted == false)
                .Order("created_at", Constants.Ordering.Descending)
                .Range(skip, skip + pageSize - 1)
                .Get();

            return response.Models.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取公开聊天室失败");
            return new List<ChatRoom>();
        }
    }

    public async Task<List<ChatRoom>> GetRoomsByCityAsync(string city, string country)
    {
        try
        {
            var response = await _supabaseClient
                .From<ChatRoomModel>()
                .Where(r => r.City == city && r.Country == country && r.IsDeleted == false)
                .Get();

            return response.Models.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据城市获取聊天室失败: City={City}, Country={Country}", city, country);
            return new List<ChatRoom>();
        }
    }

    public async Task<ChatRoom?> GetRoomByMeetupIdAsync(Guid meetupId)
    {
        try
        {
            var response = await _supabaseClient
                .From<ChatRoomModel>()
                .Where(r => r.MeetupId == meetupId && r.IsDeleted == false)
                .Single();

            return response != null ? MapToDomain(response) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据 MeetupId 获取聊天室失败: MeetupId={MeetupId}", meetupId);
            return null;
        }
    }

    public async Task<ChatRoom?> GetByIdAsync(string roomId)
    {
        try
        {
            if (!Guid.TryParse(roomId, out var guid))
            {
                // 尝试从 meetup_ 前缀提取 ID
                if (roomId.StartsWith("meetup_"))
                {
                    var meetupIdStr = roomId.Substring(7);
                    if (Guid.TryParse(meetupIdStr, out var meetupGuid))
                    {
                        return await GetRoomByMeetupIdAsync(meetupGuid);
                    }
                }
                return null;
            }

            var response = await _supabaseClient
                .From<ChatRoomModel>()
                .Where(r => r.Id == guid && r.IsDeleted == false)
                .Single();

            return response != null ? MapToDomain(response) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天室失败: RoomId={RoomId}", roomId);
            return null;
        }
    }

    public async Task<ChatRoom> CreateAsync(ChatRoom room)
    {
        try
        {
            var model = MapToModel(room);
            var response = await _supabaseClient
                .From<ChatRoomModel>()
                .Insert(model, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("创建聊天室失败");
            }

            _logger.LogInformation("创建聊天室成功: Id={Id}, Name={Name}", created.Id, created.Name);
            return MapToDomain(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建聊天室失败: Name={Name}", room.Name);
            throw;
        }
    }

    public async Task<ChatRoom> UpdateAsync(ChatRoom room)
    {
        try
        {
            var model = MapToModel(room);
            model.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<ChatRoomModel>()
                .Where(r => r.Id == room.Id)
                .Update(model);

            return room;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新聊天室失败: Id={Id}", room.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string roomId)
    {
        try
        {
            if (!Guid.TryParse(roomId, out var guid)) return;

            var model = new ChatRoomModel
            {
                IsDeleted = true,
                UpdatedAt = DateTime.UtcNow
            };

            await _supabaseClient
                .From<ChatRoomModel>()
                .Where(r => r.Id == guid)
                .Update(model);

            _logger.LogInformation("删除聊天室: Id={Id}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除聊天室失败: Id={Id}", roomId);
            throw;
        }
    }

    public async Task<List<ChatRoom>> GetUserRoomsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;

            // 先获取用户加入的聊天室 ID 列表
            var memberResponse = await _supabaseClient
                .From<ChatRoomMemberModel>()
                .Where(m => m.UserId == userId && m.HasLeft == false)
                .Select("room_id")
                .Get();

            var roomIds = memberResponse.Models.Select(m => m.RoomId).Distinct().ToList();

            if (!roomIds.Any())
            {
                return new List<ChatRoom>();
            }

            // 获取聊天室详情
            var rooms = new List<ChatRoom>();
            foreach (var roomId in roomIds.Skip(skip).Take(pageSize))
            {
                var room = await GetByIdAsync(roomId);
                if (room != null)
                {
                    rooms.Add(room);
                }
            }

            return rooms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户聊天室失败: UserId={UserId}", userId);
            return new List<ChatRoom>();
        }
    }

    public async Task<ChatRoom?> GetRoomByDirectChatKeyAsync(string directChatKey)
    {
        try
        {
            // 私聊房间的 Name 字段存储了 directChatKey
            var response = await _supabaseClient
                .From<ChatRoomModel>()
                .Where(r => r.RoomType == "direct" && r.Name == directChatKey && r.IsDeleted == false)
                .Single();

            return response != null ? MapToDomain(response) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据 DirectChatKey 获取聊天室失败: Key={Key}", directChatKey);
            return null;
        }
    }

    #region 映射方法

    private ChatRoom MapToDomain(ChatRoomModel model)
    {
        return new ChatRoom
        {
            Id = model.Id,
            RoomType = model.RoomType,
            MeetupId = model.MeetupId,
            Name = model.Name,
            Description = model.Description,
            City = model.City,
            Country = model.Country,
            ImageUrl = model.ImageUrl,
            CreatedBy = model.CreatedBy,
            IsPublic = model.IsPublic,
            TotalMembers = model.TotalMembers,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsDeleted = model.IsDeleted
        };
    }

    private ChatRoomModel MapToModel(ChatRoom room)
    {
        return new ChatRoomModel
        {
            Id = room.Id,
            RoomType = room.RoomType,
            MeetupId = room.MeetupId,
            Name = room.Name,
            Description = room.Description,
            City = room.City,
            Country = room.Country,
            ImageUrl = room.ImageUrl,
            CreatedBy = room.CreatedBy,
            IsPublic = room.IsPublic,
            TotalMembers = room.TotalMembers,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
            IsDeleted = room.IsDeleted
        };
    }

    #endregion
}

#region Supabase Models

[Table("chat_rooms")]
public class ChatRoomModel : Postgrest.Models.BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("room_type")]
    public string RoomType { get; set; } = "city";

    [Column("meetup_id")]
    public Guid? MeetupId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("country")]
    public string? Country { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("is_public")]
    public bool IsPublic { get; set; } = true;

    [Column("total_members")]
    public int TotalMembers { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

[Table("chat_room_members")]
public class ChatRoomMemberModel : Postgrest.Models.BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("room_id")]
    public string RoomId { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;

    [Column("user_avatar")]
    public string? UserAvatar { get; set; }

    [Column("role")]
    public string Role { get; set; } = "member";

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }

    [Column("last_seen_at")]
    public DateTime? LastSeenAt { get; set; }

    [Column("is_muted")]
    public bool IsMuted { get; set; }

    [Column("muted_until")]
    public DateTime? MutedUntil { get; set; }

    [Column("has_left")]
    public bool HasLeft { get; set; }

    [Column("left_at")]
    public DateTime? LeftAt { get; set; }
}

#endregion

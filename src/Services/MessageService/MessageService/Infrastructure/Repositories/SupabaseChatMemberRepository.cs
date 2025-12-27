using MessageService.Domain.Entities;
using MessageService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Postgrest;
using Client = Supabase.Client;

namespace MessageService.Infrastructure.Repositories;

/// <summary>
///     聊天室成员仓储实现 - Supabase
/// </summary>
public class SupabaseChatMemberRepository : IChatMemberRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseChatMemberRepository> _logger;

    public SupabaseChatMemberRepository(Client supabaseClient, ILogger<SupabaseChatMemberRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<ChatRoomMember>> GetMembersAsync(string roomId, int page = 1, int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var response = await _supabaseClient
                .From<ChatRoomMemberModel>()
                .Where(m => m.RoomId == roomId)
                .Where(m => m.HasLeft == false)
                .Order("joined_at", Constants.Ordering.Descending)
                .Range(skip, skip + pageSize - 1)
                .Get();

            return response.Models.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天室成员失败: RoomId={RoomId}", roomId);
            return new List<ChatRoomMember>();
        }
    }

    public async Task<List<ChatRoomMember>> GetOnlineMembersAsync(string roomId)
    {
        try
        {
            // 获取最近5分钟内活跃的用户
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            var response = await _supabaseClient
                .From<ChatRoomMemberModel>()
                .Where(m => m.RoomId == roomId)
                .Where(m => m.HasLeft == false)
                .Filter("last_seen_at", Constants.Operator.GreaterThanOrEqual, fiveMinutesAgo.ToString("O"))
                .Get();

            return response.Models.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取在线成员失败: RoomId={RoomId}", roomId);
            return new List<ChatRoomMember>();
        }
    }

    public async Task<ChatRoomMember?> GetMemberAsync(string roomId, string userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<ChatRoomMemberModel>()
                .Where(m => m.RoomId == roomId)
                .Where(m => m.UserId == userId)
                .Single();

            return response != null ? MapToDomain(response) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取成员信息失败: RoomId={RoomId}, UserId={UserId}", roomId, userId);
            return null;
        }
    }

    public async Task<bool> IsMemberAsync(string roomId, string userId)
    {
        try
        {
            var member = await GetMemberAsync(roomId, userId);
            return member != null && !member.HasLeft;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ChatRoomMember> AddMemberAsync(ChatRoomMember member)
    {
        try
        {
            // 检查是否已存在（可能是之前离开过的成员）
            var existingMember = await GetMemberAsync(member.RoomId, member.UserId);
            if (existingMember != null)
            {
                // 重新加入
                existingMember.HasLeft = false;
                existingMember.LeftAt = null;
                existingMember.LastSeenAt = DateTime.UtcNow;
                return await UpdateMemberAsync(existingMember);
            }

            var model = MapToModel(member);
            var response = await _supabaseClient
                .From<ChatRoomMemberModel>()
                .Insert(model, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("添加成员失败");
            }

            _logger.LogInformation("添加成员成功: RoomId={RoomId}, UserId={UserId}", member.RoomId, member.UserId);
            return MapToDomain(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加成员失败: RoomId={RoomId}, UserId={UserId}", member.RoomId, member.UserId);
            throw;
        }
    }

    public async Task<ChatRoomMember> UpdateMemberAsync(ChatRoomMember member)
    {
        try
        {
            var model = MapToModel(member);
            await _supabaseClient
                .From<ChatRoomMemberModel>()
                .Where(m => m.Id == member.Id)
                .Update(model);

            return member;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新成员失败: Id={Id}", member.Id);
            throw;
        }
    }

    public async Task RemoveMemberAsync(string roomId, string userId)
    {
        try
        {
            var member = await GetMemberAsync(roomId, userId);
            if (member == null) return;

            member.HasLeft = true;
            member.LeftAt = DateTime.UtcNow;
            await UpdateMemberAsync(member);

            _logger.LogInformation("成员离开: RoomId={RoomId}, UserId={UserId}", roomId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除成员失败: RoomId={RoomId}, UserId={UserId}", roomId, userId);
            throw;
        }
    }

    public async Task UpdateLastSeenAsync(string roomId, string userId)
    {
        try
        {
            var member = await GetMemberAsync(roomId, userId);
            if (member == null) return;

            member.LastSeenAt = DateTime.UtcNow;
            await UpdateMemberAsync(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新最后活跃时间失败: RoomId={RoomId}, UserId={UserId}", roomId, userId);
        }
    }

    public async Task<int> GetMemberCountAsync(string roomId)
    {
        try
        {
            var response = await _supabaseClient
                .From<ChatRoomMemberModel>()
                .Where(m => m.RoomId == roomId)
                .Where(m => m.HasLeft == false)
                .Get();

            return response.Models.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取成员数量失败: RoomId={RoomId}", roomId);
            return 0;
        }
    }

    #region 映射方法

    private ChatRoomMember MapToDomain(ChatRoomMemberModel model)
    {
        return new ChatRoomMember
        {
            Id = model.Id,
            RoomId = model.RoomId,
            UserId = model.UserId,
            UserName = model.UserName,
            UserAvatar = model.UserAvatar,
            Role = model.Role,
            JoinedAt = model.JoinedAt,
            LastSeenAt = model.LastSeenAt,
            IsMuted = model.IsMuted,
            MutedUntil = model.MutedUntil,
            HasLeft = model.HasLeft,
            LeftAt = model.LeftAt
        };
    }

    private ChatRoomMemberModel MapToModel(ChatRoomMember member)
    {
        return new ChatRoomMemberModel
        {
            Id = member.Id,
            RoomId = member.RoomId,
            UserId = member.UserId,
            UserName = member.UserName,
            UserAvatar = member.UserAvatar,
            Role = member.Role,
            JoinedAt = member.JoinedAt,
            LastSeenAt = member.LastSeenAt,
            IsMuted = member.IsMuted,
            MutedUntil = member.MutedUntil,
            HasLeft = member.HasLeft,
            LeftAt = member.LeftAt
        };
    }

    #endregion
}

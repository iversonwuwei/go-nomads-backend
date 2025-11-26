using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace EventService.Infrastructure.Repositories;

/// <summary>
///     EventParticipant 仓储实现 - Supabase
/// </summary>
public class EventParticipantRepository : IEventParticipantRepository
{
    private readonly ILogger<EventParticipantRepository> _logger;
    private readonly Client _supabaseClient;

    public EventParticipantRepository(Client supabaseClient, ILogger<EventParticipantRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<EventParticipant> CreateAsync(EventParticipant participant)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Insert(participant);

            var created = result.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建参与记录失败");

            _logger.LogInformation("✅ 参与记录创建成功，EventId: {EventId}, UserId: {UserId}",
                participant.EventId, participant.UserId);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建参与记录失败");
            throw;
        }
    }

    public async Task<EventParticipant> UpdateAsync(EventParticipant participant)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Where(p => p.Id == participant.Id)
                .Update(participant);

            var updated = result.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新参与记录失败");

            _logger.LogInformation("✅ 参与记录更新成功，ID: {Id}, Status: {Status}",
                participant.Id, participant.Status);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新参与记录失败，ID: {Id}", participant.Id);
            throw;
        }
    }

    public async Task<EventParticipant?> GetAsync(Guid eventId, Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Where(p => p.EventId == eventId && p.UserId == userId)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取参与记录失败");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<EventParticipant>()
                .Where(p => p.Id == id)
                .Delete();

            _logger.LogInformation("✅ 参与记录删除成功，ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除参与记录失败");
            throw;
        }
    }

    public async Task<List<EventParticipant>> GetByEventIdAsync(Guid eventId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Where(p => p.EventId == eventId)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 Event 参与者失败，EventId: {EventId}", eventId);
            throw;
        }
    }

    public async Task<List<EventParticipant>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Where(p => p.UserId == userId)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户参与的 Event 失败，UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsParticipantAsync(Guid eventId, Guid userId)
    {
        try
        {
            // 使用Supabase原生Filter方法在数据库层过滤
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Filter("event_id", Constants.Operator.Equals, eventId.ToString())
                .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                .Filter("status", Constants.Operator.NotEqual, "cancelled")
                .Get();

            return result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查用户是否参与失败");
            throw;
        }
    }

    public async Task<HashSet<Guid>> GetParticipatedEventIdsAsync(List<Guid> eventIds, Guid userId)
    {
        try
        {
            if (!eventIds.Any())
            {
                return new HashSet<Guid>();
            }

            _logger.LogInformation("🔍 批量查询用户 {UserId} 参与的 {Count} 个活动", userId, eventIds.Count);

            // 使用Filter方法在数据库层过滤
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Filter("user_id", Constants.Operator.Equals, userId.ToString())
                .Filter("status", Constants.Operator.NotEqual, "cancelled")
                .Get();

            // 在内存中过滤eventIds(因为IN查询较复杂)
            var participatedEventIds = result.Models
                .Where(p => eventIds.Contains(p.EventId))
                .Select(p => p.EventId)
                .ToHashSet();

            _logger.LogInformation("✅ 用户 {UserId} 参与了 {ParticipatedCount}/{TotalCount} 个活动",
                userId, participatedEventIds.Count, eventIds.Count);

            return participatedEventIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量查询用户参与状态失败");
            return new HashSet<Guid>();
        }
    }

    public async Task<List<EventParticipant>> GetByUserIdWithStatusAsync(Guid userId, string? status = null)
    {
        try
        {
            var query = _supabaseClient
                .From<EventParticipant>()
                .Filter("user_id", Constants.Operator.Equals, userId.ToString());

            // 在数据库层过滤状态
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Filter("status", Constants.Operator.Equals, status);
            }

            var result = await query.Get();
            var participants = result.Models.ToList();

            _logger.LogInformation("✅ 获取用户参与记录成功，UserId: {UserId}, Status: {Status}, Count: {Count}",
                userId, status ?? "all", participants.Count);
            
            return participants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户参与记录失败，UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<int> CountByEventIdAsync(Guid eventId)
    {
        try
        {
            _logger.LogInformation("� 开始统计Event参与者数量，EventId: {EventId}", eventId);

            var result = await _supabaseClient
                .From<EventParticipant>()
                .Where(p => p.EventId == eventId)
                .Get();

            var count = result.Models?.Count ?? 0;
            _logger.LogInformation("✅ Event {EventId} 有 {Count} 个参与者", eventId, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 Event 参与者数量失败，EventId: {EventId}", eventId);
            return 0; // 失败时返回 0 而不是抛出异常
        }
    }
}
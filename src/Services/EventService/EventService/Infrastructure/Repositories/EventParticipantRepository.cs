using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Supabase;

namespace EventService.Infrastructure.Repositories;

/// <summary>
/// EventParticipant 仓储实现 - Supabase
/// </summary>
public class EventParticipantRepository : IEventParticipantRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<EventParticipantRepository> _logger;

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
            if (created == null)
            {
                throw new InvalidOperationException("创建参与记录失败");
            }

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
            var result = await _supabaseClient
                .From<EventParticipant>()
                .Where(p => p.EventId == eventId && p.UserId == userId)
                .Get();

            return result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查用户是否参与失败");
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

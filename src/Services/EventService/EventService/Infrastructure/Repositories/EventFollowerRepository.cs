using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Supabase;

namespace EventService.Infrastructure.Repositories;

/// <summary>
/// EventFollower 仓储实现 - Supabase
/// </summary>
public class EventFollowerRepository : IEventFollowerRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<EventFollowerRepository> _logger;

    public EventFollowerRepository(Client supabaseClient, ILogger<EventFollowerRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<EventFollower> CreateAsync(EventFollower follower)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventFollower>()
                .Insert(follower);

            var created = result.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("创建关注记录失败");
            }

            _logger.LogInformation("✅ 关注记录创建成功，EventId: {EventId}, UserId: {UserId}", 
                follower.EventId, follower.UserId);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建关注记录失败");
            throw;
        }
    }

    public async Task<EventFollower?> GetAsync(Guid eventId, Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventFollower>()
                .Where(f => f.EventId == eventId && f.UserId == userId)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取关注记录失败");
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<EventFollower>()
                .Where(f => f.Id == id)
                .Delete();

            _logger.LogInformation("✅ 关注记录删除成功，ID: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除关注记录失败");
            throw;
        }
    }

    public async Task<List<EventFollower>> GetByEventIdAsync(Guid eventId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventFollower>()
                .Where(f => f.EventId == eventId)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 Event 关注者失败，EventId: {EventId}", eventId);
            throw;
        }
    }

    public async Task<List<EventFollower>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventFollower>()
                .Where(f => f.UserId == userId)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户关注的 Event 失败，UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsFollowingAsync(Guid eventId, Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventFollower>()
                .Where(f => f.EventId == eventId && f.UserId == userId)
                .Get();

            return result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查用户是否关注失败");
            throw;
        }
    }

    public async Task<int> GetFollowerCountAsync(Guid eventId)
    {
        try
        {
            var result = await _supabaseClient
                .From<EventFollower>()
                .Where(f => f.EventId == eventId)
                .Get();

            return result.Models.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取关注者数量失败，EventId: {EventId}", eventId);
            return 0;
        }
    }
}

using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Supabase;

namespace EventService.Infrastructure.Repositories;

/// <summary>
/// Event 仓储实现 - Supabase
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<EventRepository> _logger;

    public EventRepository(Client supabaseClient, ILogger<EventRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<Event> CreateAsync(Event @event)
    {
        try
        {
            // 插入数据（Supabase C# SDK 的 Insert 有时不返回数据）
            var insertResult = await _supabaseClient
                .From<Event>()
                .Insert(@event, new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation });

            // 尝试从 insert 结果获取
            var createdEvent = insertResult.Models.FirstOrDefault();

            // 如果 insert 没有返回数据，通过 ID 查询
            if (createdEvent == null || createdEvent.Id == Guid.Empty)
            {
                _logger.LogWarning("⚠️ Insert 未返回数据，尝试通过 Title 查询最新记录");

                // 通过标题和创建者查询最新创建的 Event
                var queryResult = await _supabaseClient
                    .From<Event>()
                    .Where(e => e.Title == @event.Title && e.OrganizerId == @event.OrganizerId)
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                createdEvent = queryResult.Models.FirstOrDefault();
            }

            if (createdEvent == null)
            {
                throw new InvalidOperationException("创建 Event 失败 - 无法获取创建的记录");
            }

            _logger.LogInformation("✅ Event 创建成功，ID: {EventId}, Title: {Title}", createdEvent.Id, createdEvent.Title);
            return createdEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建 Event 失败");
            throw;
        }
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        try
        {
            var result = await _supabaseClient
                .From<Event>()
                .Where(e => e.Id == id)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 Event 失败，ID: {EventId}", id);
            throw;
        }
    }

    public async Task<Event> UpdateAsync(Event @event)
    {
        try
        {
            var result = await _supabaseClient
                .From<Event>()
                .Where(e => e.Id == @event.Id)
                .Update(@event);

            var updatedEvent = result.Models.FirstOrDefault();
            if (updatedEvent == null)
            {
                throw new InvalidOperationException("更新 Event 失败");
            }

            _logger.LogInformation("✅ Event 更新成功，ID: {EventId}", updatedEvent.Id);
            return updatedEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新 Event 失败，ID: {EventId}", @event.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<Event>()
                .Where(e => e.Id == id)
                .Delete();

            _logger.LogInformation("✅ Event 删除成功，ID: {EventId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除 Event 失败，ID: {EventId}", id);
            throw;
        }
    }

    public async Task<(List<Event> Events, int Total)> GetListAsync(
        Guid? cityId = null,
        string? category = null,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var query = _supabaseClient.From<Event>();

            // 构建查询条件
            if (cityId.HasValue)
            {
                query = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                    query.Where(e => e.CityId == cityId.Value);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                    query.Where(e => e.Category == category);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                    query.Where(e => e.Status == status);
            }

            var offset = (page - 1) * pageSize;
            var result = await query
                .Order(e => e.StartTime, Postgrest.Constants.Ordering.Ascending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            return (result.Models.ToList(), result.Models.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 Event 列表失败");
            throw;
        }
    }

    public async Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId)
    {
        try
        {
            var result = await _supabaseClient
                .From<Event>()
                .Where(e => e.OrganizerId == organizerId)
                .Order(e => e.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户创建的 Event 失败，OrganizerId: {OrganizerId}", organizerId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            var result = await _supabaseClient
                .From<Event>()
                .Where(e => e.Id == id)
                .Get();

            return result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查 Event 是否存在失败，ID: {EventId}", id);
            throw;
        }
    }
}

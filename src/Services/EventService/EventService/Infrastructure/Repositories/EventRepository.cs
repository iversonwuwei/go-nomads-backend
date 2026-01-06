using EventService.Domain.Entities;
using EventService.Domain.Repositories;
using Postgrest;
using Supabase.Interfaces;
using Supabase.Realtime;
using Client = Supabase.Client;
using Constants = Postgrest.Constants;

namespace EventService.Infrastructure.Repositories;

/// <summary>
///     Event 仓储实现 - Supabase
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly ILogger<EventRepository> _logger;
    private readonly Client _supabaseClient;

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
                .Insert(@event, new QueryOptions { Returning = QueryOptions.ReturnType.Representation });

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
                    .Order("created_at", Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                createdEvent = queryResult.Models.FirstOrDefault();
            }

            if (createdEvent == null) throw new InvalidOperationException("创建 Event 失败 - 无法获取创建的记录");

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
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
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
            if (updatedEvent == null) throw new InvalidOperationException("更新 Event 失败");

            _logger.LogInformation("✅ Event 更新成功，ID: {EventId}", updatedEvent.Id);
            return updatedEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新 Event 失败，ID: {EventId}", @event.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid? deletedBy = null)
    {
        try
        {
            // 逻辑删除：先获取记录，设置属性，然后更新
            var existingEvent = await _supabaseClient
                .From<Event>()
                .Filter("id", Constants.Operator.Equals, id.ToString())
                .Single();

            if (existingEvent == null)
            {
                _logger.LogWarning("⚠️ 要删除的 Event 不存在: {EventId}", id);
                return;
            }

            // 设置逻辑删除字段
            existingEvent.IsDeleted = true;
            existingEvent.DeletedAt = DateTime.UtcNow;
            existingEvent.UpdatedAt = DateTime.UtcNow;
            if (deletedBy.HasValue)
            {
                existingEvent.DeletedBy = deletedBy.Value;
                existingEvent.UpdatedBy = deletedBy.Value;
            }

            // 更新记录
            await _supabaseClient
                .From<Event>()
                .Update(existingEvent);

            _logger.LogInformation("✅ Event 逻辑删除成功，ID: {EventId}, DeletedBy: {DeletedBy}", id, deletedBy);
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

            // 过滤已删除的记录
            query = (ISupabaseTable<Event, RealtimeChannel>)
                query.Filter("is_deleted", Constants.Operator.NotEqual, "true");

            // 构建查询条件
            if (cityId.HasValue)
                query = (ISupabaseTable<Event, RealtimeChannel>)
                    query.Where(e => e.CityId == cityId.Value);

            if (!string.IsNullOrEmpty(category))
                query = (ISupabaseTable<Event, RealtimeChannel>)
                    query.Where(e => e.Category == category);

            // 支持多状态查询，用逗号分隔
            var isQueryingActiveEvents = false;
            if (!string.IsNullOrEmpty(status))
            {
                var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                
                // 检查是否在查询活动中的状态（upcoming, ongoing）
                isQueryingActiveEvents = statuses.Any(s => s == "upcoming" || s == "ongoing") 
                    && !statuses.Any(s => s == "completed" || s == "cancelled");
                
                if (statuses.Count == 1)
                {
                    // 单状态查询：使用 Filter 的 Equals 操作符
                    query = (ISupabaseTable<Event, RealtimeChannel>)
                        query.Filter("status", Constants.Operator.Equals, statuses[0]);
                }
                else if (statuses.Count > 1)
                {
                    // 多状态查询：使用 Filter 的 In 操作符 - 需要传入 List<string>
                    query = (ISupabaseTable<Event, RealtimeChannel>)
                        query.Filter("status", Constants.Operator.In, statuses);
                }
            }

            var offset = (page - 1) * pageSize;
            var result = await query
                .Order(e => e.StartTime, Constants.Ordering.Ascending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            var events = result.Models.ToList();
            
            // 如果查询的是活动中的状态（upcoming, ongoing），在应用层过滤掉实际上已经过期的活动
            // 这是为了确保即使状态更新服务还没来得及更新，也不会显示已过期的活动
            if (isQueryingActiveEvents)
            {
                // 使用 Unix 时间戳进行比较，避免时区问题
                var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var originalCount = events.Count;
                
                _logger.LogInformation("🕒 当前 UTC 时间戳: {Timestamp}", nowTimestamp);
                
                events = events.Where(e => {
                    // 将 StartTime 和 EndTime 转为时间戳（假设数据库存储的是本地时间，需要转为 UTC）
                    var startTimestamp = new DateTimeOffset(e.StartTime, TimeSpan.FromHours(8)).ToUnixTimeSeconds();
                    var endTimestamp = e.EndTime.HasValue 
                        ? new DateTimeOffset(e.EndTime.Value, TimeSpan.FromHours(8)).ToUnixTimeSeconds() 
                        : (long?)null;
                    
                    // 判断活动是否还有效（未过期）：
                    // 1. 如果还没开始（start_time > now），肯定有效
                    if (startTimestamp > nowTimestamp) return true;
                    
                    // 2. 如果已经开始，但有 end_time 且 end_time > now，说明还在进行中
                    if (endTimestamp.HasValue && endTimestamp.Value > nowTimestamp) return true;
                    
                    // 3. 其他情况（已开始且没有end_time，或end_time已过）都认为已过期
                    _logger.LogInformation("🔍 过滤掉已过期活动: {Title}, StartTime: {Start}, EndTime: {End}", 
                        e.Title, e.StartTime, e.EndTime);
                    return false;
                }).ToList();
                
                if (originalCount != events.Count)
                {
                    _logger.LogInformation("🔍 应用层过滤掉 {Count} 个已过期活动", originalCount - events.Count);
                }
            }

            return (events, events.Count);
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
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Order(e => e.CreatedAt, Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户创建的 Event 失败，OrganizerId: {OrganizerId}", organizerId);
            throw;
        }
    }

    public async Task<(List<Event> Events, int Total)> GetByOrganizerAsync(
        Guid organizerId,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            // 构建计数查询
            var countQuery = _supabaseClient.From<Event>();
            countQuery = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                countQuery.Where(e => e.OrganizerId == organizerId);
            countQuery = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                countQuery.Filter("is_deleted", Constants.Operator.NotEqual, "true");
            if (!string.IsNullOrEmpty(status))
            {
                countQuery = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                    countQuery.Where(e => e.Status == status);
            }

            // 获取总数
            var total = await countQuery.Count(Constants.CountType.Exact);

            // 重新构建数据查询（Count 会消费查询对象）
            var dataQuery = _supabaseClient.From<Event>();
            dataQuery = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                dataQuery.Where(e => e.OrganizerId == organizerId);
            dataQuery = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                dataQuery.Filter("is_deleted", Constants.Operator.NotEqual, "true");
            if (!string.IsNullOrEmpty(status))
            {
                dataQuery = (Supabase.Interfaces.ISupabaseTable<Event, Supabase.Realtime.RealtimeChannel>)
                    dataQuery.Where(e => e.Status == status);
            }

            // 获取分页数据
            var offset = (page - 1) * pageSize;
            var result = await dataQuery
                .Order(e => e.CreatedAt, Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            _logger.LogInformation("✅ 获取组织者活动列表成功，OrganizerId: {OrganizerId}, Status: {Status}, Total: {Total}, Items: {Items}",
                organizerId, status ?? "all", total, result.Models.Count);

            return (result.Models.ToList(), total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取组织者活动列表失败，OrganizerId: {OrganizerId}, Status: {Status}",
                organizerId, status);
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

    public async Task<List<Event>> GetExpiredEventsAsync(DateTime currentTime)
    {
        try
        {
            _logger.LogInformation("🔍 查询过期活动，当前时间: {CurrentTime}", currentTime);

            // 查询状态为 upcoming 且 endTime < currentTime 的活动
            // 如果没有 endTime，则使用 startTime
            var result = await _supabaseClient
                .From<Event>()
                .Where(e => e.Status == "upcoming")
                .Get();

            // 在内存中过滤已过期的活动
            var expiredEvents = result.Models
                .Where(e =>
                {
                    // 优先使用 EndTime，如果没有则使用 StartTime
                    var endTime = e.EndTime ?? e.StartTime;
                    return endTime < currentTime;
                })
                .ToList();

            _logger.LogInformation("✅ 找到 {Count} 个过期活动", expiredEvents.Count);
            return expiredEvents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取过期活动失败");
            throw;
        }
    }

    public async Task<List<Event>> GetActiveEventsForStatusUpdateAsync()
    {
        try
        {
            _logger.LogInformation("🔍 查询需要检查状态更新的活动（upcoming 或 ongoing）");

            // 查询状态为 upcoming 或 ongoing 的活动
            var statuses = new List<string> { "upcoming", "ongoing" };
            var result = await _supabaseClient
                .From<Event>()
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Filter("status", Constants.Operator.In, statuses)
                .Get();

            _logger.LogInformation("✅ 找到 {Count} 个活动需要检查状态", result.Models.Count);
            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取活动列表失败");
            throw;
        }
    }

    public async Task<(List<Event> Events, int Total)> GetByIdsAsync(
        List<Guid> eventIds,
        string? status = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            if (!eventIds.Any())
            {
                return (new List<Event>(), 0);
            }

            _logger.LogInformation("🔍 批量查询活动，ID数量: {Count}, Status: {Status}", eventIds.Count, status ?? "all");

            // 构建查询 - 首先按 ID 过滤
            var query = _supabaseClient.From<Event>();

            // 使用 In 操作符按 eventIds 过滤
            var eventIdStrings = eventIds.Select(id => id.ToString()).ToList();
            query = (ISupabaseTable<Event, RealtimeChannel>)query.Filter("id", Constants.Operator.In, eventIdStrings);
            
            // 过滤已删除的记录
            query = (ISupabaseTable<Event, RealtimeChannel>)query.Filter("is_deleted", Constants.Operator.NotEqual, "true");

            // 在数据库层过滤状态 - 支持逗号分隔的多状态值
            var isQueryingActiveEvents = false;
            if (!string.IsNullOrEmpty(status))
            {
                var statusList = status.Split(',').Select(s => s.Trim()).ToList();
                
                // 检查是否在查询活动中的状态（upcoming, ongoing）
                isQueryingActiveEvents = statusList.Any(s => s == "upcoming" || s == "ongoing") 
                    && !statusList.Any(s => s == "completed" || s == "cancelled");
                
                if (statusList.Count > 1)
                {
                    // 多状态查询：使用 In 操作符 - 需要传入 List<string>
                    _logger.LogInformation("🔍 多状态查询，状态列表: {Statuses}", string.Join(", ", statusList));
                    query = (ISupabaseTable<Event, RealtimeChannel>)query.Filter("status", Constants.Operator.In, statusList);
                }
                else
                {
                    // 单状态查询
                    query = (ISupabaseTable<Event, RealtimeChannel>)query.Where(e => e.Status == status);
                }
            }

            var result = await query.Get();

            // 排序（按开始时间降序）
            var events = result.Models
                .OrderByDescending(e => e.StartTime)
                .ToList();
            
            // 如果查询的是活动中的状态（upcoming, ongoing），在应用层过滤掉实际上已经过期的活动
            if (isQueryingActiveEvents && events.Count > 0)
            {
                // 使用 Unix 时间戳进行比较，避免时区问题
                var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var originalCount = events.Count;
                
                events = events.Where(e => {
                    // 将 StartTime 和 EndTime 转为时间戳（假设数据库存储的是北京时间 UTC+8）
                    var startTimestamp = new DateTimeOffset(e.StartTime, TimeSpan.FromHours(8)).ToUnixTimeSeconds();
                    var endTimestamp = e.EndTime.HasValue 
                        ? new DateTimeOffset(e.EndTime.Value, TimeSpan.FromHours(8)).ToUnixTimeSeconds() 
                        : (long?)null;
                    
                    // 判断活动是否还有效（未过期）
                    if (startTimestamp > nowTimestamp) return true;
                    if (endTimestamp.HasValue && endTimestamp.Value > nowTimestamp) return true;
                    return false;
                }).ToList();
                
                if (originalCount != events.Count)
                {
                    _logger.LogInformation("🔍 已加入列表过滤掉 {Count} 个已过期活动", originalCount - events.Count);
                }
            }

            // 分页
            var total = events.Count;
            var pagedEvents = events
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogInformation("✅ 批量查询完成，总数: {Total}, 当前页: {Count}", total, pagedEvents.Count);
            return (pagedEvents, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量查询活动失败");
            throw;
        }
    }

    #region 冗余字段更新方法

    /// <summary>
    ///     更新组织者信息（冗余字段）
    ///     当收到 UserUpdatedMessage 时调用此方法
    /// </summary>
    public async Task<int> UpdateOrganizerInfoAsync(Guid organizerId, string? name, string? avatarUrl)
    {
        try
        {
            _logger.LogInformation("🔄 开始更新组织者 {OrganizerId} 的冗余字段: Name={Name}", organizerId, name);

            // 查询该组织者的所有活动
            var result = await _supabaseClient.From<Event>()
                .Select("id")
                .Filter("organizer_id", Constants.Operator.Equals, organizerId.ToString())
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Get();

            var count = result.Models.Count;
            if (count == 0)
            {
                _logger.LogInformation("📝 未找到组织者 {OrganizerId} 的活动", organizerId);
                return 0;
            }

            // 更新所有记录的冗余字段
            await _supabaseClient.From<Event>()
                .Filter("organizer_id", Constants.Operator.Equals, organizerId.ToString())
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Set(x => x.OrganizerName, name)
                .Set(x => x.OrganizerAvatar, avatarUrl)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            _logger.LogInformation("✅ 已更新 {Count} 个活动的组织者信息", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新组织者信息失败: OrganizerId={OrganizerId}", organizerId);
            throw;
        }
    }

    /// <summary>
    ///     更新城市信息（冗余字段）
    ///     当收到 CityUpdatedMessage 时调用此方法
    /// </summary>
    public async Task<int> UpdateCityInfoAsync(Guid cityId, string? name, string? nameEn, string? country)
    {
        try
        {
            _logger.LogInformation("🔄 开始更新城市 {CityId} 的冗余字段: Name={Name}, Country={Country}", cityId, name, country);

            // 查询该城市下的所有活动
            var result = await _supabaseClient.From<Event>()
                .Select("id")
                .Filter("city_id", Constants.Operator.Equals, cityId.ToString())
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Get();

            var count = result.Models.Count;
            if (count == 0)
            {
                _logger.LogInformation("📝 未找到城市 {CityId} 的活动", cityId);
                return 0;
            }

            // 更新所有记录的冗余字段
            await _supabaseClient.From<Event>()
                .Filter("city_id", Constants.Operator.Equals, cityId.ToString())
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Set(x => x.CityName, name)
                .Set(x => x.CityNameEn, nameEn)
                .Set(x => x.CityCountry, country)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            _logger.LogInformation("✅ 已更新 {Count} 个活动的城市信息", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新城市信息失败: CityId={CityId}", cityId);
            throw;
        }
    }

    #endregion
}

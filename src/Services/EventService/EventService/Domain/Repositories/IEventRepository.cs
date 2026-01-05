using EventService.Domain.Entities;

namespace EventService.Domain.Repositories;

/// <summary>
///     Event 仓储接口
/// </summary>
public interface IEventRepository
{
    /// <summary>
    ///     创建 Event
    /// </summary>
    Task<Event> CreateAsync(Event @event);

    /// <summary>
    ///     根据 ID 获取 Event
    /// </summary>
    Task<Event?> GetByIdAsync(Guid id);

    /// <summary>
    ///     更新 Event
    /// </summary>
    Task<Event> UpdateAsync(Event @event);

    /// <summary>
    ///     删除 Event（逻辑删除）
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="deletedBy">删除者ID</param>
    Task DeleteAsync(Guid id, Guid? deletedBy = null);

    /// <summary>
    ///     获取 Event 列表（支持筛选和分页）
    /// </summary>
    Task<(List<Event> Events, int Total)> GetListAsync(
        Guid? cityId = null,
        string? category = null,
        string? status = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    ///     获取用户创建的 Event
    /// </summary>
    Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId);

    /// <summary>
    ///     检查 Event 是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    ///     获取已过期的活动（状态为 upcoming 且结束时间已过）
    /// </summary>
    Task<List<Event>> GetExpiredEventsAsync(DateTime currentTime);

    /// <summary>
    ///     获取需要检查状态更新的活动（状态为 upcoming 或 ongoing）
    /// </summary>
    Task<List<Event>> GetActiveEventsForStatusUpdateAsync();

    /// <summary>
    ///     根据ID列表批量获取活动（支持状态过滤和分页）
    /// </summary>
    Task<(List<Event> Events, int Total)> GetByIdsAsync(
        List<Guid> eventIds,
        string? status = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    ///     获取用户作为组织者创建的活动（支持状态过滤和分页）
    /// </summary>
    Task<(List<Event> Events, int Total)> GetByOrganizerAsync(
        Guid organizerId,
        string? status = null,
        int page = 1,
        int pageSize = 20);
}
using EventService.Domain.Entities;

namespace EventService.Domain.Repositories;

/// <summary>
/// Event 仓储接口
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// 创建 Event
    /// </summary>
    Task<Event> CreateAsync(Event @event);

    /// <summary>
    /// 根据 ID 获取 Event
    /// </summary>
    Task<Event?> GetByIdAsync(Guid id);

    /// <summary>
    /// 更新 Event
    /// </summary>
    Task<Event> UpdateAsync(Event @event);

    /// <summary>
    /// 删除 Event
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// 获取 Event 列表（支持筛选和分页）
    /// </summary>
    Task<(List<Event> Events, int Total)> GetListAsync(
        Guid? cityId = null,
        string? category = null,
        string? status = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 获取用户创建的 Event
    /// </summary>
    Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId);

    /// <summary>
    /// 检查 Event 是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}

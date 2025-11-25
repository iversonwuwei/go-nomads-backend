using EventService.Domain.Entities;

namespace EventService.Domain.Repositories;

/// <summary>
///     聚会类型仓储接口
/// </summary>
public interface IEventTypeRepository
{
    /// <summary>
    ///     获取所有启用的聚会类型
    /// </summary>
    Task<List<EventType>> GetAllActiveAsync();

    /// <summary>
    ///     获取所有聚会类型（包括禁用的）
    /// </summary>
    Task<List<EventType>> GetAllAsync();

    /// <summary>
    ///     根据 ID 获取聚会类型
    /// </summary>
    Task<EventType?> GetByIdAsync(Guid id);

    /// <summary>
    ///     根据英文名称获取聚会类型
    /// </summary>
    Task<EventType?> GetByEnNameAsync(string enName);

    /// <summary>
    ///     创建聚会类型
    /// </summary>
    Task<EventType> CreateAsync(EventType eventType);

    /// <summary>
    ///     更新聚会类型
    /// </summary>
    Task<EventType> UpdateAsync(EventType eventType);

    /// <summary>
    ///     删除聚会类型
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    ///     检查名称是否存在
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

    /// <summary>
    ///     检查英文名称是否存在
    /// </summary>
    Task<bool> ExistsByEnNameAsync(string enName, Guid? excludeId = null);
}

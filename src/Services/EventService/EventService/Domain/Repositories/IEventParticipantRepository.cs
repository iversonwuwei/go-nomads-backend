using EventService.Domain.Entities;

namespace EventService.Domain.Repositories;

/// <summary>
///     EventParticipant 仓储接口
/// </summary>
public interface IEventParticipantRepository
{
    /// <summary>
    ///     创建参与记录
    /// </summary>
    Task<EventParticipant> CreateAsync(EventParticipant participant);

    /// <summary>
    ///     更新参与记录
    /// </summary>
    Task<EventParticipant> UpdateAsync(EventParticipant participant);

    /// <summary>
    ///     获取参与记录
    /// </summary>
    Task<EventParticipant?> GetAsync(Guid eventId, Guid userId);

    /// <summary>
    ///     删除参与记录
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    ///     获取 Event 的所有参与者
    /// </summary>
    Task<List<EventParticipant>> GetByEventIdAsync(Guid eventId);

    /// <summary>
    ///     获取用户参与的所有 Event
    /// </summary>
    Task<List<EventParticipant>> GetByUserIdAsync(Guid userId);

    /// <summary>
    ///     检查用户是否已参加
    /// </summary>
    Task<bool> IsParticipantAsync(Guid eventId, Guid userId);

    /// <summary>
    ///     批量检查用户是否参与了多个活动（优化 N+1 查询）
    /// </summary>
    /// <param name="eventIds">活动ID列表</param>
    /// <param name="userId">用户ID</param>
    /// <returns>用户参与的活动ID集合</returns>
    Task<HashSet<Guid>> GetParticipatedEventIdsAsync(List<Guid> eventIds, Guid userId);

    /// <summary>
    ///     获取 Event 的参与者数量
    /// </summary>
    Task<int> CountByEventIdAsync(Guid eventId);
}
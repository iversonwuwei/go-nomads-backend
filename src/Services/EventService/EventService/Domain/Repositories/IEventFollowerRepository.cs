using EventService.Domain.Entities;

namespace EventService.Domain.Repositories;

/// <summary>
///     EventFollower 仓储接口
/// </summary>
public interface IEventFollowerRepository
{
    /// <summary>
    ///     创建关注记录
    /// </summary>
    Task<EventFollower> CreateAsync(EventFollower follower);

    /// <summary>
    ///     获取关注记录
    /// </summary>
    Task<EventFollower?> GetAsync(Guid eventId, Guid userId);

    /// <summary>
    ///     删除关注记录
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    ///     获取 Event 的所有关注者
    /// </summary>
    Task<List<EventFollower>> GetByEventIdAsync(Guid eventId);

    /// <summary>
    ///     获取用户关注的所有 Event
    /// </summary>
    Task<List<EventFollower>> GetByUserIdAsync(Guid userId);

    /// <summary>
    ///     检查用户是否已关注
    /// </summary>
    Task<bool> IsFollowingAsync(Guid eventId, Guid userId);

    /// <summary>
    ///     获取关注者数量
    /// </summary>
    Task<int> GetFollowerCountAsync(Guid eventId);
}
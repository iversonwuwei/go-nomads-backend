using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     UserStats 仓储接口 - 定义在领域层
/// </summary>
public interface IUserStatsRepository
{
    /// <summary>
    ///     创建用户统计数据
    /// </summary>
    Task<UserStats> CreateAsync(UserStats stats, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据用户ID获取统计数据
    /// </summary>
    Task<UserStats?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新用户统计数据
    /// </summary>
    Task<UserStats> UpdateAsync(UserStats stats, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取或创建用户统计数据（如果不存在则创建默认值）
    /// </summary>
    Task<UserStats> GetOrCreateAsync(string userId, CancellationToken cancellationToken = default);
}

using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     UserPreferences 仓储接口 - 定义在领域层
/// </summary>
public interface IUserPreferencesRepository
{
    /// <summary>
    ///     创建用户偏好设置
    /// </summary>
    Task<UserPreferences> CreateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据用户ID获取偏好设置
    /// </summary>
    Task<UserPreferences?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新用户偏好设置
    /// </summary>
    Task<UserPreferences> UpdateAsync(UserPreferences preferences, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取或创建用户偏好设置（如果不存在则创建默认值）
    /// </summary>
    Task<UserPreferences> GetOrCreateAsync(string userId, CancellationToken cancellationToken = default);
}

using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     版主申请仓储接口
/// </summary>
public interface IModeratorApplicationRepository
{
    /// <summary>
    ///     创建申请
    /// </summary>
    Task<ModeratorApplication> CreateAsync(ModeratorApplication application);

    /// <summary>
    ///     更新申请
    /// </summary>
    Task<ModeratorApplication> UpdateAsync(ModeratorApplication application);

    /// <summary>
    ///     根据ID获取申请
    /// </summary>
    Task<ModeratorApplication?> GetByIdAsync(Guid id);

    /// <summary>
    ///     获取用户的所有申请
    /// </summary>
    Task<List<ModeratorApplication>> GetByUserIdAsync(Guid userId);

    /// <summary>
    ///     获取城市的所有申请
    /// </summary>
    Task<List<ModeratorApplication>> GetByCityIdAsync(Guid cityId);

    /// <summary>
    ///     获取待处理的申请列表
    /// </summary>
    Task<List<ModeratorApplication>> GetPendingApplicationsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    ///     检查用户是否已申请过该城市的版主
    /// </summary>
    Task<bool> HasPendingApplicationAsync(Guid userId, Guid cityId);

    /// <summary>
    ///     获取申请统计
    /// </summary>
    Task<(int Total, int Pending, int Approved, int Rejected)> GetStatisticsAsync();
}

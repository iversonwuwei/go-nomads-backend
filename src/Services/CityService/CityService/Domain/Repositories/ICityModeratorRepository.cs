using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
/// 城市版主仓储接口
/// </summary>
public interface ICityModeratorRepository
{
    /// <summary>
    /// 获取城市的所有版主
    /// </summary>
    Task<List<CityModerator>> GetByCityIdAsync(Guid cityId, bool activeOnly = true);

    /// <summary>
    /// 获取用户管理的所有城市版主记录
    /// </summary>
    Task<List<CityModerator>> GetByUserIdAsync(Guid userId, bool activeOnly = true);

    /// <summary>
    /// 检查用户是否为城市版主
    /// </summary>
    Task<bool> IsModeratorAsync(Guid cityId, Guid userId);

    /// <summary>
    /// 添加版主
    /// </summary>
    Task<CityModerator> AddAsync(CityModerator moderator);

    /// <summary>
    /// 更新版主信息
    /// </summary>
    Task<bool> UpdateAsync(CityModerator moderator);

    /// <summary>
    /// 删除版主（软删除，设置为不激活）
    /// </summary>
    Task<bool> RemoveAsync(Guid cityId, Guid userId);

    /// <summary>
    /// 根据ID获取版主记录
    /// </summary>
    Task<CityModerator?> GetByIdAsync(Guid id);
}

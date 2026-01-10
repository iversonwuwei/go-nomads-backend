using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     版主转让仓储接口
/// </summary>
public interface IModeratorTransferRepository
{
    /// <summary>
    ///     创建转让请求
    /// </summary>
    Task<ModeratorTransfer> CreateAsync(ModeratorTransfer transfer);

    /// <summary>
    ///     更新转让请求
    /// </summary>
    Task<ModeratorTransfer> UpdateAsync(ModeratorTransfer transfer);

    /// <summary>
    ///     根据ID获取转让请求
    /// </summary>
    Task<ModeratorTransfer?> GetByIdAsync(Guid id);

    /// <summary>
    ///     获取用户发起的所有转让请求
    /// </summary>
    Task<List<ModeratorTransfer>> GetByFromUserIdAsync(Guid userId);

    /// <summary>
    ///     获取用户收到的所有转让请求
    /// </summary>
    Task<List<ModeratorTransfer>> GetByToUserIdAsync(Guid userId);

    /// <summary>
    ///     获取城市的所有转让请求
    /// </summary>
    Task<List<ModeratorTransfer>> GetByCityIdAsync(Guid cityId);

    /// <summary>
    ///     获取用户收到的待处理转让请求
    /// </summary>
    Task<List<ModeratorTransfer>> GetPendingTransfersForUserAsync(Guid userId);

    /// <summary>
    ///     检查是否存在待处理的转让请求（同一城市、同一接收者）
    /// </summary>
    Task<bool> HasPendingTransferAsync(Guid cityId, Guid toUserId);

    /// <summary>
    ///     使过期的转让请求失效
    /// </summary>
    Task<int> ExpirePendingTransfersAsync();
}

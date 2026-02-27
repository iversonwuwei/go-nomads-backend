using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     附近城市Repository接口
/// </summary>
public interface INearbyCityRepository
{
    /// <summary>
    ///     根据用户ID和源城市ID获取所有附近城市
    /// </summary>
    Task<List<NearbyCity>> GetByUserAndSourceCityIdAsync(string userId, string sourceCityId);

    /// <summary>
    ///     保存附近城市列表 (批量新增或更新)
    /// </summary>
    Task<List<NearbyCity>> SaveBatchAsync(string userId, string sourceCityId, List<NearbyCity> nearbyCities);

    /// <summary>
    ///     保存单个附近城市
    /// </summary>
    Task<NearbyCity> SaveAsync(NearbyCity nearbyCity);

    /// <summary>
    ///     删除用户的源城市的所有附近城市记录
    /// </summary>
    Task<bool> DeleteByUserAndSourceCityIdAsync(string userId, string sourceCityId);

    /// <summary>
    ///     检查用户的源城市是否有附近城市数据
    /// </summary>
    Task<bool> ExistsByUserAndSourceCityIdAsync(string userId, string sourceCityId);

    /// <summary>
    ///     获取用户的附近城市数量
    /// </summary>
    Task<int> GetCountByUserAndSourceCityIdAsync(string userId, string sourceCityId);
}

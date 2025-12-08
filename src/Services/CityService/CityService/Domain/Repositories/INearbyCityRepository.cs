using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     附近城市Repository接口
/// </summary>
public interface INearbyCityRepository
{
    /// <summary>
    ///     根据源城市ID获取所有附近城市
    /// </summary>
    Task<List<NearbyCity>> GetBySourceCityIdAsync(string sourceCityId);

    /// <summary>
    ///     保存附近城市列表 (批量新增或更新)
    /// </summary>
    Task<List<NearbyCity>> SaveBatchAsync(string sourceCityId, List<NearbyCity> nearbyCities);

    /// <summary>
    ///     保存单个附近城市
    /// </summary>
    Task<NearbyCity> SaveAsync(NearbyCity nearbyCity);

    /// <summary>
    ///     删除源城市的所有附近城市记录
    /// </summary>
    Task<bool> DeleteBySourceCityIdAsync(string sourceCityId);

    /// <summary>
    ///     检查源城市是否有附近城市数据
    /// </summary>
    Task<bool> ExistsBySourceCityIdAsync(string sourceCityId);

    /// <summary>
    ///     获取附近城市数量
    /// </summary>
    Task<int> GetCountBySourceCityIdAsync(string sourceCityId);
}

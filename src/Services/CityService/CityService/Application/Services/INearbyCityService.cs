using CityService.Domain.Entities;

namespace CityService.Application.Services;

/// <summary>
///     附近城市服务接口
/// </summary>
public interface INearbyCityService
{
    /// <summary>
    ///     根据源城市ID获取所有附近城市
    /// </summary>
    Task<List<NearbyCity>> GetBySourceCityIdAsync(string sourceCityId);

    /// <summary>
    ///     批量保存附近城市
    /// </summary>
    Task<List<NearbyCity>> SaveBatchAsync(string sourceCityId, List<NearbyCity> nearbyCities);

    /// <summary>
    ///     删除源城市的所有附近城市记录
    /// </summary>
    Task<bool> DeleteBySourceCityIdAsync(string sourceCityId);

    /// <summary>
    ///     检查源城市是否有附近城市数据
    /// </summary>
    Task<bool> ExistsBySourceCityIdAsync(string sourceCityId);
}

using CityService.Domain.Entities;

namespace CityService.Application.Services;

/// <summary>
///     附近城市服务接口
/// </summary>
public interface INearbyCityService
{
    /// <summary>
    ///     根据用户ID和源城市ID获取所有附近城市
    /// </summary>
    Task<List<NearbyCity>> GetByUserAndSourceCityIdAsync(string userId, string sourceCityId);

    /// <summary>
    ///     批量保存附近城市
    /// </summary>
    Task<List<NearbyCity>> SaveBatchAsync(string userId, string sourceCityId, List<NearbyCity> nearbyCities);

    /// <summary>
    ///     删除用户的源城市的所有附近城市记录
    /// </summary>
    Task<bool> DeleteByUserAndSourceCityIdAsync(string userId, string sourceCityId);

    /// <summary>
    ///     检查用户的源城市是否有附近城市数据
    /// </summary>
    Task<bool> ExistsByUserAndSourceCityIdAsync(string userId, string sourceCityId);
}

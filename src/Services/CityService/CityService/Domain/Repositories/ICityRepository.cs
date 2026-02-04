using CityService.Domain.Entities;
using CityService.Domain.ValueObjects;

namespace CityService.Domain.Repositories;

/// <summary>
///     城市仓储接口
/// </summary>
public interface ICityRepository
{
    Task<IEnumerable<City>> GetAllAsync(int pageNumber, int pageSize);
    Task<City?> GetByIdAsync(Guid id);
    Task<IEnumerable<City>> SearchAsync(CitySearchCriteria criteria);
    Task<City> CreateAsync(City city);
    Task<City?> UpdateAsync(Guid id, City city);
    
    /// <summary>
    ///     删除城市（逻辑删除）
    /// </summary>
    /// <param name="id">城市ID</param>
    /// <param name="deletedBy">删除操作执行者ID</param>
    Task<bool> DeleteAsync(Guid id, Guid? deletedBy = null);
    
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<City>> GetRecommendedAsync(int count);
    Task<IEnumerable<City>> GetPopularAsync(int limit);
    Task<IEnumerable<City>> GetByCountryAsync(string countryName);

    /// <summary>
    /// 根据国家 ID 获取城市列表（推荐使用，性能更好）
    /// </summary>
    Task<IEnumerable<City>> GetByCountryIdAsync(Guid countryId);

    Task<IEnumerable<City>> GetByIdsAsync(IEnumerable<Guid> cityIds);
    
    /// <summary>
    /// 直接更新城市图片字段（绕过 ORM）
    /// </summary>
    Task<bool> UpdateImagesDirectAsync(Guid cityId, string? imageUrl, string? portraitImageUrl, List<string>? landscapeImageUrls);

    /// <summary>
    /// 直接更新城市经纬度（绕过 ORM）
    /// </summary>
    Task<bool> UpdateCoordinatesDirectAsync(Guid cityId, double latitude, double longitude);

    /// <summary>
    /// 按名称搜索城市（支持模糊匹配）
    /// </summary>
    /// <param name="cityName">城市名称</param>
    /// <param name="countryName">国家名称（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的城市列表</returns>
    Task<IEnumerable<City>> SearchByNameAsync(
        string cityName,
        string? countryName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找最近的城市（基于经纬度）
    /// </summary>
    /// <param name="latitude">纬度</param>
    /// <param name="longitude">经度</param>
    /// <param name="maxDistanceKm">最大搜索距离（公里）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近的城市</returns>
    Task<City?> FindNearestCityAsync(
        double latitude,
        double longitude,
        double maxDistanceKm = 50,
        CancellationToken cancellationToken = default);
}
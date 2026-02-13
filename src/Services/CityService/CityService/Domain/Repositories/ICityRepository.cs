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
    /// 获取所有活跃城市的简要信息（仅 id, country_id, country）
    /// 用于内存中批量统计，避免多次数据库查询
    /// </summary>
    Task<IEnumerable<City>> GetAllActiveCityBriefAsync();

    /// <summary>
    /// 获取所有不同的区域（大洲）列表
    /// </summary>
    Task<IEnumerable<string>> GetDistinctRegionsAsync();

    /// <summary>
    /// 根据区域获取城市列表（分页）
    /// </summary>
    Task<IEnumerable<City>> GetByRegionAsync(string region, int pageNumber, int pageSize);

    /// <summary>
    /// 获取某区域的城市总数
    /// </summary>
    Task<int> GetCountByRegionAsync(string region);

    /// <summary>
    /// 根据多个国家ID获取城市列表（分页）
    /// </summary>
    Task<IEnumerable<City>> GetByCountryIdsAsync(IEnumerable<Guid> countryIds, int pageNumber, int pageSize);

    /// <summary>
    /// 根据多个国家ID获取城市总数
    /// </summary>
    Task<int> GetCountByCountryIdsAsync(IEnumerable<Guid> countryIds);

    /// <summary>
    /// 根据大洲筛选城市（同时支持 country_id 和 country name 匹配）
    /// </summary>
    Task<IEnumerable<City>> GetByContinentAsync(IEnumerable<Guid> countryIds, IEnumerable<string> countryNames, int pageNumber, int pageSize);

    /// <summary>
    /// 根据大洲统计城市总数（同时支持 country_id 和 country name 匹配）
    /// </summary>
    Task<int> GetCountByContinentAsync(IEnumerable<Guid> countryIds, IEnumerable<string> countryNames);

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
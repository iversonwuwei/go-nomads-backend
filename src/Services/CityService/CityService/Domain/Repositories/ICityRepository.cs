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
    Task<bool> DeleteAsync(Guid id);
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
}
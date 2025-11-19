using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
/// 城市评分项仓储接口
/// </summary>
public interface ICityRatingCategoryRepository
{
    Task<CityRatingCategory> CreateAsync(CityRatingCategory category);
    Task<CityRatingCategory?> GetByIdAsync(Guid id);
    Task<List<CityRatingCategory>> GetAllActiveAsync();
    Task<CityRatingCategory> UpdateAsync(CityRatingCategory category);
    Task DeleteAsync(Guid id);
}

/// <summary>
/// 用户城市评分仓储接口
/// </summary>
public interface ICityRatingRepository
{
    Task<CityRating> CreateAsync(CityRating rating);
    Task<CityRating?> GetByIdAsync(Guid id);
    Task<CityRating?> GetUserRatingAsync(Guid cityId, Guid userId, Guid categoryId);
    Task<List<CityRating>> GetCityRatingsAsync(Guid cityId);
    Task<List<CityRating>> GetUserRatingsAsync(Guid userId, Guid cityId);
    Task<Dictionary<Guid, double>> GetCityAverageRatingsAsync(Guid cityId);
    Task<CityRating> UpdateAsync(CityRating rating);
    Task DeleteAsync(Guid id);
}

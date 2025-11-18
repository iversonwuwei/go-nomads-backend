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
    Task<IEnumerable<City>> GetByCountryAsync(string countryName);
    Task<IEnumerable<City>> GetByIdsAsync(IEnumerable<Guid> cityIds);
}
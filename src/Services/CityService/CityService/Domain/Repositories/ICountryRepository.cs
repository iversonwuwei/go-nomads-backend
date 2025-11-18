using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     国家仓储接口
/// </summary>
public interface ICountryRepository
{
    Task<IEnumerable<Country>> GetAllCountriesAsync();
    Task<Country?> GetCountryByIdAsync(Guid id);
    Task<Country?> GetCountryByCodeAsync(string code);
    Task<Country> CreateCountryAsync(Country country);
    Task<Country> UpdateCountryAsync(Country country);
    Task<bool> DeleteCountryAsync(Guid id);
}
using CityService.Models;

namespace CityService.Repositories;

public interface ICountryRepository
{
    Task<IEnumerable<Country>> GetAllCountriesAsync();
    Task<Country?> GetCountryByIdAsync(Guid id);
    Task<Country?> GetCountryByCodeAsync(string code);
    Task<Country> CreateCountryAsync(Country country);
    Task<Country> UpdateCountryAsync(Country country);
    Task<bool> DeleteCountryAsync(Guid id);
}

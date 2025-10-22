using CityService.Models;
using CityService.DTOs;

namespace CityService.Repositories;

public interface ICityRepository
{
    Task<IEnumerable<City>> GetAllAsync(int pageNumber, int pageSize);
    Task<City?> GetByIdAsync(Guid id);
    Task<IEnumerable<City>> SearchAsync(CitySearchDto searchDto);
    Task<City> CreateAsync(City city);
    Task<City?> UpdateAsync(Guid id, City city);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<City>> GetRecommendedAsync(int count);
}

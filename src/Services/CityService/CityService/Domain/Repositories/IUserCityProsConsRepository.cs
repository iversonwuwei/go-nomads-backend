using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

public interface IUserCityProsConsRepository
{
    Task<CityProsCons> AddAsync(CityProsCons prosCons);
    Task<List<CityProsCons>> GetByCityIdAsync(string cityId, bool? isPro = null);
    Task<CityProsCons?> GetByIdAsync(Guid id);
    Task<CityProsCons> UpdateAsync(CityProsCons prosCons);
    Task<bool> DeleteAsync(Guid id, Guid userId);
}

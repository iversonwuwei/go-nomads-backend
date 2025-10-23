using CityService.Models;

namespace CityService.Repositories;

public interface IProvinceRepository
{
    Task<IEnumerable<Province>> GetAllProvincesAsync();
    Task<IEnumerable<Province>> GetProvincesByCountryIdAsync(Guid countryId);
    Task<Province?> GetProvinceByIdAsync(Guid id);
    Task<Province> CreateProvinceAsync(Province province);
    Task<Province> UpdateProvinceAsync(Province province);
    Task<bool> DeleteProvinceAsync(Guid id);
    Task<int> BulkCreateProvincesAsync(IEnumerable<Province> provinces);
}

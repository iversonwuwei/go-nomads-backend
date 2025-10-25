using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
/// 省份仓储接口
/// </summary>
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

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

    /// <summary>
    /// 获取所有不同的大洲列表
    /// </summary>
    /// <summary>
    /// 获取所有活跃国家（含 continent 等完整字段）
    /// </summary>
    Task<IEnumerable<Country>> GetAllActiveCountriesAsync();

    Task<IEnumerable<string>> GetDistinctContinentsAsync();

    /// <summary>
    /// 获取指定大洲下的所有国家ID
    /// </summary>
    Task<IEnumerable<Guid>> GetCountryIdsByContinentAsync(string continent);

    /// <summary>
    /// 获取指定大洲下的所有国家名称（英文）
    /// </summary>
    Task<IEnumerable<string>> GetCountryNamesByContinentAsync(string continent);

    /// <summary>
    /// 获取指定大洲下的城市总数（通过国家关联）
    /// </summary>
    Task<int> GetCityCountByContinentAsync(string continent, ICityRepository cityRepository);
}
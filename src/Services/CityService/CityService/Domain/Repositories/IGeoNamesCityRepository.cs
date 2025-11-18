using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     GeoNames 城市数据仓储接口
/// </summary>
public interface IGeoNamesCityRepository
{
    /// <summary>
    ///     添加或更新 GeoNames 城市
    /// </summary>
    Task<GeoNamesCity> UpsertAsync(GeoNamesCity city);

    /// <summary>
    ///     批量添加或更新 GeoNames 城市
    /// </summary>
    Task<IEnumerable<GeoNamesCity>> UpsertBatchAsync(IEnumerable<GeoNamesCity> cities);

    /// <summary>
    ///     根据 GeoNames ID 获取城市
    /// </summary>
    Task<GeoNamesCity?> GetByGeonameIdAsync(long geonameId);

    /// <summary>
    ///     根据城市名称和国家代码获取城市
    /// </summary>
    Task<GeoNamesCity?> GetByNameAndCountryAsync(string name, string countryCode);

    /// <summary>
    ///     获取指定国家的所有城市
    /// </summary>
    Task<IEnumerable<GeoNamesCity>> GetByCountryCodeAsync(string countryCode);

    /// <summary>
    ///     获取未同步到 cities 表的数据
    /// </summary>
    Task<IEnumerable<GeoNamesCity>> GetUnsyncedAsync(int limit = 100);

    /// <summary>
    ///     标记为已同步
    /// </summary>
    Task MarkAsSyncedAsync(Guid id, Guid cityId);

    /// <summary>
    ///     搜索城市
    /// </summary>
    Task<IEnumerable<GeoNamesCity>> SearchAsync(string? namePattern = null, string? countryCode = null,
        long? minPopulation = null);

    /// <summary>
    ///     获取总数
    /// </summary>
    Task<int> GetCountAsync(string? countryCode = null);

    /// <summary>
    ///     删除指定国家的所有数据
    /// </summary>
    Task<bool> DeleteByCountryCodeAsync(string countryCode);
}
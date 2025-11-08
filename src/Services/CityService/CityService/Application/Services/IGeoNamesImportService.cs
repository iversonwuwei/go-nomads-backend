using CityService.Application.DTOs;

namespace CityService.Application.Services;

/// <summary>
/// GeoNames 数据导入服务接口
/// </summary>
public interface IGeoNamesImportService
{
    /// <summary>
    /// 从 GeoNames 导入城市数据到 geonames_cities 表
    /// </summary>
    Task<GeoNamesImportResult> ImportCitiesAsync(GeoNamesImportOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索 GeoNames 城市 (预览) - 返回 DTO
    /// </summary>
    Task<List<DTOs.GeoNamesCity>> SearchCitiesAsync(string query, int maxRows = 10);

    /// <summary>
    /// 根据城市名和国家获取 GeoNames 信息 - 返回 DTO
    /// </summary>
    Task<DTOs.GeoNamesCity?> GetCityByNameAsync(string cityName, string countryCode);
}

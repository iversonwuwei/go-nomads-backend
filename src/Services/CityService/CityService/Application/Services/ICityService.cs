using CityService.Application.DTOs;

namespace CityService.Application.Services;

/// <summary>
///     城市应用服务接口
/// </summary>
public interface ICityService
{
    Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize, Guid? userId = null,
        string? userRole = null);

    /// <summary>
    /// 获取城市列表（轻量级版本，不包含天气数据）
    /// 用于城市列表页面，提升加载性能
    /// </summary>
    Task<IEnumerable<CityListItemDto>> GetCityListAsync(int pageNumber, int pageSize, string? search = null, Guid? userId = null, string? userRole = null);

    Task<CityDto?> GetCityByIdAsync(Guid id, Guid? userId = null, string? userRole = null);
    Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto, Guid? userId = null, string? userRole = null);
    Task<CityDto> CreateCityAsync(CreateCityDto createCityDto, Guid userId);
    Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId);
    
    /// <summary>
    ///     删除城市（逻辑删除）
    /// </summary>
    /// <param name="id">城市ID</param>
    /// <param name="deletedBy">删除操作执行者ID</param>
    Task<bool> DeleteCityAsync(Guid id, Guid? deletedBy = null);
    
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<CityDto>> GetRecommendedCitiesAsync(int count, Guid? userId = null);
    Task<IEnumerable<CityDto>> GetPopularCitiesAsync(int limit, Guid? userId = null);
    Task<CityStatisticsDto?> GetCityStatisticsAsync(Guid id);
    Task<IEnumerable<CountryCitiesDto>> GetCitiesGroupedByCountryAsync();
    Task<IEnumerable<CitySummaryDto>> GetCitiesByCountryIdAsync(Guid countryId);
    Task<IEnumerable<CountryDto>> GetAllCountriesAsync();
    Task<WeatherDto?> GetCityWeatherAsync(Guid id, bool includeForecast = false, int days = 7);
    Task<IEnumerable<CityDto>> GetCitiesByIdsAsync(IEnumerable<Guid> cityIds);

    /// <summary>
    ///     申请成为城市版主
    /// </summary>
    Task<bool> ApplyModeratorAsync(Guid userId, ApplyModeratorDto dto);

    /// <summary>
    ///     指定城市版主 (仅管理员)
    /// </summary>
    Task<bool> AssignModeratorAsync(AssignModeratorDto dto);

    /// <summary>
    ///     更新城市图片 URL（简单版本，只更新主图）
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="imageUrl">新的图片URL</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateCityImageAsync(Guid cityId, string imageUrl);

    /// <summary>
    ///     更新城市所有图片（竖屏 + 横屏）
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="portraitImageUrl">竖屏封面图 URL</param>
    /// <param name="landscapeImageUrls">横屏图片 URL 列表</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateCityImagesAsync(Guid cityId, string? portraitImageUrl, List<string>? landscapeImageUrls);
}
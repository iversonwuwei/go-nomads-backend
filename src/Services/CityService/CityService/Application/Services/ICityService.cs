using CityService.Application.DTOs;

namespace CityService.Application.Services;

/// <summary>
///     城市应用服务接口
/// </summary>
public interface ICityService
{
    Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize, Guid? userId = null,
        string? userRole = null);

    Task<CityDto?> GetCityByIdAsync(Guid id, Guid? userId = null, string? userRole = null);
    Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto, Guid? userId = null, string? userRole = null);
    Task<CityDto> CreateCityAsync(CreateCityDto createCityDto, Guid userId);
    Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId);
    Task<bool> DeleteCityAsync(Guid id);
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<CityDto>> GetRecommendedCitiesAsync(int count, Guid? userId = null);
    Task<CityStatisticsDto?> GetCityStatisticsAsync(Guid id);
    Task<IEnumerable<CountryCitiesDto>> GetCitiesGroupedByCountryAsync();
    Task<IEnumerable<CitySummaryDto>> GetCitiesByCountryIdAsync(Guid countryId);
    Task<IEnumerable<CountryDto>> GetAllCountriesAsync();
    Task<WeatherDto?> GetCityWeatherAsync(Guid id, bool includeForecast = false, int days = 7);

    /// <summary>
    ///     申请成为城市版主
    /// </summary>
    Task<bool> ApplyModeratorAsync(Guid userId, ApplyModeratorDto dto);

    /// <summary>
    ///     指定城市版主 (仅管理员)
    /// </summary>
    Task<bool> AssignModeratorAsync(AssignModeratorDto dto);
}
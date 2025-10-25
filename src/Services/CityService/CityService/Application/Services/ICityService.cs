using System;
using System.Collections.Generic;
using CityService.Application.DTOs;

namespace CityService.Application.Services;

/// <summary>
/// 城市应用服务接口
/// </summary>
public interface ICityService
{
    Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize);
    Task<CityDto?> GetCityByIdAsync(Guid id);
    Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto);
    Task<CityDto> CreateCityAsync(CreateCityDto createCityDto, Guid userId);
    Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId);
    Task<bool> DeleteCityAsync(Guid id);
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<CityDto>> GetRecommendedCitiesAsync(int count);
    Task<CityStatisticsDto?> GetCityStatisticsAsync(Guid id);
    Task<IEnumerable<CountryCitiesDto>> GetCitiesGroupedByCountryAsync();
    Task<IEnumerable<CitySummaryDto>> GetCitiesByCountryIdAsync(Guid countryId);
    Task<IEnumerable<CountryDto>> GetAllCountriesAsync();
}

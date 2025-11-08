using System;
using System.Collections.Generic;
using System.Linq;
using CityService.Application.Abstractions.Services;
using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CityService.Application.Services;

/// <summary>
/// 城市应用服务实现
/// </summary>
public class CityApplicationService : ICityService
{
    private readonly ICityRepository _cityRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly IWeatherService _weatherService;
    private readonly IUserFavoriteCityService _favoriteCityService;
    private readonly ILogger<CityApplicationService> _logger;

    public CityApplicationService(
        ICityRepository cityRepository,
        ICountryRepository countryRepository,
        IWeatherService weatherService,
        IUserFavoriteCityService favoriteCityService,
        ILogger<CityApplicationService> logger)
    {
        _cityRepository = cityRepository;
        _countryRepository = countryRepository;
        _weatherService = weatherService;
        _favoriteCityService = favoriteCityService;
        _logger = logger;
    }

    public async Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize, Guid? userId = null)
    {
        var cities = await _cityRepository.GetAllAsync(pageNumber, pageSize);
        var cityDtos = cities.Select(MapToDto).ToList();
        await EnrichCitiesWithWeatherAsync(cityDtos);
        
        // 填充收藏状态
        if (userId.HasValue)
        {
            await EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value);
        }
        
        return cityDtos;
    }

    public async Task<CityDto?> GetCityByIdAsync(Guid id, Guid? userId = null)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null) return null;
        
        var cityDto = MapToDto(city);
        
        // 填充收藏状态
        if (userId.HasValue)
        {
            cityDto.IsFavorite = await _favoriteCityService.IsCityFavoritedAsync(userId.Value, id.ToString());
        }
        
        return cityDto;
    }

    public async Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto, Guid? userId = null)
    {
        var criteria = new CitySearchCriteria
        {
            Name = searchDto.Name,
            Country = searchDto.Country,
            Region = searchDto.Region,
            MinCostOfLiving = searchDto.MinCostOfLiving,
            MaxCostOfLiving = searchDto.MaxCostOfLiving,
            MinScore = searchDto.MinScore,
            Tags = searchDto.Tags,
            PageNumber = searchDto.PageNumber,
            PageSize = searchDto.PageSize
        };

        var cities = await _cityRepository.SearchAsync(criteria);
        var cityDtos = cities.Select(MapToDto).ToList();
        await EnrichCitiesWithWeatherAsync(cityDtos);
        
        // 填充收藏状态
        if (userId.HasValue)
        {
            await EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value);
        }
        
        return cityDtos;
    }

    public async Task<CityDto> CreateCityAsync(CreateCityDto createCityDto, Guid userId)
    {
        var city = new City
        {
            Name = createCityDto.Name,
            Country = createCityDto.Country,
            Region = createCityDto.Region,
            Description = createCityDto.Description,
            Latitude = createCityDto.Latitude,
            Longitude = createCityDto.Longitude,
            Population = createCityDto.Population,
            Climate = createCityDto.Climate,
            TimeZone = createCityDto.TimeZone,
            Currency = createCityDto.Currency,
            ImageUrl = createCityDto.ImageUrl,
            AverageCostOfLiving = createCityDto.AverageCostOfLiving,
            Tags = createCityDto.Tags,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (createCityDto.Latitude.HasValue && createCityDto.Longitude.HasValue)
        {
            city.Location = $"POINT({createCityDto.Longitude.Value} {createCityDto.Latitude.Value})";
        }

        var createdCity = await _cityRepository.CreateAsync(city);
        _logger.LogInformation("City created: {CityId} - {CityName}", createdCity.Id, createdCity.Name);
        return MapToDto(createdCity);
    }

    public async Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId)
    {
        var existingCity = await _cityRepository.GetByIdAsync(id);
        if (existingCity == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(updateCityDto.Name)) existingCity.Name = updateCityDto.Name;
        if (!string.IsNullOrWhiteSpace(updateCityDto.Country)) existingCity.Country = updateCityDto.Country;
        if (updateCityDto.Region != null) existingCity.Region = updateCityDto.Region;
        if (updateCityDto.Description != null) existingCity.Description = updateCityDto.Description;
        if (updateCityDto.Latitude.HasValue) existingCity.Latitude = updateCityDto.Latitude;
        if (updateCityDto.Longitude.HasValue) existingCity.Longitude = updateCityDto.Longitude;

        if (updateCityDto.Latitude.HasValue && updateCityDto.Longitude.HasValue)
        {
            existingCity.Location = $"POINT({updateCityDto.Longitude.Value} {updateCityDto.Latitude.Value})";
        }

        if (updateCityDto.Population.HasValue) existingCity.Population = updateCityDto.Population;
        if (updateCityDto.Climate != null) existingCity.Climate = updateCityDto.Climate;
        if (updateCityDto.TimeZone != null) existingCity.TimeZone = updateCityDto.TimeZone;
        if (updateCityDto.Currency != null) existingCity.Currency = updateCityDto.Currency;
        if (updateCityDto.ImageUrl != null) existingCity.ImageUrl = updateCityDto.ImageUrl;
        if (updateCityDto.AverageCostOfLiving.HasValue) existingCity.AverageCostOfLiving = updateCityDto.AverageCostOfLiving;
        if (updateCityDto.Tags != null) existingCity.Tags = updateCityDto.Tags;
        if (updateCityDto.IsActive.HasValue) existingCity.IsActive = updateCityDto.IsActive.Value;

        existingCity.UpdatedById = userId;
        existingCity.UpdatedAt = DateTime.UtcNow;

        var updatedCity = await _cityRepository.UpdateAsync(id, existingCity);
        if (updatedCity == null)
        {
            return null;
        }

        _logger.LogInformation("City updated: {CityId} - {CityName}", id, existingCity.Name);
        return MapToDto(updatedCity);
    }

    public async Task<bool> DeleteCityAsync(Guid id)
    {
        var result = await _cityRepository.DeleteAsync(id);
        if (result)
        {
            _logger.LogInformation("City deleted: {CityId}", id);
        }

        return result;
    }

    public Task<int> GetTotalCountAsync()
    {
        return _cityRepository.GetTotalCountAsync();
    }

    public async Task<IEnumerable<CityDto>> GetRecommendedCitiesAsync(int count, Guid? userId = null)
    {
        var cities = await _cityRepository.GetRecommendedAsync(count);
        var cityDtos = cities.Select(MapToDto).ToList();
        
        // 填充收藏状态
        if (userId.HasValue)
        {
            await EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value);
        }
        
        return cityDtos;
    }

    public async Task<CityStatisticsDto?> GetCityStatisticsAsync(Guid id)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null)
        {
            return null;
        }

        return new CityStatisticsDto
        {
            CityId = city.Id,
            CityName = city.Name,
            TotalCoworkingSpaces = 0,
            TotalAccommodations = 0,
            TotalEvents = 0,
            TotalNomads = 0,
            AverageRating = city.OverallScore ?? 0
        };
    }

    public async Task<IEnumerable<CountryCitiesDto>> GetCitiesGroupedByCountryAsync()
    {
        try
        {
            var countries = await _countryRepository.GetAllCountriesAsync();
            var result = new List<CountryCitiesDto>();

            foreach (var country in countries)
            {
                var cities = await _cityRepository.GetByCountryAsync(country.Name);

                var countryDto = new CountryCitiesDto
                {
                    Country = country.Name,
                    Cities = cities.Select(city => new CitySummaryDto
                    {
                        Id = city.Id,
                        Name = city.Name,
                        Region = city.Region
                    }).ToList()
                };

                if (countryDto.Cities.Any())
                {
                    result.Add(countryDto);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities grouped by country");
            throw;
        }
    }

    public async Task<IEnumerable<CitySummaryDto>> GetCitiesByCountryIdAsync(Guid countryId)
    {
        try
        {
            var country = await _countryRepository.GetCountryByIdAsync(countryId);
            if (country == null)
            {
                return Enumerable.Empty<CitySummaryDto>();
            }

            var cities = await _cityRepository.GetByCountryAsync(country.Name);

            return cities.Select(city => new CitySummaryDto
            {
                Id = city.Id,
                Name = city.Name,
                Region = city.Region
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities by country ID {CountryId}", countryId);
            throw;
        }
    }

    public async Task<IEnumerable<CountryDto>> GetAllCountriesAsync()
    {
        try
        {
            var countries = await _countryRepository.GetAllCountriesAsync();
            return countries.Select(MapToCountryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all countries");
            throw;
        }
    }

    public async Task<WeatherDto?> GetCityWeatherAsync(Guid id, bool includeForecast = false, int days = 7)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null)
        {
            return null;
        }

        try
        {
            // 免费 API 最多支持 5 天预报
            var normalizedDays = Math.Clamp(days, 1, 5);
            if (city.Latitude.HasValue && city.Longitude.HasValue)
            {
                var weather = await _weatherService.GetWeatherByCoordinatesAsync(
                    city.Latitude.Value,
                    city.Longitude.Value);

                if (weather != null && includeForecast)
                {
                    weather.Forecast = await _weatherService.GetDailyForecastAsync(
                        city.Latitude.Value,
                        city.Longitude.Value,
                        normalizedDays);
                }

                return weather;
            }

            // 优先使用英文名称获取天气,如果没有英文名则使用中文名
            var cityName = !string.IsNullOrWhiteSpace(city.NameEn) ? city.NameEn : city.Name;
            var cityWeather = await _weatherService.GetWeatherByCityNameAsync(cityName);

            if (cityWeather != null && includeForecast)
            {
                if (cityWeather.Latitude.HasValue && cityWeather.Longitude.HasValue)
                {
                    cityWeather.Forecast = await _weatherService.GetDailyForecastAsync(
                        cityWeather.Latitude.Value,
                        cityWeather.Longitude.Value,
                        normalizedDays);
                }
                else
                {
                    cityWeather.Forecast = await _weatherService.GetDailyForecastByCityNameAsync(
                        cityName,
                        normalizedDays);
                }
            }

            return cityWeather;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取城市天气失败: {CityName}", city.Name);
            return null;
        }
    }

    private static CityDto MapToDto(City city)
    {
        return new CityDto
        {
            Id = city.Id,
            Name = city.Name,
            NameEn = city.NameEn,
            Country = city.Country,
            Region = city.Region,
            Description = city.Description,
            Latitude = city.Latitude,
            Longitude = city.Longitude,
            Population = city.Population,
            Climate = city.Climate,
            TimeZone = city.TimeZone,
            Currency = city.Currency,
            ImageUrl = city.ImageUrl,
            AverageCostOfLiving = city.AverageCostOfLiving,
            OverallScore = city.OverallScore,
            InternetQualityScore = city.InternetQualityScore,
            SafetyScore = city.SafetyScore,
            CostScore = city.CostScore,
            CommunityScore = city.CommunityScore,
            WeatherScore = city.WeatherScore,
            Tags = city.Tags,
            IsActive = city.IsActive,
            CreatedAt = city.CreatedAt,
            UpdatedAt = city.UpdatedAt
        };
    }

    private static CountryDto MapToCountryDto(Country country)
    {
        return new CountryDto
        {
            Id = country.Id,
            Name = country.Name,
            NameZh = country.NameZh,
            Code = country.Code,
            CodeAlpha3 = country.CodeAlpha3,
            Continent = country.Continent,
            FlagUrl = country.FlagUrl,
            CallingCode = country.CallingCode,
            IsActive = country.IsActive
        };
    }

    private async Task EnrichCitiesWithWeatherAsync(List<CityDto> cities)
    {
        try
        {
            var weatherTasks = cities.Select(async city =>
            {
                try
                {
                    if (city.Latitude.HasValue && city.Longitude.HasValue)
                    {
                        city.Weather = await _weatherService.GetWeatherByCoordinatesAsync(city.Latitude.Value, city.Longitude.Value);
                    }
                    else
                    {
                        // 优先使用英文名称获取天气,如果没有英文名则使用中文名
                        var cityName = !string.IsNullOrWhiteSpace(city.NameEn) ? city.NameEn : city.Name;
                        city.Weather = await _weatherService.GetWeatherByCityNameAsync(cityName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取城市天气失败: {CityName}", city.Name);
                    city.Weather = null;
                }
            });

            await Task.WhenAll(weatherTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取天气数据失败");
        }
    }

    /// <summary>
    /// 批量填充城市的收藏状态
    /// </summary>
    private async Task EnrichCitiesWithFavoriteStatusAsync(List<CityDto> cities, Guid userId)
    {
        try
        {
            // 获取用户收藏的所有城市ID列表
            var favoriteCityIds = await _favoriteCityService.GetUserFavoriteCityIdsAsync(userId);
            var favoriteSet = new HashSet<string>(favoriteCityIds);
            
            // 填充每个城市的收藏状态
            foreach (var city in cities)
            {
                city.IsFavorite = favoriteSet.Contains(city.Id.ToString());
            }
            
            _logger.LogDebug("已为 {Count} 个城市填充收藏状态 (用户: {UserId})", cities.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "填充城市收藏状态失败 (用户: {UserId})", userId);
            // 失败时默认所有城市都未收藏
            foreach (var city in cities)
            {
                city.IsFavorite = false;
            }
        }
    }
}

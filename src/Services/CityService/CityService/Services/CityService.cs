using CityService.DTOs;
using CityService.Models;
using CityService.Repositories;

namespace CityService.Services;

public class CityService : ICityService
{
    private readonly ICityRepository _repository;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<CityService> _logger;

    public CityService(
        ICityRepository repository,
        IWeatherService weatherService,
        ILogger<CityService> logger)
    {
        _repository = repository;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize)
    {
        var cities = await _repository.GetAllAsync(pageNumber, pageSize);
        var cityDtos = cities.Select(MapToDto).ToList();

        // 并行获取天气数据
        await EnrichCitiesWithWeatherAsync(cityDtos);

        return cityDtos;
    }

    public async Task<CityDto?> GetCityByIdAsync(Guid id)
    {
        var city = await _repository.GetByIdAsync(id);
        return city == null ? null : MapToDto(city);
    }

    public async Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto)
    {
        var cities = await _repository.SearchAsync(searchDto);
        return cities.Select(MapToDto);
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
            CreatedAt = DateTime.UtcNow
        };

        // Create PostGIS Point string if coordinates are provided
        if (createCityDto.Latitude.HasValue && createCityDto.Longitude.HasValue)
        {
            city.Location = $"POINT({createCityDto.Longitude.Value} {createCityDto.Latitude.Value})";
        }

        var createdCity = await _repository.CreateAsync(city);
        _logger.LogInformation("City created: {CityId} - {CityName}", createdCity.Id, createdCity.Name);
        return MapToDto(createdCity);
    }

    public async Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId)
    {
        var existingCity = await _repository.GetByIdAsync(id);
        if (existingCity == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(updateCityDto.Name))
            existingCity.Name = updateCityDto.Name;

        if (!string.IsNullOrWhiteSpace(updateCityDto.Country))
            existingCity.Country = updateCityDto.Country;

        if (updateCityDto.Region != null)
            existingCity.Region = updateCityDto.Region;

        if (updateCityDto.Description != null)
            existingCity.Description = updateCityDto.Description;

        if (updateCityDto.Latitude.HasValue)
            existingCity.Latitude = updateCityDto.Latitude;

        if (updateCityDto.Longitude.HasValue)
            existingCity.Longitude = updateCityDto.Longitude;

        if (updateCityDto.Latitude.HasValue && updateCityDto.Longitude.HasValue)
        {
            existingCity.Location = $"POINT({updateCityDto.Longitude.Value} {updateCityDto.Latitude.Value})";
        }

        if (updateCityDto.Population.HasValue)
            existingCity.Population = updateCityDto.Population;

        if (updateCityDto.Climate != null)
            existingCity.Climate = updateCityDto.Climate;

        if (updateCityDto.TimeZone != null)
            existingCity.TimeZone = updateCityDto.TimeZone;

        if (updateCityDto.Currency != null)
            existingCity.Currency = updateCityDto.Currency;

        if (updateCityDto.ImageUrl != null)
            existingCity.ImageUrl = updateCityDto.ImageUrl;

        if (updateCityDto.AverageCostOfLiving.HasValue)
            existingCity.AverageCostOfLiving = updateCityDto.AverageCostOfLiving;

        if (updateCityDto.Tags != null)
            existingCity.Tags = updateCityDto.Tags;

        if (updateCityDto.IsActive.HasValue)
            existingCity.IsActive = updateCityDto.IsActive.Value;

        existingCity.UpdatedById = userId;
        existingCity.UpdatedAt = DateTime.UtcNow;

        var updatedCity = await _repository.UpdateAsync(id, existingCity);
        _logger.LogInformation("City updated: {CityId} - {CityName}", id, existingCity.Name);
        return updatedCity == null ? null : MapToDto(updatedCity);
    }

    public async Task<bool> DeleteCityAsync(Guid id)
    {
        var result = await _repository.DeleteAsync(id);
        if (result)
        {
            _logger.LogInformation("City deleted: {CityId}", id);
        }
        return result;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _repository.GetTotalCountAsync();
    }

    public async Task<IEnumerable<CityDto>> GetRecommendedCitiesAsync(int count)
    {
        var cities = await _repository.GetRecommendedAsync(count);
        return cities.Select(MapToDto);
    }

    public async Task<CityStatisticsDto?> GetCityStatisticsAsync(Guid id)
    {
        var city = await _repository.GetByIdAsync(id);
        if (city == null)
        {
            return null;
        }

        // TODO: Implement cross-service calls to get statistics
        // For now, return mock data
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

    private static CityDto MapToDto(City city)
    {
        return new CityDto
        {
            Id = city.Id,
            Name = city.Name,
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

    /// <summary>
    /// 为城市列表添加天气信息
    /// </summary>
    private async Task EnrichCitiesWithWeatherAsync(List<CityDto> cities)
    {
        try
        {
            // 并行获取所有城市的天气数据
            var weatherTasks = cities.Select(async city =>
            {
                try
                {
                    // 优先使用经纬度获取天气（更精确）
                    if (city.Latitude.HasValue && city.Longitude.HasValue)
                    {
                        city.Weather = await _weatherService.GetWeatherByCoordinatesAsync(
                            city.Latitude.Value,
                            city.Longitude.Value);
                    }
                    else
                    {
                        // 否则使用城市名称
                        city.Weather = await _weatherService.GetWeatherByCityNameAsync(city.Name);
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
}

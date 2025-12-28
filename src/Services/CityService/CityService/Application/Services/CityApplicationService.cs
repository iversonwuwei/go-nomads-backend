using System.Diagnostics;
using CityService.Application.Abstractions.Services;
using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Domain.ValueObjects;
using Dapr.Client;
using GoNomads.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CityService.Application.Services;

/// <summary>
///     城市应用服务实现
/// </summary>
public class CityApplicationService : ICityService
{
    private readonly IMemoryCache _cache;
    private readonly ICityRepository _cityRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly DaprClient _daprClient;
    private readonly IUserFavoriteCityService _favoriteCityService;
    private readonly ILogger<CityApplicationService> _logger;
    private readonly ICityModeratorRepository _moderatorRepository;
    private readonly IWeatherService _weatherService;
    private readonly IConfiguration _configuration;

    public CityApplicationService(
        ICityRepository cityRepository,
        ICountryRepository countryRepository,
        IWeatherService weatherService,
        IUserFavoriteCityService favoriteCityService,
        ICityModeratorRepository moderatorRepository,
        DaprClient daprClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<CityApplicationService> logger)
    {
        _cityRepository = cityRepository;
        _countryRepository = countryRepository;
        _weatherService = weatherService;
        _favoriteCityService = favoriteCityService;
        _moderatorRepository = moderatorRepository;
        _daprClient = daprClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize, Guid? userId = null,
        string? userRole = null)
    {
        var cities = await _cityRepository.GetAllAsync(pageNumber, pageSize);
        var cityDtos = cities.Select(MapToDto).ToList();

        // 并行填充数据
        var weatherTask = EnrichCitiesWithWeatherAsync(cityDtos);
        var moderatorTask = EnrichCitiesWithModeratorInfoAsync(cityDtos);
        var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(cityDtos);
        var favoriteTask = userId.HasValue
            ? EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value)
            : Task.CompletedTask;

        // 等待所有任务完成（即使某些任务失败，其他任务也会继续执行）
        var allTasks = new[] { weatherTask, moderatorTask, ratingsAndCostsTask, favoriteTask };
        await Task.WhenAll(allTasks.Select(t => t.ContinueWith(_ => { })));

        // 设置用户上下文
        foreach (var cityDto in cityDtos) cityDto.SetUserContext(userId, userRole);

        // 数据库已按 OverallScore 降序排序，无需再次排序
        return cityDtos;
    }

    public async Task<CityDto?> GetCityByIdAsync(Guid id, Guid? userId = null, string? userRole = null)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null) return null;

        // 调试日志 - 打印图片字段
        _logger.LogInformation(
            "🖼️ [GetCityById] 图片字段调试: CityId={CityId}, Name={CityName}, ImageUrl={ImageUrl}, PortraitImageUrl={PortraitImageUrl}, LandscapeImageUrls={LandscapeImageUrls}, LandscapeCount={Count}",
            id, city.Name, city.ImageUrl, city.PortraitImageUrl, 
            city.LandscapeImageUrls != null ? string.Join(", ", city.LandscapeImageUrls) : "null",
            city.LandscapeImageUrls?.Count ?? 0);

        var cityDto = MapToDto(city);

        // 并行填充数据
        var favoriteTask = userId.HasValue
            ? _favoriteCityService.IsCityFavoritedAsync(userId.Value, id.ToString())
            : Task.FromResult(false);
        var moderatorTask = EnrichCityWithModeratorInfoAsync(cityDto);
        var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(new List<CityDto> { cityDto });

        await Task.WhenAll(favoriteTask, moderatorTask, ratingsAndCostsTask);

        if (userId.HasValue) cityDto.IsFavorite = await favoriteTask;

        // 调试日志（Debug 级别）
        _logger.LogDebug(
            "🔍 [GetCityById] CityId: {CityId}, CurrentUserId: {UserId}, UserRole: {UserRole}, ModeratorId: {ModeratorId}",
            id, userId, userRole, cityDto.ModeratorId);

        // 设置用户上下文（包括是否为管理员和是否为该城市版主）
        cityDto.SetUserContext(userId, userRole);

        _logger.LogDebug("✅ [GetCityById] IsCurrentUserAdmin: {IsAdmin}, IsCurrentUserModerator: {IsModerator}",
            cityDto.IsCurrentUserAdmin, cityDto.IsCurrentUserModerator);

        return cityDto;
    }

    public async Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto, Guid? userId = null,
        string? userRole = null)
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

        // 并行填充数据
        var weatherTask = EnrichCitiesWithWeatherAsync(cityDtos);
        var moderatorTask = EnrichCitiesWithModeratorInfoAsync(cityDtos);
        var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(cityDtos);
        var favoriteTask = userId.HasValue
            ? EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value)
            : Task.CompletedTask;

        // 等待所有任务完成（即使某些任务失败，其他任务也会继续执行）
        var allTasks = new[] { weatherTask, moderatorTask, ratingsAndCostsTask, favoriteTask };
        await Task.WhenAll(allTasks.Select(t => t.ContinueWith(_ => { })));

        // 设置用户上下文
        foreach (var cityDto in cityDtos) cityDto.SetUserContext(userId, userRole);

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
            city.Location = $"POINT({createCityDto.Longitude.Value} {createCityDto.Latitude.Value})";

        var createdCity = await _cityRepository.CreateAsync(city);
        _logger.LogInformation("City created: {CityId} - {CityName}", createdCity.Id, createdCity.Name);
        return MapToDto(createdCity);
    }

    public async Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId)
    {
        var existingCity = await _cityRepository.GetByIdAsync(id);
        if (existingCity == null) return null;

        if (!string.IsNullOrWhiteSpace(updateCityDto.Name)) existingCity.Name = updateCityDto.Name;
        if (!string.IsNullOrWhiteSpace(updateCityDto.Country)) existingCity.Country = updateCityDto.Country;
        if (updateCityDto.Region != null) existingCity.Region = updateCityDto.Region;
        if (updateCityDto.Description != null) existingCity.Description = updateCityDto.Description;
        if (updateCityDto.Latitude.HasValue) existingCity.Latitude = updateCityDto.Latitude;
        if (updateCityDto.Longitude.HasValue) existingCity.Longitude = updateCityDto.Longitude;

        if (updateCityDto.Latitude.HasValue && updateCityDto.Longitude.HasValue)
            existingCity.Location = $"POINT({updateCityDto.Longitude.Value} {updateCityDto.Latitude.Value})";

        if (updateCityDto.Population.HasValue) existingCity.Population = updateCityDto.Population;
        if (updateCityDto.Climate != null) existingCity.Climate = updateCityDto.Climate;
        if (updateCityDto.TimeZone != null) existingCity.TimeZone = updateCityDto.TimeZone;
        if (updateCityDto.Currency != null) existingCity.Currency = updateCityDto.Currency;
        if (updateCityDto.ImageUrl != null) existingCity.ImageUrl = updateCityDto.ImageUrl;
        if (updateCityDto.AverageCostOfLiving.HasValue)
            existingCity.AverageCostOfLiving = updateCityDto.AverageCostOfLiving;
        if (updateCityDto.Tags != null) existingCity.Tags = updateCityDto.Tags;
        if (updateCityDto.IsActive.HasValue) existingCity.IsActive = updateCityDto.IsActive.Value;

        existingCity.UpdatedById = userId;
        existingCity.UpdatedAt = DateTime.UtcNow;

        var updatedCity = await _cityRepository.UpdateAsync(id, existingCity);
        if (updatedCity == null) return null;

        _logger.LogInformation("City updated: {CityId} - {CityName}", id, existingCity.Name);
        return MapToDto(updatedCity);
    }

    public async Task<bool> DeleteCityAsync(Guid id)
    {
        var result = await _cityRepository.DeleteAsync(id);
        if (result) _logger.LogInformation("City deleted: {CityId}", id);

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
        if (userId.HasValue) await EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value);

        return cityDtos;
    }

    public async Task<CityStatisticsDto?> GetCityStatisticsAsync(Guid id)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null) return null;

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

                if (countryDto.Cities.Any()) result.Add(countryDto);
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
            // 直接使用 country_id 查询，只需一次数据库查询，性能更好
            var cities = await _cityRepository.GetByCountryIdAsync(countryId);

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

    public async Task<IEnumerable<CityDto>> GetCitiesByIdsAsync(IEnumerable<Guid> cityIds)
    {
        var normalized = cityIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalized == null || normalized.Count == 0)
        {
            _logger.LogWarning("[CityBatch] 请求的城市ID列表为空或无效");
            return Enumerable.Empty<CityDto>();
        }

        var cities = await _cityRepository.GetByIdsAsync(normalized);
        var cityDtos = cities.Select(MapToDto).ToList();

        // 填充天气信息（静默失败，不影响主流程）
        try
        {
            await EnrichCitiesWithWeatherAsync(cityDtos);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CityBatch] 填充天气信息失败，继续返回城市数据");
        }

        return cityDtos;
    }

    public async Task<WeatherDto?> GetCityWeatherAsync(Guid id, bool includeForecast = false, int days = 7)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null) return null;

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
                    weather.Forecast = await _weatherService.GetDailyForecastAsync(
                        city.Latitude.Value,
                        city.Longitude.Value,
                        normalizedDays);

                return weather;
            }

            // 优先使用英文名称获取天气,如果没有英文名则使用中文名
            var cityName = !string.IsNullOrWhiteSpace(city.NameEn) ? city.NameEn : city.Name;
            var cityWeather = await _weatherService.GetWeatherByCityNameAsync(cityName);

            if (cityWeather != null && includeForecast)
            {
                if (cityWeather.Latitude.HasValue && cityWeather.Longitude.HasValue)
                    cityWeather.Forecast = await _weatherService.GetDailyForecastAsync(
                        cityWeather.Latitude.Value,
                        cityWeather.Longitude.Value,
                        normalizedDays);
                else
                    cityWeather.Forecast = await _weatherService.GetDailyForecastByCityNameAsync(
                        cityName,
                        normalizedDays);
            }

            // 如果城市没有经纬度但天气API返回了经纬度，则更新城市的经纬度
            if (!city.Latitude.HasValue && !city.Longitude.HasValue &&
                cityWeather?.Latitude.HasValue == true && cityWeather?.Longitude.HasValue == true)
            {
                try
                {
                    // 使用直接 HTTP API 更新，绕过 ORM
                    var success = await _cityRepository.UpdateCoordinatesDirectAsync(
                        city.Id,
                        cityWeather.Latitude.Value,
                        cityWeather.Longitude.Value);

                    if (success)
                    {
                        _logger.LogInformation(
                            "已从天气API更新城市经纬度: CityId={CityId}, CityName={CityName}, Lat={Latitude}, Lng={Longitude}",
                            city.Id, city.Name, cityWeather.Latitude.Value, cityWeather.Longitude.Value);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "更新城市经纬度返回失败: CityId={CityId}, CityName={CityName}",
                            city.Id, city.Name);
                    }
                }
                catch (Exception updateEx)
                {
                    // 更新经纬度失败不影响返回天气数据
                    _logger.LogWarning(updateEx,
                        "更新城市经纬度失败: CityId={CityId}, CityName={CityName}",
                        city.Id, city.Name);
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

    /// <summary>
    ///     申请成为城市版主 (普通用户)
    /// </summary>
    public async Task<bool> ApplyModeratorAsync(Guid userId, ApplyModeratorDto dto)
    {
        try
        {
            var city = await _cityRepository.GetByIdAsync(dto.CityId);
            if (city == null)
            {
                _logger.LogWarning("城市不存在: {CityId}", dto.CityId);
                return false;
            }

            if (city.ModeratorId.HasValue)
            {
                _logger.LogWarning("城市已有版主: {CityId}, ModeratorId: {ModeratorId}", dto.CityId, city.ModeratorId);
                return false;
            }

            // TODO: 这里可以添加申请记录到数据库，等待管理员审核
            // 目前简化流程：直接设置为版主
            city.ModeratorId = userId;
            city.UpdatedAt = DateTime.UtcNow;
            city.UpdatedById = userId;

            await _cityRepository.UpdateAsync(city.Id, city);

            _logger.LogInformation("用户 {UserId} 申请成为城市 {CityId} 的版主成功", userId, dto.CityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "申请城市版主失败: UserId={UserId}, CityId={CityId}", userId, dto.CityId);
            throw;
        }
    }

    /// <summary>
    ///     指定城市版主 (仅管理员)
    /// </summary>
    public async Task<bool> AssignModeratorAsync(AssignModeratorDto dto)
    {
        try
        {
            var city = await _cityRepository.GetByIdAsync(dto.CityId);
            if (city == null)
            {
                _logger.LogWarning("城市不存在: {CityId}", dto.CityId);
                return false;
            }

            // TODO: 验证目标用户是否存在且角色为 moderator
            // 这里需要调用 UserService 验证

            // 使用新的多版主系统：在 city_moderators 表中创建关联
            // 先检查是否已经是版主
            var existingModerators = await _moderatorRepository.GetByCityIdAsync(dto.CityId, activeOnly: false);
            var existingModerator = existingModerators.FirstOrDefault(m => m.UserId == dto.UserId);
            
            if (existingModerator != null)
            {
                // 如果已存在但是被停用，重新激活
                if (!existingModerator.IsActive)
                {
                    existingModerator.IsActive = true;
                    existingModerator.AssignedAt = DateTime.UtcNow;
                    await _moderatorRepository.UpdateAsync(existingModerator);
                    _logger.LogInformation("重新激活版主 - CityId: {CityId}, UserId: {UserId}", dto.CityId, dto.UserId);
                }
                else
                {
                    _logger.LogInformation("用户已经是该城市的版主 - CityId: {CityId}, UserId: {UserId}", dto.CityId, dto.UserId);
                }
                return true;
            }

            // 创建新的版主关联
            var cityModerator = new CityModerator
            {
                Id = Guid.NewGuid(),
                CityId = dto.CityId,
                UserId = dto.UserId,
                IsActive = true,
                CanEditCity = true,
                CanManageCoworks = true,
                CanManageCosts = true,
                CanManageVisas = true,
                CanModerateChats = true,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _moderatorRepository.AddAsync(cityModerator);

            _logger.LogInformation("城市 {CityId} 的版主已设置为 {UserId}", dto.CityId, dto.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "指定城市版主失败: CityId={CityId}, UserId={UserId}", dto.CityId, dto.UserId);
            throw;
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
            CountryId = city.CountryId,
            Region = city.Region,
            Description = city.Description,
            Latitude = city.Latitude,
            Longitude = city.Longitude,
            Population = city.Population,
            Climate = city.Climate,
            TimeZone = city.TimeZone,
            Currency = city.Currency,
            ImageUrl = city.ImageUrl,
            PortraitImageUrl = city.PortraitImageUrl,
            LandscapeImageUrls = city.LandscapeImageUrls,
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
            UpdatedAt = city.UpdatedAt,
            ModeratorId = city.ModeratorId
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

    /// <summary>
    /// 批量填充城市的评分数量和平均花费
    /// </summary>
    private async Task EnrichCitiesWithRatingsAndCostsAsync(List<CityDto> cities)
    {
        if (cities.Count == 0) return;

        _logger.LogInformation("🔧 开始批量填充评分和花费信息: {Count} 个城市", cities.Count);

        try
        {
            var cityIds = cities.Select(c => c.Id).ToList();

            // 🆕 通过 CacheService 批量获取城市总评分
            var overallScores = await GetCityScoresFromCacheServiceAsync(cityIds);

            // 🆕 通过 CacheService 批量获取城市平均费用
            var averageCosts = await GetCityCostsFromCacheServiceAsync(cityIds);

            // 填充数据（仅当 CacheService 返回有效值时更新，保留数据库原有排序）
            foreach (var city in cities)
            {
                // 只有当 CacheService 返回了有效评分时才更新，否则保留数据库原值
                if (overallScores.TryGetValue(city.Id, out var score) && score > 0)
                {
                    city.OverallScore = score;
                }
                // AverageCost 可以直接更新
                city.AverageCost = averageCosts.GetValueOrDefault(city.Id);

                _logger.LogDebug("📊 城市 {CityName}({CityId}): OverallScore={OverallScore}, AverageCost={AverageCost}",
                    city.Name, city.Id, city.OverallScore, city.AverageCost);
            }

            _logger.LogInformation("💰 批量填充评分和花费信息完成: {Count} 个城市, 总评分: {ScoreCount} 个, 费用: {CostCount} 个",
                cities.Count, overallScores.Count, averageCosts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量填充评分和花费信息失败");
        }
    }

    /// <summary>
    /// 通过 CacheService 批量获取城市总评分 (Dapr Service Invocation)
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> GetCityScoresFromCacheServiceAsync(List<Guid> cityIds)
    {
        var scores = new Dictionary<Guid, decimal>();

        if (cityIds.Count == 0) return scores;

        try
        {
            _logger.LogDebug("🔍 通过 CacheService 批量获取城市评分: {Count} 个城市", cityIds.Count);

            // 转换为字符串 ID
            var cityIdStrings = cityIds.Select(id => id.ToString()).ToList();

            // 调用 CacheService 的批量获取接口
            var response = await _daprClient.InvokeMethodAsync<List<string>, BatchScoreResponse>(
                HttpMethod.Post,
                "cache-service",
                "api/v1/cache/scores/city/batch",
                cityIdStrings
            );

            if (response?.Scores != null)
            {
                foreach (var score in response.Scores)
                {
                    if (Guid.TryParse(score.EntityId, out var cityId))
                    {
                        scores[cityId] = (decimal)score.OverallScore;
                    }
                }

                _logger.LogInformation("✅ 成功获取城市评分: {Count} 个, 缓存命中: {CachedCount}, 实时计算: {CalculatedCount}",
                    response.Scores.Count, response.CachedCount, response.CalculatedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 从 CacheService 获取评分失败,将使用空评分");
        }

        return scores;
    }

    /// <summary>
    /// 通过 CacheService 批量获取城市平均费用 (Dapr Service Invocation)
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> GetCityCostsFromCacheServiceAsync(List<Guid> cityIds)
    {
        var costs = new Dictionary<Guid, decimal>();

        if (cityIds.Count == 0) return costs;

        try
        {
            _logger.LogDebug("🔍 通过 CacheService 批量获取城市费用: {Count} 个城市", cityIds.Count);

            // 转换为字符串 ID
            var cityIdStrings = cityIds.Select(id => id.ToString()).ToList();

            // 调用 CacheService 的批量获取接口
            var response = await _daprClient.InvokeMethodAsync<List<string>, BatchCostResponse>(
                HttpMethod.Post,
                "cache-service",
                "api/v1/cache/costs/city/batch",
                cityIdStrings
            );

            if (response?.Costs != null)
            {
                foreach (var cost in response.Costs)
                {
                    if (Guid.TryParse(cost.EntityId, out var cityId))
                    {
                        costs[cityId] = cost.AverageCost;
                    }
                }

                _logger.LogInformation("✅ 成功获取城市费用: {Count} 个, 缓存命中: {CachedCount}, 实时计算: {CalculatedCount}",
                    response.Costs.Count, response.CachedCount, response.CalculatedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 从 CacheService 获取费用失败,将使用空费用");
        }

        return costs;
    }

    /// <summary>
    /// CacheService 批量响应模型
    /// </summary>
    private class BatchScoreResponse
    {
        public List<ScoreItem> Scores { get; set; } = new();
        public int TotalCount { get; set; }
        public int CachedCount { get; set; }
        public int CalculatedCount { get; set; }
    }

    /// <summary>
    /// CacheService 评分项模型
    /// </summary>
    private class ScoreItem
    {
        public string EntityId { get; set; } = string.Empty;
        public double OverallScore { get; set; }
        public bool FromCache { get; set; }
    }

    /// <summary>
    /// CacheService 费用批量响应模型
    /// </summary>
    private class BatchCostResponse
    {
        public List<CostItem> Costs { get; set; } = new();
        public int TotalCount { get; set; }
        public int CachedCount { get; set; }
        public int CalculatedCount { get; set; }
    }

    /// <summary>
    /// CacheService 费用项模型
    /// </summary>
    private class CostItem
    {
        public string EntityId { get; set; } = string.Empty;
        public decimal AverageCost { get; set; }
        public bool FromCache { get; set; }
    }

    /// <summary>
    ///     批量填充城市天气信息（优化版：使用批量API和缓存）
    /// </summary>
    private async Task EnrichCitiesWithWeatherAsync(List<CityDto> cities)
    {
        if (cities.Count == 0) return;

        try
        {
            _logger.LogInformation("🌦️ 开始批量填充天气信息: {TotalCities} 个城市", cities.Count);
            var stopwatch = Stopwatch.StartNew();

            // 准备坐标字典（优先使用坐标，更精确）
            var cityCoordinates = cities
                .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                .ToDictionary(
                    c => c.Id,
                    c => (c.Latitude!.Value, c.Longitude!.Value, c.Name)
                );

            // 批量获取有坐标的城市天气
            Dictionary<Guid, WeatherDto?> weatherByCoord = new();
            if (cityCoordinates.Count > 0)
            {
                weatherByCoord = await _weatherService.GetWeatherForCitiesByCoordinatesAsync(cityCoordinates);
            }

            // 填充有坐标的城市
            foreach (var city in cities.Where(c => cityCoordinates.ContainsKey(c.Id)))
            {
                if (weatherByCoord.TryGetValue(city.Id, out var weather))
                {
                    city.Weather = weather;
                }
            }

            // 处理没有坐标的城市（使用城市名称）
            var citiesWithoutCoords = cities
                .Where(c => !c.Latitude.HasValue || !c.Longitude.HasValue)
                .ToList();

            if (citiesWithoutCoords.Count > 0)
            {
                var cityNames = citiesWithoutCoords
                    .Select(c => !string.IsNullOrWhiteSpace(c.NameEn) ? c.NameEn : c.Name)
                    .ToList();

                var weatherByName = await _weatherService.GetWeatherForCitiesAsync(cityNames);

                // 收集需要更新经纬度的城市
                var citiesToUpdate = new List<(Guid Id, double Lat, double Lng, string Name)>();

                for (int i = 0; i < citiesWithoutCoords.Count; i++)
                {
                    var city = citiesWithoutCoords[i];
                    var cityName = !string.IsNullOrWhiteSpace(city.NameEn) ? city.NameEn : city.Name;

                    if (weatherByName.TryGetValue(cityName, out var weather))
                    {
                        city.Weather = weather;

                        // 如果天气API返回了经纬度，收集起来批量更新
                        if (weather?.Latitude.HasValue == true && weather?.Longitude.HasValue == true)
                        {
                            citiesToUpdate.Add((city.Id, weather.Latitude.Value, weather.Longitude.Value, city.Name));
                            // 同时更新 DTO 以便前端立即可用
                            city.Latitude = weather.Latitude.Value;
                            city.Longitude = weather.Longitude.Value;
                        }
                    }
                }

                // 批量更新城市经纬度到数据库（异步执行，不阻塞返回）
                if (citiesToUpdate.Count > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        foreach (var (cityId, lat, lng, name) in citiesToUpdate)
                        {
                            try
                            {
                                // 使用直接 HTTP API 更新，绕过 ORM
                                var success = await _cityRepository.UpdateCoordinatesDirectAsync(cityId, lat, lng);
                                if (success)
                                {
                                    _logger.LogInformation(
                                        "已从天气API更新城市经纬度: CityId={CityId}, CityName={CityName}, Lat={Latitude}, Lng={Longitude}",
                                        cityId, name, lat, lng);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "更新城市经纬度返回失败: CityId={CityId}, CityName={CityName}",
                                        cityId, name);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "更新城市经纬度失败: CityId={CityId}, CityName={CityName}", cityId, name);
                            }
                        }
                    });
                }
            }

            stopwatch.Stop();
            var successCount = cities.Count(c => c.Weather != null);

            _logger.LogInformation(
                "✅ 天气信息填充完成: {SuccessCount}/{TotalCount} 成功, 耗时 {ElapsedMs}ms",
                successCount, cities.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取天气信息失败");
            // 优雅降级：失败时不影响其他数据
        }
    }

    /// <summary>
    ///     填充城市的版主信息（从 city_moderators 表查询第一个活跃的版主）
    /// </summary>
    private async Task EnrichCityWithModeratorInfoAsync(CityDto cityDto)
    {
        try
        {
            var moderators = await _moderatorRepository.GetByCityIdAsync(cityDto.Id);
            var firstActiveModerator = moderators.FirstOrDefault(m => m.IsActive);

            if (firstActiveModerator != null)
            {
                // 设置版主ID
                cityDto.ModeratorId = firstActiveModerator.UserId;
                _logger.LogInformation("✅ [EnrichModerator] 已设置版主ID - CityId: {CityId}, ModeratorId: {ModeratorId}", 
                    cityDto.Id, cityDto.ModeratorId);

                // 通过缓存或 Dapr 获取用户信息
                var userInfo = await GetUserInfoWithCacheAsync(firstActiveModerator.UserId);

                if (userInfo != null)
                {
                    cityDto.Moderator = new ModeratorDto
                    {
                        Id = userInfo.Id,
                        Name = userInfo.Name,
                        Email = userInfo.Email,
                        Avatar = userInfo.Avatar,
                        Stats = userInfo.Stats != null ? new ModeratorTravelStatsDto
                        {
                            CountriesVisited = userInfo.Stats.CountriesVisited,
                            CitiesVisited = userInfo.Stats.CitiesVisited,
                            TotalDays = userInfo.Stats.TotalDays,
                            TotalTrips = userInfo.Stats.TotalTrips
                        } : null,
                        LatestTravelHistory = userInfo.LatestTravelHistory != null ? new ModeratorTravelHistoryDto
                        {
                            CityName = userInfo.LatestTravelHistory.CityName,
                            CountryName = userInfo.LatestTravelHistory.CountryName,
                            StartDate = userInfo.LatestTravelHistory.StartDate,
                            EndDate = userInfo.LatestTravelHistory.EndDate,
                            Status = userInfo.LatestTravelHistory.Status
                        } : null
                    };
                    _logger.LogInformation("✅ [EnrichModerator] 已填充版主信息 - Name: {Name}, Email: {Email}, Stats: {HasStats}, TravelHistory: {HasTravelHistory}",
                        userInfo.Name, userInfo.Email, userInfo.Stats != null, userInfo.LatestTravelHistory != null);
                }
                else
                {
                    _logger.LogWarning("⚠️ [EnrichModerator] 获取用户信息失败 - UserId: {UserId}", firstActiveModerator.UserId);
                }
            }
            else
            {
                _logger.LogDebug("ℹ️ [EnrichModerator] 该城市没有活跃版主 - CityId: {CityId}", cityDto.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "填充城市版主信息失败: CityId={CityId}", cityDto.Id);
        }
    }

    /// <summary>
    ///     批量填充城市的版主信息（优化 N+1 查询问题）
    /// </summary>
    private async Task EnrichCitiesWithModeratorInfoAsync(List<CityDto> cities)
    {
        if (cities.Count == 0) return;

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var cityIds = cities.Select(c => c.Id).ToList();

            // 🚀 优化：使用批量查询接口
            var allModerators = await _moderatorRepository.GetByCityIdsAsync(cityIds);

            // 按城市分组，取每个城市的第一个活跃版主
            var cityModeratorMap = allModerators
                .Where(m => m.IsActive)
                .GroupBy(m => m.CityId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.CreatedAt).First()
                );

            // 收集所有需要查询的用户ID
            var userIds = cityModeratorMap.Values
                .Select(m => m.UserId)
                .Distinct()
                .ToList();

            // 批量获取用户信息（使用缓存）
            var userInfoMap = new Dictionary<Guid, SimpleUserDto>();
            foreach (var userId in userIds)
            {
                var userInfo = await GetUserInfoWithCacheAsync(userId);
                if (userInfo != null) userInfoMap[userId] = userInfo;
            }

            // 填充每个城市的版主信息
            foreach (var city in cities)
                if (cityModeratorMap.TryGetValue(city.Id, out var moderator))
                {
                    city.ModeratorId = moderator.UserId;

                    if (userInfoMap.TryGetValue(moderator.UserId, out var userInfo))
                        city.Moderator = new ModeratorDto
                        {
                            Id = userInfo.Id,
                            Name = userInfo.Name,
                            Email = userInfo.Email,
                            Avatar = userInfo.Avatar
                        };
                }

            stopwatch.Stop();
            _logger.LogInformation(
                "✅ 版主信息填充完成: {Count} 个城市, 耗时 {ElapsedMs}ms",
                cities.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "批量填充城市版主信息失败");
        }
    }

    /// <summary>
    ///     批量填充城市的收藏状态
    /// </summary>
    private async Task EnrichCitiesWithFavoriteStatusAsync(List<CityDto> cities, Guid userId)
    {
        try
        {
            // 获取用户收藏的所有城市ID列表
            var favoriteCityIds = await _favoriteCityService.GetUserFavoriteCityIdsAsync(userId);
            var favoriteSet = new HashSet<string>(favoriteCityIds);

            // 填充每个城市的收藏状态
            foreach (var city in cities) city.IsFavorite = favoriteSet.Contains(city.Id.ToString());

            _logger.LogDebug("已为 {Count} 个城市填充收藏状态 (用户: {UserId})", cities.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "填充城市收藏状态失败 (用户: {UserId})", userId);
            // 失败时默认所有城市都未收藏
            foreach (var city in cities) city.IsFavorite = false;
        }
    }

    /// <summary>
    ///     通过缓存获取用户信息（带重试机制）
    /// </summary>
    private async Task<SimpleUserDto?> GetUserInfoWithCacheAsync(Guid userId)
    {
        var cacheKey = $"user_info:{userId}";

        // 尝试从缓存获取
        if (_cache.TryGetValue<SimpleUserDto>(cacheKey, out var cachedUser))
        {
            _logger.LogDebug("从缓存获取用户信息: UserId={UserId}", userId);
            return cachedUser;
        }

        // 缓存未命中，调用 Dapr（带重试）
        const int maxRetries = 2;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
            try
            {
                var userResponse = await _daprClient.InvokeMethodAsync<ApiResponse<SimpleUserDto>>(
                    HttpMethod.Get,
                    "user-service",
                    $"api/v1/users/{userId}");

                if (userResponse?.Success == true && userResponse.Data != null)
                {
                    // 缓存用户信息（15分钟）
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetPriority(CacheItemPriority.Normal);

                    _cache.Set(cacheKey, userResponse.Data, cacheOptions);

                    _logger.LogDebug("获取并缓存用户信息: UserId={UserId}", userId);
                    return userResponse.Data;
                }

                _logger.LogWarning("用户服务返回失败: UserId={UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "获取用户信息失败，准备重试 ({Attempt}/{MaxRetries}): UserId={UserId}",
                        attempt + 1, maxRetries, userId);
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1))); // 指数退避
                }
                else
                {
                    _logger.LogError(ex, "获取用户信息失败（已达最大重试次数）: UserId={UserId}", userId);
                    return null; // 返回 null 而不是抛出异常
                }
            }

        return null;
    }

    /// <summary>
    ///     更新城市图片 URL（简单版本，只更新主图）
    /// </summary>
    public async Task<bool> UpdateCityImageAsync(Guid cityId, string imageUrl)
    {
        try
        {
            _logger.LogInformation("🖼️ 更新城市图片: CityId={CityId}, ImageUrl={ImageUrl}", cityId, imageUrl);

            var city = await _cityRepository.GetByIdAsync(cityId);
            if (city == null)
            {
                _logger.LogWarning("城市不存在: CityId={CityId}", cityId);
                return false;
            }

            city.ImageUrl = imageUrl;
            city.UpdatedAt = DateTime.UtcNow;

            var result = await _cityRepository.UpdateAsync(cityId, city);

            if (result != null)
            {
                _logger.LogInformation("✅ 城市图片更新成功: CityId={CityId}", cityId);
                return true;
            }
            else
            {
                _logger.LogWarning("⚠️ 城市图片更新失败: CityId={CityId}", cityId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新城市图片异常: CityId={CityId}", cityId);
            return false;
        }
    }

    /// <summary>
    ///     更新城市所有图片（竖屏 + 横屏）
    /// </summary>
    public async Task<bool> UpdateCityImagesAsync(Guid cityId, string? portraitImageUrl, List<string>? landscapeImageUrls)
    {
        try
        {
            _logger.LogInformation(
                "🖼️ 更新城市所有图片: CityId={CityId}, PortraitUrl={PortraitUrl}, LandscapeCount={LandscapeCount}",
                cityId, portraitImageUrl, landscapeImageUrls?.Count ?? 0);

            // 直接使用 HttpClient 更新，绕过 ORM
            var result = await _cityRepository.UpdateImagesDirectAsync(
                cityId, 
                portraitImageUrl,  // 同时更新 image_url
                portraitImageUrl, 
                landscapeImageUrls);

            if (result)
            {
                _logger.LogInformation("✅ 城市图片全部更新成功: CityId={CityId}", cityId);
                return true;
            }
            else
            {
                _logger.LogWarning("⚠️ 城市图片更新失败: CityId={CityId}", cityId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新城市图片异常: CityId={CityId}", cityId);
            return false;
        }
    }
}

// 临时 DTO - 用于 Dapr 服务间调用
internal class SimpleUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;

    // 兼容性属性：将 AvatarUrl 映射到 Avatar
    public string? Avatar => AvatarUrl;

    // 旅行统计
    public SimpleUserTravelStatsDto? Stats { get; set; }

    // 最新旅行历史
    public SimpleUserTravelHistoryDto? LatestTravelHistory { get; set; }
}

internal class SimpleUserTravelStatsDto
{
    public int CountriesVisited { get; set; }
    public int CitiesVisited { get; set; }
    public int TotalDays { get; set; }
    public int TotalTrips { get; set; }
}

internal class SimpleUserTravelHistoryDto
{
    // 匹配 UserService 的 TravelHistoryDto 字段名
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime? DepartureTime { get; set; }
    public bool IsOngoing { get; set; }

    // 兼容性属性
    public string? CityName => City;
    public string? CountryName => Country;
    public DateTime? StartDate => ArrivalTime;
    public DateTime? EndDate => DepartureTime;
    public string? Status => IsOngoing ? "current" : "completed";
}
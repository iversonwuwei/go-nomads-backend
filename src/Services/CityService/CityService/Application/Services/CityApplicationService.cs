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
        var favoriteTask = userId.HasValue
            ? EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value)
            : Task.CompletedTask;

        await Task.WhenAll(weatherTask, moderatorTask, favoriteTask);

        // 设置用户上下文
        foreach (var cityDto in cityDtos) cityDto.SetUserContext(userId, userRole);

        return cityDtos;
    }

    public async Task<CityDto?> GetCityByIdAsync(Guid id, Guid? userId = null, string? userRole = null)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null) return null;

        var cityDto = MapToDto(city);

        // 并行填充数据
        var favoriteTask = userId.HasValue
            ? _favoriteCityService.IsCityFavoritedAsync(userId.Value, id.ToString())
            : Task.FromResult(false);
        var moderatorTask = EnrichCityWithModeratorInfoAsync(cityDto);

        await Task.WhenAll(favoriteTask, moderatorTask);

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

        await Task.WhenAll(weatherTask, moderatorTask, ratingsAndCostsTask, favoriteTask);

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
            var country = await _countryRepository.GetCountryByIdAsync(countryId);
            if (country == null) return Enumerable.Empty<CitySummaryDto>();

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
        return cities.Select(MapToDto).ToList();
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

            city.ModeratorId = dto.UserId;
            city.UpdatedAt = DateTime.UtcNow;

            await _cityRepository.UpdateAsync(city.Id, city);

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

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var cityIds = cities.Select(c => c.Id).ToList();

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // 批量查询评分数量
            var ratingCountsQuery = @"
                SELECT city_id, COUNT(DISTINCT user_id) as count
                FROM city_ratings
                WHERE city_id = ANY(@cityIds)
                GROUP BY city_id";

            var ratingCounts = new Dictionary<Guid, int>();
            using (var command = new NpgsqlCommand(ratingCountsQuery, connection))
            {
                command.Parameters.AddWithValue("cityIds", cityIds.ToArray());
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var cityId = reader.GetGuid(0);
                    var count = Convert.ToInt32(reader.GetInt64(1));
                    ratingCounts[cityId] = count;
                }
            }

            // 批量查询平均花费
            var avgCostsQuery = @"
                SELECT city_id, AVG(total) as avg_cost
                FROM user_city_expenses
                WHERE city_id = ANY(@cityIds)
                GROUP BY city_id";

            var avgCosts = new Dictionary<string, decimal>();
            using (var command = new NpgsqlCommand(avgCostsQuery, connection))
            {
                command.Parameters.AddWithValue("cityIds", cityIds.Select(id => id.ToString()).ToArray());
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var cityId = reader.GetString(0);
                    var avgCost = reader.GetDecimal(1);
                    avgCosts[cityId] = avgCost;
                }
            }

            // 填充数据
            foreach (var city in cities)
            {
                city.ReviewCount = ratingCounts.GetValueOrDefault(city.Id, 0);
                city.AverageCost = avgCosts.GetValueOrDefault(city.Id.ToString());

                _logger.LogDebug("📊 城市 {CityName}({CityId}): ReviewCount={ReviewCount}, AverageCost={AverageCost}",
                    city.Name, city.Id, city.ReviewCount, city.AverageCost);
            }

            _logger.LogInformation("💰 批量填充评分和花费信息完成: {Count} 个城市, 评分数据: {RatingCount} 个, 花费数据: {CostCount} 个",
                cities.Count, ratingCounts.Count, avgCosts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量填充评分和花费信息失败");
        }
    }

    private async Task EnrichCitiesWithWeatherAsync(List<CityDto> cities)
    {
        if (cities.Count == 0) return;

        try
        {
            // 优化策略：分批处理，避免并发过高
            const int batchSize = 10; // 每批处理 10 个城市
            var batches = cities
                .Select((city, index) => new { city, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.city).ToList())
                .ToList();

            _logger.LogDebug("🌦️ 开始批量填充天气信息: {TotalCities} 个城市, {BatchCount} 批次",
                cities.Count, batches.Count);

            var stopwatch = Stopwatch.StartNew();

            foreach (var batch in batches)
            {
                // 每批次并发处理
                var weatherTasks = batch.Select(async city =>
                {
                    try
                    {
                        if (city.Latitude.HasValue && city.Longitude.HasValue)
                        {
                            city.Weather = await _weatherService.GetWeatherByCoordinatesAsync(
                                city.Latitude.Value,
                                city.Longitude.Value);
                        }
                        else
                        {
                            // 优先使用英文名称获取天气
                            var cityName = !string.IsNullOrWhiteSpace(city.NameEn) ? city.NameEn : city.Name;
                            city.Weather = await _weatherService.GetWeatherByCityNameAsync(cityName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取城市天气失败: {CityName}", city.Name);
                        city.Weather = null; // 优雅降级
                    }
                });

                await Task.WhenAll(weatherTasks);

                // 批次间略微延迟，避免 API 频率限制
                if (batches.IndexOf(batch) < batches.Count - 1) await Task.Delay(100); // 100ms 延迟
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

                // 通过缓存或 Dapr 获取用户信息
                var userInfo = await GetUserInfoWithCacheAsync(firstActiveModerator.UserId);

                if (userInfo != null)
                    cityDto.Moderator = new ModeratorDto
                    {
                        Id = userInfo.Id,
                        Name = userInfo.Name,
                        Email = userInfo.Email,
                        Avatar = userInfo.Avatar
                    };
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
                    return null;
                }
            }

        return null;
    }
}

// 临时 DTO - 用于 Dapr 服务间调用
internal class SimpleUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Role { get; set; } = string.Empty;
}
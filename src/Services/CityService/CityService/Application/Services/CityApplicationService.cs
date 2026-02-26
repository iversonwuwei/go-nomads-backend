using System.Diagnostics;
using System.Net.Http.Json;
using CityService.Application.Abstractions.Services;
using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Domain.ValueObjects;
using GoNomads.Shared.Models;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Shared.Messages;

namespace CityService.Application.Services;

/// <summary>
///     城市应用服务实现
/// </summary>
public class CityApplicationService : ICityService
{
    private readonly IMemoryCache _cache;
    private readonly ICityRepository _cityRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly ICityRatingRepository _ratingRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserFavoriteCityService _favoriteCityService;
    private readonly ILogger<CityApplicationService> _logger;
    private readonly ICityModeratorRepository _moderatorRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IWeatherService _weatherService;
    private readonly IConfiguration _configuration;

    public CityApplicationService(
        ICityRepository cityRepository,
        ICountryRepository countryRepository,
        ICityRatingRepository ratingRepository,
        IWeatherService weatherService,
        IUserFavoriteCityService favoriteCityService,
        ICityModeratorRepository moderatorRepository,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        IPublishEndpoint publishEndpoint,
        ILogger<CityApplicationService> logger)
    {
        _cityRepository = cityRepository;
        _countryRepository = countryRepository;
        _ratingRepository = ratingRepository;
        _weatherService = weatherService;
        _favoriteCityService = favoriteCityService;
        _moderatorRepository = moderatorRepository;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize, Guid? userId = null,
        string? userRole = null)
    {
        var cities = await _cityRepository.GetAllAsync(pageNumber, pageSize);
        var cityDtos = cities.Select(MapToDto).ToList();

        // 并行填充数据（不再获取天气数据）
        var moderatorTask = EnrichCitiesWithModeratorInfoAsync(cityDtos);
        var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(cityDtos);
        var meetupAndCoworkingTask = EnrichCitiesWithMeetupAndCoworkingCountsAsync(cityDtos);
        var favoriteTask = userId.HasValue
            ? EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value)
            : Task.CompletedTask;

        // 等待所有任务完成（即使某些任务失败，其他任务也会继续执行）
        var allTasks = new[] { moderatorTask, ratingsAndCostsTask, meetupAndCoworkingTask, favoriteTask };
        await Task.WhenAll(allTasks.Select(t => t.ContinueWith(_ => { })));

        // 设置用户上下文
        foreach (var cityDto in cityDtos) cityDto.SetUserContext(userId, userRole);

        // 数据库已按 OverallScore 降序排序，无需再次排序
        return cityDtos;
    }

    /// <summary>
    /// 清除城市列表缓存（当城市数据变更时调用）
    /// </summary>
    public void InvalidateCityListCache()
    {
        // 使用 CancellationTokenSource 来使所有城市列表缓存失效
        // 由于 IMemoryCache 没有直接的 Clear 或 RemoveByPrefix 方法，
        // 我们通过递增版本号来使旧缓存失效
        var newVersion = DateTime.UtcNow.Ticks;
        _cache.Set("city_list:version", newVersion);
        _logger.LogInformation("🗑️ [Cache] 城市列表缓存已失效, 新版本号: {Version}", newVersion);
    }

    /// <summary>
    /// 清除城市详情缓存（当城市数据变更时调用）
    /// </summary>
    public void InvalidateCityDetailCache(Guid? cityId = null)
    {
        if (cityId.HasValue)
        {
            // 清除特定城市的缓存：直接递增全局版本号（简单有效）
            var newVersion = DateTime.UtcNow.Ticks;
            _cache.Set("city_detail:version", newVersion);
            _logger.LogInformation("🗑️ [Cache] 城市详情缓存已失效: CityId={CityId}, 新版本号: {Version}", cityId, newVersion);
        }
        else
        {
            // 清除所有城市详情缓存
            var newVersion = DateTime.UtcNow.Ticks;
            _cache.Set("city_detail:version", newVersion);
            _logger.LogInformation("🗑️ [Cache] 所有城市详情缓存已失效, 新版本号: {Version}", newVersion);
        }
    }

    /// <summary>
    /// 清除所有城市相关缓存
    /// </summary>
    public void InvalidateAllCityCaches(Guid? cityId = null)
    {
        InvalidateCityListCache();
        InvalidateCityDetailCache(cityId);
    }

    /// <summary>
    /// 发布城市更新事件到 MassTransit，用于同步到 Elasticsearch
    /// </summary>
    private async Task PublishCityUpdatedMessageAsync(City city, List<string> updatedFields)
    {
        try
        {
            var message = new CityUpdatedMessage
            {
                CityId = city.Id.ToString(),
                Name = city.Name,
                NameEn = city.NameEn,
                Country = city.Country,
                CountryCode = null,
                UpdatedAt = DateTime.UtcNow,
                UpdatedFields = updatedFields
            };

            await _publishEndpoint.Publish(message);
            _logger.LogInformation("📤 已发布城市更新事件: CityId={CityId}, UpdatedFields=[{Fields}]",
                city.Id, string.Join(", ", updatedFields));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 发布城市更新事件失败: CityId={CityId}", city.Id);
            // 不抛出异常，主流程已成功，事件发布失败不影响主流程
        }
    }

    /// <summary>
    /// 发布城市版主变更消息到 MassTransit，用于通过 SignalR 广播给客户端
    /// </summary>
    private async Task PublishCityModeratorUpdatedMessageAsync(City city, string changeType, string? userId = null)
    {
        try
        {
            var message = new CityModeratorUpdatedMessage
            {
                CityId = city.Id.ToString(),
                CityName = city.Name,
                CityNameEn = city.NameEn,
                ChangeType = changeType,
                UserId = userId,
                UpdatedAt = DateTime.UtcNow,
            };

            await _publishEndpoint.Publish(message);
            _logger.LogInformation("📤 已发布城市版主变更事件: CityId={CityId}, ChangeType={ChangeType}",
                city.Id, changeType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 发布城市版主变更事件失败: CityId={CityId}", city.Id);
        }
    }

    /// <summary>
    /// 获取城市列表（轻量级版本，不包含天气数据）
    /// 用于城市列表页面，提升加载性能
    /// </summary>
    public async Task<IEnumerable<CityListItemDto>> GetCityListAsync(int pageNumber, int pageSize, string? search = null, Guid? userId = null, string? userRole = null)
    {
        _logger.LogInformation("🚀 [GetCityList] 开始获取轻量级城市列表: page={Page}, size={Size}, search={Search}",
            pageNumber, pageSize, search);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 获取当前缓存版本号
        var cacheVersion = _cache.GetOrCreate("city_list:version", entry => DateTime.UtcNow.Ticks);

        // 尝试从缓存获取基础城市列表数据（不包含用户相关数据）
        var baseCacheKey = $"city_list:v{cacheVersion}:p{pageNumber}:s{pageSize}:q{search ?? "all"}";
        List<CityListItemDto> cityListItems;
        bool fromCache = false;

        if (_cache.TryGetValue(baseCacheKey, out List<CityListItemDto>? cachedItems) && cachedItems != null)
        {
            // 深拷贝缓存数据，避免修改缓存中的对象
            cityListItems = cachedItems.Select(c => c.Clone()).ToList();
            fromCache = true;
            _logger.LogInformation("📦 [GetCityList] 从缓存获取城市列表: {Count} 个城市", cityListItems.Count);
        }
        else
        {
            // 缓存未命中，从数据库获取
            IEnumerable<Domain.Entities.City> cities;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var criteria = new Domain.ValueObjects.CitySearchCriteria
                {
                    Name = search,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                cities = await _cityRepository.SearchAsync(criteria);
            }
            else
            {
                cities = await _cityRepository.GetAllAsync(pageNumber, pageSize);
            }

            cityListItems = cities.Select(city => new CityListItemDto
            {
                Id = city.Id,
                Name = city.Name,
                NameEn = city.NameEn,
                Country = city.Country,
                CountryId = city.CountryId,
                Region = city.Region,
                ImageUrl = city.ImageUrl,
                PortraitImageUrl = city.PortraitImageUrl,
                LandscapeImageUrls = city.LandscapeImageUrls,

                // 城市基本信息 - 帮助数字游民了解城市
                Description = city.Description,
                TimeZone = city.TimeZone,
                Currency = city.Currency,

                // 综合评分
                OverallScore = city.OverallScore,

                // 数字游民核心关注指标（静态评分，非实时数据）
                InternetQualityScore = city.InternetQualityScore,
                SafetyScore = city.SafetyScore,
                CostScore = city.CostScore,
                CommunityScore = city.CommunityScore,
                WeatherScore = city.WeatherScore,

                // 城市标签
                Tags = city.Tags,

                // 地理位置
                Latitude = city.Latitude,
                Longitude = city.Longitude,
            }).ToList();

            // 并行填充非用户相关的数据
            var ratingsTask = EnrichCityListWithRatingsAndCostsAsync(cityListItems);
            var countsTask = EnrichCityListWithCountsAsync(cityListItems);
            var moderatorTask = EnrichCityListWithModeratorInfoAsync(cityListItems, null, null); // 不传用户信息

            await Task.WhenAll(ratingsTask, countsTask, moderatorTask);

            // 缓存基础数据（2分钟过期）
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(2))
                .SetSlidingExpiration(TimeSpan.FromMinutes(1));
            _cache.Set(baseCacheKey, cityListItems.Select(c => c.Clone()).ToList(), cacheOptions);

            _logger.LogInformation("💾 [GetCityList] 城市列表已缓存: key={CacheKey}", baseCacheKey);
        }

        // 填充用户相关数据（收藏状态、权限等）- 不缓存
        if (userId.HasValue || !string.IsNullOrEmpty(userRole))
        {
            // 更新版主相关的用户权限
            var isAdmin = userRole?.ToLower() == "admin";
            foreach (var city in cityListItems)
            {
                city.IsCurrentUserAdmin = isAdmin;
                city.IsCurrentUserModerator = userId.HasValue && city.ModeratorId.HasValue && city.ModeratorId.Value == userId.Value;
            }

            // 填充收藏状态
            if (userId.HasValue)
            {
                await EnrichCityListWithFavoriteStatusAsync(cityListItems, userId.Value);
            }
        }

        stopwatch.Stop();
        _logger.LogInformation("✅ [GetCityList] 轻量级城市列表获取完成: {Count} 个城市, 耗时 {Elapsed}ms, 缓存命中={FromCache}",
            cityListItems.Count, stopwatch.ElapsedMilliseconds, fromCache);

        return cityListItems;
    }

    /// <summary>
    /// 获取城市列表（基础版本，不包含聚合数据）
    /// 用于快速首屏加载，聚合数据（MeetupCount, CoworkingCount等）后续异步加载
    /// 支持可选 region 筛选
    /// </summary>
    public async Task<IEnumerable<CityListItemDto>> GetCityListBasicAsync(int pageNumber, int pageSize, string? search = null, string? region = null, Guid? userId = null, string? userRole = null)
    {
        _logger.LogInformation("🚀 [GetCityListBasic] 开始获取基础城市列表: page={Page}, size={Size}, search={Search}, region={Region}",
            pageNumber, pageSize, search, region);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 从数据库获取基础数据
        IEnumerable<Domain.Entities.City> cities;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var criteria = new Domain.ValueObjects.CitySearchCriteria
            {
                Name = search,
                Region = region,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            cities = await _cityRepository.SearchAsync(criteria);
        }
        else if (!string.IsNullOrWhiteSpace(region))
        {
            // 按大洲（continent）筛选：同时通过 country_id 和 country name 匹配
            var countryIdsTask = _countryRepository.GetCountryIdsByContinentAsync(region);
            var countryNamesTask = _countryRepository.GetCountryNamesByContinentAsync(region);
            await Task.WhenAll(countryIdsTask, countryNamesTask);

            var countryIdList = countryIdsTask.Result.ToList();
            var countryNameList = countryNamesTask.Result.ToList();

            if (countryIdList.Count > 0 || countryNameList.Count > 0)
            {
                cities = await _cityRepository.GetByContinentAsync(countryIdList, countryNameList, pageNumber, pageSize);
            }
            else
            {
                cities = Enumerable.Empty<Domain.Entities.City>();
            }
        }
        else
        {
            cities = await _cityRepository.GetAllAsync(pageNumber, pageSize);
        }

        var cityListItems = cities.Select(city => new CityListItemDto
        {
            Id = city.Id,
            Name = city.Name,
            NameEn = city.NameEn,
            Country = city.Country,
            CountryId = city.CountryId,
            Region = city.Region,
            ImageUrl = city.ImageUrl,
            PortraitImageUrl = city.PortraitImageUrl,
            LandscapeImageUrls = city.LandscapeImageUrls,
            Description = city.Description,
            TimeZone = city.TimeZone,
            Currency = city.Currency,
            OverallScore = city.OverallScore,
            InternetQualityScore = city.InternetQualityScore,
            SafetyScore = city.SafetyScore,
            CostScore = city.CostScore,
            CommunityScore = city.CommunityScore,
            WeatherScore = city.WeatherScore,
            Tags = city.Tags,
            Latitude = city.Latitude,
            Longitude = city.Longitude,
            // 聚合数据设为默认值，后续异步加载
            MeetupCount = 0,
            CoworkingCount = 0,
            ReviewCount = 0,
            AverageCost = 0,
        }).ToList();

        // 只填充版主信息（快速）
        await EnrichCityListWithModeratorInfoAsync(cityListItems, userId, userRole);

        // 填充用户相关数据（收藏状态）
        if (userId.HasValue)
        {
            await EnrichCityListWithFavoriteStatusAsync(cityListItems, userId.Value);
        }

        // 更新用户权限
        var isAdmin = userRole?.ToLower() == "admin";
        foreach (var city in cityListItems)
        {
            city.IsCurrentUserAdmin = isAdmin;
            city.IsCurrentUserModerator = userId.HasValue && city.ModeratorId.HasValue && city.ModeratorId.Value == userId.Value;
        }

        stopwatch.Stop();
        _logger.LogInformation("✅ [GetCityListBasic] 基础城市列表获取完成: {Count} 个城市, 耗时 {Elapsed}ms",
            cityListItems.Count, stopwatch.ElapsedMilliseconds);

        return cityListItems;
    }

    /// <summary>
    /// 获取所有可用的区域标签（基于国家表的 continent 字段，用于前端 Tab 展示）
    /// 返回按 displayOrder 排序的大洲列表，包含每个大洲的城市数量
    /// </summary>
    public async Task<IEnumerable<CityRegionTabDto>> GetRegionTabsAsync()
    {
        _logger.LogInformation("🌍 [GetRegionTabs] 开始获取区域标签（基于 continent）");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 预定义的大洲排序和显示名称
            var continentConfig = new Dictionary<string, (int Order, string Label)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Asia", (1, "Asia") },
                { "Europe", (2, "Europe") },
                { "North America", (3, "North America") },
                { "South America", (4, "South America") },
                { "Africa", (5, "Africa") },
                { "Oceania", (6, "Oceania") },
            };

            // 只需 2 次查询：获取所有国家 + 所有城市，内存中计算
            var countriesTask = _countryRepository.GetAllActiveCountriesAsync();
            var citiesTask = _cityRepository.GetAllActiveCityBriefAsync();
            await Task.WhenAll(countriesTask, citiesTask);

            var countries = (await countriesTask).ToList();
            var cities = (await citiesTask).ToList();

            // 构建 country -> continent 映射
            var countryIdToContinent = new Dictionary<Guid, string>();
            var countryNameToContinent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var country in countries)
            {
                if (string.IsNullOrWhiteSpace(country.Continent)) continue;
                countryIdToContinent[country.Id] = country.Continent;
                if (!string.IsNullOrWhiteSpace(country.Name))
                    countryNameToContinent[country.Name] = country.Continent;
            }

            // 按大洲统计城市数（去重）
            var continentCityIds = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);
            foreach (var city in cities)
            {
                string? continent = null;

                // 优先通过 country_id 匹配
                if (city.CountryId.HasValue && city.CountryId != Guid.Empty
                    && countryIdToContinent.TryGetValue(city.CountryId.Value, out var c1))
                {
                    continent = c1;
                }
                // 其次通过 country name 匹配
                else if (!string.IsNullOrWhiteSpace(city.Country)
                    && countryNameToContinent.TryGetValue(city.Country, out var c2))
                {
                    continent = c2;
                }

                if (continent != null)
                {
                    if (!continentCityIds.ContainsKey(continent))
                        continentCityIds[continent] = new HashSet<Guid>();
                    continentCityIds[continent].Add(city.Id);
                }
            }

            var tabs = continentCityIds.Select(kvp =>
            {
                var order = 99;
                var label = kvp.Key;
                if (continentConfig.TryGetValue(kvp.Key, out var cfg))
                {
                    order = cfg.Item1;
                    label = cfg.Item2;
                }
                return new CityRegionTabDto
                {
                    Key = kvp.Key,
                    Label = label,
                    CityCount = kvp.Value.Count,
                    DisplayOrder = order,
                };
            }).OrderBy(t => t.DisplayOrder).ToList();

            stopwatch.Stop();
            _logger.LogInformation("✅ [GetRegionTabs] 获取到 {Count} 个区域标签（continent），耗时 {Ms}ms", tabs.Count, stopwatch.ElapsedMilliseconds);
            return tabs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [GetRegionTabs] 获取区域标签失败");
            return Enumerable.Empty<CityRegionTabDto>();
        }
    }

    /// <summary>
    /// 批量获取城市聚合数据（MeetupCount, CoworkingCount, ReviewCount, AverageCost）
    /// </summary>
    public async Task<Dictionary<Guid, CityCountsDto>> GetCityCountsBatchAsync(IEnumerable<Guid> cityIds)
    {
        var idList = cityIds.ToList();
        _logger.LogInformation("🚀 [GetCityCountsBatch] 开始获取城市聚合数据: {Count} 个城市", idList.Count);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = new Dictionary<Guid, CityCountsDto>();

        if (idList.Count == 0) return result;

        try
        {
            // 并行获取所有聚合数据
            var meetupCountsTask = GetMeetupCountsFromEventServiceAsync(idList);
            var coworkingCountsTask = GetCoworkingCountsFromCoworkingServiceAsync(idList);
            var reviewCountsTask = _ratingRepository.GetCityReviewCountsBatchAsync(idList);
            var costsTask = GetCityCostsFromCacheServiceAsync(idList);

            await Task.WhenAll(meetupCountsTask, coworkingCountsTask, reviewCountsTask, costsTask);

            var meetupCounts = await meetupCountsTask;
            var coworkingCounts = await coworkingCountsTask;
            var reviewCounts = await reviewCountsTask;
            var costs = await costsTask;

            foreach (var cityId in idList)
            {
                result[cityId] = new CityCountsDto
                {
                    CityId = cityId,
                    MeetupCount = meetupCounts.GetValueOrDefault(cityId),
                    CoworkingCount = coworkingCounts.GetValueOrDefault(cityId),
                    ReviewCount = reviewCounts.GetValueOrDefault(cityId),
                    AverageCost = costs.GetValueOrDefault(cityId),
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取城市聚合数据时出错");
        }

        stopwatch.Stop();
        _logger.LogInformation("✅ [GetCityCountsBatch] 城市聚合数据获取完成: {Count} 个城市, 耗时 {Elapsed}ms",
            result.Count, stopwatch.ElapsedMilliseconds);

        return result;
    }

    /// <summary>
    /// 为轻量级城市列表填充评分和费用
    /// </summary>
    private async Task EnrichCityListWithRatingsAndCostsAsync(List<CityListItemDto> cities)
    {
        if (cities.Count == 0) return;

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var cityIds = cities.Select(c => c.Id).ToList();

            // 并行获取评分、费用和总评分
            var reviewCountsTask = _ratingRepository.GetCityReviewCountsBatchAsync(cityIds);
            var costsTask = GetCityCostsFromCacheServiceAsync(cityIds);
            var scoresTask = GetCityScoresFromCacheServiceAsync(cityIds);

            await Task.WhenAll(reviewCountsTask, costsTask, scoresTask);

            var reviewCounts = await reviewCountsTask;
            var costs = await costsTask;
            var scores = await scoresTask;

            foreach (var city in cities)
            {
                city.ReviewCount = reviewCounts.GetValueOrDefault(city.Id);
                city.AverageCost = costs.GetValueOrDefault(city.Id);

                // 只有当 CacheService 返回了有效评分时才更新，否则保留数据库原值
                if (scores.TryGetValue(city.Id, out var score) && score > 0)
                {
                    city.OverallScore = score;
                }
                else if (!city.OverallScore.HasValue || city.OverallScore == 0)
                {
                    // 如果数据库也没有评分，尝试从其他评分计算一个平均值
                    var availableScores = new List<decimal?> 
                    { 
                        city.InternetQualityScore, 
                        city.SafetyScore, 
                        city.CostScore, 
                        city.CommunityScore,
                        city.WeatherScore 
                    }.Where(s => s.HasValue && s > 0).ToList();
                    
                    if (availableScores.Count > 0)
                    {
                        city.OverallScore = availableScores.Average(s => s!.Value);
                    }
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("⏱️ [EnrichRatings] 评分费用填充耗时: {Elapsed}ms, Scores={ScoreCount}, Costs={CostCount}, Reviews={ReviewCount}",
                stopwatch.ElapsedMilliseconds, scores.Count, costs.Count, reviewCounts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "填充轻量级城市列表评分和费用时出错");
        }
    }

    /// <summary>
    /// 为轻量级城市列表填充 Meetup 和 Coworking 数量
    /// </summary>
    private async Task EnrichCityListWithCountsAsync(List<CityListItemDto> cities)
    {
        if (cities.Count == 0) return;

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var cityIds = cities.Select(c => c.Id).ToList();

            // 并行获取 Meetup 和 Coworking 数量
            var meetupCountsTask = GetMeetupCountsFromEventServiceAsync(cityIds);
            var coworkingCountsTask = GetCoworkingCountsFromCoworkingServiceAsync(cityIds);

            await Task.WhenAll(meetupCountsTask, coworkingCountsTask);

            var meetupCounts = await meetupCountsTask;
            var coworkingCounts = await coworkingCountsTask;

            foreach (var city in cities)
            {
                city.MeetupCount = meetupCounts.GetValueOrDefault(city.Id);
                city.CoworkingCount = coworkingCounts.GetValueOrDefault(city.Id);
            }

            stopwatch.Stop();
            _logger.LogInformation("⏱️ [EnrichCounts] Meetup/Coworking 数量填充耗时: {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "填充轻量级城市列表数量时出错");
        }
    }

    /// <summary>
    /// 为轻量级城市列表填充收藏状态
    /// </summary>
    private async Task EnrichCityListWithFavoriteStatusAsync(List<CityListItemDto> cities, Guid userId)
    {
        if (cities.Count == 0) return;

        try
        {
            // 获取用户收藏的所有城市ID
            var favoriteCityIds = await _favoriteCityService.GetUserFavoriteCityIdsAsync(userId);
            var favoriteCityIdSet = new HashSet<string>(favoriteCityIds);

            foreach (var city in cities)
            {
                city.IsFavorite = favoriteCityIdSet.Contains(city.Id.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "填充轻量级城市列表收藏状态时出错");
        }
    }

    /// <summary>
    /// 为轻量级城市列表填充版主信息
    /// </summary>
    private async Task EnrichCityListWithModeratorInfoAsync(List<CityListItemDto> cities, Guid? userId, string? userRole)
    {
        if (cities.Count == 0) return;

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var cityIds = cities.Select(c => c.Id).ToList();

            // 批量获取所有城市的版主信息
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
            var userInfoMap = await GetUsersInfoBatchWithCacheAsync(userIds);

            // 判断当前用户是否为管理员
            var isAdmin = userRole?.ToLower() == "admin";

            // 填充每个城市的版主信息
            foreach (var city in cities)
            {
                if (cityModeratorMap.TryGetValue(city.Id, out var moderator))
                {
                    city.ModeratorId = moderator.UserId;

                    if (userInfoMap.TryGetValue(moderator.UserId, out var userInfo))
                    {
                        city.Moderator = new ModeratorDto
                        {
                            Id = userInfo.Id,
                            Name = userInfo.Name,
                            Email = userInfo.Email,
                            Avatar = userInfo.Avatar
                        };
                    }

                    // 设置当前用户权限
                    city.IsCurrentUserAdmin = isAdmin;
                    city.IsCurrentUserModerator = userId.HasValue && moderator.UserId == userId.Value;
                }
                else
                {
                    city.IsCurrentUserAdmin = isAdmin;
                    city.IsCurrentUserModerator = false;
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("⏱️ [EnrichModerators] 版主信息填充耗时: {Elapsed}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "填充轻量级城市列表版主信息时出错");
        }
    }

    public async Task<CityDto?> GetCityByIdAsync(Guid id, Guid? userId = null, string? userRole = null)
    {
        var stopwatch = Stopwatch.StartNew();

        // 🚀 优化：使用内存缓存存储城市基础数据（不包含用户相关数据）
        var cacheVersion = _cache.GetOrCreate("city_detail:version", entry => DateTime.UtcNow.Ticks);
        var baseCacheKey = $"city_detail:v{cacheVersion}:{id}";
        CityDto? cityDto;

        if (_cache.TryGetValue(baseCacheKey, out CityDto? cachedCity) && cachedCity != null)
        {
            // 深拷贝缓存数据，避免修改缓存中的对象
            cityDto = cachedCity.DeepClone();
            _logger.LogInformation("📦 [GetCityById] 从缓存获取城市详情: CityId={CityId}, Name={CityName}", id, cityDto.Name);
        }
        else
        {
            var city = await _cityRepository.GetByIdAsync(id);
            if (city == null) return null;

            _logger.LogDebug(
                "🖼️ [GetCityById] 图片字段: CityId={CityId}, Name={CityName}, LandscapeCount={Count}",
                id, city.Name, city.LandscapeImageUrls?.Count ?? 0);

            cityDto = MapToDto(city);

            // 并行填充非用户相关数据（版主信息 + 评分费用）
            var moderatorTask = EnrichCityWithModeratorInfoAsync(cityDto);
            var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(new List<CityDto> { cityDto });

            await Task.WhenAll(moderatorTask, ratingsAndCostsTask);

            // 缓存基础数据（不含用户收藏状态）— 90秒过期，滑动60秒
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(90))
                .SetSlidingExpiration(TimeSpan.FromSeconds(60));
            _cache.Set(baseCacheKey, cityDto.DeepClone(), cacheOptions);

            _logger.LogInformation("💾 [GetCityById] 城市详情已缓存: CityId={CityId}, key={CacheKey}", id, baseCacheKey);
        }

        // 用户相关数据每次实时获取（不缓存）
        if (userId.HasValue)
        {
            var isFavorite = await _favoriteCityService.IsCityFavoritedAsync(userId.Value, id.ToString());
            cityDto.IsFavorite = isFavorite;
        }

        // 设置用户上下文（包括是否为管理员和是否为该城市版主）
        cityDto.SetUserContext(userId, userRole);

        stopwatch.Stop();
        _logger.LogInformation("⏱️ [GetCityById] 总耗时: {Elapsed}ms, CityId={CityId}, FromCache={FromCache}",
            stopwatch.ElapsedMilliseconds, id, _cache.TryGetValue(baseCacheKey, out _));

        return cityDto;
    }

    public async Task<CityModeratorSummaryDto?> GetCityModeratorSummaryAsync(Guid id, Guid? userId = null, string? userRole = null)
    {
        var city = await _cityRepository.GetByIdAsync(id);
        if (city == null) return null;

        var summary = new CityModeratorSummaryDto
        {
            CityId = id,
            IsCurrentUserAdmin = userRole?.ToLower() == "admin"
        };

        try
        {
            var moderators = await _moderatorRepository.GetByCityIdAsync(id);
            var firstActiveModerator = moderators.FirstOrDefault(m => m.IsActive);

            if (firstActiveModerator != null)
            {
                summary.ModeratorId = firstActiveModerator.UserId;
                summary.IsCurrentUserModerator = userId.HasValue && firstActiveModerator.UserId == userId.Value;

                var userInfo = await GetUserInfoWithCacheAsync(firstActiveModerator.UserId);
                if (userInfo != null)
                {
                    summary.Moderator = new ModeratorDto
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
                }
            }
            else
            {
                summary.IsCurrentUserModerator = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取城市版主摘要失败: CityId={CityId}", id);
        }

        return summary;
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

        // 并行填充数据（不再获取天气数据）
        var moderatorTask = EnrichCitiesWithModeratorInfoAsync(cityDtos);
        var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(cityDtos);
        var favoriteTask = userId.HasValue
            ? EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value)
            : Task.CompletedTask;

        // 等待所有任务完成（即使某些任务失败，其他任务也会继续执行）
        var allTasks = new[] { moderatorTask, ratingsAndCostsTask, favoriteTask };
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

        // 清除所有城市相关缓存
        InvalidateAllCityCaches(createdCity.Id);

        _logger.LogInformation("City created: {CityId} - {CityName}", createdCity.Id, createdCity.Name);
        return MapToDto(createdCity);
    }

    public async Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId)
    {
        var existingCity = await _cityRepository.GetByIdAsync(id);
        if (existingCity == null) return null;

        // 记录更新的字段（用于事件通知）
        var updatedFields = new List<string>();
        if (!string.IsNullOrWhiteSpace(updateCityDto.Name) && updateCityDto.Name != existingCity.Name)
            updatedFields.Add("name");
        if (!string.IsNullOrWhiteSpace(updateCityDto.Country) && updateCityDto.Country != existingCity.Country)
            updatedFields.Add("country");

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

        // 发布城市更新事件到 Elasticsearch（任何字段变更都同步）
        await PublishCityUpdatedMessageAsync(updatedCity, updatedFields);

        // 清除所有城市相关缓存
        InvalidateAllCityCaches(id);

        _logger.LogInformation("City updated: {CityId} - {CityName}", id, existingCity.Name);
        return MapToDto(updatedCity);
    }

    public async Task<bool> DeleteCityAsync(Guid id, Guid? deletedBy = null)
    {
        var result = await _cityRepository.DeleteAsync(id, deletedBy);
        if (result)
        {
            // 清除所有城市相关缓存
            InvalidateAllCityCaches(id);
            _logger.LogInformation("City deleted: {CityId}, DeletedBy: {DeletedBy}", id, deletedBy);
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
        if (userId.HasValue) await EnrichCitiesWithFavoriteStatusAsync(cityDtos, userId.Value);

        return cityDtos;
    }

    public async Task<IEnumerable<CityDto>> GetPopularCitiesAsync(int limit, Guid? userId = null)
    {
        var cities = await _cityRepository.GetPopularAsync(limit);
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
                        NameEn = city.NameEn,
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
                NameEn = city.NameEn,
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

    public async Task<IEnumerable<CityDto>> GetCitiesByIdsAsync(IEnumerable<Guid> cityIds, bool includeWeather = true)
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

        // 并行填充数据（不包含天气数据以提升性能）
        var moderatorTask = EnrichCitiesWithModeratorInfoAsync(cityDtos);
        var ratingsAndCostsTask = EnrichCitiesWithRatingsAndCostsAsync(cityDtos);
        var meetupAndCoworkingTask = EnrichCitiesWithMeetupAndCoworkingCountsAsync(cityDtos);

        // 等待所有任务完成
        await Task.WhenAll(
            moderatorTask.ContinueWith(_ => { }),
            ratingsAndCostsTask.ContinueWith(_ => { }),
            meetupAndCoworkingTask.ContinueWith(_ => { })
        );

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

            // 失效所有城市相关缓存，确保下次获取时能看到新版主
            InvalidateAllCityCaches(dto.CityId);
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
                    // 失效所有城市相关缓存
                    InvalidateAllCityCaches(dto.CityId);
                    // 发布城市更新事件，同步到 Elasticsearch
                    await PublishCityUpdatedMessageAsync(city, ["moderator"]);
                    // 通过 SignalR 广播版主变更
                    await PublishCityModeratorUpdatedMessageAsync(city, "assigned", dto.UserId.ToString());
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

            // 失效所有城市相关缓存，确保下次获取时能看到新版主
            InvalidateAllCityCaches(dto.CityId);
            // 发布城市更新事件，同步到 Elasticsearch
            await PublishCityUpdatedMessageAsync(city, ["moderator"]);
            // 通过 SignalR 广播版主变更
            await PublishCityModeratorUpdatedMessageAsync(city, "assigned", dto.UserId.ToString());
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
            
            // 🆕 批量获取城市评论数量（去重后的用户数）
            var reviewCounts = await _ratingRepository.GetCityReviewCountsBatchAsync(cityIds);

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
                
                // 填充 ReviewCount
                city.ReviewCount = reviewCounts.GetValueOrDefault(city.Id);

                _logger.LogDebug("📊 城市 {CityName}({CityId}): OverallScore={OverallScore}, AverageCost={AverageCost}, ReviewCount={ReviewCount}",
                    city.Name, city.Id, city.OverallScore, city.AverageCost, city.ReviewCount);
            }

            _logger.LogInformation("💰 批量填充评分和花费信息完成: {Count} 个城市, 总评分: {ScoreCount} 个, 费用: {CostCount} 个, 评论: {ReviewCount} 个",
                cities.Count, overallScores.Count, averageCosts.Count, reviewCounts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量填充评分和花费信息失败");
        }
    }
    
    /// <summary>
    /// 批量填充城市的 Meetup 和 Coworking 数量
    /// </summary>
    private async Task EnrichCitiesWithMeetupAndCoworkingCountsAsync(List<CityDto> cities)
    {
        if (cities.Count == 0) return;

        _logger.LogInformation("🔧 开始批量填充 Meetup 和 Coworking 数量: {Count} 个城市", cities.Count);

        try
        {
            var cityIds = cities.Select(c => c.Id).ToList();

            // 并行获取 Meetup 和 Coworking 数量
            var meetupCountsTask = GetMeetupCountsFromEventServiceAsync(cityIds);
            var coworkingCountsTask = GetCoworkingCountsFromCoworkingServiceAsync(cityIds);

            await Task.WhenAll(meetupCountsTask, coworkingCountsTask);

            var meetupCounts = await meetupCountsTask;
            var coworkingCounts = await coworkingCountsTask;

            // 填充数据
            foreach (var city in cities)
            {
                city.MeetupCount = meetupCounts.GetValueOrDefault(city.Id);
                city.CoworkingCount = coworkingCounts.GetValueOrDefault(city.Id);
            }

            _logger.LogInformation("✅ 批量填充 Meetup 和 Coworking 数量完成: Meetup={MeetupCount} 个城市, Coworking={CoworkingCount} 个城市",
                meetupCounts.Count, coworkingCounts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量填充 Meetup 和 Coworking 数量失败");
        }
    }

    /// <summary>
    /// 从 EventService 批量获取城市 Meetup 数量
    /// </summary>
    private async Task<Dictionary<Guid, int>> GetMeetupCountsFromEventServiceAsync(List<Guid> cityIds)
    {
        var counts = new Dictionary<Guid, int>();

        if (cityIds.Count == 0) return counts;

        try
        {
            _logger.LogDebug("🔍 通过 EventService 批量获取城市 Meetup 数量: {Count} 个城市", cityIds.Count);

            // 调用 EventService 的批量获取接口
            var cityIdStrings = cityIds.Select(id => id.ToString()).ToList();
            var client = _httpClientFactory.CreateClient("event-service");
            var resp = await client.PostAsJsonAsync("api/v1/events/cities/counts", cityIdStrings);
            resp.EnsureSuccessStatusCode();
            var response = await resp.Content.ReadFromJsonAsync<BatchCountResponse>();

            if (response?.Counts != null)
            {
                foreach (var item in response.Counts)
                {
                    if (Guid.TryParse(item.CityId, out var cityId))
                    {
                        counts[cityId] = item.Count;
                    }
                }

                _logger.LogInformation("✅ 成功获取城市 Meetup 数量: {Count} 个城市", counts.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 从 EventService 获取 Meetup 数量失败");
        }

        return counts;
    }

    /// <summary>
    /// 从 CoworkingService 批量获取城市 Coworking 数量
    /// </summary>
    private async Task<Dictionary<Guid, int>> GetCoworkingCountsFromCoworkingServiceAsync(List<Guid> cityIds)
    {
        var counts = new Dictionary<Guid, int>();

        if (cityIds.Count == 0) return counts;

        try
        {
            _logger.LogDebug("🔍 通过 CoworkingService 批量获取城市 Coworking 数量: {Count} 个城市", cityIds.Count);

            // 调用 CoworkingService 的批量获取接口
            var cityIdStrings = cityIds.Select(id => id.ToString()).ToList();
            var client = _httpClientFactory.CreateClient("coworking-service");
            var resp = await client.PostAsJsonAsync("api/v1/coworking/cities/counts", cityIdStrings);
            resp.EnsureSuccessStatusCode();
            var response = await resp.Content.ReadFromJsonAsync<BatchCountResponse>();

            if (response?.Counts != null)
            {
                foreach (var item in response.Counts)
                {
                    if (Guid.TryParse(item.CityId, out var cityId))
                    {
                        counts[cityId] = item.Count;
                    }
                }

                _logger.LogInformation("✅ 成功获取城市 Coworking 数量: {Count} 个城市", counts.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 从 CoworkingService 获取 Coworking 数量失败");
        }

        return counts;
    }

    /// <summary>
    /// 批量获取数量响应模型
    /// </summary>
    private class BatchCountResponse
    {
        public List<CityCountItem> Counts { get; set; } = new();
    }

    /// <summary>
    /// 城市数量项模型
    /// </summary>
    private class CityCountItem
    {
        public string CityId { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// 通过 CacheService 批量获取城市总评分 (Service Invocation)
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
            var cacheClient = _httpClientFactory.CreateClient("cache-service");
            var cacheResp = await cacheClient.PostAsJsonAsync("api/v1/cache/scores/city/batch", cityIdStrings);
            cacheResp.EnsureSuccessStatusCode();
            var response = await cacheResp.Content.ReadFromJsonAsync<BatchScoreResponse>();

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
    /// 通过 CacheService 批量获取城市平均费用 (Service Invocation)
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
            var cacheClient = _httpClientFactory.CreateClient("cache-service");
            var cacheResp = await cacheClient.PostAsJsonAsync("api/v1/cache/costs/city/batch", cityIdStrings);
            cacheResp.EnsureSuccessStatusCode();
            var response = await cacheResp.Content.ReadFromJsonAsync<BatchCostResponse>();

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

                // 通过缓存或 HttpClient 获取用户信息
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

            // 🚀 优化：并行获取用户信息（使用缓存）
            var userInfoMap = await GetUsersInfoBatchWithCacheAsync(userIds);

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
    /// 批量获取用户信息（带缓存和并行请求）
    /// </summary>
    private async Task<Dictionary<Guid, SimpleUserDto>> GetUsersInfoBatchWithCacheAsync(List<Guid> userIds)
    {
        var result = new Dictionary<Guid, SimpleUserDto>();

        if (userIds.Count == 0) return result;

        var uncachedUserIds = new List<Guid>();

        // 首先检查缓存
        foreach (var userId in userIds)
        {
            var cacheKey = $"user_info:{userId}";
            if (_cache.TryGetValue<SimpleUserDto>(cacheKey, out var cachedUser) && cachedUser != null)
            {
                result[userId] = cachedUser;
            }
            else
            {
                uncachedUserIds.Add(userId);
            }
        }

        _logger.LogDebug("🔍 批量获取用户信息: 缓存命中={CacheHit}, 需要查询={NeedFetch}",
            result.Count, uncachedUserIds.Count);

        // 并行获取未缓存的用户信息
        if (uncachedUserIds.Count > 0)
        {
            var tasks = uncachedUserIds.Select(async userId =>
            {
                var userInfo = await GetUserInfoWithCacheAsync(userId);
                return (userId, userInfo);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (userId, userInfo) in results)
            {
                if (userInfo != null)
                {
                    result[userId] = userInfo;
                }
            }
        }

        return result;
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

        // 缓存未命中，调用 UserService（带重试）
        const int maxRetries = 2;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
            try
            {
                var userClient = _httpClientFactory.CreateClient("user-service");
                var userResponse = await userClient.GetFromJsonAsync<ApiResponse<SimpleUserDto>>(
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
                // 失效所有城市相关缓存，确保所有 Pod 返回最新图片
                InvalidateAllCityCaches(cityId);
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
                // 失效所有城市相关缓存，确保下次获取时能看到新图片
                InvalidateAllCityCaches(cityId);
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

// 临时 DTO - 用于服务间调用
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
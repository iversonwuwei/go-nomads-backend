using System.Security.Claims;
using CityService.Application.DTOs;
using CityService.Application.Services;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using System.Net.Http.Json;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Postgrest.Attributes;
using Postgrest.Models;
using Supabase;

namespace CityService.API.Controllers;

/// <summary>
///     Cities API - RESTful endpoints for city management
/// </summary>
[ApiController]
[Route("api/v1/cities")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly ICityMatchingService _cityMatchingService;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDigitalNomadGuideService _guideService;
    private readonly INearbyCityService _nearbyCityService;
    private readonly ILogger<CitiesController> _logger;
    private readonly ICityModeratorRepository _moderatorRepository;
    private readonly Client _supabaseClient;

    public CitiesController(
        ICityService cityService,
        ICityMatchingService cityMatchingService,
        IDigitalNomadGuideService guideService,
        INearbyCityService nearbyCityService,
        ICityModeratorRepository moderatorRepository,
        IHttpClientFactory httpClientFactory,
        Client supabaseClient,
        ICurrentUserService currentUser,
        ILogger<CitiesController> logger)
    {
        _cityService = cityService;
        _cityMatchingService = cityMatchingService;
        _guideService = guideService;
        _nearbyCityService = nearbyCityService;
        _moderatorRepository = moderatorRepository;
        _httpClientFactory = httpClientFactory;
        _supabaseClient = supabaseClient;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    ///     Get all cities with pagination and optional search
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCities(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var userRole = _currentUser.GetUserRole();

            IEnumerable<CityDto> cities;
            int totalCount;

            // 如果有搜索参数,使用搜索接口(支持中英文搜索)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchDto = new CitySearchDto
                {
                    Name = search,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                cities = await _cityService.SearchCitiesAsync(searchDto, userId, userRole);
                totalCount = cities.Count(); // 搜索结果的总数
            }
            else
            {
                // 并行执行数据查询和计数查询以提升性能
                var citiesTask = _cityService.GetAllCitiesAsync(pageNumber, pageSize, userId, userRole);
                var countTask = _cityService.GetTotalCountAsync();
                await Task.WhenAll(citiesTask, countTask);
                cities = await citiesTask;
                totalCount = await countTask;
            }

            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Number", pageNumber.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            return Ok(new ApiResponse<PaginatedResponse<CityDto>>
            {
                Success = true,
                Message = "Cities retrieved successfully",
                Data = new PaginatedResponse<CityDto>
                {
                    Items = cities.ToList(),
                    TotalCount = totalCount,
                    Page = pageNumber,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities");
            return StatusCode(500, new ApiResponse<PaginatedResponse<CityDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving cities",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get city list (lightweight version without weather data)
    ///     Optimized for city list page performance
    /// </summary>
    [HttpGet("list")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityListItemDto>>>> GetCityList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var userRole = _currentUser.GetUserRole();

            // 并行执行数据查询和计数查询以提升性能
            var citiesTask = _cityService.GetCityListAsync(pageNumber, pageSize, search, userId, userRole);
            var countTask = _cityService.GetTotalCountAsync();
            await Task.WhenAll(citiesTask, countTask);
            var cities = await citiesTask;
            var totalCount = await countTask;

            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Number", pageNumber.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            return Ok(new ApiResponse<PaginatedResponse<CityListItemDto>>
            {
                Success = true,
                Message = "City list retrieved successfully",
                Data = new PaginatedResponse<CityListItemDto>
                {
                    Items = cities.ToList(),
                    TotalCount = totalCount,
                    Page = pageNumber,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city list");
            return StatusCode(500, new ApiResponse<PaginatedResponse<CityListItemDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving city list",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get city list (basic version without aggregated data)
    ///     Optimized for fast first screen loading, aggregated data loaded async
    /// </summary>
    [HttpGet("list-basic")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityListItemDto>>>> GetCityListBasic(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? region = null)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var userRole = _currentUser.GetUserRole();

            // 并行执行数据查询和计数查询以提升性能
            var citiesTask = _cityService.GetCityListBasicAsync(pageNumber, pageSize, search, region, userId, userRole);
            var countTask = _cityService.GetTotalCountAsync();
            await Task.WhenAll(citiesTask, countTask);
            var cities = await citiesTask;
            var totalCount = await countTask;

            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Number", pageNumber.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());

            return Ok(new ApiResponse<PaginatedResponse<CityListItemDto>>
            {
                Success = true,
                Message = "City list (basic) retrieved successfully",
                Data = new PaginatedResponse<CityListItemDto>
                {
                    Items = cities.ToList(),
                    TotalCount = totalCount,
                    Page = pageNumber,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting basic city list");
            return StatusCode(500, new ApiResponse<PaginatedResponse<CityListItemDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving basic city list",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get available region tabs for city filtering
    ///     Returns distinct regions with city counts, controlled by backend
    /// </summary>
    [HttpGet("region-tabs")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CityRegionTabDto>>>> GetRegionTabs()
    {
        try
        {
            var tabs = await _cityService.GetRegionTabsAsync();
            return Ok(new ApiResponse<List<CityRegionTabDto>>
            {
                Success = true,
                Message = "Region tabs retrieved successfully",
                Data = tabs.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting region tabs");
            return StatusCode(500, new ApiResponse<List<CityRegionTabDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving region tabs",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get city counts batch (MeetupCount, CoworkingCount, ReviewCount, AverageCost)
    ///     For async loading of aggregated data
    /// </summary>
    [HttpPost("counts")]
    [AllowAnonymous]

    public async Task<ActionResult<ApiResponse<Dictionary<Guid, CityCountsDto>>>> GetCityCountsBatch([FromBody] CityBatchRequest request)
    {
        if (request.CityIds == null || request.CityIds.Count == 0)
            return BadRequest(new ApiResponse<Dictionary<Guid, CityCountsDto>>
            {
                Success = false,
                Message = "CityIds cannot be empty"
            });

        if (request.CityIds.Count > 100)
            return BadRequest(new ApiResponse<Dictionary<Guid, CityCountsDto>>
            {
                Success = false,
                Message = "Up to 100 cityIds are allowed per request"
            });

        try
        {
            var counts = await _cityService.GetCityCountsBatchAsync(request.CityIds);
            return Ok(new ApiResponse<Dictionary<Guid, CityCountsDto>>
            {
                Success = true,
                Message = "City counts retrieved successfully",
                Data = counts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city counts batch");
            return StatusCode(500, new ApiResponse<Dictionary<Guid, CityCountsDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving city counts",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     批量根据 ID 获取城市信息
    /// </summary>
    [HttpPost("lookup")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CityDto>>>> GetCitiesByIds([FromBody] CityBatchRequest request)
    {
        if (request.CityIds == null || request.CityIds.Count == 0)
            return BadRequest(new ApiResponse<List<CityDto>>
            {
                Success = false,
                Message = "CityIds cannot be empty"
            });

        if (request.CityIds.Count > 100)
            return BadRequest(new ApiResponse<List<CityDto>>
            {
                Success = false,
                Message = "Up to 100 cityIds are allowed per request"
            });

        try
        {
            var cities = await _cityService.GetCitiesByIdsAsync(request.CityIds);
            return Ok(new ApiResponse<List<CityDto>>
            {
                Success = true,
                Message = "Cities retrieved successfully",
                Data = cities.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing city lookup");
            return StatusCode(500, new ApiResponse<List<CityDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving cities",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     城市匹配 - 根据经纬度和城市名称匹配现有城市
    ///     POST /api/v1/cities/match
    /// </summary>
    [HttpPost("match")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CityMatchResult>>> MatchCity([FromBody] CityMatchRequest request)
    {
        try
        {
            _logger.LogInformation(
                "📍 [MatchCity] 接收城市匹配请求: Lat={Lat}, Lng={Lng}, CityName={CityName}, CityNameEn={CityNameEn}",
                request.Latitude, request.Longitude, request.CityName, request.CityNameEn);

            var result = await _cityMatchingService.MatchCityAsync(request);

            if (result.IsMatched)
            {
                _logger.LogInformation(
                    "✅ [MatchCity] 匹配成功: CityId={CityId}, Method={Method}, Confidence={Confidence}",
                    result.CityId, result.MatchMethod, result.Confidence);
            }
            else
            {
                _logger.LogWarning("⚠️ [MatchCity] 匹配失败: {ErrorMessage}", result.ErrorMessage);
            }

            return Ok(new ApiResponse<CityMatchResult>
            {
                Success = result.IsMatched,
                Message = result.IsMatched ? "城市匹配成功" : "未找到匹配的城市",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [MatchCity] 城市匹配出错");
            return StatusCode(500, new ApiResponse<CityMatchResult>
            {
                Success = false,
                Message = "城市匹配过程中发生错误",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get recommended cities
    ///     GET /api/v1/cities/recommended?count=10
    /// </summary>
    [HttpGet("recommended")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<CityDto>>>> GetRecommendedCities([FromQuery] int count = 10)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var cities = await _cityService.GetRecommendedCitiesAsync(count, userId);
            return Ok(new ApiResponse<IEnumerable<CityDto>>
            {
                Success = true,
                Message = "Recommended cities retrieved successfully",
                Data = cities.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended cities");
            return StatusCode(500, new ApiResponse<IEnumerable<CityDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving recommended cities",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get popular cities
    ///     GET /api/v1/cities/popular?limit=10
    /// </summary>
    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<CityDto>>>> GetPopularCities([FromQuery] int limit = 10)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var cities = await _cityService.GetPopularCitiesAsync(limit, userId);
            return Ok(new ApiResponse<IEnumerable<CityDto>>
            {
                Success = true,
                Message = "Popular cities retrieved successfully",
                Data = cities.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular cities");
            return StatusCode(500, new ApiResponse<IEnumerable<CityDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving popular cities",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get cities by country ID (Query parameter approach)
    ///     GET /api/v1/cities?countryId={guid}
    /// </summary>
    [HttpGet("by-country/{countryId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<CitySummaryDto>>>> GetCitiesByCountryId(Guid countryId)
    {
        try
        {
            var cities = await _cityService.GetCitiesByCountryIdAsync(countryId);
            return Ok(new ApiResponse<IEnumerable<CitySummaryDto>>
            {
                Success = true,
                Message = "Cities retrieved successfully",
                Data = cities.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities by country ID {CountryId}", countryId);
            return StatusCode(500, new ApiResponse<IEnumerable<CitySummaryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving cities by country",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get cities grouped by country
    ///     GET /api/v1/cities/grouped-by-country
    /// </summary>
    [HttpGet("grouped-by-country")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<CountryCitiesDto>>>> GetCitiesGroupedByCountry()
    {
        try
        {
            var groupedCities = await _cityService.GetCitiesGroupedByCountryAsync();
            return Ok(new ApiResponse<IEnumerable<CountryCitiesDto>>
            {
                Success = true,
                Message = "Cities grouped by country retrieved successfully",
                Data = groupedCities.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities grouped by country");
            return StatusCode(500, new ApiResponse<IEnumerable<CountryCitiesDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving cities grouped by country",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get all countries (as a related resource)
    ///     GET /api/v1/cities/countries
    ///     Note: Consider moving to separate /api/v1/countries endpoint
    /// </summary>
    [HttpGet("countries")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<CountryDto>>>> GetAllCountries()
    {
        try
        {
            var countries = await _cityService.GetAllCountriesAsync();
            return Ok(new ApiResponse<IEnumerable<CountryDto>>
            {
                Success = true,
                Message = "Countries retrieved successfully",
                Data = countries.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all countries");
            return StatusCode(500, new ApiResponse<IEnumerable<CountryDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving countries",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Search cities with filters
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<CityDto>>>> SearchCities([FromQuery] CitySearchDto searchDto)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var userRole = _currentUser.GetUserRole();
            var cities = await _cityService.SearchCitiesAsync(searchDto, userId, userRole);
            return Ok(new ApiResponse<IEnumerable<CityDto>>
            {
                Success = true,
                Message = "Cities search completed successfully",
                Data = cities.ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cities");
            return StatusCode(500, new ApiResponse<IEnumerable<CityDto>>
            {
                Success = false,
                Message = "An error occurred while searching cities",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get city by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CityDto>>> GetCity(Guid id)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var userRole = _currentUser.GetUserRole();
            var city = await _cityService.GetCityByIdAsync(id, userId, userRole);
            if (city == null)
                return NotFound(new ApiResponse<CityDto>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });

            return Ok(new ApiResponse<CityDto>
            {
                Success = true,
                Message = "City retrieved successfully",
                Data = city
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city {CityId}", id);
            return StatusCode(500, new ApiResponse<CityDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the city",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get city moderator summary (lightweight)
    /// </summary>
    [HttpGet("{id:guid}/moderator-summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CityModeratorSummaryDto>>> GetCityModeratorSummary(Guid id)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            var userRole = _currentUser.GetUserRole();
            var summary = await _cityService.GetCityModeratorSummaryAsync(id, userId, userRole);
            if (summary == null)
                return NotFound(new ApiResponse<CityModeratorSummaryDto>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });

            return Ok(new ApiResponse<CityModeratorSummaryDto>
            {
                Success = true,
                Message = "City moderator summary retrieved successfully",
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city moderator summary {CityId}", id);
            return StatusCode(500, new ApiResponse<CityModeratorSummaryDto>
            {
                Success = false,
                Message = "An error occurred while retrieving city moderator summary",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get city statistics
    /// </summary>
    [HttpGet("{id:guid}/statistics")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CityStatisticsDto>>> GetCityStatistics(Guid id)
    {
        try
        {
            var statistics = await _cityService.GetCityStatisticsAsync(id);
            if (statistics == null)
                return NotFound(new ApiResponse<CityStatisticsDto>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });

            return Ok(new ApiResponse<CityStatisticsDto>
            {
                Success = true,
                Message = "City statistics retrieved successfully",
                Data = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city statistics {CityId}", id);
            return StatusCode(500, new ApiResponse<CityStatisticsDto>
            {
                Success = false,
                Message = "An error occurred while retrieving city statistics",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get current weather for a city
    ///     GET /api/v1/cities/{id}/weather
    /// </summary>
    [HttpGet("{id:guid}/weather")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<WeatherDto>>> GetCityWeather(
        Guid id,
        [FromQuery] bool includeForecast = false,
        [FromQuery] int days = 7)
    {
        try
        {
            var weather = await _cityService.GetCityWeatherAsync(id, includeForecast, days);
            if (weather == null)
                return NotFound(new ApiResponse<WeatherDto>
                {
                    Success = false,
                    Message = "Weather data is not available for this city",
                    Errors = new List<string> { "Weather data not available" }
                });

            return Ok(new ApiResponse<WeatherDto>
            {
                Success = true,
                Message = includeForecast
                    ? "Weather with forecast retrieved successfully"
                    : "Weather retrieved successfully",
                Data = weather
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weather for city {CityId}", id);
            return StatusCode(500, new ApiResponse<WeatherDto>
            {
                Success = false,
                Message = "An error occurred while retrieving city weather",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get coworking space count for a city
    ///     GET /api/v1/cities/{id}/coworking-count
    /// </summary>
    [HttpGet("{id:guid}/coworking-count")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CoworkingCountDto>>> GetCityCoworkingCount(Guid id)
    {
        try
        {
            // 调用 CoworkingService 获取数量
            var cityIdStrings = new List<string> { id.ToString() };
            var client = _httpClientFactory.CreateClient("coworking-service");
            var resp = await client.PostAsJsonAsync("api/v1/coworking/cities/counts", cityIdStrings);
            resp.EnsureSuccessStatusCode();
            var response = await resp.Content.ReadFromJsonAsync<BatchCountResponse>();

            var count = response?.Counts?.FirstOrDefault(c => c.CityId == id.ToString())?.Count ?? 0;

            return Ok(new ApiResponse<CoworkingCountDto>
            {
                Success = true,
                Message = "Coworking count retrieved successfully",
                Data = new CoworkingCountDto { CityId = id.ToString(), Count = count }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coworking count for city {CityId}", id);
            return Ok(new ApiResponse<CoworkingCountDto>
            {
                Success = true,
                Message = "Coworking count retrieved (default)",
                Data = new CoworkingCountDto { CityId = id.ToString(), Count = 0 }
            });
        }
    }

    /// <summary>
    ///     Create a new city (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CityDto>>> CreateCity([FromBody] CreateCityDto createCityDto)
    {
        try
        {
            var userId = _currentUser.GetUserId();
            var city = await _cityService.CreateCityAsync(createCityDto, userId);
            return CreatedAtAction(
                nameof(GetCity),
                new { id = city.Id },
                new ApiResponse<CityDto>
                {
                    Success = true,
                    Message = "City created successfully",
                    Data = city
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating city");
            return StatusCode(500, new ApiResponse<CityDto>
            {
                Success = false,
                Message = "An error occurred while creating the city",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Update a city (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CityDto>>> UpdateCity(Guid id, [FromBody] UpdateCityDto updateCityDto)
    {
        try
        {
            var userId = _currentUser.GetUserId();
            var city = await _cityService.UpdateCityAsync(id, updateCityDto, userId);
            if (city == null)
                return NotFound(new ApiResponse<CityDto>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });

            return Ok(new ApiResponse<CityDto>
            {
                Success = true,
                Message = "City updated successfully",
                Data = city
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating city {CityId}", id);
            return StatusCode(500, new ApiResponse<CityDto>
            {
                Success = false,
                Message = "An error occurred while updating the city",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Delete a city (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCity(Guid id)
    {
        try
        {
            // 检查管理员权限
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.Role != "admin")
            {
                return StatusCode(403, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "只有管理员可以删除城市",
                    Errors = new List<string> { "权限不足" }
                });
            }

            var userId = !string.IsNullOrEmpty(userContext.UserId) ? Guid.Parse(userContext.UserId) : (Guid?)null;
            var result = await _cityService.DeleteCityAsync(id, userId);
            if (!result)
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });

            _logger.LogInformation("✅ 管理员 {UserId} 成功删除城市 {CityId}", userContext.UserId, id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "City deleted successfully",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting city {CityId}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "An error occurred while deleting the city",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     Get cities with coworking count for coworking home page
    ///     专门为 coworking_home 页面提供城市列表和每个城市的 coworking 数量
    ///     只返回有 coworking 空间的城市
    ///     优化：使用数据库 RPC 函数，一次查询获取所有数据
    /// </summary>
    [HttpGet("with-coworking-count")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCitiesWithCoworkingCount(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 使用 RPC 函数一次查询获取城市信息和 coworking 数量
            var result = await GetCitiesWithCoworkingFromDbAsync(page, pageSize);

            if (result.Cities.Count == 0)
            {
                return Ok(new ApiResponse<PaginatedResponse<CityDto>>
                {
                    Success = true,
                    Message = page > 1 ? "已无更多数据" : "暂无有 Coworking 空间的城市",
                    Data = new PaginatedResponse<CityDto>
                    {
                        Items = new List<CityDto>(),
                        TotalCount = result.TotalCount,
                        Page = page,
                        PageSize = pageSize
                    }
                });
            }

            _logger.LogInformation(
                "获取城市列表(含Coworking数量)成功: {CityCount} 个城市, 第 {Page} 页, 共 {TotalCount} 个",
                result.Cities.Count,
                page,
                result.TotalCount);

            return Ok(new ApiResponse<PaginatedResponse<CityDto>>
            {
                Success = true,
                Message = "城市列表(含Coworking数量)获取成功",
                Data = new PaginatedResponse<CityDto>
                {
                    Items = result.Cities,
                    TotalCount = result.TotalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市列表(含Coworking数量)失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<CityDto>>
            {
                Success = false,
                Message = "获取城市列表失败，请稍后重试",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     使用数据库 RPC 函数获取有 coworking 空间的城市列表
    ///     一次查询获取城市信息、coworking 数量和总数
    /// </summary>
    private async Task<(List<CityDto> Cities, int TotalCount)> GetCitiesWithCoworkingFromDbAsync(int page, int pageSize)
    {
        try
        {
            // 调用数据库 RPC 函数
            var response = await _supabaseClient.Rpc(
                "get_cities_with_coworking_details",
                new { p_page = page, p_page_size = pageSize });

            if (response.Content == null)
            {
                _logger.LogWarning("RPC 函数返回空结果");
                return (new List<CityDto>(), 0);
            }

            var results = System.Text.Json.JsonSerializer.Deserialize<List<CityWithCoworkingResult>>(
                response.Content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (results == null || results.Count == 0)
            {
                return (new List<CityDto>(), 0);
            }

            var totalCount = (int)(results.FirstOrDefault()?.TotalCount ?? 0);
            var cities = results.Select(r => new CityDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Country = r.Country ?? string.Empty,
                Region = r.Region,
                Description = r.Description,
                ImageUrl = r.ImageUrl,
                OverallScore = r.OverallScore,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                CoworkingCount = (int)(r.CoworkingCount ?? 0)
            }).ToList();

            return (cities, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RPC 函数调用失败，回退到传统查询方式");
            // 回退到传统方式
            return await GetCitiesWithCoworkingFallbackAsync(page, pageSize);
        }
    }

    /// <summary>
    ///     回退方法：使用传统方式获取有 coworking 空间的城市列表
    /// </summary>
    private async Task<(List<CityDto> Cities, int TotalCount)> GetCitiesWithCoworkingFallbackAsync(int page, int pageSize)
    {
        // 查询 coworking_spaces 表统计
        var coworkingResponse = await _supabaseClient
            .From<CoworkingSpaceSimpleDto>()
            .Where(x => x.IsActive == true)
            .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
            .Get();

        var coworkingCountByCity = coworkingResponse.Models
            .Where(x => x.CityId.HasValue)
            .GroupBy(x => x.CityId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        if (coworkingCountByCity.Count == 0)
        {
            return (new List<CityDto>(), 0);
        }

        var cityIdsWithCoworking = coworkingCountByCity.Keys.ToList();
        var totalCount = cityIdsWithCoworking.Count;

        var allCities = (await _cityService.GetCitiesByIdsAsync(cityIdsWithCoworking))
            .OrderByDescending(c => c.OverallScore ?? 0)
            .ToList();

        var skip = (page - 1) * pageSize;
        var cityList = allCities.Skip(skip).Take(pageSize).ToList();

        foreach (var city in cityList)
        {
            city.CoworkingCount = coworkingCountByCity.TryGetValue(city.Id, out var count) ? count : 0;
        }

        return (cityList, totalCount);
    }

    /// <summary>
    ///     RPC 函数返回结果 DTO
    /// </summary>
    private class CityWithCoworkingResult
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? Description { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("overall_score")]
        public decimal? OverallScore { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("coworking_count")]
        public long? CoworkingCount { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("total_count")]
        public long? TotalCount { get; set; }
    }

    /// <summary>
    ///     Coworking 空间简单 DTO（用于回退查询）
    /// </summary>
    [Table("coworking_spaces")]
    private class CoworkingSpaceSimpleDto : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("city_id")]
        public Guid? CityId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }

    /// <summary>
    ///     Get city IDs that have coworking spaces (lightweight API for fast loading)
    ///     获取有 coworking 空间的城市ID列表（轻量级接口，用于快速加载）
    ///     优化：使用数据库 RPC 函数
    /// </summary>
    [HttpGet("with-coworking-ids")]
    public async Task<ActionResult<ApiResponse<List<Guid>>>> GetCityIdsWithCoworking()
    {
        try
        {
            var cityIds = await GetCityIdsWithCoworkingFromDbAsync();

            _logger.LogInformation("获取有 Coworking 空间的城市ID列表: {Count} 个", cityIds.Count);

            return Ok(new ApiResponse<List<Guid>>
            {
                Success = true,
                Message = $"获取成功，共 {cityIds.Count} 个城市有 Coworking 空间",
                Data = cityIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取有 Coworking 空间的城市ID列表失败");
            return StatusCode(500, new ApiResponse<List<Guid>>
            {
                Success = false,
                Message = "获取失败，请稍后重试",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     使用数据库 RPC 函数获取有 coworking 空间的城市ID列表
    /// </summary>
    private async Task<List<Guid>> GetCityIdsWithCoworkingFromDbAsync()
    {
        try
        {
            var response = await _supabaseClient.Rpc(
                "get_cities_with_coworking_count",
                null);

            if (response.Content == null)
            {
                return new List<Guid>();
            }

            var results = System.Text.Json.JsonSerializer.Deserialize<List<CoworkingCountResult>>(
                response.Content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return results?.Select(r => r.CityId).ToList() ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RPC 函数调用失败，回退到传统方式");
            // 回退到传统方式
            var response = await _supabaseClient
                .From<CoworkingSpaceSimpleDto>()
                .Where(x => x.IsActive == true)
                .Filter("is_deleted", Postgrest.Constants.Operator.NotEqual, "true")
                .Get();

            return response.Models
                .Where(x => x.CityId.HasValue)
                .Select(x => x.CityId!.Value)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>
    ///     Coworking 数量统计结果 DTO
    /// </summary>
    private class CoworkingCountResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("city_id")]
        public Guid CityId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("coworking_count")]
        public long CoworkingCount { get; set; }
    }

    /// <summary>
    ///     Get cities that have coworking spaces (for coworking home page)
    ///     获取有共享办公空间的城市列表（专门为 coworking_home 页面优化）
    ///     优化：使用数据库 RPC 函数，一次查询获取所有数据
    /// </summary>
    [HttpGet("with-coworking")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCitiesWithCoworking(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 使用 RPC 函数一次查询获取城市信息和 coworking 数量
            var result = await GetCitiesWithCoworkingFromDbAsync(page, pageSize);

            if (result.Cities.Count == 0)
            {
                return Ok(new ApiResponse<PaginatedResponse<CityDto>>
                {
                    Success = true,
                    Message = page > 1 ? "已无更多数据" : "暂无有 Coworking 空间的城市",
                    Data = new PaginatedResponse<CityDto>
                    {
                        Items = new List<CityDto>(),
                        TotalCount = result.TotalCount,
                        Page = page,
                        PageSize = pageSize
                    }
                });
            }

            _logger.LogInformation(
                "获取有 Coworking 空间的城市列表成功: {CityCount} 个城市, 第 {Page} 页, 共 {TotalCount} 个",
                result.Cities.Count,
                page,
                result.TotalCount);

            return Ok(new ApiResponse<PaginatedResponse<CityDto>>
            {
                Success = true,
                Message = "城市列表获取成功",
                Data = new PaginatedResponse<CityDto>
                {
                    Items = result.Cities,
                    TotalCount = result.TotalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取有 Coworking 空间的城市列表失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<CityDto>>
            {
                Success = false,
                Message = "获取城市列表失败，请稍后重试",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #region Digital Nomad Guide APIs

    /// <summary>
    ///     Get digital nomad guide for a city
    /// </summary>
    /// <param name="cityId">City ID</param>
    /// <returns>Digital nomad guide or null if not found</returns>
    [HttpGet("{cityId}/guide")]
    [ProducesResponseType(typeof(ApiResponse<DigitalNomadGuideDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DigitalNomadGuideDto>>> GetDigitalNomadGuide(string cityId)
    {
        try
        {
            _logger.LogInformation("📖 获取数字游民指南: cityId={CityId}", cityId);

            var guide = await _guideService.GetByCityIdAsync(cityId);

            if (guide == null)
            {
                // 没有找到指南是正常的业务状态，返回 200 + null data
                _logger.LogInformation("📭 未找到指南: cityId={CityId}，这是正常状态", cityId);
                return Ok(new ApiResponse<DigitalNomadGuideDto>
                {
                    Success = true,
                    Message = "No guide found for this city yet",
                    Data = null
                });
            }

            var guideDto = MapToDto(guide);

            _logger.LogInformation("✅ 返回指南: guideId={GuideId}, cityName={CityName}", guide.Id, guide.CityName);

            return Ok(new ApiResponse<DigitalNomadGuideDto>
            {
                Success = true,
                Message = "Guide retrieved successfully",
                Data = guideDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取指南失败: cityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<DigitalNomadGuideDto>
            {
                Success = false,
                Message = $"Failed to retrieve guide: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    ///     Save or update digital nomad guide for a city
    /// </summary>
    /// <param name="cityId">City ID</param>
    /// <param name="request">Guide data</param>
    /// <returns>Saved guide</returns>
    [HttpPost("{cityId}/guide")]
    [ProducesResponseType(typeof(ApiResponse<DigitalNomadGuideDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DigitalNomadGuideDto>>> SaveDigitalNomadGuide(
        string cityId,
        [FromBody] SaveDigitalNomadGuideRequest request)
    {
        try
        {
            _logger.LogInformation("💾 保存数字游民指南: cityId={CityId}, cityName={CityName}",
                cityId, request.CityName);

            // 验证cityId匹配
            if (request.CityId != cityId)
                return BadRequest(new ApiResponse<DigitalNomadGuideDto>
                {
                    Success = false,
                    Message = "City ID in URL does not match request body",
                    Data = null
                });

            // 映射到实体
            var guide = new DigitalNomadGuide
            {
                CityId = request.CityId,
                CityName = request.CityName,
                Overview = request.Overview,
                VisaInfo = new VisaInfo
                {
                    Type = request.VisaInfo.Type,
                    Duration = request.VisaInfo.Duration,
                    Requirements = request.VisaInfo.Requirements,
                    Cost = request.VisaInfo.Cost,
                    Process = request.VisaInfo.Process
                },
                BestAreas = request.BestAreas.Select(a => new BestArea
                {
                    Name = a.Name,
                    Description = a.Description,
                    EntertainmentScore = a.EntertainmentScore,
                    EntertainmentDescription = a.EntertainmentDescription,
                    TourismScore = a.TourismScore,
                    TourismDescription = a.TourismDescription,
                    EconomyScore = a.EconomyScore,
                    EconomyDescription = a.EconomyDescription,
                    CultureScore = a.CultureScore,
                    CultureDescription = a.CultureDescription
                }).ToList(),
                WorkspaceRecommendations = request.WorkspaceRecommendations,
                Tips = request.Tips,
                EssentialInfo = request.EssentialInfo
            };

            // 保存到数据库
            var savedGuide = await _guideService.SaveAsync(guide);
            var guideDto = MapToDto(savedGuide);

            _logger.LogInformation("✅ 指南保存成功: guideId={GuideId}", savedGuide.Id);

            return Ok(new ApiResponse<DigitalNomadGuideDto>
            {
                Success = true,
                Message = "Guide saved successfully",
                Data = guideDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 保存指南失败: cityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<DigitalNomadGuideDto>
            {
                Success = false,
                Message = $"Failed to save guide: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    ///     Map entity to DTO
    /// </summary>
    private DigitalNomadGuideDto MapToDto(DigitalNomadGuide guide)
    {
        return new DigitalNomadGuideDto
        {
            Id = guide.Id,
            CityId = guide.CityId,
            CityName = guide.CityName,
            Overview = guide.Overview,
            VisaInfo = new VisaInfoDto
            {
                Type = guide.VisaInfo.Type,
                Duration = guide.VisaInfo.Duration,
                Requirements = guide.VisaInfo.Requirements,
                Cost = guide.VisaInfo.Cost,
                Process = guide.VisaInfo.Process
            },
            BestAreas = guide.BestAreas.Select(a => new BestAreaDto
            {
                Name = a.Name,
                Description = a.Description,
                EntertainmentScore = a.EntertainmentScore,
                EntertainmentDescription = a.EntertainmentDescription,
                TourismScore = a.TourismScore,
                TourismDescription = a.TourismDescription,
                EconomyScore = a.EconomyScore,
                EconomyDescription = a.EconomyDescription,
                CultureScore = a.CultureScore,
                CultureDescription = a.CultureDescription
            }).ToList(),
            WorkspaceRecommendations = guide.WorkspaceRecommendations,
            Tips = guide.Tips,
            EssentialInfo = guide.EssentialInfo,
            CreatedAt = guide.CreatedAt,
            UpdatedAt = guide.UpdatedAt
        };
    }

    #endregion

    #region 版主管理

    // ⚠️ 已废弃: 申请成为版主的功能已迁移到 ModeratorApplicationController
    // 现在使用完整的申请审核流程,详见 ModeratorApplicationController.Apply

    /// <summary>
    ///     指定城市版主 (仅管理员)
    /// </summary>
    [HttpPost("moderator/assign")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignModerator([FromBody] AssignModeratorDto dto)
    {
        try
        {
            // 检查用户角色 (从 Gateway 传递的 UserContext 获取)
            if (!_currentUser.IsAdmin())
            {
                return StatusCode(403, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "无权限，仅管理员可以指定版主",
                    Data = false
                });
            }

            var result = await _cityService.AssignModeratorAsync(dto);

            if (result)
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "版主指定成功",
                    Data = true
                });

            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "指定失败，城市不存在",
                Data = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "指定城市版主失败: CityId={CityId}, UserId={UserId}",
                dto.CityId, dto.UserId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = $"指定失败: {ex.Message}",
                Data = false
            });
        }
    }

    #endregion

    #region 城市版主管理（多版主支持）

    /// <summary>
    ///     获取城市的所有版主列表
    /// </summary>
    [HttpGet("{id}/moderators")]
    public async Task<ActionResult<ApiResponse<List<CityModeratorDto>>>> GetCityModerators(Guid id)
    {
        try
        {
            _logger.LogInformation("📋 获取城市版主列表 - CityId: {CityId}", id);

            var moderators = await _moderatorRepository.GetByCityIdAsync(id);

            // 获取版主的用户信息
            var moderatorDtos = new List<CityModeratorDto>();
            foreach (var moderator in moderators)
                // TODO: 通过 UserService 获取用户详细信息
                // 目前先返回基本信息
                moderatorDtos.Add(new CityModeratorDto
                {
                    Id = moderator.Id,
                    CityId = moderator.CityId,
                    UserId = moderator.UserId,
                    User = new ModeratorUserDto
                    {
                        Id = moderator.UserId,
                        Name = "Loading...", // 后续通过 HttpClient 获取
                        Email = "",
                        Role = "moderator"
                    },
                    CanEditCity = moderator.CanEditCity,
                    CanManageCoworks = moderator.CanManageCoworks,
                    CanManageCosts = moderator.CanManageCosts,
                    CanManageVisas = moderator.CanManageVisas,
                    CanModerateChats = moderator.CanModerateChats,
                    AssignedBy = moderator.AssignedBy,
                    AssignedAt = moderator.AssignedAt,
                    IsActive = moderator.IsActive,
                    Notes = moderator.Notes,
                    CreatedAt = moderator.CreatedAt,
                    UpdatedAt = moderator.UpdatedAt
                });

            return Ok(new ApiResponse<List<CityModeratorDto>>
            {
                Success = true,
                Message = "版主列表获取成功",
                Data = moderatorDtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市版主列表失败 - CityId: {CityId}", id);
            return StatusCode(500, new ApiResponse<List<CityModeratorDto>>
            {
                Success = false,
                Message = "获取版主列表失败"
            });
        }
    }

    /// <summary>
    ///     添加城市版主（仅管理员）
    ///     自动为用户分配 moderator 角色
    /// </summary>
    [HttpPost("{id}/moderators")]
    public async Task<ActionResult<ApiResponse<CityModeratorDto>>> AddCityModerator(
        Guid id,
        [FromBody] AddCityModeratorDto dto)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);

        // Gateway 已完成 token 验证，这里只验证角色权限
        if (userContext?.Role != "admin")
            return StatusCode(403, new ApiResponse<CityModeratorDto>
            {
                Success = false,
                Message = "需要管理员权限"
            });

        try
        {
            _logger.LogInformation("➕ 添加城市版主 - CityId: {CityId}, UserId: {UserId}, AdminId: {AdminId}",
                id, dto.UserId, userContext.UserId);

            // 检查城市是否存在
            var city = await _cityService.GetCityByIdAsync(id);
            if (city == null)
                return NotFound(new ApiResponse<CityModeratorDto>
                {
                    Success = false,
                    Message = "城市不存在"
                });

            // 检查用户是否已经是版主
            var isExisting = await _moderatorRepository.IsModeratorAsync(id, dto.UserId);
            if (isExisting)
                return BadRequest(new ApiResponse<CityModeratorDto>
                {
                    Success = false,
                    Message = "该用户已经是此城市的版主"
                });

            // 步骤 1: 通过 UserService API 获取 moderator 角色
            _logger.LogInformation("🔍 通过 UserService API 获取 moderator 角色");
            var userServiceClient = _httpClientFactory.CreateClient("user-service");
            var roleResponse = await userServiceClient.GetFromJsonAsync<ApiResponse<SimpleRoleDto>>("api/v1/roles/by-name/moderator");

            if (roleResponse?.Success != true || roleResponse.Data == null)
            {
                _logger.LogError("❌ 获取 moderator 角色失败: {Message}",
                    roleResponse?.Message ?? "响应为空");
                return StatusCode(500, new ApiResponse<CityModeratorDto>
                {
                    Success = false,
                    Message = "系统配置错误: moderator 角色不存在，请联系管理员"
                });
            }

            var moderatorRoleId = roleResponse.Data.Id;
            _logger.LogInformation("✅ 成功获取 moderator 角色 - RoleId: {RoleId}, RoleName: {RoleName}",
                moderatorRoleId, roleResponse.Data.Name);

            // 步骤 2: 通过 UserService API 为用户分配 moderator 角色
            _logger.LogInformation("🔄 通过 UserService API 为用户分配 moderator 角色");
            var changeRoleRequest = new { roleId = moderatorRoleId };
            var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"api/v1/users/{dto.UserId}/role") { Content = JsonContent.Create(changeRoleRequest) };
            var patchResp = await userServiceClient.SendAsync(patchReq);
            patchResp.EnsureSuccessStatusCode();
            var changeRoleResponse = await patchResp.Content.ReadFromJsonAsync<ApiResponse<SimpleUserDto>>();

            if (changeRoleResponse?.Success != true)
            {
                _logger.LogError("❌ 为用户分配 moderator 角色失败: {Message}",
                    changeRoleResponse?.Message ?? "响应为空");
                return StatusCode(500, new ApiResponse<CityModeratorDto>
                {
                    Success = false,
                    Message = "为用户分配版主角色失败，请稍后重试"
                });
            }

            _logger.LogInformation("✅ 成功为用户分配 moderator 角色 - UserId: {UserId}", dto.UserId);

            // 步骤 3: 创建城市版主记录
            var moderator = new CityModerator
            {
                CityId = id,
                UserId = dto.UserId,
                CanEditCity = dto.CanEditCity,
                CanManageCoworks = dto.CanManageCoworks,
                CanManageCosts = dto.CanManageCosts,
                CanManageVisas = dto.CanManageVisas,
                CanModerateChats = dto.CanModerateChats,
                AssignedBy = Guid.TryParse(userContext.UserId, out var assignedById) ? assignedById : null,
                AssignedAt = DateTime.UtcNow,
                IsActive = true,
                Notes = dto.Notes
            };

            var added = await _moderatorRepository.AddAsync(moderator);
            _logger.LogInformation("✅ 成功创建城市版主记录 - ModeratorId: {ModeratorId}", added.Id);

            return Ok(new ApiResponse<CityModeratorDto>
            {
                Success = true,
                Message = "版主添加成功，已自动分配版主角色",
                Data = new CityModeratorDto
                {
                    Id = added.Id,
                    CityId = added.CityId,
                    UserId = added.UserId,
                    User = new ModeratorUserDto
                    {
                        Id = added.UserId,
                        Name = changeRoleResponse.Data?.Name ?? "",
                        Email = changeRoleResponse.Data?.Email ?? "",
                        Role = "moderator"
                    },
                    CanEditCity = added.CanEditCity,
                    CanManageCoworks = added.CanManageCoworks,
                    CanManageCosts = added.CanManageCosts,
                    CanManageVisas = added.CanManageVisas,
                    CanModerateChats = added.CanModerateChats,
                    AssignedBy = added.AssignedBy,
                    AssignedAt = added.AssignedAt,
                    IsActive = added.IsActive,
                    Notes = added.Notes,
                    CreatedAt = added.CreatedAt,
                    UpdatedAt = added.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加城市版主失败");
            return StatusCode(500, new ApiResponse<CityModeratorDto>
            {
                Success = false,
                Message = $"添加版主失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     删除城市版主（仅管理员）
    /// </summary>
    [HttpDelete("{cityId}/moderators/{userId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveCityModerator(Guid cityId, Guid userId)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);

        if (userContext?.Role != "admin") return Forbid();

        try
        {
            _logger.LogInformation("🗑️ 删除城市版主 - CityId: {CityId}, UserId: {UserId}, AdminId: {AdminId}",
                cityId, userId, userContext.UserId);

            var result = await _moderatorRepository.RemoveAsync(cityId, userId);

            if (!result)
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "版主记录不存在"
                });

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "版主已移除",
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除城市版主失败");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = $"删除版主失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     更新城市版主权限（仅管理员）
    /// </summary>
    [HttpPatch("{cityId}/moderators/{moderatorId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateCityModerator(
        Guid cityId,
        Guid moderatorId,
        [FromBody] UpdateCityModeratorDto dto)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);

        if (userContext?.Role != "admin") return Forbid();

        try
        {
            _logger.LogInformation("✏️ 更新城市版主权限 - ModeratorId: {ModeratorId}, AdminId: {AdminId}",
                moderatorId, userContext.UserId);

            var moderator = await _moderatorRepository.GetByIdAsync(moderatorId);
            if (moderator == null || moderator.CityId != cityId)
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "版主记录不存在"
                });

            // 更新权限
            if (dto.CanEditCity.HasValue) moderator.CanEditCity = dto.CanEditCity.Value;
            if (dto.CanManageCoworks.HasValue) moderator.CanManageCoworks = dto.CanManageCoworks.Value;
            if (dto.CanManageCosts.HasValue) moderator.CanManageCosts = dto.CanManageCosts.Value;
            if (dto.CanManageVisas.HasValue) moderator.CanManageVisas = dto.CanManageVisas.Value;
            if (dto.CanModerateChats.HasValue) moderator.CanModerateChats = dto.CanModerateChats.Value;
            if (dto.IsActive.HasValue) moderator.IsActive = dto.IsActive.Value;
            if (dto.Notes != null) moderator.Notes = dto.Notes;

            var result = await _moderatorRepository.UpdateAsync(moderator);

            return Ok(new ApiResponse<bool>
            {
                Success = result,
                Message = result ? "版主权限更新成功" : "更新失败",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新城市版主权限失败");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = $"更新失败: {ex.Message}"
            });
        }
    }

    #endregion

    #region AI 图片生成

    /// <summary>
    ///     为城市生成 AI 图片（异步模式）
    /// </summary>
    /// <remarks>
    ///     异步调用 AIService 的通义万象 API 生成城市图片：
    ///     - 1张竖屏封面图 (720*1280)，存储路径：portrait/{cityId}/
    ///     - 4张横屏图片 (1280*720)，存储路径：landscape/{cityId}/
    ///     
    ///     接口立即返回任务ID，生成完成后：
    ///     1. AIService 通过 MassTransit 发送 CityImageGeneratedMessage
    ///     2. CityService 消费消息并更新数据库
    ///     3. MessageService 消费消息并通过 SignalR 通知 Flutter
    ///     
    ///     注意：Token 验证在 Gateway 层完成，此处通过 UserContext 获取用户信息
    /// </remarks>
    /// <param name="cityId">城市ID</param>
    /// <param name="request">生成请求（可选参数）</param>
    /// <returns>任务创建结果</returns>
    [HttpPost("{cityId:guid}/generate-images")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<GenerateCityImagesTaskResponseDto>>> GenerateCityImages(
        Guid cityId,
        [FromBody] GenerateCityImagesRequest? request,
        [FromServices] CityService.Infrastructure.Clients.IAIServiceClient aiServiceClient)
    {
        try
        {
            // 通过 UserContext 获取用户信息（Gateway 已验证 Token）
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            _logger.LogInformation("🖼️ 用户 {UserId} ({Role}) 请求生成城市图片",
                userContext?.UserId, userContext?.Role);

            // 获取城市信息
            var city = await _cityService.GetCityByIdAsync(cityId);
            if (city == null)
            {
                return NotFound(new ApiResponse<GenerateCityImagesTaskResponseDto>
                {
                    Success = false,
                    Message = "城市不存在"
                });
            }

            _logger.LogInformation("🖼️ 开始为城市创建图片生成任务: CityId={CityId}, CityName={CityName}",
                cityId, city.Name);

            // 获取当前用户ID（用于推送通知）
            var userId = userContext?.UserId ?? "00000000-0000-0000-0000-000000000001";

            // 调用 AIService 创建异步任务（立即返回，不等待结果）
            var result = await aiServiceClient.GenerateCityImagesAsyncTask(
                cityId.ToString(),
                city.NameEn ?? city.Name,
                city.Country,
                userId,  // 传递用户ID
                request?.Style ?? "<photography>",
                request?.Bucket ?? "city-photos");

            if (result == null || !result.Success)
            {
                return BadRequest(new ApiResponse<GenerateCityImagesTaskResponseDto>
                {
                    Success = false,
                    Message = result?.Message ?? "创建图片生成任务失败"
                });
            }

            _logger.LogInformation("✅ 城市图片生成任务已创建: CityId={CityId}, TaskId={TaskId}",
                cityId, result.TaskId);

            return Ok(new ApiResponse<GenerateCityImagesTaskResponseDto>
            {
                Success = true,
                Message = "图片生成任务已创建，完成后将通过通知推送结果",
                Data = new GenerateCityImagesTaskResponseDto
                {
                    TaskId = result.TaskId ?? "",
                    CityId = cityId.ToString(),
                    CityName = city.Name,
                    Status = result.Status ?? "queued",
                    EstimatedTimeSeconds = result.EstimatedTimeSeconds,
                    Message = result.Message ?? "任务已创建，正在处理中"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 为城市创建图片生成任务失败: CityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<GenerateCityImagesTaskResponseDto>
            {
                Success = false,
                Message = $"创建图片生成任务失败: {ex.Message}"
            });
        }
    }

    #endregion

    #region Nearby Cities APIs

    /// <summary>
    ///     Get nearby cities for a city
    /// </summary>
    /// <param name="cityId">City ID</param>
    /// <returns>List of nearby cities</returns>
    [HttpGet("{cityId}/nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<NearbyCityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<NearbyCityDto>>>> GetNearbyCities(string cityId)
    {
        try
        {
            _logger.LogInformation("📖 获取附近城市: cityId={CityId}", cityId);

            var nearbyCities = await _nearbyCityService.GetBySourceCityIdAsync(cityId);

            var dtos = nearbyCities.Select(MapToDto).ToList();

            _logger.LogInformation("✅ 返回 {Count} 个附近城市: cityId={CityId}", dtos.Count, cityId);

            return Ok(new ApiResponse<List<NearbyCityDto>>
            {
                Success = true,
                Message = nearbyCities.Count > 0
                    ? "Nearby cities retrieved successfully"
                    : "No nearby cities found for this city yet",
                Data = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取附近城市失败: cityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<List<NearbyCityDto>>
            {
                Success = false,
                Message = $"Failed to retrieve nearby cities: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    ///     Save nearby cities for a city (batch save)
    /// </summary>
    /// <param name="cityId">City ID</param>
    /// <param name="request">Nearby cities data</param>
    /// <returns>Saved nearby cities</returns>
    [HttpPost("{cityId}/nearby")]
    [ProducesResponseType(typeof(ApiResponse<List<NearbyCityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<NearbyCityDto>>>> SaveNearbyCities(
        string cityId,
        [FromBody] SaveNearbyCitiesRequest request)
    {
        try
        {
            _logger.LogInformation("💾 保存附近城市: cityId={CityId}, count={Count}",
                cityId, request.NearbyCities.Count);

            // 验证cityId匹配
            if (request.SourceCityId != cityId)
                return BadRequest(new ApiResponse<List<NearbyCityDto>>
                {
                    Success = false,
                    Message = "City ID in URL does not match request body",
                    Data = null
                });

            // 映射到实体
            var nearbyCities = request.NearbyCities.Select(dto => new NearbyCity
            {
                SourceCityId = cityId,
                TargetCityId = dto.TargetCityId,
                TargetCityName = dto.TargetCityName,
                Country = dto.Country,
                DistanceKm = dto.DistanceKm,
                TransportationType = dto.TransportationType,
                TravelTimeMinutes = dto.TravelTimeMinutes,
                Highlights = dto.Highlights,
                NomadFeatures = new NearbyCityNomadFeatures
                {
                    MonthlyCostUsd = dto.NomadFeatures.MonthlyCostUsd,
                    InternetSpeedMbps = dto.NomadFeatures.InternetSpeedMbps,
                    CoworkingSpaces = dto.NomadFeatures.CoworkingSpaces,
                    VisaInfo = dto.NomadFeatures.VisaInfo,
                    SafetyScore = dto.NomadFeatures.SafetyScore,
                    QualityOfLife = dto.NomadFeatures.QualityOfLife
                },
                ImageUrl = dto.ImageUrl,
                OverallScore = dto.OverallScore,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsAIGenerated = dto.IsAIGenerated
            }).ToList();

            var savedCities = await _nearbyCityService.SaveBatchAsync(cityId, nearbyCities);

            var dtos = savedCities.Select(MapToDto).ToList();

            _logger.LogInformation("✅ 附近城市保存成功: cityId={CityId}, savedCount={Count}",
                cityId, dtos.Count);

            return Ok(new ApiResponse<List<NearbyCityDto>>
            {
                Success = true,
                Message = "Nearby cities saved successfully",
                Data = dtos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 保存附近城市失败: cityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<List<NearbyCityDto>>
            {
                Success = false,
                Message = $"Failed to save nearby cities: {ex.Message}",
                Data = null
            });
        }
    }

    /// <summary>
    ///     Delete all nearby cities for a city
    /// </summary>
    /// <param name="cityId">City ID</param>
    /// <returns>Success or failure</returns>
    [HttpDelete("{cityId}/nearby")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNearbyCities(string cityId)
    {
        try
        {
            _logger.LogInformation("🗑️ 删除附近城市: cityId={CityId}", cityId);

            var result = await _nearbyCityService.DeleteBySourceCityIdAsync(cityId);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = result ? "Nearby cities deleted successfully" : "No nearby cities found to delete",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除附近城市失败: cityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = $"Failed to delete nearby cities: {ex.Message}",
                Data = false
            });
        }
    }

    /// <summary>
    ///     Map NearbyCity entity to DTO
    /// </summary>
    private static NearbyCityDto MapToDto(NearbyCity entity)
    {
        return new NearbyCityDto
        {
            Id = entity.Id,
            SourceCityId = entity.SourceCityId,
            TargetCityId = entity.TargetCityId,
            TargetCityName = entity.TargetCityName,
            Country = entity.Country,
            DistanceKm = entity.DistanceKm,
            TransportationType = entity.TransportationType,
            TravelTimeMinutes = entity.TravelTimeMinutes,
            Highlights = entity.Highlights,
            NomadFeatures = new NearbyCityNomadFeaturesDto
            {
                MonthlyCostUsd = entity.NomadFeatures.MonthlyCostUsd,
                InternetSpeedMbps = entity.NomadFeatures.InternetSpeedMbps,
                CoworkingSpaces = entity.NomadFeatures.CoworkingSpaces,
                VisaInfo = entity.NomadFeatures.VisaInfo,
                SafetyScore = entity.NomadFeatures.SafetyScore,
                QualityOfLife = entity.NomadFeatures.QualityOfLife
            },
            ImageUrl = entity.ImageUrl,
            OverallScore = entity.OverallScore,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            IsAIGenerated = entity.IsAIGenerated,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    #endregion
}

/// <summary>
///     城市图片生成任务响应 DTO
/// </summary>
public class GenerateCityImagesTaskResponseDto
{
    /// <summary>
    ///     任务ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    ///     城市ID
    /// </summary>
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称
    /// </summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     任务状态
    /// </summary>
    public string Status { get; set; } = "queued";

    /// <summary>
    ///     预计完成时间（秒）
    /// </summary>
    public int EstimatedTimeSeconds { get; set; }

    /// <summary>
    ///     消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
///     简单的用户 DTO - 用于服务间调用
///     映射自 UserService.Application.DTOs.UserDto
/// </summary>
public class SimpleUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
///     简单的角色 DTO - 用于服务间调用
///     映射自 UserService.Application.DTOs.RoleDto
/// </summary>
public class SimpleRoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
///     Coworking 数量 DTO
/// </summary>
public class CoworkingCountDto
{
    public string CityId { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
///     批量获取数量响应模型 - 用于服务间调用
/// </summary>
internal class BatchCountResponse
{
    public List<CityCountItem> Counts { get; set; } = new();
}

/// <summary>
///     城市数量项模型
/// </summary>
internal class CityCountItem
{
    public string CityId { get; set; } = string.Empty;
    public int Count { get; set; }
}
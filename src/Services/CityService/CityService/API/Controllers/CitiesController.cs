using System.Security.Claims;
using CityService.Application.DTOs;
using CityService.Application.Services;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Dapr.Client;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
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
    private readonly DaprClient _daprClient;
    private readonly IDigitalNomadGuideService _guideService;
    private readonly ILogger<CitiesController> _logger;
    private readonly ICityModeratorRepository _moderatorRepository;
    private readonly Client _supabaseClient;

    public CitiesController(
        ICityService cityService,
        IDigitalNomadGuideService guideService,
        ICityModeratorRepository moderatorRepository,
        DaprClient daprClient,
        Client supabaseClient,
        ILogger<CitiesController> logger)
    {
        _cityService = cityService;
        _guideService = guideService;
        _moderatorRepository = moderatorRepository;
        _daprClient = daprClient;
        _supabaseClient = supabaseClient;
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
            var userId = TryGetCurrentUserId();
            var userRole = TryGetCurrentUserRole();

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
                cities = await _cityService.GetAllCitiesAsync(pageNumber, pageSize, userId, userRole);
                totalCount = await _cityService.GetTotalCountAsync();
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
    ///     Get recommended cities
    ///     GET /api/v1/cities/recommended?count=10
    /// </summary>
    [HttpGet("recommended")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<CityDto>>>> GetRecommendedCities([FromQuery] int count = 10)
    {
        try
        {
            var userId = TryGetCurrentUserId();
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
            var userId = TryGetCurrentUserId();
            var userRole = TryGetCurrentUserRole();
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
            var userId = TryGetCurrentUserId();
            var userRole = TryGetCurrentUserRole();
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
    ///     Create a new city (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CityDto>>> CreateCity([FromBody] CreateCityDto createCityDto)
    {
        try
        {
            var userId = GetUserId();
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
            var userId = GetUserId();
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
            var result = await _cityService.DeleteCityAsync(id);
            if (!result)
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });

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
    /// </summary>
    [HttpGet("with-coworking-count")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCitiesWithCoworkingCount(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 获取城市列表
            var userId = TryGetCurrentUserId();
            var userRole = TryGetCurrentUserRole();
            var cities = await _cityService.GetAllCitiesAsync(page, pageSize, userId, userRole);
            var totalCount = await _cityService.GetTotalCountAsync();
            var cityList = cities.ToList();

            // 批量获取每个城市的 coworking 数量
            await EnrichCitiesWithCoworkingCountAsync(cityList);

            _logger.LogInformation(
                "获取城市列表(含Coworking数量)成功: {CityCount} 个城市, 第 {Page} 页",
                cityList.Count,
                page);

            return Ok(new ApiResponse<PaginatedResponse<CityDto>>
            {
                Success = true,
                Message = "城市列表(含Coworking数量)获取成功",
                Data = new PaginatedResponse<CityDto>
                {
                    Items = cityList,
                    TotalCount = totalCount,
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
    ///     直接从数据库查询城市的 coworking 数量（避免跨服务HTTP调用）
    /// </summary>
    private async Task EnrichCitiesWithCoworkingCountAsync(List<CityDto> cities)
    {
        if (cities == null || cities.Count == 0) return;

        try
        {
            _logger.LogInformation("开始统计 {CityCount} 个城市的 Coworking 数量", cities.Count);

            // 收集所有城市 ID
            var cityIds = cities.Select(c => c.Id).ToList();

            // 直接查询 coworking_spaces 表
            var response = await _supabaseClient
                .From<CoworkingSpaceDto>()
                .Where(x => x.IsActive == true)
                .Get();

            // 过滤出目标城市，并按城市ID分组统计
            var countByCity = response.Models
                .Where(x => x.CityId.HasValue && cityIds.Contains(x.CityId.Value))
                .GroupBy(x => x.CityId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // 填充每个城市的 coworking 数量
            foreach (var city in cities)
            {
                city.CoworkingCount = countByCity.TryGetValue(city.Id, out var count) ? count : 0;
            }

            _logger.LogInformation(
                "成功统计 {CityCount} 个城市的 Coworking 数量，其中 {ActiveCount} 个城市有空间",
                cities.Count,
                countByCity.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "统计 Coworking 数量失败，使用默认值 0");
            // 容错: 如果查询失败，将所有城市的 CoworkingCount 设为 0
            foreach (var city in cities)
            {
                city.CoworkingCount = 0;
            }
        }
    }

    /// <summary>
    ///     Coworking 空间 DTO（用于统计数量）
    /// </summary>
    [Table("coworking_spaces")]
    private class CoworkingSpaceDto : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("city_id")]
        public Guid? CityId { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
    }

    #region Helper Methods

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    ///     尝试获取当前用户ID（从 UserContext 中获取）
    ///     如果用户未认证，返回 null
    /// </summary>
    private Guid? TryGetCurrentUserId()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
                if (Guid.TryParse(userContext.UserId, out var userId))
                    return userId;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取当前用户ID失败，将返回 null");
        }

        return null;
    }

    private string? TryGetCurrentUserRole()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            return userContext?.Role;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取当前用户角色失败，将返回 null");
            return null;
        }
    }

    private Guid GetCurrentUserId()
    {
        var userId = TryGetCurrentUserId();
        if (!userId.HasValue) throw new UnauthorizedAccessException("用户未登录");
        return userId.Value;
    }

    #endregion

    #region Digital Nomad Guide APIs

    /// <summary>
    ///     Get digital nomad guide for a city
    /// </summary>
    /// <param name="cityId">City ID</param>
    /// <returns>Digital nomad guide or 404 if not found</returns>
    [HttpGet("{cityId}/guide")]
    [ProducesResponseType(typeof(ApiResponse<DigitalNomadGuideDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DigitalNomadGuideDto>>> GetDigitalNomadGuide(string cityId)
    {
        try
        {
            _logger.LogInformation("📖 获取数字游民指南: cityId={CityId}", cityId);

            var guide = await _guideService.GetByCityIdAsync(cityId);

            if (guide == null)
            {
                _logger.LogInformation("📭 未找到指南: cityId={CityId}", cityId);
                return NotFound(new ApiResponse<DigitalNomadGuideDto>
                {
                    Success = false,
                    Message = "Guide not found for this city",
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

    /// <summary>
    ///     申请成为城市版主 (需要登录)
    /// </summary>
    [HttpPost("moderator/apply")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ApplyModerator([FromBody] ApplyModeratorDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _cityService.ApplyModeratorAsync(userId, dto);

            if (result)
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "申请成功！您已成为该城市的版主",
                    Data = true
                });

            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "申请失败，该城市已有版主",
                Data = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "申请城市版主失败: UserId={UserId}, CityId={CityId}",
                GetCurrentUserId(), dto.CityId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = $"申请失败: {ex.Message}",
                Data = false
            });
        }
    }

    /// <summary>
    ///     指定城市版主 (仅管理员)
    /// </summary>
    [HttpPost("moderator/assign")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignModerator([FromBody] AssignModeratorDto dto)
    {
        try
        {
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
                // TODO: 通过 Dapr 调用 UserService 获取用户详细信息
                // 目前先返回基本信息
                moderatorDtos.Add(new CityModeratorDto
                {
                    Id = moderator.Id,
                    CityId = moderator.CityId,
                    UserId = moderator.UserId,
                    User = new ModeratorUserDto
                    {
                        Id = moderator.UserId,
                        Name = "Loading...", // 后续通过 Dapr 获取
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

            // 步骤 1: 通过 Dapr 获取 moderator 角色
            _logger.LogInformation("🔍 通过 UserService API 获取 moderator 角色");
            var roleResponse = await _daprClient.InvokeMethodAsync<ApiResponse<SimpleRoleDto>>(
                HttpMethod.Get,
                "user-service",
                "api/v1/roles/by-name/moderator");

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

            // 步骤 2: 通过 Dapr 为用户分配 moderator 角色
            _logger.LogInformation("🔄 通过 UserService API 为用户分配 moderator 角色");
            var changeRoleRequest = new { roleId = moderatorRoleId };
            var changeRoleResponse = await _daprClient.InvokeMethodAsync<object, ApiResponse<SimpleUserDto>>(
                HttpMethod.Patch,
                "user-service",
                $"api/v1/users/{dto.UserId}/role",
                changeRoleRequest);

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
}

/// <summary>
///     简单的用户 DTO - 用于 Dapr 服务间调用
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
///     简单的角色 DTO - 用于 Dapr 服务间调用
///     映射自 UserService.Application.DTOs.RoleDto
/// </summary>
public class SimpleRoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
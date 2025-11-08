using CityService.Application.DTOs;
using CityService.Application.Services;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using Dapr.Client;

namespace CityService.API.Controllers;

/// <summary>
/// Cities API - RESTful endpoints for city management
/// </summary>
[ApiController]
[Route("api/v1/cities")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly DaprClient _daprClient;
    private readonly ILogger<CitiesController> _logger;

    public CitiesController(
        ICityService cityService,
        DaprClient daprClient,
        ILogger<CitiesController> logger)
    {
        _cityService = cityService;
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all cities with pagination and optional search
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCities(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            var userId = TryGetCurrentUserId();
            
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
                cities = await _cityService.SearchCitiesAsync(searchDto, userId);
                totalCount = cities.Count(); // 搜索结果的总数
            }
            else
            {
                cities = await _cityService.GetAllCitiesAsync(pageNumber, pageSize, userId);
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
    /// Get recommended cities
    /// GET /api/v1/cities/recommended?count=10
    /// </summary>
    [HttpGet("recommended")]
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
    /// Get cities by country ID (Query parameter approach)
    /// GET /api/v1/cities?countryId={guid}
    /// </summary>
    [HttpGet("by-country/{countryId:guid}")]
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
    /// Get cities grouped by country
    /// GET /api/v1/cities/grouped-by-country
    /// </summary>
    [HttpGet("grouped-by-country")]
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
    /// Get all countries (as a related resource)
    /// GET /api/v1/cities/countries
    /// Note: Consider moving to separate /api/v1/countries endpoint
    /// </summary>
    [HttpGet("countries")]
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
    /// Search cities with filters
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CityDto>>>> SearchCities([FromQuery] CitySearchDto searchDto)
    {
        try
        {
            var userId = TryGetCurrentUserId();
            var cities = await _cityService.SearchCitiesAsync(searchDto, userId);
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
    /// Get city by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CityDto>>> GetCity(Guid id)
    {
        try
        {
            var userId = TryGetCurrentUserId();
            var city = await _cityService.GetCityByIdAsync(id, userId);
            if (city == null)
            {
                return NotFound(new ApiResponse<CityDto>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });
            }

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
    /// Get city statistics
    /// </summary>
    [HttpGet("{id:guid}/statistics")]
    public async Task<ActionResult<ApiResponse<CityStatisticsDto>>> GetCityStatistics(Guid id)
    {
        try
        {
            var statistics = await _cityService.GetCityStatisticsAsync(id);
            if (statistics == null)
            {
                return NotFound(new ApiResponse<CityStatisticsDto>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });
            }

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
    /// Get current weather for a city
    /// GET /api/v1/cities/{id}/weather
    /// </summary>
    [HttpGet("{id:guid}/weather")]
    public async Task<ActionResult<ApiResponse<WeatherDto>>> GetCityWeather(
        Guid id,
        [FromQuery] bool includeForecast = false,
        [FromQuery] int days = 7)
    {
        try
        {
            var weather = await _cityService.GetCityWeatherAsync(id, includeForecast, days);
            if (weather == null)
            {
                return NotFound(new ApiResponse<WeatherDto>
                {
                    Success = false,
                    Message = "Weather data is not available for this city",
                    Errors = new List<string> { "Weather data not available" }
                });
            }

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
    /// Create a new city (Admin only)
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
    /// Update a city (Admin only)
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
            {
                return NotFound(new ApiResponse<CityDto>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });
            }

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
    /// Delete a city (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCity(Guid id)
    {
        try
        {
            var result = await _cityService.DeleteCityAsync(id);
            if (!result)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"City with ID {id} not found",
                    Errors = new List<string> { "City not found" }
                });
            }

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
    /// Get cities with coworking count for coworking home page
    /// 专门为 coworking_home 页面提供城市列表和每个城市的 coworking 数量
    /// </summary>
    [HttpGet("with-coworking-count")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCitiesWithCoworkingCount(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 获取城市列表
            var cities = await _cityService.GetAllCitiesAsync(page, pageSize);
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
    /// 通过 Dapr 调用 CoworkingService 批量获取城市的 coworking 数量
    /// </summary>
    private async Task EnrichCitiesWithCoworkingCountAsync(List<CityDto> cities)
    {
        if (cities == null || cities.Count == 0)
        {
            return;
        }

        try
        {
            // 收集所有城市 ID
            var cityIds = string.Join(",", cities.Select(c => c.Id));

            // 调用 CoworkingService 的批量查询接口 (返回 ApiResponse 包装)
            var apiResponse = await _daprClient.InvokeMethodAsync<ApiResponse<Dictionary<Guid, int>>>(
                HttpMethod.Get,
                "coworking-service",
                $"api/v1/coworking/count-by-cities?cityIds={cityIds}");

            // 检查响应是否成功
            if (apiResponse?.Success == true && apiResponse.Data != null)
            {
                // 填充每个城市的 coworking 数量
                foreach (var city in cities)
                {
                    if (apiResponse.Data.TryGetValue(city.Id, out var count))
                    {
                        city.CoworkingCount = count;
                    }
                    else
                    {
                        city.CoworkingCount = 0;
                    }
                }

                _logger.LogInformation(
                    "成功从 CoworkingService 获取 {CityCount} 个城市的 Coworking 数量",
                    cities.Count);
            }
            else
            {
                _logger.LogWarning(
                    "CoworkingService 返回非成功结果: {Message}",
                    apiResponse?.Message ?? "响应为空");
                
                // 设置默认值
                foreach (var city in cities)
                {
                    city.CoworkingCount = 0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调用 CoworkingService 获取 Coworking 数量失败，使用默认值 0");
            // 容错: 如果调用失败，将所有城市的 CoworkingCount 设为 0
            foreach (var city in cities)
            {
                city.CoworkingCount = 0;
            }
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// 尝试获取当前用户ID（从 UserContext 中获取）
    /// 如果用户未认证，返回 null
    /// </summary>
    private Guid? TryGetCurrentUserId()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
            {
                if (Guid.TryParse(userContext.UserId, out var userId))
                {
                    return userId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取当前用户ID失败，将返回 null");
        }

        return null;
    }
}

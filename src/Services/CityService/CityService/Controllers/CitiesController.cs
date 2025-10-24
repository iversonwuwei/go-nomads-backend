using CityService.DTOs;
using CityService.Services;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;

namespace CityService.Controllers;

/// <summary>
/// Cities API - RESTful endpoints for city management
/// </summary>
[ApiController]
[Route("api/v1/cities")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly ILogger<CitiesController> _logger;

    public CitiesController(ICityService cityService, ILogger<CitiesController> logger)
    {
        _cityService = cityService;
        _logger = logger;
    }

    /// <summary>
    /// Get all cities with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CityDto>>>> GetCities(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var cities = await _cityService.GetAllCitiesAsync(pageNumber, pageSize);
            var totalCount = await _cityService.GetTotalCountAsync();

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
            var cities = await _cityService.GetRecommendedCitiesAsync(count);
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
            var cities = await _cityService.SearchCitiesAsync(searchDto);
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
            var city = await _cityService.GetCityByIdAsync(id);
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

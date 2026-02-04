using Microsoft.AspNetCore.Mvc;
using SearchService.Application.Interfaces;
using SearchService.Domain.Models;

namespace SearchService.Controllers;

/// <summary>
/// 搜索控制器
/// </summary>
[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// 统一搜索接口
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="type">搜索类型: city, coworking, all</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="country">国家过滤</param>
    /// <param name="cityId">城市ID过滤</param>
    /// <param name="minRating">最低评分</param>
    /// <param name="sortBy">排序字段</param>
    /// <param name="sortOrder">排序方向</param>
    /// <param name="lat">纬度(地理搜索)</param>
    /// <param name="lon">经度(地理搜索)</param>
    /// <param name="radiusKm">搜索半径(公里)</param>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? query = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? country = null,
        [FromQuery] Guid? cityId = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortOrder = "desc",
        [FromQuery] double? lat = null,
        [FromQuery] double? lon = null,
        [FromQuery] double? radiusKm = null)
    {
        var request = new SearchRequest
        {
            Query = query ?? string.Empty,
            Type = type,
            Page = page,
            PageSize = pageSize,
            Country = country,
            CityId = cityId,
            MinRating = minRating,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Latitude = lat,
            Longitude = lon,
            RadiusKm = radiusKm
        };

        _logger.LogInformation("收到搜索请求: Query={Query}, Type={Type}", query, type);

        var result = await _searchService.SearchAllAsync(request);
        return Ok(new ApiResponse<UnifiedSearchResult>
        {
            Success = true,
            Data = result
        });
    }

    /// <summary>
    /// 搜索城市
    /// </summary>
    [HttpGet("cities")]
    public async Task<IActionResult> SearchCities(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? country = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortOrder = "desc")
    {
        var request = new SearchRequest
        {
            Query = query ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Country = country,
            MinRating = minRating,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _searchService.SearchCitiesAsync(request);
        return Ok(new ApiResponse<SearchResult<CitySearchDocument>>
        {
            Success = true,
            Data = result
        });
    }

    /// <summary>
    /// 搜索共享办公空间
    /// </summary>
    [HttpGet("coworkings")]
    public async Task<IActionResult> SearchCoworkings(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cityId = null,
        [FromQuery] string? country = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortOrder = "desc")
    {
        var request = new SearchRequest
        {
            Query = query ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            CityId = cityId,
            Country = country,
            MinRating = minRating,
            SortBy = sortBy,
            SortOrder = sortOrder
        };

        var result = await _searchService.SearchCoworkingsAsync(request);
        return Ok(new ApiResponse<SearchResult<CoworkingSearchDocument>>
        {
            Success = true,
            Data = result
        });
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    [HttpGet("suggest")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string prefix,
        [FromQuery] string? type = null,
        [FromQuery] int size = 10)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "搜索前缀不能为空"
            });
        }

        var request = new SuggestRequest
        {
            Prefix = prefix,
            Type = type,
            Size = size
        };

        var result = await _searchService.GetSuggestionsAsync(request);
        return Ok(new ApiResponse<SuggestResponse>
        {
            Success = true,
            Data = result
        });
    }
}

/// <summary>
/// API响应包装类
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

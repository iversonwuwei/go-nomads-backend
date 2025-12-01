using CityService.Application.DTOs;
using CityService.Application.Services;
using CityService.DTOs;
using GoNomads.Shared.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

/// <summary>
///     用户收藏城市API控制器
/// </summary>
[ApiController]
[Route("api/v1/user-favorite-cities")]
public class UserFavoriteCitiesController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly IUserFavoriteCityService _favoriteCityService;
    private readonly ILogger<UserFavoriteCitiesController> _logger;

    public UserFavoriteCitiesController(
        IUserFavoriteCityService favoriteCityService,
        ICityService cityService,
        ILogger<UserFavoriteCitiesController> logger)
    {
        _favoriteCityService = favoriteCityService;
        _cityService = cityService;
        _logger = logger;
    }

    /// <summary>
    ///     检查城市是否已收藏
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <returns>收藏状态</returns>
    [HttpGet("check/{cityId}")]
    [ProducesResponseType(typeof(CheckFavoriteStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckFavoriteStatus(string cityId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isFavorited = await _favoriteCityService.IsCityFavoritedAsync(userId, cityId);

            return Ok(new CheckFavoriteStatusResponse { IsFavorited = isFavorited });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查收藏状态失败: CityId={CityId}", cityId);
            return StatusCode(500, new { error = "检查收藏状态失败" });
        }
    }

    /// <summary>
    ///     添加收藏城市
    /// </summary>
    /// <param name="request">添加收藏请求</param>
    /// <returns>收藏记录</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserFavoriteCityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavoriteCity([FromBody] AddFavoriteCityRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // 检查是否已存在
            var exists = await _favoriteCityService.IsCityFavoritedAsync(userId, request.CityId);
            if (exists) return Conflict(new { error = "城市已在收藏列表中" });

            var result = await _favoriteCityService.AddFavoriteCityAsync(userId, request.CityId);

            var dto = new UserFavoriteCityDto
            {
                Id = result.Id.ToString(),
                UserId = result.UserId.ToString(),
                CityId = result.CityId,
                CreatedAt = result.CreatedAt,
                UpdatedAt = result.UpdatedAt
            };

            return CreatedAtAction(
                nameof(CheckFavoriteStatus),
                new { cityId = request.CityId },
                dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加收藏失败: CityId={CityId}", request.CityId);
            return StatusCode(500, new { error = "添加收藏失败" });
        }
    }

    /// <summary>
    ///     移除收藏城市
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{cityId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFavoriteCity(string cityId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _favoriteCityService.RemoveFavoriteCityAsync(userId, cityId);

            if (!success) return NotFound(new { error = "收藏记录不存在" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消收藏失败: CityId={CityId}", cityId);
            return StatusCode(500, new { error = "取消收藏失败" });
        }
    }

    /// <summary>
    ///     获取用户收藏的城市ID列表
    /// </summary>
    /// <returns>城市ID列表</returns>
    [HttpGet("ids")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserFavoriteCityIds()
    {
        try
        {
            var userId = GetCurrentUserId();
            var cityIds = await _favoriteCityService.GetUserFavoriteCityIdsAsync(userId);

            return Ok(cityIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收藏城市ID列表失败");
            return StatusCode(500, new { error = "获取收藏列表失败" });
        }
    }

    /// <summary>
    ///     获取指定用户收藏的城市数量（供其他服务调用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>收藏城市数量</returns>
    [HttpGet("user/{userId}/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserFavoriteCitiesCount(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(new { error = "无效的用户ID格式" });
            }

            var count = await _favoriteCityService.GetUserFavoriteCitiesCountAsync(userGuid);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户收藏城市数量失败: UserId={UserId}", userId);
            return StatusCode(500, new { error = "获取收藏数量失败" });
        }
    }

    /// <summary>
    ///     获取用户收藏的城市列表（分页）
    /// </summary>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认20，最大100）</param>
    /// <returns>收藏城市列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(FavoriteCitiesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserFavoriteCities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var (items, total) = await _favoriteCityService.GetUserFavoriteCitiesAsync(userId, page, pageSize);

            var dtos = items.Select(item => new UserFavoriteCityDto
            {
                Id = item.Id.ToString(),
                UserId = item.UserId.ToString(),
                CityId = item.CityId,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            }).ToList();

            var response = new FavoriteCitiesResponse
            {
                Items = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收藏城市列表失败");
            return StatusCode(500, new { error = "获取收藏列表失败" });
        }
    }

    /// <summary>
    ///     获取用户收藏的城市详情列表（包含完整城市信息）
    /// </summary>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认20，最大100）</param>
    /// <returns>收藏城市详情列表</returns>
    [HttpGet("details")]
    [ProducesResponseType(typeof(FavoriteCitiesDetailResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserFavoriteCitiesWithDetails(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // 获取收藏记录
            var (items, total) = await _favoriteCityService.GetUserFavoriteCitiesAsync(userId, page, pageSize);
            
            if (items.Count == 0)
            {
                return Ok(new FavoriteCitiesDetailResponse
                {
                    Items = new List<CityDto>(),
                    Total = 0,
                    Page = page,
                    PageSize = pageSize
                });
            }
            
            // 获取城市详情
            var cityIds = items
                .Select(x => Guid.TryParse(x.CityId, out var id) ? id : (Guid?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
                
            var cities = await _cityService.GetCitiesByIdsAsync(cityIds);
            
            // 按收藏顺序排序
            var cityDict = cities.ToDictionary(c => c.Id);
            var orderedCities = items
                .Select(item => Guid.TryParse(item.CityId, out var id) && cityDict.TryGetValue(id, out var city) ? city : null)
                .Where(c => c != null)
                .Cast<CityDto>()
                .ToList();

            var response = new FavoriteCitiesDetailResponse
            {
                Items = orderedCities,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收藏城市详情列表失败");
            return StatusCode(500, new { error = "获取收藏列表失败" });
        }
    }

    /// <summary>
    ///     获取当前用户ID（从 UserContext 中获取）
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            throw new UnauthorizedAccessException("用户未认证");

        return Guid.Parse(userContext.UserId);
    }
}
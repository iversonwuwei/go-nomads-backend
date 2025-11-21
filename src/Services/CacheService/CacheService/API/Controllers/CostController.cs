using CacheService.Application.Abstractions.Services;
using CacheService.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CacheService.API.Controllers;

/// <summary>
/// 保存费用请求DTO
/// </summary>
public class SaveCostRequest
{
    public decimal AverageCost { get; set; }
    public string? Statistics { get; set; }
}

[ApiController]
[Route("api/v1/cache/costs")]
public class CostController : ControllerBase
{
    private readonly ICostCacheService _costCacheService;
    private readonly ILogger<CostController> _logger;

    public CostController(
        ICostCacheService costCacheService,
        ILogger<CostController> logger)
    {
        _costCacheService = costCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 获取城市平均费用 (带缓存)
    /// GET /api/v1/cache/costs/city/{cityId}
    /// </summary>
    [HttpGet("city/{cityId}")]
    public async Task<ActionResult<CostResponseDto>> GetCityCost(string cityId)
    {
        try
        {
            var result = await _costCacheService.GetCityCostAsync(cityId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city cost for cityId: {CityId}", cityId);
            return StatusCode(500, new { error = "Failed to get city cost" });
        }
    }

    /// <summary>
    /// 批量获取城市平均费用 (带缓存)
    /// POST /api/v1/cache/costs/city/batch
    /// </summary>
    [HttpPost("city/batch")]
    public async Task<ActionResult<BatchCostResponseDto>> GetCityCostsBatch([FromBody] List<string> cityIds)
    {
        try
        {
            if (cityIds == null || !cityIds.Any())
            {
                return BadRequest(new { error = "City IDs are required" });
            }

            var result = await _costCacheService.GetCityCostsBatchAsync(cityIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch city costs");
            return StatusCode(500, new { error = "Failed to get batch city costs" });
        }
    }

    /// <summary>
    /// 保存/更新城市平均费用到缓存
    /// PUT /api/v1/cache/costs/city/{cityId}
    /// </summary>
    [HttpPut("city/{cityId}")]
    public async Task<ActionResult> SaveCityCost(string cityId, [FromBody] SaveCostRequest request)
    {
        try
        {
            await _costCacheService.SaveCityCostAsync(cityId, request.AverageCost, request.Statistics);
            return Ok(new { message = $"City cost saved for cityId: {cityId}", averageCost = request.AverageCost });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving city cost for cityId: {CityId}", cityId);
            return StatusCode(500, new { error = "Failed to save city cost" });
        }
    }

    /// <summary>
    /// 使城市费用缓存失效
    /// DELETE /api/v1/cache/costs/city/{cityId}
    /// </summary>
    [HttpDelete("city/{cityId}")]
    public async Task<ActionResult> InvalidateCityCost(string cityId)
    {
        try
        {
            await _costCacheService.InvalidateCityCostAsync(cityId);
            return Ok(new { message = $"City cost cache invalidated for cityId: {cityId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating city cost cache for cityId: {CityId}", cityId);
            return StatusCode(500, new { error = "Failed to invalidate city cost cache" });
        }
    }
}

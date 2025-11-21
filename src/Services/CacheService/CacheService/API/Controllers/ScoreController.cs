using CacheService.Application.Abstractions.Services;
using CacheService.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CacheService.API.Controllers;

/// <summary>
/// 保存评分请求DTO
/// </summary>
public class SaveScoreRequest
{
    public double OverallScore { get; set; }
    public string? Statistics { get; set; }
}

[ApiController]
[Route("api/v1/cache/scores")]
public class ScoreController : ControllerBase
{
    private readonly IScoreCacheService _scoreCacheService;
    private readonly ILogger<ScoreController> _logger;

    public ScoreController(
        IScoreCacheService scoreCacheService,
        ILogger<ScoreController> logger)
    {
        _scoreCacheService = scoreCacheService;
        _logger = logger;
    }

    /// <summary>
    /// 获取城市评分 (带缓存)
    /// </summary>
    [HttpGet("city/{cityId}")]
    public async Task<ActionResult<ScoreResponseDto>> GetCityScore(string cityId)
    {
        try
        {
            var result = await _scoreCacheService.GetCityScoreAsync(cityId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city score for cityId: {CityId}", cityId);
            return StatusCode(500, new { error = "Failed to get city score" });
        }
    }

    /// <summary>
    /// 批量获取城市评分 (带缓存)
    /// </summary>
    [HttpPost("city/batch")]
    public async Task<ActionResult<BatchScoreResponseDto>> GetCityScoresBatch([FromBody] List<string> cityIds)
    {
        try
        {
            if (cityIds == null || !cityIds.Any())
            {
                return BadRequest(new { error = "City IDs are required" });
            }

            var result = await _scoreCacheService.GetCityScoresBatchAsync(cityIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch city scores");
            return StatusCode(500, new { error = "Failed to get batch city scores" });
        }
    }

    /// <summary>
    /// 保存/更新城市评分到缓存
    /// </summary>
    [HttpPut("city/{cityId}")]
    public async Task<ActionResult> SaveCityScore(string cityId, [FromBody] SaveScoreRequest request)
    {
        try
        {
            await _scoreCacheService.SaveCityScoreAsync(cityId, request.OverallScore, request.Statistics);
            return Ok(new { message = $"City score saved for cityId: {cityId}", overallScore = request.OverallScore });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving city score for cityId: {CityId}", cityId);
            return StatusCode(500, new { error = "Failed to save city score" });
        }
    }

    /// <summary>
    /// 获取共享办公空间评分 (带缓存)
    /// </summary>
    [HttpGet("coworking/{coworkingId}")]
    public async Task<ActionResult<ScoreResponseDto>> GetCoworkingScore(string coworkingId)
    {
        try
        {
            var result = await _scoreCacheService.GetCoworkingScoreAsync(coworkingId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coworking score for coworkingId: {CoworkingId}", coworkingId);
            return StatusCode(500, new { error = "Failed to get coworking score" });
        }
    }

    /// <summary>
    /// 批量获取共享办公空间评分 (带缓存)
    /// </summary>
    [HttpPost("coworking/batch")]
    public async Task<ActionResult<BatchScoreResponseDto>> GetCoworkingScoresBatch([FromBody] List<string> coworkingIds)
    {
        try
        {
            if (coworkingIds == null || !coworkingIds.Any())
            {
                return BadRequest(new { error = "Coworking IDs are required" });
            }

            var result = await _scoreCacheService.GetCoworkingScoresBatchAsync(coworkingIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch coworking scores");
            return StatusCode(500, new { error = "Failed to get batch coworking scores" });
        }
    }

    /// <summary>
    /// 保存/更新共享办公空间评分到缓存
    /// </summary>
    [HttpPut("coworking/{coworkingId}")]
    public async Task<ActionResult> SaveCoworkingScore(string coworkingId, [FromBody] SaveScoreRequest request)
    {
        try
        {
            await _scoreCacheService.SaveCoworkingScoreAsync(coworkingId, request.OverallScore, request.Statistics);
            return Ok(new { message = $"Coworking score saved for coworkingId: {coworkingId}", overallScore = request.OverallScore });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving coworking score for coworkingId: {CoworkingId}", coworkingId);
            return StatusCode(500, new { error = "Failed to save coworking score" });
        }
    }

    /// <summary>
    /// 使城市评分缓存失效
    /// </summary>
    [HttpDelete("city/{cityId}")]
    public async Task<ActionResult> InvalidateCityScore(string cityId)
    {
        try
        {
            await _scoreCacheService.InvalidateCityScoreAsync(cityId);
            return Ok(new { message = $"City score cache invalidated for cityId: {cityId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating city score cache for cityId: {CityId}", cityId);
            return StatusCode(500, new { error = "Failed to invalidate city score cache" });
        }
    }

    /// <summary>
    /// 批量使城市评分缓存失效
    /// </summary>
    [HttpPost("city/invalidate-batch")]
    public async Task<ActionResult> InvalidateCityScoresBatch([FromBody] List<string> cityIds)
    {
        try
        {
            if (cityIds == null || !cityIds.Any())
            {
                return BadRequest(new { error = "City IDs are required" });
            }

            await _scoreCacheService.InvalidateCityScoresBatchAsync(cityIds);
            return Ok(new { message = $"City score caches invalidated for {cityIds.Count} cities" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating batch city score cache");
            return StatusCode(500, new { error = "Failed to invalidate batch city score cache" });
        }
    }

    /// <summary>
    /// 使共享办公空间评分缓存失效
    /// </summary>
    [HttpDelete("coworking/{coworkingId}")]
    public async Task<ActionResult> InvalidateCoworkingScore(string coworkingId)
    {
        try
        {
            await _scoreCacheService.InvalidateCoworkingScoreAsync(coworkingId);
            return Ok(new { message = $"Coworking score cache invalidated for coworkingId: {coworkingId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating coworking score cache for coworkingId: {CoworkingId}", coworkingId);
            return StatusCode(500, new { error = "Failed to invalidate coworking score cache" });
        }
    }

    /// <summary>
    /// 批量使共享办公空间评分缓存失效
    /// </summary>
    [HttpPost("coworking/invalidate-batch")]
    public async Task<ActionResult> InvalidateCoworkingScoresBatch([FromBody] List<string> coworkingIds)
    {
        try
        {
            if (coworkingIds == null || !coworkingIds.Any())
            {
                return BadRequest(new { error = "Coworking IDs are required" });
            }

            await _scoreCacheService.InvalidateCoworkingScoresBatchAsync(coworkingIds);
            return Ok(new { message = $"Coworking score caches invalidated for {coworkingIds.Count} coworkings" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating batch coworking score cache");
            return StatusCode(500, new { error = "Failed to invalidate batch coworking score cache" });
        }
    }

    /// <summary>
    /// 清理所有零值的城市评分缓存 (管理接口)
    /// </summary>
    [HttpPost("city/cleanup-zero-scores")]
    public async Task<ActionResult> CleanupZeroCityScores()
    {
        try
        {
            var cleanedCount = await _scoreCacheService.CleanupZeroScoresAsync();
            return Ok(new 
            { 
                message = "Cleanup completed", 
                cleanedCount = cleanedCount,
                description = "Removed city score caches with zero or negative overall scores"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up zero city score caches");
            return StatusCode(500, new { error = "Failed to cleanup zero score caches" });
        }
    }
}

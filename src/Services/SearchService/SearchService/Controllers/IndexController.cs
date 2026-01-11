using Microsoft.AspNetCore.Mvc;
using SearchService.Application.Interfaces;

namespace SearchService.Controllers;

/// <summary>
/// 索引管理控制器
/// </summary>
[ApiController]
[Route("api/v1/index")]
public class IndexController : ControllerBase
{
    private readonly IIndexSyncService _indexSyncService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<IndexController> _logger;

    public IndexController(
        IIndexSyncService indexSyncService,
        IElasticsearchService elasticsearchService,
        ILogger<IndexController> logger)
    {
        _indexSyncService = indexSyncService;
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    /// <summary>
    /// 获取Elasticsearch健康状态
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        var isHealthy = await _elasticsearchService.IsHealthyAsync();
        return Ok(new ApiResponse<object>
        {
            Success = isHealthy,
            Message = isHealthy ? "Elasticsearch连接正常" : "Elasticsearch连接失败",
            Data = new { healthy = isHealthy }
        });
    }

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var cityStats = await _elasticsearchService.GetIndexStatsAsync("cities");
        var coworkingStats = await _elasticsearchService.GetIndexStatsAsync("coworking_spaces");

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                cities = cityStats,
                coworkings = coworkingStats
            }
        });
    }

    /// <summary>
    /// 同步所有数据
    /// </summary>
    [HttpPost("sync/all")]
    public async Task<IActionResult> SyncAll()
    {
        _logger.LogInformation("收到同步所有数据请求");
        var result = await _indexSyncService.SyncAllAsync();
        return Ok(new ApiResponse<object>
        {
            Success = result.Success,
            Message = result.Success ? "同步完成" : result.ErrorMessage,
            Data = result
        });
    }

    /// <summary>
    /// 同步城市数据
    /// </summary>
    [HttpPost("sync/cities")]
    public async Task<IActionResult> SyncCities()
    {
        _logger.LogInformation("收到同步城市数据请求");
        var result = await _indexSyncService.SyncAllCitiesAsync();
        return Ok(new ApiResponse<object>
        {
            Success = result.Success,
            Message = result.Success ? "城市数据同步完成" : result.ErrorMessage,
            Data = result
        });
    }

    /// <summary>
    /// 同步共享办公空间数据
    /// </summary>
    [HttpPost("sync/coworkings")]
    public async Task<IActionResult> SyncCoworkings()
    {
        _logger.LogInformation("收到同步共享办公空间数据请求");
        var result = await _indexSyncService.SyncAllCoworkingsAsync();
        return Ok(new ApiResponse<object>
        {
            Success = result.Success,
            Message = result.Success ? "共享办公空间数据同步完成" : result.ErrorMessage,
            Data = result
        });
    }

    /// <summary>
    /// 重建所有索引
    /// </summary>
    [HttpPost("rebuild")]
    public async Task<IActionResult> RebuildAll()
    {
        _logger.LogInformation("收到重建索引请求");
        var result = await _indexSyncService.RebuildAllIndexesAsync();
        return Ok(new ApiResponse<object>
        {
            Success = result.Success,
            Message = result.Success ? "索引重建完成" : result.ErrorMessage,
            Data = result
        });
    }

    /// <summary>
    /// 同步单个城市
    /// </summary>
    [HttpPost("sync/cities/{id:guid}")]
    public async Task<IActionResult> SyncCity(Guid id)
    {
        var success = await _indexSyncService.SyncCityAsync(id);
        return Ok(new ApiResponse<object>
        {
            Success = success,
            Message = success ? "城市同步成功" : "城市同步失败"
        });
    }

    /// <summary>
    /// 同步单个共享办公空间
    /// </summary>
    [HttpPost("sync/coworkings/{id:guid}")]
    public async Task<IActionResult> SyncCoworking(Guid id)
    {
        var success = await _indexSyncService.SyncCoworkingAsync(id);
        return Ok(new ApiResponse<object>
        {
            Success = success,
            Message = success ? "共享办公空间同步成功" : "共享办公空间同步失败"
        });
    }
}

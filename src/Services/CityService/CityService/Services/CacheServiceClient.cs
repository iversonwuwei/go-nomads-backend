using Dapr.Client;

namespace CityService.Services;

/// <summary>
/// CacheService 客户端接口
/// </summary>
public interface ICacheServiceClient
{
    /// <summary>
    /// 更新城市评分缓存
    /// </summary>
    Task UpdateCityScoreCacheAsync(string cityId, decimal overallScore, string? statistics = null);
    
    /// <summary>
    /// 更新城市费用缓存
    /// </summary>
    Task UpdateCityCostCacheAsync(string cityId, decimal averageCost, string? statistics = null);
}

/// <summary>
/// CacheService 客户端实现 - 通过 Dapr Service Invocation 调用
/// </summary>
public class CacheServiceClient : ICacheServiceClient
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<CacheServiceClient> _logger;
    private readonly string _cacheServiceAppId;

    public CacheServiceClient(
        DaprClient daprClient,
        IConfiguration configuration,
        ILogger<CacheServiceClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
        _cacheServiceAppId = configuration["Dapr:CacheServiceAppId"] ?? "cache-service";
    }

    public async Task UpdateCityScoreCacheAsync(string cityId, decimal overallScore, string? statistics = null)
    {
        try
        {
            _logger.LogInformation("Updating score cache for city {CityId}", cityId);
            
            var request = new
            {
                OverallScore = overallScore,
                Statistics = statistics
            };

            await _daprClient.InvokeMethodAsync(
                HttpMethod.Put,
                _cacheServiceAppId,
                $"api/v1/cache/scores/city/{cityId}",
                request
            );

            _logger.LogInformation("Successfully updated score cache for city {CityId}", cityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update score cache for city {CityId}", cityId);
            // 不抛出异常,避免影响主流程
        }
    }

    public async Task UpdateCityCostCacheAsync(string cityId, decimal averageCost, string? statistics = null)
    {
        try
        {
            _logger.LogInformation("Updating cost cache for city {CityId}", cityId);
            
            var request = new
            {
                AverageCost = averageCost,
                Statistics = statistics
            };

            await _daprClient.InvokeMethodAsync(
                HttpMethod.Put,
                _cacheServiceAppId,
                $"api/v1/cache/costs/city/{cityId}",
                request
            );

            _logger.LogInformation("Successfully updated cost cache for city {CityId}", cityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cost cache for city {CityId}", cityId);
            // 不抛出异常,避免影响主流程
        }
    }
}

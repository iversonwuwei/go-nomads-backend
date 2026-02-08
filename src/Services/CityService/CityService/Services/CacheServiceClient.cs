using System.Net.Http.Json;

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
/// CacheService 客户端实现 - 通过 HttpClient 调用
/// </summary>
public class CacheServiceClient : ICacheServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CacheServiceClient> _logger;

    public CacheServiceClient(
        HttpClient httpClient,
        ILogger<CacheServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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

            var resp = await _httpClient.PutAsJsonAsync($"api/v1/cache/scores/city/{cityId}", request);
            resp.EnsureSuccessStatusCode();

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

            var resp = await _httpClient.PutAsJsonAsync($"api/v1/cache/costs/city/{cityId}", request);
            resp.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully updated cost cache for city {CityId}", cityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cost cache for city {CityId}", cityId);
            // 不抛出异常,避免影响主流程
        }
    }
}

using Dapr.Client;

namespace CoworkingService.Services;

/// <summary>
/// CacheService 客户端接口
/// </summary>
public interface ICacheServiceClient
{
    /// <summary>
    /// 更新 Coworking 评分缓存
    /// </summary>
    Task UpdateCoworkingScoreCacheAsync(Guid coworkingId, double averageRating, int reviewCount);
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

    public async Task UpdateCoworkingScoreCacheAsync(Guid coworkingId, double averageRating, int reviewCount)
    {
        try
        {
            _logger.LogInformation("📊 更新 Coworking {CoworkingId} 评分缓存: Rating={Rating}, ReviewCount={Count}", 
                coworkingId, averageRating, reviewCount);
            
            var request = new
            {
                OverallScore = averageRating,
                Statistics = $"{{\"reviewCount\":{reviewCount},\"averageRating\":{averageRating}}}"
            };

            await _daprClient.InvokeMethodAsync(
                HttpMethod.Put,
                _cacheServiceAppId,
                $"api/v1/cache/scores/coworking/{coworkingId}",
                request
            );

            _logger.LogInformation("✅ Coworking {CoworkingId} 评分缓存更新成功", coworkingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新 Coworking {CoworkingId} 评分缓存失败", coworkingId);
            // 不抛出异常,避免影响主流程
        }
    }
}

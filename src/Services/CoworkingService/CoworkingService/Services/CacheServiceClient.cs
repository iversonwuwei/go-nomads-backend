using System.Net.Http.Json;

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

            (await _httpClient.PutAsJsonAsync(
                $"api/v1/cache/scores/coworking/{coworkingId}",
                request
            )).EnsureSuccessStatusCode();

            _logger.LogInformation("✅ Coworking {CoworkingId} 评分缓存更新成功", coworkingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新 Coworking {CoworkingId} 评分缓存失败", coworkingId);
            // 不抛出异常,避免影响主流程
        }
    }
}

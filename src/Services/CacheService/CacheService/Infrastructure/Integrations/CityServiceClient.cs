using System.Text.Json;
using Dapr.Client;

namespace CacheService.Infrastructure.Integrations;

/// <summary>
/// CityService 客户端实现 (通过 Dapr Service Invocation)
/// </summary>
public class CityServiceClient : ICityServiceClient
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<CityServiceClient> _logger;
    private const string CityServiceAppId = "city-service";

    public CityServiceClient(DaprClient daprClient, ILogger<CityServiceClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<CityScoreDto> CalculateCityScoreAsync(string cityId)
    {
        try
        {
            _logger.LogInformation("Calling CityService to calculate score for city {CityId}", cityId);
            
            // 调用 CityService 的评分统计接口
            var response = await _daprClient.InvokeMethodAsync<CityRatingStatsResponse>(
                HttpMethod.Get,
                CityServiceAppId,
                $"api/v1/cities/{cityId}/ratings/statistics"
            );

            if (response?.Statistics == null || !response.Statistics.Any())
            {
                _logger.LogWarning("No rating statistics found for city {CityId}", cityId);
                return new CityScoreDto
                {
                    CityId = cityId,
                    OverallScore = 0,
                    Statistics = null
                };
            }

            // 计算总评分 (有评分的分类的平均分)
            var overallScore = response.Statistics
                .Where(s => s.RatingCount > 0)
                .Average(s => s.AverageRating);

            return new CityScoreDto
            {
                CityId = cityId,
                OverallScore = overallScore,
                Statistics = response.Statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating city score for city {CityId}", cityId);
            throw;
        }
    }

    public async Task<List<CityScoreDto>> CalculateCityScoresBatchAsync(IEnumerable<string> cityIds)
    {
        var result = new List<CityScoreDto>();
        var cityIdList = cityIds.ToList();

        _logger.LogInformation("Calculating batch city scores for {Count} cities", cityIdList.Count);

        // 并发调用多个城市的评分计算
        var tasks = cityIdList.Select(async cityId =>
        {
            try
            {
                return await CalculateCityScoreAsync(cityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating score for city {CityId} in batch", cityId);
                return new CityScoreDto { CityId = cityId, OverallScore = 0 };
            }
        });

        result = (await Task.WhenAll(tasks)).ToList();
        return result;
    }
}

/// <summary>
/// CityService 评分统计响应
/// </summary>
internal class CityRatingStatsResponse
{
    public List<CategoryStatistics>? Statistics { get; set; }
}

internal class CategoryStatistics
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
}

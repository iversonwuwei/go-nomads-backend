using System.Text.Json;
using GoNomads.Shared.Communication;

namespace CacheService.Infrastructure.Integrations;

/// <summary>
/// CityService 客户端实现
/// </summary>
public class CityServiceClient : ICityServiceClient
{
    private readonly ILogger<CityServiceClient> _logger;
    private readonly ServiceInvocationClient _serviceInvocationClient;
    private const string CityServiceAppId = "city-service";

    public CityServiceClient(ServiceInvocationClient serviceInvocationClient, ILogger<CityServiceClient> logger)
    {
        _serviceInvocationClient = serviceInvocationClient;
        _logger = logger;
    }

    public async Task<CityScoreDto> CalculateCityScoreAsync(string cityId)
    {
        try
        {
            _logger.LogInformation("Calling CityService to calculate score for city {CityId}", cityId);

            var response = await _serviceInvocationClient.InvokeAsync<CityRatingStatsResponse>(
                HttpMethod.Get,
                CityServiceAppId,
                $"api/v1/cities/{cityId}/ratings/statistics");

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
            var ratingsWithData = response.Statistics.Where(s => s.RatingCount > 0).ToList();
            var overallScore = ratingsWithData.Any() 
                ? ratingsWithData.Average(s => s.AverageRating)
                : 0.0;

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
            // 返回默认值而不是抛出异常
            return new CityScoreDto
            {
                CityId = cityId,
                OverallScore = 0,
                Statistics = null
            };
        }
    }

    public async Task<List<CityScoreDto>> CalculateCityScoresBatchAsync(IEnumerable<string> cityIds)
    {
        var result = new List<CityScoreDto>();
        var cityIdList = cityIds.ToList();

        if (cityIdList.Count == 0)
            return result;

        _logger.LogInformation("🚀 Calculating batch city scores for {Count} cities using batch API", cityIdList.Count);

        try
        {
            // 使用新的批量 API 端点（一次请求获取所有城市的评分统计）
            var response = await _serviceInvocationClient.InvokeAsync<List<string>, BatchCityRatingStatsResponse>(
                HttpMethod.Post,
                CityServiceAppId,
                "api/v1/cities/ratings/statistics/batch",
                cityIdList
            );

            if (response?.CityStatistics != null)
            {
                foreach (var kvp in response.CityStatistics)
                {
                    var cityId = kvp.Key;
                    var stats = kvp.Value;
                    // 计算总评分 (有评分的分类的平均分)
                    var ratingsWithData = stats.Statistics?.Where(s => s.RatingCount > 0).ToList() ?? new List<CategoryStatistics>();
                    var overallScore = ratingsWithData.Any()
                        ? ratingsWithData.Average(s => s.AverageRating)
                        : 0.0;

                    result.Add(new CityScoreDto
                    {
                        CityId = cityId,
                        OverallScore = overallScore,
                        Statistics = stats.Statistics
                    });
                }

                _logger.LogInformation("✅ Batch city scores calculated: {Count} cities", result.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Batch API failed, falling back to individual calls for {Count} cities", cityIdList.Count);

            // 降级：如果批量 API 失败，回退到单独调用（但这会慢很多）
            var tasks = cityIdList.Select(async cityId =>
            {
                try
                {
                    return await CalculateCityScoreAsync(cityId);
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Error calculating score for city {CityId} in fallback", cityId);
                    return new CityScoreDto { CityId = cityId, OverallScore = 0 };
                }
            });

            result = (await Task.WhenAll(tasks)).ToList();
        }

        return result;
    }

    public async Task<CityCostDto> CalculateCityCostAsync(string cityId)
    {
        try
        {
            _logger.LogInformation("Calling CityService to calculate cost for city {CityId}", cityId);

            // 调用 CityService 的费用统计接口
            var response = await _serviceInvocationClient.InvokeAsync<CityCostStatsResponse>(
                HttpMethod.Get,
                CityServiceAppId,
                $"api/v1/cities/{cityId}/expenses/statistics"
            );

            if (response == null || response.TotalAverageCost == 0)
            {
                _logger.LogWarning("No expense statistics found for city {CityId}", cityId);
                return new CityCostDto
                {
                    CityId = cityId,
                    AverageCost = 0,
                    Statistics = null
                };
            }

            // 将完整的统计信息序列化为 JSON 字符串
            var statisticsJson = JsonSerializer.Serialize(new
            {
                response.TotalAverageCost,
                response.CategoryCosts,
                response.ContributorCount,
                response.TotalExpenseCount,
                response.Currency,
                response.UpdatedAt
            });

            return new CityCostDto
            {
                CityId = cityId,
                AverageCost = response.TotalAverageCost,
                Statistics = statisticsJson
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating city cost for city {CityId}", cityId);
            // 返回默认值而不是抛出异常
            return new CityCostDto
            {
                CityId = cityId,
                AverageCost = 0,
                Statistics = null
            };
        }
    }

    public async Task<List<CityCostDto>> CalculateCityCostsBatchAsync(IEnumerable<string> cityIds)
    {
        var result = new List<CityCostDto>();
        var cityIdList = cityIds.ToList();

        _logger.LogInformation("Calculating batch city costs for {Count} cities", cityIdList.Count);

        // 并发调用多个城市的费用计算
        var tasks = cityIdList.Select(async cityId =>
        {
            try
            {
                return await CalculateCityCostAsync(cityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cost for city {CityId} in batch", cityId);
                return new CityCostDto { CityId = cityId, AverageCost = 0 };
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

/// <summary>
/// 批量城市评分统计响应
/// </summary>
internal class BatchCityRatingStatsResponse
{
    public Dictionary<string, CityRatingStatsResponse> CityStatistics { get; set; } = new();
}

/// <summary>
/// CityService 费用统计响应
/// </summary>
internal class CityCostStatsResponse
{
    public decimal TotalAverageCost { get; set; }
    public Dictionary<string, decimal> CategoryCosts { get; set; } = new();
    public int ContributorCount { get; set; }
    public int TotalExpenseCount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime UpdatedAt { get; set; }
}

internal class CategoryStatistics
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
}

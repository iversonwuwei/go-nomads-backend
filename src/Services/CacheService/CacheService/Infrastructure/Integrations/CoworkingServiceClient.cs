using System.Net.Http.Json;

namespace CacheService.Infrastructure.Integrations;

/// <summary>
/// CoworkingService 客户端实现 (通过 HttpClient)
/// </summary>
public class CoworkingServiceClient : ICoworkingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoworkingServiceClient> _logger;

    public CoworkingServiceClient(HttpClient httpClient, ILogger<CoworkingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CoworkingScoreDto> GetCoworkingScoreAsync(string coworkingId)
    {
        try
        {
            _logger.LogInformation("Calling CoworkingService to get score for coworking {CoworkingId}", coworkingId);

            // 调用 CoworkingService 获取详情
            var response = await _httpClient.GetFromJsonAsync<CoworkingDetailResponse>(
                $"api/coworkings/{coworkingId}"
            );

            return new CoworkingScoreDto
            {
                Id = coworkingId,
                Rating = response.Rating,
                ReviewCount = response.ReviewCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting coworking score for coworking {CoworkingId}", coworkingId);
            throw;
        }
    }

    public async Task<List<CoworkingScoreDto>> GetCoworkingScoresBatchAsync(IEnumerable<string> coworkingIds)
    {
        var result = new List<CoworkingScoreDto>();
        var coworkingIdList = coworkingIds.ToList();

        _logger.LogInformation("Getting batch coworking scores for {Count} coworkings", coworkingIdList.Count);

        // 并发调用多个共享办公空间
        var tasks = coworkingIdList.Select(async coworkingId =>
        {
            try
            {
                return await GetCoworkingScoreAsync(coworkingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting score for coworking {CoworkingId} in batch", coworkingId);
                return new CoworkingScoreDto { Id = coworkingId, Rating = 0, ReviewCount = 0 };
            }
        });

        result = (await Task.WhenAll(tasks)).ToList();
        return result;
    }
}

/// <summary>
/// CoworkingService 详情响应
/// </summary>
internal class CoworkingDetailResponse
{
    public string Id { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
}

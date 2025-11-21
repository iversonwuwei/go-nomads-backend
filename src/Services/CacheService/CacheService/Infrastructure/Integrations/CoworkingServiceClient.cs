using Dapr.Client;

namespace CacheService.Infrastructure.Integrations;

/// <summary>
/// CoworkingService 客户端实现 (通过 Dapr Service Invocation)
/// </summary>
public class CoworkingServiceClient : ICoworkingServiceClient
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<CoworkingServiceClient> _logger;
    private const string CoworkingServiceAppId = "coworking-service";

    public CoworkingServiceClient(DaprClient daprClient, ILogger<CoworkingServiceClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<CoworkingScoreDto> GetCoworkingScoreAsync(string coworkingId)
    {
        try
        {
            _logger.LogInformation("Calling CoworkingService to get score for coworking {CoworkingId}", coworkingId);
            
            // 调用 CoworkingService 获取详情
            var response = await _daprClient.InvokeMethodAsync<CoworkingDetailResponse>(
                HttpMethod.Get,
                CoworkingServiceAppId,
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

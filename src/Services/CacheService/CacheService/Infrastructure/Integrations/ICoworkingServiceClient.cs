namespace CacheService.Infrastructure.Integrations;

/// <summary>
/// Coworking Service 响应 DTO
/// </summary>
public class CoworkingScoreDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
}

/// <summary>
/// CoworkingService 客户端接口
/// </summary>
public interface ICoworkingServiceClient
{
    Task<CoworkingScoreDto> GetCoworkingScoreAsync(string coworkingId);
    Task<List<CoworkingScoreDto>> GetCoworkingScoresBatchAsync(IEnumerable<string> coworkingIds);
}

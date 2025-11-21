namespace CacheService.Infrastructure.Integrations;

/// <summary>
/// City Service 响应 DTO
/// </summary>
public class CityScoreDto
{
    public string CityId { get; set; } = string.Empty;
    public double OverallScore { get; set; }
    public object? Statistics { get; set; }
}

/// <summary>
/// City Cost 响应 DTO
/// </summary>
public class CityCostDto
{
    public string CityId { get; set; } = string.Empty;
    public decimal AverageCost { get; set; }
    public object? Statistics { get; set; }
}

/// <summary>
/// CityService 客户端接口
/// </summary>
public interface ICityServiceClient
{
    Task<CityScoreDto> CalculateCityScoreAsync(string cityId);
    Task<List<CityScoreDto>> CalculateCityScoresBatchAsync(IEnumerable<string> cityIds);
    Task<CityCostDto> CalculateCityCostAsync(string cityId);
    Task<List<CityCostDto>> CalculateCityCostsBatchAsync(IEnumerable<string> cityIds);
}

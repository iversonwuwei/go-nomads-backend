namespace CacheService.Application.DTOs;

/// <summary>
/// 分数响应 DTO
/// </summary>
public class ScoreResponseDto
{
    public string EntityId { get; set; } = string.Empty;
    public decimal OverallScore { get; set; }
    public bool FromCache { get; set; }
    public string? Statistics { get; set; }
}

/// <summary>
/// 批量分数响应 DTO
/// </summary>
public class BatchScoreResponseDto
{
    public List<ScoreResponseDto> Scores { get; set; } = new();
    public int TotalCount { get; set; }
    public int CachedCount { get; set; }
    public int CalculatedCount { get; set; }
}

/// <summary>
/// 缓存失效请求 DTO
/// </summary>
public class InvalidateCacheRequestDto
{
    public string EntityId { get; set; } = string.Empty;
}

/// <summary>
/// 批量缓存失效请求 DTO
/// </summary>
public class BatchInvalidateCacheRequestDto
{
    public List<string> EntityIds { get; set; } = new();
}

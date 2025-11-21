namespace CacheService.Application.DTOs;

/// <summary>
/// 费用响应 DTO
/// </summary>
public class CostResponseDto
{
    public string EntityId { get; set; } = string.Empty;
    public decimal AverageCost { get; set; }
    public bool FromCache { get; set; }
    public string? Statistics { get; set; }
}

/// <summary>
/// 批量费用响应 DTO
/// </summary>
public class BatchCostResponseDto
{
    public List<CostResponseDto> Costs { get; set; } = new();
    public int TotalCount { get; set; }
    public int CachedCount { get; set; }
    public int CalculatedCount { get; set; }
}

namespace CityService.Domain.ValueObjects;

/// <summary>
///     城市搜索条件 - 用于仓储查询
/// </summary>
public class CitySearchCriteria
{
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public decimal? MinCostOfLiving { get; set; }
    public decimal? MaxCostOfLiving { get; set; }
    public decimal? MinScore { get; set; }
    public List<string>? Tags { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
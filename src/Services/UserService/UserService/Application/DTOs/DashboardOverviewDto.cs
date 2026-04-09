namespace UserService.Application.DTOs;

/// <summary>
///     管理后台概览数据
/// </summary>
public class DashboardOverviewDto
{
    public DateTime CalculatedDate { get; set; }
    public DashboardUserMetricsDto Users { get; set; } = new();
}

public class DashboardUserMetricsDto
{
    public int TotalUsers { get; set; }
    public int NewUsers { get; set; }
}
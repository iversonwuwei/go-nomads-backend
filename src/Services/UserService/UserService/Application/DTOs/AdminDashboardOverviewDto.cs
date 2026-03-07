namespace UserService.Application.DTOs;

public class AdminDashboardOverviewDto
{
    public string CalculatedDate { get; set; } = string.Empty;
    public AdminDashboardUserMetricsDto Users { get; set; } = new();
}

public class AdminDashboardUserMetricsDto
{
    public int TotalUsers { get; set; }
    public int NewUsers { get; set; }
}

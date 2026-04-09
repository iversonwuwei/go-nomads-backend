using UserService.Application.DTOs;

namespace UserService.Application.Services;

public interface IReportService
{
    Task<List<ReportDto>> GetReportsAsync(CancellationToken cancellationToken = default);
    Task<ReportDto?> GetReportByIdAsync(string reportId, CancellationToken cancellationToken = default);
    Task<ReportActionResponse> ApplyActionAsync(string reportId, string action, string adminUserId, string? note,
        CancellationToken cancellationToken = default);
}
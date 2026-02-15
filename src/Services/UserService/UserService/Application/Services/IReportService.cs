using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
///     举报服务接口
/// </summary>
public interface IReportService
{
    /// <summary>
    ///     创建举报记录（保存到数据库并通知管理员）
    /// </summary>
    Task<ReportDto> CreateReportAsync(
        string reporterId,
        string? reporterName,
        CreateReportDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取举报记录详情
    /// </summary>
    Task<ReportDto?> GetReportByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取所有举报记录（管理员分页查询）
    /// </summary>
    Task<(List<ReportDto> Items, int Total)> GetAllReportsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取当前用户的举报记录
    /// </summary>
    Task<List<ReportDto>> GetMyReportsAsync(string reporterId, CancellationToken cancellationToken = default);
}

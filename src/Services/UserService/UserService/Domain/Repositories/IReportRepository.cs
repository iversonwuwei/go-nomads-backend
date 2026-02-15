using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     举报记录仓储接口
/// </summary>
public interface IReportRepository
{
    /// <summary>
    ///     创建举报记录
    /// </summary>
    Task<Report> CreateAsync(Report report, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取举报记录
    /// </summary>
    Task<Report?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取所有举报记录（分页，管理员使用）
    /// </summary>
    Task<(List<Report> Items, int Total)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户提交的举报记录
    /// </summary>
    Task<List<Report>> GetByReporterIdAsync(string reporterId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新举报记录状态
    /// </summary>
    Task<Report> UpdateAsync(Report report, CancellationToken cancellationToken = default);
}

using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IReportRepository
{
    Task<List<Report>> ListAsync(CancellationToken cancellationToken = default);
    Task<Report?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Report> UpdateAsync(Report report, CancellationToken cancellationToken = default);
}
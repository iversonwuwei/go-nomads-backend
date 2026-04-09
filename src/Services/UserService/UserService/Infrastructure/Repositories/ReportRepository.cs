using Postgrest;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly ILogger<ReportRepository> _logger;
    private readonly Client _supabaseClient;

    public ReportRepository(Client supabaseClient, ILogger<ReportRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<Report>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Report>()
                .Order("created_at", Constants.Ordering.Descending)
                .Get(cancellationToken: cancellationToken);

            return response.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取举报列表失败");
            throw;
        }
    }

    public async Task<Report?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _supabaseClient
                .From<Report>()
                .Where(r => r.Id == id)
                .Single(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到举报: {ReportId}", id);
            return null;
        }
    }

    public async Task<Report> UpdateAsync(Report report, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Report>()
                .Where(r => r.Id == report.Id)
                .Update(report, cancellationToken: cancellationToken);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                throw new KeyNotFoundException($"举报不存在: {report.Id}");
            }

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新举报失败: {ReportId}", report.Id);
            throw;
        }
    }
}
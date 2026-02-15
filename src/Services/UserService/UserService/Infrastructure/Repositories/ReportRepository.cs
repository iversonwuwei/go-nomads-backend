using Postgrest;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     举报记录仓储 Supabase 实现
/// </summary>
public class ReportRepository : IReportRepository
{
    private readonly ILogger<ReportRepository> _logger;
    private readonly Client _supabaseClient;

    public ReportRepository(Client supabaseClient, ILogger<ReportRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<Report> CreateAsync(Report report, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建举报记录: ContentType={ContentType}, TargetId={TargetId}, ReporterId={ReporterId}",
            report.ContentType, report.TargetId, report.ReporterId);

        try
        {
            var result = await _supabaseClient
                .From<Report>()
                .Insert(report, cancellationToken: cancellationToken);

            var created = result.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建举报记录失败");

            _logger.LogInformation("✅ 成功创建举报记录: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建举报记录失败: ContentType={ContentType}, TargetId={TargetId}",
                report.ContentType, report.TargetId);
            throw;
        }
    }

    public async Task<Report?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据 ID 查询举报记录: {Id}", id);

        try
        {
            var response = await _supabaseClient
                .From<Report>()
                .Where(r => r.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到举报记录: {Id}", id);
            return null;
        }
    }

    public async Task<(List<Report> Items, int Total)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询举报记录列表: Page={Page}, PageSize={PageSize}, Status={Status}",
            page, pageSize, status);

        try
        {
            var offset = (page - 1) * pageSize;

            // 获取总数
            int total;
            if (!string.IsNullOrEmpty(status))
            {
                total = await _supabaseClient
                    .From<Report>()
                    .Where(r => r.Status == status)
                    .Count(Constants.CountType.Exact, cancellationToken);
            }
            else
            {
                total = await _supabaseClient
                    .From<Report>()
                    .Count(Constants.CountType.Exact, cancellationToken);
            }

            // 获取分页数据
            Postgrest.Responses.ModeledResponse<Report> response;
            if (!string.IsNullOrEmpty(status))
            {
                response = await _supabaseClient
                    .From<Report>()
                    .Where(r => r.Status == status)
                    .Order(r => r.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get(cancellationToken);
            }
            else
            {
                response = await _supabaseClient
                    .From<Report>()
                    .Order(r => r.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get(cancellationToken);
            }

            _logger.LogInformation("✅ 查询到 {Count}/{Total} 条举报记录", response.Models.Count, total);
            return (response.Models, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询举报记录列表失败");
            throw;
        }
    }

    public async Task<List<Report>> GetByReporterIdAsync(string reporterId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户举报记录: ReporterId={ReporterId}", reporterId);

        try
        {
            var response = await _supabaseClient
                .From<Report>()
                .Where(r => r.ReporterId == reporterId)
                .Order(r => r.CreatedAt, Constants.Ordering.Descending)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 查询到 {Count} 条举报记录", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询用户举报记录失败: ReporterId={ReporterId}", reporterId);
            throw;
        }
    }

    public async Task<Report> UpdateAsync(Report report, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新举报记录: {Id}", report.Id);

        try
        {
            report.UpdatedAt = DateTime.UtcNow;

            var result = await _supabaseClient
                .From<Report>()
                .Where(r => r.Id == report.Id)
                .Update(report, cancellationToken: cancellationToken);

            var updated = result.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新举报记录失败");

            _logger.LogInformation("✅ 成功更新举报记录: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新举报记录失败: {Id}", report.Id);
            throw;
        }
    }
}

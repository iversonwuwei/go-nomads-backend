using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Services;

/// <summary>
///     举报服务实现 - 保存举报记录到数据库并通知管理员
/// </summary>
public class ReportApplicationService : IReportService
{
    private readonly ILogger<ReportApplicationService> _logger;
    private readonly IReportRepository _reportRepository;
    private readonly IMessageServiceClient _messageServiceClient;

    public ReportApplicationService(
        IReportRepository reportRepository,
        IMessageServiceClient messageServiceClient,
        ILogger<ReportApplicationService> logger)
    {
        _reportRepository = reportRepository;
        _messageServiceClient = messageServiceClient;
        _logger = logger;
    }

    public async Task<ReportDto> CreateReportAsync(
        string reporterId,
        string? reporterName,
        CreateReportDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建举报记录 - ReporterId: {ReporterId}, ContentType: {ContentType}, TargetId: {TargetId}",
            reporterId, dto.ContentType, dto.TargetId);

        // 1. 保存到数据库
        var report = Report.Create(
            reporterId,
            reporterName,
            dto.ContentType,
            dto.TargetId,
            dto.TargetName,
            dto.ReasonId,
            dto.ReasonLabel);

        var created = await _reportRepository.CreateAsync(report, cancellationToken);
        _logger.LogInformation("✅ 举报记录已保存: {Id}", created.Id);

        // 2. 异步通知管理员（不阻塞主流程）
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyAdminsAsync(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ 通知管理员失败，但举报记录已保存: {ReportId}", created.Id);
            }
        }, cancellationToken);

        return MapToDto(created);
    }

    public async Task<ReportDto?> GetReportByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(id, cancellationToken);
        return report != null ? MapToDto(report) : null;
    }

    public async Task<(List<ReportDto> Items, int Total)> GetAllReportsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _reportRepository.GetAllAsync(page, pageSize, status, cancellationToken);
        return (items.Select(MapToDto).ToList(), total);
    }

    public async Task<List<ReportDto>> GetMyReportsAsync(string reporterId, CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.GetByReporterIdAsync(reporterId, cancellationToken);
        return reports.Select(MapToDto).ToList();
    }

    #region 私有方法

    /// <summary>
    ///     通知管理员有新的举报
    /// </summary>
    private async Task NotifyAdminsAsync(Report report)
    {
        var contentTypeLabel = GetContentTypeLabel(report.ContentType);
        var title = $"⚠️ 用户举报: {contentTypeLabel}";
        var message = BuildReportMessage(report, contentTypeLabel);

        await _messageServiceClient.SendNotificationToAdminsAsync(
            title,
            message,
            "user_report",
            report.TargetId,
            new Dictionary<string, object>
            {
                ["reportId"] = report.Id,
                ["reportContentType"] = report.ContentType,
                ["targetId"] = report.TargetId,
                ["targetName"] = report.TargetName ?? "",
                ["reasonId"] = report.ReasonId,
                ["reasonLabel"] = report.ReasonLabel,
                ["reporterId"] = report.ReporterId,
                ["reporterName"] = report.ReporterName ?? ""
            });

        _logger.LogInformation("✅ 已通知管理员: ReportId={ReportId}", report.Id);
    }

    private static string GetContentTypeLabel(string contentType)
    {
        return contentType switch
        {
            "user" => "用户",
            "message" => "聊天消息",
            "meetup" => "聚会活动",
            "innovationProject" => "创意项目",
            "chatRoom" => "聊天室",
            _ => contentType
        };
    }

    private static string BuildReportMessage(Report report, string contentTypeLabel)
    {
        var lines = new List<string>
        {
            $"举报类型: {contentTypeLabel}"
        };

        if (!string.IsNullOrEmpty(report.TargetName))
            lines.Add($"举报对象: {report.TargetName}");

        lines.Add($"举报原因: {report.ReasonLabel}");
        lines.Add($"举报人: {report.ReporterName ?? "未知"}");
        lines.Add($"举报人ID: {report.ReporterId}");
        lines.Add($"被举报ID: {report.TargetId}");
        lines.Add($"举报记录ID: {report.Id}");

        return string.Join("\n", lines);
    }

    private static ReportDto MapToDto(Report report)
    {
        return new ReportDto
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            ReporterName = report.ReporterName,
            ContentType = report.ContentType,
            TargetId = report.TargetId,
            TargetName = report.TargetName,
            ReasonId = report.ReasonId,
            ReasonLabel = report.ReasonLabel,
            Status = report.Status,
            AdminNotes = report.AdminNotes,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    #endregion
}

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

        // 2. 异步通知相关人员（管理员 + 城市举报额外通知版主，不阻塞主流程）
        // 注意: 使用 CancellationToken.None，因为 HTTP 请求结束后原始 token 会被取消，
        // 导致通知任务还未执行就被中止
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyReportAsync(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ 通知失败，但举报记录已保存: {ReportId}", created.Id);
            }
        }, CancellationToken.None);

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

    public async Task<ReportDto> HandleReportActionAsync(
        string reportId,
        string action,
        string adminId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var normalizedAction = (action ?? string.Empty).Trim().ToLowerInvariant();
        var targetStatus = normalizedAction switch
        {
            "assign" => "reviewed",
            "resolve" => "resolved",
            "dismiss" => "dismissed",
            _ => throw new ArgumentException($"Unsupported action: {action}", nameof(action))
        };

        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report == null)
            throw new KeyNotFoundException($"Report not found: {reportId}");

        report.Status = targetStatus;
        report.AdminNotes = string.IsNullOrWhiteSpace(note)
            ? $"action={normalizedAction}; admin={adminId}; at={DateTime.UtcNow:O}"
            : $"action={normalizedAction}; admin={adminId}; note={note}";
        report.UpdatedAt = DateTime.UtcNow;

        var updated = await _reportRepository.UpdateAsync(report, cancellationToken);
        _logger.LogInformation(
            "✅ 举报处置成功: ReportId={ReportId}, Action={Action}, Status={Status}, AdminId={AdminId}",
            reportId,
            normalizedAction,
            updated.Status,
            adminId);

        return MapToDto(updated);
    }

    #region 私有方法

    /// <summary>
    ///     通知相关人员有新的举报
    ///     所有举报类型都会通知管理员；城市举报额外通知城市版主
    /// </summary>
    private async Task NotifyReportAsync(Report report)
    {
        var contentTypeLabel = GetContentTypeLabel(report.ContentType);
        var title = $"⚠️ 用户举报: {contentTypeLabel}";
        var message = BuildReportMessage(report, contentTypeLabel);
        var metadata = new Dictionary<string, object>
        {
            ["reportId"] = report.Id,
            ["reportContentType"] = report.ContentType,
            ["targetId"] = report.TargetId,
            ["targetName"] = report.TargetName ?? "",
            ["reasonId"] = report.ReasonId,
            ["reasonLabel"] = report.ReasonLabel,
            ["reporterId"] = report.ReporterId,
            ["reporterName"] = report.ReporterName ?? ""
        };

        // 1. 所有举报都通知管理员
        await _messageServiceClient.SendNotificationToAdminsAsync(
            title,
            message,
            "user_report",
            report.TargetId,
            metadata);

        _logger.LogInformation("✅ 已通知管理员: ReportId={ReportId}", report.Id);

        // 2. 城市举报额外通知城市版主
        if (report.ContentType == "city")
        {
            try
            {
                var moderatorTitle = $"⚠️ 城市举报通知: {report.TargetName ?? report.TargetId}";
                await _messageServiceClient.SendNotificationToCityModeratorsAsync(
                    report.TargetId,
                    moderatorTitle,
                    message,
                    "city_report",
                    report.TargetId,
                    metadata);

                _logger.LogInformation("✅ 已通知城市版主: ReportId={ReportId}, CityId={CityId}",
                    report.Id, report.TargetId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ 通知城市版主失败，但管理员已通知: ReportId={ReportId}", report.Id);
            }
        }
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
            "city" => "城市",
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

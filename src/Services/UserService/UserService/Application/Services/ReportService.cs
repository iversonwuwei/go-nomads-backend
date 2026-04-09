using System.Text.Json;
using GoNomads.Shared.Communication;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Services;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly IReportRepository _reportRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICityServiceClient _cityServiceClient;
    private readonly ServiceInvocationClient _serviceInvocationClient;

    public ReportService(
        IReportRepository reportRepository,
        IUserRepository userRepository,
        ICityServiceClient cityServiceClient,
        ServiceInvocationClient serviceInvocationClient,
        ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _userRepository = userRepository;
        _cityServiceClient = cityServiceClient;
        _serviceInvocationClient = serviceInvocationClient;
        _logger = logger;
    }

    public async Task<List<ReportDto>> GetReportsAsync(CancellationToken cancellationToken = default)
    {
        var reports = await _reportRepository.ListAsync(cancellationToken);
        var tasks = reports.Select(report => MapToDtoAsync(report, cancellationToken));
        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<ReportDto?> GetReportByIdAsync(string reportId, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        return report == null ? null : await MapToDtoAsync(report, cancellationToken);
    }

    public async Task<ReportActionResponse> ApplyActionAsync(string reportId, string action, string adminUserId,
        string? note, CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report == null)
        {
            throw new KeyNotFoundException("举报不存在");
        }

        report.ApplyAction(action, adminUserId, note);
        var updated = await _reportRepository.UpdateAsync(report, cancellationToken);

        _logger.LogInformation("✅ 举报已执行动作: ReportId={ReportId}, Action={Action}, Status={Status}, Admin={Admin}",
            updated.Id, action, updated.Status, adminUserId);

        return new ReportActionResponse
        {
            ReportId = updated.Id,
            Action = action,
            Status = updated.Status,
            OperatedAt = updated.UpdatedAt
        };
    }

    private async Task<ReportDto> MapToDtoAsync(Report report, CancellationToken cancellationToken)
    {
        var reporterTask = ResolveReporterAsync(report, cancellationToken);
        var targetTask = ResolveTargetAsync(report, cancellationToken);

        await Task.WhenAll(reporterTask, targetTask);

        var reporter = await reporterTask;
        var target = await targetTask;

        return new ReportDto
        {
            Id = report.Id,
            ReporterId = report.ReporterId,
            ReporterName = FirstNonEmpty(reporter.DisplayName, report.ReporterNameSnapshot),
            ReporterDisplayName = FirstNonEmpty(reporter.DisplayName, report.ReporterNameSnapshot, report.ReporterId),
            ReporterSummary = reporter.Summary,
            ContentType = report.ContentType,
            TargetId = report.TargetId,
            TargetName = FirstNonEmpty(target.DisplayName, report.TargetNameSnapshot),
            TargetDisplayName = FirstNonEmpty(target.DisplayName, report.TargetNameSnapshot, report.TargetId),
            TargetSummary = target.Summary,
            ReasonId = report.ReasonId,
            ReasonLabel = report.ReasonLabel,
            Status = report.Status,
            AdminNotes = report.AdminNotes ?? string.Empty,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    private async Task<ResolvedReference> ResolveReporterAsync(Report report, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(report.ReporterId))
        {
            return ResolvedReference.FromFallback(report.ReporterNameSnapshot);
        }

        try
        {
            var user = await _userRepository.GetByIdAsync(report.ReporterId, cancellationToken);
            if (user == null)
            {
                return ResolvedReference.FromFallback(report.ReporterNameSnapshot);
            }

            return new ResolvedReference(
                FirstNonEmpty(user.Name, report.ReporterNameSnapshot, report.ReporterId),
                NormalizeUserSummary(user.Email, user.Phone));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 解析举报人显示信息失败: ReportId={ReportId}, ReporterId={ReporterId}", report.Id,
                report.ReporterId);
            return ResolvedReference.FromFallback(report.ReporterNameSnapshot);
        }
    }

    private async Task<ResolvedReference> ResolveTargetAsync(Report report, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(report.TargetId))
        {
            return ResolvedReference.FromFallback(report.TargetNameSnapshot);
        }

        try
        {
            return NormalizeContentType(report.ContentType) switch
            {
                "user" => await ResolveUserTargetAsync(report, cancellationToken),
                "city" => await ResolveCityTargetAsync(report, cancellationToken),
                "coworking" => await ResolveCoworkingTargetAsync(report, cancellationToken),
                "meetup" or "event" => await ResolveEventTargetAsync(report, cancellationToken),
                "innovationproject" or "innovation" => await ResolveInnovationTargetAsync(report, cancellationToken),
                _ => ResolvedReference.FromFallback(report.TargetNameSnapshot)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "⚠️ 解析举报对象显示信息失败: ReportId={ReportId}, ContentType={ContentType}, TargetId={TargetId}",
                report.Id, report.ContentType, report.TargetId);
            return ResolvedReference.FromFallback(report.TargetNameSnapshot);
        }
    }

    private async Task<ResolvedReference> ResolveUserTargetAsync(Report report, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(report.TargetId, cancellationToken);
        if (user == null)
        {
            return ResolvedReference.FromFallback(report.TargetNameSnapshot);
        }

        return new ResolvedReference(
            FirstNonEmpty(user.Name, report.TargetNameSnapshot, report.TargetId),
            NormalizeUserSummary(user.Email, user.Phone));
    }

    private async Task<ResolvedReference> ResolveCityTargetAsync(Report report, CancellationToken cancellationToken)
    {
        var city = await _cityServiceClient.GetCityDetailAsync(report.TargetId, cancellationToken);
        if (city == null)
        {
            return ResolvedReference.FromFallback(report.TargetNameSnapshot);
        }

        return new ResolvedReference(
            FirstNonEmpty(city.Name, report.TargetNameSnapshot, report.TargetId),
            city.Country ?? string.Empty);
    }

    private Task<ResolvedReference> ResolveCoworkingTargetAsync(Report report, CancellationToken cancellationToken)
    {
        return ResolveServiceTargetAsync(
            report,
            "coworking-service",
            $"api/v1/coworking/{Uri.EscapeDataString(report.TargetId)}",
            new[] { "name", "Name", "title", "Title" },
            new[] { "cityName", "CityName", "cityId", "CityId" },
            cancellationToken);
    }

    private Task<ResolvedReference> ResolveEventTargetAsync(Report report, CancellationToken cancellationToken)
    {
        return ResolveServiceTargetAsync(
            report,
            "event-service",
            $"api/v1/events/{Uri.EscapeDataString(report.TargetId)}",
            new[] { "title", "Title", "name", "Name" },
            new[] { "organizerName", "OrganizerName", "cityName", "CityName", "organizerId", "OrganizerId" },
            cancellationToken);
    }

    private Task<ResolvedReference> ResolveInnovationTargetAsync(Report report, CancellationToken cancellationToken)
    {
        return ResolveServiceTargetAsync(
            report,
            "innovation-service",
            $"api/v1/innovations/{Uri.EscapeDataString(report.TargetId)}",
            new[] { "title", "Title", "name", "Name" },
            new[] { "creatorName", "CreatorName", "creatorId", "CreatorId", "category", "Category" },
            cancellationToken);
    }

    private async Task<ResolvedReference> ResolveServiceTargetAsync(
        Report report,
        string serviceName,
        string path,
        string[] displayNameKeys,
        string[] summaryKeys,
        CancellationToken cancellationToken)
    {
        var payload = await GetServicePayloadAsync(serviceName, path, cancellationToken);
        if (payload == null)
        {
            return ResolvedReference.FromFallback(report.TargetNameSnapshot);
        }

        var displayName = ReadString(payload.Value, displayNameKeys);
        var summary = ReadString(payload.Value, summaryKeys);

        return new ResolvedReference(
            FirstNonEmpty(displayName, report.TargetNameSnapshot, report.TargetId),
            summary ?? string.Empty);
    }

    private async Task<JsonElement?> GetServicePayloadAsync(
        string serviceName,
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await _serviceInvocationClient.InvokeAsync<ServiceEnvelope<JsonElement>>(
                HttpMethod.Get,
                serviceName,
                path,
                cancellationToken);

            if (envelope?.Success != true)
            {
                return null;
            }

            return envelope.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? null
                : envelope.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 下游资源解析失败: Service={ServiceName}, Path={Path}", serviceName, path);
            return null;
        }
    }

    private static string NormalizeContentType(string? contentType)
    {
        return (contentType ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return string.Empty;
    }

    private static string NormalizeUserSummary(string? email, string? phone)
    {
        return FirstNonEmpty(email, phone);
    }

    private static string? ReadString(JsonElement payload, params string[] keys)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (!payload.TryGetProperty(key, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var raw = value.GetString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    return raw.Trim();
                }

                continue;
            }

            var stringValue = value.ToString();
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue.Trim();
            }
        }

        return null;
    }

    private sealed record ResolvedReference(string DisplayName, string Summary)
    {
        public static ResolvedReference FromFallback(string? fallback)
        {
            return new ResolvedReference(FirstNonEmpty(fallback), string.Empty);
        }
    }

    private sealed class ServiceEnvelope<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T Data { get; set; } = default!;
    }
}
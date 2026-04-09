using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class ReportDto
{
    public string Id { get; set; } = string.Empty;
    public string ReporterId { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string ReporterDisplayName { get; set; } = string.Empty;
    public string ReporterSummary { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetDisplayName { get; set; } = string.Empty;
    public string TargetSummary { get; set; } = string.Empty;
    public string ReasonId { get; set; } = string.Empty;
    public string ReasonLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AdminNotes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReportActionRequest
{
    [MaxLength(1000, ErrorMessage = "备注长度不能超过1000")]
    public string? Note { get; set; }
}

public class ReportActionResponse
{
    public string ReportId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
}
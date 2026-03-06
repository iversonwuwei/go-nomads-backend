using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

/// <summary>
///     管理后台审计事件
/// </summary>
public class AdminAuditEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Scope { get; set; } = "global";
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime HappenedAt { get; set; }
}

/// <summary>
///     写入审计事件请求
/// </summary>
public class CreateAdminAuditEventRequest
{
    [Required] public string Scope { get; set; } = "global";
    public string EntityId { get; set; } = string.Empty;
    [Required] public string Action { get; set; } = string.Empty;
    [Required] public string Note { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime? HappenedAt { get; set; }
}

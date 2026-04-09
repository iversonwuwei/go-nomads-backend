using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class AdminAuditEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public DateTime HappenedAt { get; set; }
}

public class CreateAdminAuditEventRequest
{
    [Required(ErrorMessage = "scope 不能为空")]
    public string Scope { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    [Required(ErrorMessage = "action 不能为空")]
    public string Action { get; set; } = string.Empty;

    [Required(ErrorMessage = "note 不能为空")]
    [MaxLength(2000, ErrorMessage = "note 长度不能超过2000")]
    public string Note { get; set; } = string.Empty;

    public Dictionary<string, object?>? Metadata { get; set; }

    public DateTime? HappenedAt { get; set; }
}
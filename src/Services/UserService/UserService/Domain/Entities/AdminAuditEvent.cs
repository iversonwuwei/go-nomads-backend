using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     管理后台审计事件实体
/// </summary>
[Table("admin_audit_events")]
public class AdminAuditEvent : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("scope")]
    public string Scope { get; set; } = "global";

    [Column("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("note")]
    public string Note { get; set; } = string.Empty;

    [Column("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [Column("happened_at")]
    public DateTime HappenedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

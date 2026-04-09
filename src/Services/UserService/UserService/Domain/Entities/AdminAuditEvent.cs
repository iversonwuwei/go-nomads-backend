using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using UserService.Infrastructure.Converters;

namespace UserService.Domain.Entities;

[Table("admin_audit_events")]
public class AdminAuditEvent : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("scope")]
    public string Scope { get; set; } = string.Empty;

    [Column("entity_id")]
    public string EntityId { get; set; } = string.Empty;

    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Column("note")]
    public string Note { get; set; } = string.Empty;

    [Column("metadata_json")]
    [JsonConverter(typeof(JsonbStringConverter))]
    public string MetadataJson { get; set; } = "{}";

    [Column("happened_at")]
    public DateTime HappenedAt { get; set; }

    [Column("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
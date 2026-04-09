using Postgrest.Attributes;
using GoNomads.Shared.Models;

namespace ConfigService.Domain.Entities;

/// <summary>
/// 静态文本 — 存储 App 内可配置的文案内容
/// </summary>
[Table("app_static_texts")]
public class StaticText : BaseEntityWithId
{
    [Column("text_key")]
    public string TextKey { get; set; } = string.Empty;

    [Column("locale")]
    public string Locale { get; set; } = "zh-CN";

    [Column("text_value")]
    public string TextValue { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("updated_by")]
    public new Guid? UpdatedBy { get; set; }
}

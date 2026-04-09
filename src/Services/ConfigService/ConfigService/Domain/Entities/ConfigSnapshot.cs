using Postgrest.Attributes;
using GoNomads.Shared.Models;

namespace ConfigService.Domain.Entities;

/// <summary>
/// 配置快照 — 存储已发布的全量配置版本
/// </summary>
[Table("app_config_snapshots")]
public class ConfigSnapshot : BaseEntityWithId
{
    [Column("version")]
    public int Version { get; set; }

    [Column("static_texts")]
    public string StaticTexts { get; set; } = "{}";

    [Column("option_groups")]
    public string OptionGroups { get; set; } = "{}";

    [Column("system_settings")]
    public string SystemSettings { get; set; } = "{}";

    [Column("is_published")]
    public bool IsPublished { get; set; } = false;

    [Column("published_by")]
    public Guid? PublishedBy { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }
}

using GoNomads.Shared.Models;
using Postgrest.Attributes;

namespace ConfigService.Domain.Entities;

[Table("app_system_settings")]
public class SystemSetting : BaseEntityWithId
{
    [Column("section")]
    public string Section { get; set; } = string.Empty;

    [Column("setting_key")]
    public string SettingKey { get; set; } = string.Empty;

    [Column("label")]
    public string Label { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("value_type")]
    public string ValueType { get; set; } = "string";

    [Column("value")]
    public string Value { get; set; } = string.Empty;

    [Column("default_value")]
    public string? DefaultValue { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_secret")]
    public bool IsSecret { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }
}
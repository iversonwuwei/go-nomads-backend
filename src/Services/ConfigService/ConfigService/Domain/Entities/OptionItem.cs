using Postgrest.Attributes;
using GoNomads.Shared.Models;

namespace ConfigService.Domain.Entities;

/// <summary>
/// 选项明细 — 每个选项分组下的具体选项条目
/// </summary>
[Table("app_option_items")]
public class OptionItem : BaseEntityWithId
{
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("option_code")]
    public string OptionCode { get; set; } = string.Empty;

    [Column("option_value")]
    public string OptionValue { get; set; } = string.Empty;

    [Column("option_value_en")]
    public string? OptionValueEn { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("color")]
    public string? Color { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("metadata")]
    public string? Metadata { get; set; }
}

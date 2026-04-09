using Postgrest.Attributes;
using GoNomads.Shared.Models;

namespace ConfigService.Domain.Entities;

/// <summary>
/// 选项分组 — 管理下拉选项的分组容器
/// </summary>
[Table("app_option_groups")]
public class OptionGroup : BaseEntityWithId
{
    [Column("group_code")]
    public string GroupCode { get; set; } = string.Empty;

    [Column("group_name")]
    public string GroupName { get; set; } = string.Empty;

    [Column("group_name_en")]
    public string? GroupNameEn { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_system")]
    public bool IsSystem { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

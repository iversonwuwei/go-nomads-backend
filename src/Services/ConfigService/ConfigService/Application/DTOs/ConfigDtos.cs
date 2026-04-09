namespace ConfigService.Application.DTOs;

// ── 静态文本 ──

public class StaticTextDto
{
    public Guid Id { get; set; }
    public string TextKey { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public string TextValue { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateStaticTextDto
{
    public string TextKey { get; set; } = string.Empty;
    public string Locale { get; set; } = "zh-CN";
    public string TextValue { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateStaticTextDto
{
    public string? TextValue { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

// ── 选项分组 ──

public class OptionGroupDto
{
    public Guid Id { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? GroupNameEn { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOptionGroupDto
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? GroupNameEn { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false;
}

public class UpdateOptionGroupDto
{
    public string? GroupName { get; set; }
    public string? GroupNameEn { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

// ── 选项明细 ──

public class OptionItemDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string OptionCode { get; set; } = string.Empty;
    public string OptionValue { get; set; } = string.Empty;
    public string? OptionValueEn { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOptionItemDto
{
    public string OptionCode { get; set; } = string.Empty;
    public string OptionValue { get; set; } = string.Empty;
    public string? OptionValueEn { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; } = 0;
    public string? Metadata { get; set; }
}

public class UpdateOptionItemDto
{
    public string? OptionValue { get; set; }
    public string? OptionValueEn { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public string? Metadata { get; set; }
}

public class ReorderItemsDto
{
    public List<Guid> OrderedIds { get; set; } = new();

    /// <summary>
    /// Admin 前端发送 itemIds，自动映射到 OrderedIds
    /// </summary>
    public List<Guid>? ItemIds
    {
        get => null;
        set { if (value != null) OrderedIds = value; }
    }
}

// ── 系统配置 ──

public class SystemSettingDto
{
    public Guid Id { get; set; }
    public string Section { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; }
    public bool IsSecret { get; set; }
    public int SortOrder { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSystemSettingDto
{
    public string Section { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = "string";
    public string Value { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSecret { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateSystemSettingDto
{
    public string Section { get; set; } = string.Empty;
    public string SettingKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = "string";
    public string Value { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSecret { get; set; }
    public int SortOrder { get; set; }
}

// ── 配置快照 ──

public class ConfigSnapshotDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public bool IsPublished { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConfigSnapshotDetailDto : ConfigSnapshotDto
{
    public object? StaticTexts { get; set; }
    public object? OptionGroups { get; set; }
    public object? SystemSettings { get; set; }
}

// ── App 端配置响应 ──

public class AppConfigDto
{
    public int Version { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Dictionary<string, string> StaticTexts { get; set; } = new();
    public Dictionary<string, List<AppOptionItemDto>> OptionGroups { get; set; } = new();
    public Dictionary<string, Dictionary<string, AppSystemSettingDto>> SystemSettings { get; set; } = new();
}

public class AppOptionItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? LabelEn { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class AppSystemSettingDto
{
    public string Label { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}

public class AppConfigVersionDto
{
    public int Version { get; set; }
    public DateTime? PublishedAt { get; set; }
}

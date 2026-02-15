using System.Text.Json;

namespace UserService.Application.DTOs;

/// <summary>
///     法律文档响应 DTO
/// </summary>
public class LegalDocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public bool IsCurrent { get; set; }

    /// <summary>
    ///     章节列表 — 解析后的 JSON 数组
    /// </summary>
    public List<LegalSectionDto> Sections { get; set; } = new();

    /// <summary>
    ///     摘要列表 — 解析后的 JSON 数组（用于首启弹窗）
    /// </summary>
    public List<LegalSummaryDto> Summary { get; set; } = new();
}

public class LegalSectionDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class LegalSummaryDto
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

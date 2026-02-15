using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     法律文档实体 — 存储隐私政策、服务条款等法律文档
///     支持多语言、版本控制、按章节存储内容
/// </summary>
[Table("legal_documents")]
public class LegalDocument : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     文档类型: privacy_policy | terms_of_service | community_guidelines
    /// </summary>
    [Column("document_type")]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    ///     版本号: 如 "1.0.0"
    /// </summary>
    [Column("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    ///     语言: zh, en
    /// </summary>
    [Column("language")]
    public string Language { get; set; } = "zh";

    /// <summary>
    ///     文档标题
    /// </summary>
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     生效日期
    /// </summary>
    [Column("effective_date")]
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    ///     是否为当前生效版本
    /// </summary>
    [Column("is_current")]
    public bool IsCurrent { get; set; }

    /// <summary>
    ///     章节内容 JSONB — [{ "title": "...", "content": "..." }, ...]
    /// </summary>
    [Column("sections")]
    public string Sections { get; set; } = "[]";

    /// <summary>
    ///     摘要内容 JSONB — [{ "icon": "...", "title": "...", "content": "..." }, ...]
    /// </summary>
    [Column("summary")]
    public string Summary { get; set; } = "[]";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

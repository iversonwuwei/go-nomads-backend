using Newtonsoft.Json;
using Postgrest.Attributes;
using Postgrest.Models;
using UserService.Infrastructure.Converters;

namespace UserService.Domain.Entities;

/// <summary>
///     法律文档实体 — 隐私政策、用户协议等
///     sections / summary / sdk_list 以 JSON 字符串存储，在应用层反序列化
/// </summary>
[Table("legal_documents")]
public class LegalDocument : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("document_type")]
    public string DocumentType { get; set; } = string.Empty;

    [Column("version")]
    public string Version { get; set; } = string.Empty;

    [Column("language")]
    public string Language { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("effective_date")]
    public DateTime EffectiveDate { get; set; }

    [Column("is_current")]
    public bool IsCurrent { get; set; }

    /// <summary>
    ///     章节列表 — JSON 字符串, [{title, content}]
    /// </summary>
    [Column("sections")]
    [JsonConverter(typeof(JsonbStringConverter))]
    public string Sections { get; set; } = "[]";

    /// <summary>
    ///     摘要列表 — JSON 字符串, [{icon, title, content}]
    /// </summary>
    [Column("summary")]
    [JsonConverter(typeof(JsonbStringConverter))]
    public string Summary { get; set; } = "[]";

    /// <summary>
    ///     第三方 SDK 信息 — JSON 字符串, [{name, company, purpose, dataCollected, privacyUrl}]
    /// </summary>
    [Column("sdk_list")]
    [JsonConverter(typeof(JsonbStringConverter))]
    public string SdkList { get; set; } = "[]";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

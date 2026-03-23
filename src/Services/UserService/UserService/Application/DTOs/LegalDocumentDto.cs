namespace UserService.Application.DTOs;

/// <summary>
///     法律文档 DTO — 隐私政策等法律文档的响应模型
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
    public List<LegalSectionDto> Sections { get; set; } = new();
    public List<LegalSummaryDto> Summary { get; set; } = new();
    public List<SdkInfoDto> SdkList { get; set; } = new();
}

public class LegalDocumentVersionDto
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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

public class SdkInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public List<string> DataCollected { get; set; } = new();
    public string PrivacyUrl { get; set; } = string.Empty;
}

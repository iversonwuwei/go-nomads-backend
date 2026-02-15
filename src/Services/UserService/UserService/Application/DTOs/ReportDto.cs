namespace UserService.Application.DTOs;

/// <summary>
///     创建举报请求 DTO
/// </summary>
public class CreateReportDto
{
    /// <summary>
    ///     举报内容类型: user / message / meetup / innovationProject / chatRoom
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    ///     被举报对象 ID
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    ///     被举报对象名称（可选）
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    ///     举报原因标识: spam / harassment / inappropriate / fraud / violence / other
    /// </summary>
    public string ReasonId { get; set; } = string.Empty;

    /// <summary>
    ///     举报原因描述文本
    /// </summary>
    public string ReasonLabel { get; set; } = string.Empty;
}

/// <summary>
///     举报记录响应 DTO
/// </summary>
public class ReportDto
{
    public string Id { get; set; } = string.Empty;
    public string ReporterId { get; set; } = string.Empty;
    public string? ReporterName { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? TargetName { get; set; }
    public string ReasonId { get; set; } = string.Empty;
    public string ReasonLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

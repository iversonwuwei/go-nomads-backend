using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     举报记录实体 - 存储用户举报信息
/// </summary>
[Table("reports")]
public class Report : BaseModel
{
    public Report()
    {
        Id = Guid.NewGuid().ToString();
        Status = "pending";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     举报人用户 ID
    /// </summary>
    [Required]
    [Column("reporter_id")]
    public string ReporterId { get; set; } = string.Empty;

    /// <summary>
    ///     举报人名称
    /// </summary>
    [Column("reporter_name")]
    public string? ReporterName { get; set; }

    /// <summary>
    ///     举报内容类型: user / message / meetup / innovationProject / chatRoom
    /// </summary>
    [Required]
    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    ///     被举报对象 ID
    /// </summary>
    [Required]
    [Column("target_id")]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    ///     被举报对象名称
    /// </summary>
    [Column("target_name")]
    public string? TargetName { get; set; }

    /// <summary>
    ///     举报原因标识: spam / harassment / inappropriate / fraud / violence / other
    /// </summary>
    [Required]
    [Column("reason_id")]
    public string ReasonId { get; set; } = string.Empty;

    /// <summary>
    ///     举报原因描述文本
    /// </summary>
    [Required]
    [Column("reason_label")]
    public string ReasonLabel { get; set; } = string.Empty;

    /// <summary>
    ///     处理状态: pending / reviewed / resolved / dismissed
    /// </summary>
    [Required]
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    ///     管理员备注
    /// </summary>
    [Column("admin_notes")]
    public string? AdminNotes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region 工厂方法

    /// <summary>
    ///     创建新的举报记录
    /// </summary>
    public static Report Create(
        string reporterId,
        string? reporterName,
        string contentType,
        string targetId,
        string? targetName,
        string reasonId,
        string reasonLabel)
    {
        if (string.IsNullOrWhiteSpace(reporterId))
            throw new ArgumentException("举报人ID不能为空", nameof(reporterId));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("举报内容类型不能为空", nameof(contentType));
        if (string.IsNullOrWhiteSpace(targetId))
            throw new ArgumentException("被举报对象ID不能为空", nameof(targetId));
        if (string.IsNullOrWhiteSpace(reasonId))
            throw new ArgumentException("举报原因不能为空", nameof(reasonId));

        return new Report
        {
            Id = Guid.NewGuid().ToString(),
            ReporterId = reporterId,
            ReporterName = reporterName,
            ContentType = contentType,
            TargetId = targetId,
            TargetName = targetName,
            ReasonId = reasonId,
            ReasonLabel = reasonLabel,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

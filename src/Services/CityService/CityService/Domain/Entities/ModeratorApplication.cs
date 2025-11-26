using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     版主申请实体
/// </summary>
[Table("moderator_applications")]
public class ModeratorApplication : BaseModel
{
    /// <summary>
    ///     申请ID
    /// </summary>
    [PrimaryKey("id")]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    ///     申请用户ID
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    ///     申请的城市ID
    /// </summary>
    [Column("city_id")]
    public Guid CityId { get; set; }

    /// <summary>
    ///     申请原因/说明
    /// </summary>
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    ///     申请状态: pending, approved, rejected
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    ///     处理该申请的管理员ID
    /// </summary>
    [Column("processed_by")]
    public Guid? ProcessedBy { get; set; }

    /// <summary>
    ///     处理时间
    /// </summary>
    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    ///     拒绝原因（如果被拒绝）
    /// </summary>
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     更新时间
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     创建申请 - 工厂方法
    /// </summary>
    public static ModeratorApplication Create(Guid userId, Guid cityId, string reason)
    {
        return new ModeratorApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CityId = cityId,
            Reason = reason,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     批准申请
    /// </summary>
    public void Approve(Guid adminId)
    {
        Status = "approved";
        ProcessedBy = adminId;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     拒绝申请
    /// </summary>
    public void Reject(Guid adminId, string reason)
    {
        Status = "rejected";
        ProcessedBy = adminId;
        ProcessedAt = DateTime.UtcNow;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}

using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     用户上传的城市照片实体
/// </summary>
[Table("user_city_photos")]
public class UserCityPhoto : BaseModel
{
    public static class ModerationStatuses
    {
        public const string Pending = "pending";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
    }

    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required] [Column("user_id")] public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("city_id")]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(500)] [Column("caption")] public string? Caption { get; set; }

    [Column("description")] public string? Description { get; set; }

    [MaxLength(200)] [Column("location")] public string? Location { get; set; }

    [Column("place_name")] public string? PlaceName { get; set; }

    [Column("address")] public string? Address { get; set; }

    [Column("latitude")] public double? Latitude { get; set; }

    [Column("longitude")] public double? Longitude { get; set; }

    [Column("taken_at")] public DateTime? TakenAt { get; set; }

    [Column("moderation_status")] public string ModerationStatus { get; set; } = ModerationStatuses.Pending;

    [Column("moderation_reason")] public string? ModerationReason { get; set; }

    [Column("reviewed_at")] public DateTime? ReviewedAt { get; set; }

    [Column("reviewed_by")] public Guid? ReviewedBy { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }

    public void Approve(Guid reviewedBy, string? reason = null)
    {
        ModerationStatus = ModerationStatuses.Approved;
        ModerationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(Guid reviewedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("拒绝原因不能为空", nameof(reason));

        ModerationStatus = ModerationStatuses.Rejected;
        ModerationReason = reason.Trim();
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
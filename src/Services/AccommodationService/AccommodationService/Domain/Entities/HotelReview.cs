using Postgrest.Attributes;
using Postgrest.Models;

namespace AccommodationService.Domain.Entities;

/// <summary>
///     酒店评论实体
/// </summary>
[Table("hotel_reviews")]
public class HotelReview : BaseModel
{
    /// <summary>
    ///     公共无参构造函数 (ORM 需要)
    /// </summary>
    public HotelReview()
    {
    }

    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("visit_date")]
    public DateTime? VisitDate { get; set; }

    [Column("photo_urls")]
    public string[]? PhotoUrls { get; set; }

    [Column("is_verified")]
    public bool IsVerified { get; set; }

    [Column("helpful_count")]
    public int HelpfulCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

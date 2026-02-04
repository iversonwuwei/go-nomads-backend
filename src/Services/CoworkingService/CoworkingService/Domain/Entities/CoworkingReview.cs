using Postgrest.Attributes;
using Postgrest.Models;

namespace CoworkingService.Domain.Entities;

/// <summary>
/// Coworking 空间评论实体
/// 注意：username 和 user_avatar 字段已从数据库中删除
/// 用户信息现在通过 UserService 动态获取
/// </summary>
[Table("coworking_reviews")]
public class CoworkingReview : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Column("coworking_id")] public Guid CoworkingId { get; set; }

    [Column("user_id")] public Guid UserId { get; set; }

    [Column("rating")] public double Rating { get; set; } // 1.0 - 5.0

    [Column("title")] public string Title { get; set; } = string.Empty;

    [Column("content")] public string Content { get; set; } = string.Empty;

    [Column("visit_date")] public DateTime? VisitDate { get; set; }

    [Column("photo_urls")] public List<string>? PhotoUrls { get; set; }

    [Column("is_verified")] public bool IsVerified { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 验证评分
    /// </summary>
    public static void ValidateRating(double rating)
    {
        if (rating < 1.0 || rating > 5.0)
            throw new ArgumentException("Rating must be between 1.0 and 5.0", nameof(rating));
    }

    /// <summary>
    /// 验证标题
    /// </summary>
    public static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (title.Trim().Length < 5)
            throw new ArgumentException("Title must be at least 5 characters", nameof(title));
        if (title.Length > 100)
            throw new ArgumentException("Title cannot exceed 100 characters", nameof(title));
    }

    /// <summary>
    /// 验证内容
    /// </summary>
    public static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));
        if (content.Trim().Length < 20)
            throw new ArgumentException("Content must be at least 20 characters", nameof(content));
        if (content.Length > 1000)
            throw new ArgumentException("Content cannot exceed 1000 characters", nameof(content));
    }
}

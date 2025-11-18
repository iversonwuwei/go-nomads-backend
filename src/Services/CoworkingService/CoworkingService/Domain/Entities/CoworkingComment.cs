using Postgrest.Attributes;
using Postgrest.Models;

namespace CoworkingService.Domain.Entities;

/// <summary>
///     Coworking 空间评论实体
/// </summary>
[Table("coworking_comments")]
public class CoworkingComment : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Column("coworking_id")] public Guid CoworkingId { get; set; }

    [Column("user_id")] public Guid UserId { get; set; }

    [Column("content")] public string Content { get; set; } = string.Empty;

    [Column("rating")] public int? Rating { get; set; }

    [Column("images")] public List<string>? Images { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }

    [Column("is_active")] public bool IsActive { get; set; } = true;

    /// <summary>
    ///     创建评论
    /// </summary>
    public static CoworkingComment Create(
        Guid coworkingId,
        Guid userId,
        string content,
        int? rating = null,
        List<string>? images = null)
    {
        if (coworkingId == Guid.Empty)
            throw new ArgumentException("coworkingId 不能为空", nameof(coworkingId));
        if (userId == Guid.Empty)
            throw new ArgumentException("userId 不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("评论内容不能为空", nameof(content));
        if (rating.HasValue && (rating.Value < 0 || rating.Value > 5))
            throw new ArgumentException("评分必须在 0-5 之间", nameof(rating));

        var normalizedRating = rating ?? 0;

        return new CoworkingComment
        {
            Id = Guid.NewGuid(),
            CoworkingId = coworkingId,
            UserId = userId,
            Content = content.Trim(),
            Rating = normalizedRating,
            Images = images,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    ///     更新评论内容
    /// </summary>
    public void Update(string content, int? rating = null, List<string>? images = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("评论内容不能为空", nameof(content));
        if (rating.HasValue && (rating.Value < 0 || rating.Value > 5))
            throw new ArgumentException("评分必须在 0-5 之间", nameof(rating));

        Content = content.Trim();
        if (rating.HasValue)
            Rating = rating;
        Images = images;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     软删除评论
    /// </summary>
    public void SoftDelete()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

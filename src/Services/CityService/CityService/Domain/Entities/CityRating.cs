using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
/// 用户城市评分实体
/// </summary>
[Table("city_ratings")]
public class CityRating : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("city_id")]
    public Guid CityId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public static CityRating Create(
        Guid cityId,
        Guid userId,
        Guid categoryId,
        int rating)
    {
        if (cityId == Guid.Empty)
            throw new ArgumentException("城市ID不能为空", nameof(cityId));
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("评分项ID不能为空", nameof(categoryId));
        if (rating < 0 || rating > 5)
            throw new ArgumentException("评分必须在0-5之间", nameof(rating));

        return new CityRating
        {
            Id = Guid.NewGuid(),
            CityId = cityId,
            UserId = userId,
            CategoryId = categoryId,
            Rating = rating,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateRating(int rating)
    {
        if (rating < 0 || rating > 5)
            throw new ArgumentException("评分必须在0-5之间", nameof(rating));

        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
    }
}

namespace CityService.Domain.Entities;

/// <summary>
/// 用户收藏城市实体
/// </summary>
public class UserFavoriteCity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

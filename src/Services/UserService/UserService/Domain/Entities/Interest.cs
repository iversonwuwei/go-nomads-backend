using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
/// 兴趣爱好实体
/// </summary>
[Table("interests")]
public class Interest : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 用户兴趣关联实体
/// </summary>
[Table("user_interests")]
public class UserInterest : BaseModel
{
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("interest_id")]
    public string InterestId { get; set; } = string.Empty;

    [Column("intensity_level")]
    public string? IntensityLevel { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

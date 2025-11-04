using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
/// 城市 Pros & Cons 实体
/// </summary>
[Table("city_pros_cons")]
public class CityProsCons : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("city_id")]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("text")]
    public string Text { get; set; } = string.Empty;

    [Required]
    [Column("is_pro")]
    public bool IsPro { get; set; }

    [Column("upvotes")]
    public int Upvotes { get; set; }

    [Column("downvotes")]
    public int Downvotes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

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

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}
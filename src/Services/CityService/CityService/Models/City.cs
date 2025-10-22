using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Models;

[Table("cities")]
public class City : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("country")]
    public string Country { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("region")]
    public string? Region { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// PostGIS POINT - 存储为字符串 "POINT(longitude latitude)"
    /// </summary>
    [Column("location")]
    public string? Location { get; set; }

    [Column("latitude")]
    public double? Latitude { get; set; }
    [Column("longitude")]
    public double? Longitude { get; set; }

    public int? Population { get; set; }

    [MaxLength(50)]
    public string? Climate { get; set; }

    [MaxLength(50)]
    public string? TimeZone { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public decimal? AverageCostOfLiving { get; set; } // Monthly cost in USD

    public decimal? OverallScore { get; set; } // 0-10 rating

    public decimal? InternetQualityScore { get; set; }
    public decimal? SafetyScore { get; set; }
    public decimal? CostScore { get; set; }
    public decimal? CommunityScore { get; set; }
    public decimal? WeatherScore { get; set; }

    public List<string> Tags { get; set; } = new();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }
}

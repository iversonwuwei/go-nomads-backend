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

    /// <summary>
    /// 国家外键关联
    /// </summary>
    [Column("country_id")]
    public Guid? CountryId { get; set; }

    /// <summary>
    /// 省份外键关联
    /// </summary>
    [Column("province_id")]
    public Guid? ProvinceId { get; set; }

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

    [Column("population")]
    public int? Population { get; set; }

    [MaxLength(50)]
    [Column("climate")]
    public string? Climate { get; set; }

    [MaxLength(50)]
    [Column("time_zone")]
    public string? TimeZone { get; set; }

    [MaxLength(10)]
    [Column("currency")]
    public string? Currency { get; set; }

    [MaxLength(500)]
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("average_cost_of_living")]
    public decimal? AverageCostOfLiving { get; set; } // Monthly cost in USD

    [Column("overall_score")]
    public decimal? OverallScore { get; set; } // 0-10 rating

    [Column("internet_quality_score")]
    public decimal? InternetQualityScore { get; set; }
    [Column("safety_score")]
    public decimal? SafetyScore { get; set; }
    [Column("cost_score")]
    public decimal? CostScore { get; set; }
    [Column("community_score")]
    public decimal? CommunityScore { get; set; }
    [Column("weather_score")]
    public decimal? WeatherScore { get; set; }

    [Column("tags")]
    public List<string> Tags { get; set; } = new();

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("created_by_id")]
    public Guid? CreatedById { get; set; }
    [Column("updated_by_id")]
    public Guid? UpdatedById { get; set; }

    // Navigation properties (not mapped to database)
    [Reference(typeof(Country), ReferenceAttribute.JoinType.Inner, false)]
    public Country? CountryDetails { get; set; }

    [Reference(typeof(Province), ReferenceAttribute.JoinType.Inner, false)]
    public Province? ProvinceDetails { get; set; }
}

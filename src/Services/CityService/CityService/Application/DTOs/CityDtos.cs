using System.ComponentModel.DataAnnotations;

namespace CityService.Application.DTOs;

public class CityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Population { get; set; }
    public string? Climate { get; set; }
    public string? TimeZone { get; set; }
    public string? Currency { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? AverageCostOfLiving { get; set; }
    public decimal? OverallScore { get; set; }
    public decimal? InternetQualityScore { get; set; }
    public decimal? SafetyScore { get; set; }
    public decimal? CostScore { get; set; }
    public decimal? CommunityScore { get; set; }
    public decimal? WeatherScore { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public WeatherDto? Weather { get; set; }
    public int MeetupCount { get; set; }
    public int CoworkingCount { get; set; }
    
    /// <summary>
    /// 当前用户是否已收藏该城市
    /// 注意: 此字段需要在查询时根据当前用户动态填充
    /// </summary>
    public bool IsFavorite { get; set; }
}

public class CreateCityDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NameEn { get; set; }

    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public double? Latitude { get; set; }
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

    public decimal? AverageCostOfLiving { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UpdateCityDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? NameEn { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public double? Latitude { get; set; }
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

    public decimal? AverageCostOfLiving { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsActive { get; set; }
}

public class CitySearchDto
{
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public decimal? MinCostOfLiving { get; set; }
    public decimal? MaxCostOfLiving { get; set; }
    public decimal? MinScore { get; set; }
    public List<string>? Tags { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CityStatisticsDto
{
    public Guid CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int TotalCoworkingSpaces { get; set; }
    public int TotalAccommodations { get; set; }
    public int TotalEvents { get; set; }
    public int TotalNomads { get; set; }
    public decimal AverageRating { get; set; }
}

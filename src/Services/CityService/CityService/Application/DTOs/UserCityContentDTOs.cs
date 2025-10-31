using System.ComponentModel.DataAnnotations;

namespace CityService.Application.DTOs;

#region 照片相关 DTOs

/// <summary>
/// 用户城市照片 DTO
/// </summary>
public class UserCityPhotoDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public DateTime? TakenAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 添加照片请求
/// </summary>
public class AddCityPhotoRequest
{
    [Required]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Caption { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public DateTime? TakenAt { get; set; }
}

#endregion

#region 费用相关 DTOs

/// <summary>
/// 用户城市费用 DTO
/// </summary>
public class UserCityExpenseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 添加费用请求
/// </summary>
public class AddCityExpenseRequest
{
    [Required]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime Date { get; set; }
}

#endregion

#region 评论相关 DTOs

/// <summary>
/// 用户城市评论 DTO
/// </summary>
public class UserCityReviewDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
    public int? InternetQualityScore { get; set; }
    public int? SafetyScore { get; set; }
    public int? CostScore { get; set; }
    public int? CommunityScore { get; set; }
    public int? WeatherScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 添加/更新评论请求
/// </summary>
public class UpsertCityReviewRequest
{
    [Required]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? ReviewText { get; set; }

    [Range(1, 5)]
    public int? InternetQualityScore { get; set; }

    [Range(1, 5)]
    public int? SafetyScore { get; set; }

    [Range(1, 5)]
    public int? CostScore { get; set; }

    [Range(1, 5)]
    public int? CommunityScore { get; set; }

    [Range(1, 5)]
    public int? WeatherScore { get; set; }
}

#endregion

#region 统计相关 DTOs

/// <summary>
/// 城市用户内容统计
/// </summary>
public class CityUserContentStatsDto
{
    public string CityId { get; set; } = string.Empty;
    public int PhotoCount { get; set; }
    public int ExpenseCount { get; set; }
    public int ReviewCount { get; set; }
    public decimal? AverageRating { get; set; }
}

#endregion

#region 常量

/// <summary>
/// 费用分类常量
/// </summary>
public static class ExpenseCategory
{
    public const string Food = "food";
    public const string Transport = "transport";
    public const string Accommodation = "accommodation";
    public const string Activity = "activity";
    public const string Shopping = "shopping";
    public const string Other = "other";

    public static readonly string[] All = { Food, Transport, Accommodation, Activity, Shopping, Other };
}

#endregion

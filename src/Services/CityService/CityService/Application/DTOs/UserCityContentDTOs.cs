using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? PlaceName { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? TakenAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 添加照片请求
/// </summary>
public class AddCityPhotoRequest
{
    [JsonIgnore]
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

/// <summary>
/// 批量提交照片请求
/// </summary>
public class SubmitCityPhotoBatchRequest
{
    [JsonIgnore]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(10)]
    public List<string> ImageUrls { get; set; } = new();

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? LocationNote { get; set; }
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
    [Range(0.01, 999999999.99)]
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
    public string Username { get; set; } = string.Empty; // ✅ 新增：从 UserService 获取的用户名
    public string? UserAvatar { get; set; } // ✅ 新增：用户头像 URL（可选）
    public string CityId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? VisitDate { get; set; }
    public string? ReviewText { get; set; }
    public int? InternetQualityScore { get; set; }
    public int? SafetyScore { get; set; }
    public int? CostScore { get; set; }
    public int? CommunityScore { get; set; }
    public int? WeatherScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// 该用户在该城市上传的照片URL列表
    /// </summary>
    public List<string> PhotoUrls { get; set; } = new();
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

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime? VisitDate { get; set; }

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

/// <summary>
/// 城市综合费用统计 - 基于用户提交的实际费用计算
/// </summary>
public class CityCostSummaryDto
{
    /// <summary>
    /// 城市ID
    /// </summary>
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    /// 总平均费用
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// 住宿平均费用
    /// </summary>
    public decimal Accommodation { get; set; }

    /// <summary>
    /// 餐饮平均费用
    /// </summary>
    public decimal Food { get; set; }

    /// <summary>
    /// 交通平均费用
    /// </summary>
    public decimal Transportation { get; set; }

    /// <summary>
    /// 活动/娱乐平均费用
    /// </summary>
    public decimal Activity { get; set; }

    /// <summary>
    /// 购物平均费用
    /// </summary>
    public decimal Shopping { get; set; }

    /// <summary>
    /// 其他平均费用
    /// </summary>
    public decimal Other { get; set; }

    /// <summary>
    /// 数据来源用户数
    /// </summary>
    public int ContributorCount { get; set; }

    /// <summary>
    /// 总费用记录数
    /// </summary>
    public int TotalExpenseCount { get; set; }

    /// <summary>
    /// 货币单位（统一转换为USD）
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// 数据更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
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

#region Pros & Cons 相关 DTOs

/// <summary>
/// 城市 Pros & Cons DTO
/// </summary>
public class CityProsConsDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsPro { get; set; } // true = 优点, false = 挑战
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 添加 Pros & Cons 请求
/// </summary>
public class AddCityProsConsRequest
{
    [Required]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public bool IsPro { get; set; } // true = 优点, false = 挑战
}

/// <summary>
/// 更新 Pros & Cons 请求
/// </summary>
public class UpdateCityProsConsRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public bool IsPro { get; set; } // true = 优点, false = 挑战
}

#endregion

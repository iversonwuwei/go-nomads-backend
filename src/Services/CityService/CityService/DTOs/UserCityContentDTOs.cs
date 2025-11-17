using System.Collections.Generic;

namespace CityService.DTOs;

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
    public string CityId { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Location { get; set; }
    public DateTime? TakenAt { get; set; }
}

/// <summary>
/// 批量提交照片请求
/// </summary>
public class SubmitCityPhotoBatchRequest
{
    public string CityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string? Description { get; set; }
    public string? LocationNote { get; set; }
}

/// <summary>
/// 用户城市费用 DTO
/// </summary>
public class UserCityExpenseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // food, transport, accommodation, activity, shopping, other
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
    public string CityId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Description { get; set; }
    public DateTime Date { get; set; }
}

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
    public int Rating { get; set; } // 1-5
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? VisitDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 添加/更新评论请求
/// </summary>
public class UpsertCityReviewRequest
{
    public string CityId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? VisitDate { get; set; }
}

/// <summary>
/// 城市用户内容统计
/// </summary>
public class CityUserContentStatsDto
{
    public string CityId { get; set; } = string.Empty;
    public int PhotoCount { get; set; }
    public int ExpenseCount { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
    public int PhotoContributors { get; set; }
    public int ExpenseContributors { get; set; }
    public int ReviewContributors { get; set; }
}

/// <summary>
/// 费用分类枚举
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

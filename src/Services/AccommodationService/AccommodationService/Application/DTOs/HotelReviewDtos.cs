using System.ComponentModel.DataAnnotations;

namespace AccommodationService.Application.DTOs;

/// <summary>
///     酒店评论响应 DTO
/// </summary>
public class HotelReviewDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime? VisitDate { get; set; }
    public string[]? PhotoUrls { get; set; }
    public bool IsVerified { get; set; }
    public int HelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
///     创建酒店评论请求 DTO
/// </summary>
public class CreateHotelReviewRequest
{
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime? VisitDate { get; set; }

    public List<string>? PhotoUrls { get; set; }
}

/// <summary>
///     更新酒店评论请求 DTO
/// </summary>
public class UpdateHotelReviewRequest
{
    [Range(1, 5)]
    public int? Rating { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Content { get; set; }

    public DateTime? VisitDate { get; set; }

    public List<string>? PhotoUrls { get; set; }
}

/// <summary>
///     酒店评论列表响应 DTO
/// </summary>
public class HotelReviewListResponse
{
    public List<HotelReviewDto> Reviews { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public double AverageRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
}

/// <summary>
///     酒店评论查询参数
/// </summary>
public class HotelReviewQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; } = "newest"; // newest, oldest, highest, lowest, helpful
}

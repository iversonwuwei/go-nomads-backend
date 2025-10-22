using System.ComponentModel.DataAnnotations;

namespace CoworkingService.DTOs;

/// <summary>
/// Coworking 空间 DTO
/// </summary>
public class CoworkingSpaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CityId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }
    public decimal? PricePerDay { get; set; }
    public decimal? PricePerMonth { get; set; }
    public decimal? PricePerHour { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public decimal? WifiSpeed { get; set; }
    public bool HasMeetingRoom { get; set; }
    public bool HasCoffee { get; set; }
    public bool HasParking { get; set; }
    public bool Has247Access { get; set; }
    public string[]? Amenities { get; set; }
    public int? Capacity { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? OpeningHours { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建/更新 Coworking 空间请求 DTO
/// </summary>
public class CreateCoworkingSpaceRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? CityId { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string[]? Images { get; set; }

    [Range(0, 10000)]
    public decimal? PricePerDay { get; set; }

    [Range(0, 50000)]
    public decimal? PricePerMonth { get; set; }

    [Range(0, 500)]
    public decimal? PricePerHour { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    [Range(0, 1000)]
    public decimal? WifiSpeed { get; set; }

    public bool HasMeetingRoom { get; set; }
    public bool HasCoffee { get; set; }
    public bool HasParking { get; set; }
    public bool Has247Access { get; set; }
    public string[]? Amenities { get; set; }

    [Range(1, 1000)]
    public int? Capacity { get; set; }

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }

    public string? OpeningHours { get; set; }
}

/// <summary>
/// Coworking 预订 DTO
/// </summary>
public class CoworkingBookingDto
{
    public Guid Id { get; set; }
    public Guid CoworkingId { get; set; }
    public Guid UserId { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string BookingType { get; set; } = "daily";
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "pending";
    public string? SpecialRequests { get; set; }
    public DateTime CreatedAt { get; set; }

    // 附加信息
    public string? CoworkingName { get; set; }
    public string? CoworkingAddress { get; set; }
}

/// <summary>
/// 创建预订请求 DTO
/// </summary>
public class CreateBookingRequest
{
    [Required]
    public Guid CoworkingId { get; set; }

    [Required]
    public DateTime BookingDate { get; set; }

    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    [Required]
    [RegularExpression("^(hourly|daily|monthly)$")]
    public string BookingType { get; set; } = "daily";

    public string? SpecialRequests { get; set; }
}

/// <summary>
/// 搜索 Coworking 空间请求 DTO
/// </summary>
public class SearchCoworkingRequest
{
    public string? SearchTerm { get; set; }
    public Guid? CityId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string PriceType { get; set; } = "day"; // day, hour, month
    public decimal? MinRating { get; set; }
    public bool? HasMeetingRoom { get; set; }
    public bool? HasCoffee { get; set; }
    public bool? HasParking { get; set; }
    public bool? Has247Access { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

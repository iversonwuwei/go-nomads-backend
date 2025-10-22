using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CoworkingService.Models;

/// <summary>
/// 共享办公空间实体模型
/// </summary>
[Table("coworking_spaces")]
public class CoworkingSpace : BaseModel
{
    [PrimaryKey("id", false)] // 数据库生成UUID
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("city_id")]
    public Guid? CityId { get; set; }

    [Required]
    [Column("address")]
    public string Address { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("images")]
    public string[]? Images { get; set; }

    [Column("price_per_day")]
    public decimal? PricePerDay { get; set; }

    [Column("price_per_month")]
    public decimal? PricePerMonth { get; set; }

    [Column("price_per_hour")]
    public decimal? PricePerHour { get; set; }

    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("rating")]
    public decimal Rating { get; set; } = 0;

    [Column("review_count")]
    public int ReviewCount { get; set; } = 0;

    [Column("wifi_speed")]
    public decimal? WifiSpeed { get; set; }

    [Column("has_meeting_room")]
    public bool HasMeetingRoom { get; set; }

    [Column("has_coffee")]
    public bool HasCoffee { get; set; }

    [Column("has_parking")]
    public bool HasParking { get; set; }

    [Column("has_24_7_access")]
    public bool Has247Access { get; set; }

    [Column("amenities")]
    public string[]? Amenities { get; set; }

    [Column("capacity")]
    public int? Capacity { get; set; }

    /// <summary>
    /// PostGIS POINT geometry - 使用字符串存储 "POINT(longitude latitude)"
    /// </summary>
    [Column("location")]
    public string? Location { get; set; }

    [Column("latitude")]
    public decimal? Latitude { get; set; }

    [Column("longitude")]
    public decimal? Longitude { get; set; }

    [MaxLength(50)]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [Column("email")]
    public string? Email { get; set; }

    [Column("website")]
    public string? Website { get; set; }

    /// <summary>
    /// JSONB 营业时间 - 使用字符串存储JSON格式
    /// </summary>
    [Column("opening_hours")]
    public string? OpeningHours { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 共享办公空间预订实体模型
/// </summary>
[Table("coworking_bookings")]
public class CoworkingBooking : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("coworking_id")]
    public Guid CoworkingId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("booking_date")]
    public DateTime BookingDate { get; set; }

    [Column("start_time")]
    public TimeSpan? StartTime { get; set; }

    [Column("end_time")]
    public TimeSpan? EndTime { get; set; }

    [MaxLength(20)]
    [Column("booking_type")]
    public string BookingType { get; set; } = "daily"; // hourly, daily, monthly

    [Required]
    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending"; // pending, confirmed, cancelled, completed

    [Column("special_requests")]
    public string? SpecialRequests { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

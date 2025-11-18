using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace AccommodationService.Models;

/// <summary>
///     酒店实体模型
/// </summary>
[Table("hotels")]
public class Hotel : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("city_id")] public Guid? CityId { get; set; }

    [Required] [Column("address")] public string Address { get; set; } = string.Empty;

    /// <summary>
    ///     PostGIS POINT - 存储为字符串 "POINT(longitude latitude)"
    /// </summary>
    [Column("location")]
    public string? Location { get; set; }

    [Column("latitude")] public decimal? Latitude { get; set; }

    [Column("longitude")] public decimal? Longitude { get; set; }

    [Column("rating")] public decimal Rating { get; set; } = 0;

    [Column("review_count")] public int ReviewCount { get; set; } = 0;

    [Column("description")] public string? Description { get; set; }

    [Column("amenities")] public string[]? Amenities { get; set; }

    [Column("images")] public string[]? Images { get; set; }

    [MaxLength(50)]
    [Column("category")]
    public string Category { get; set; } = "mid-range"; // budget, mid-range, luxury, boutique

    [Column("star_rating")] public int? StarRating { get; set; }

    [Column("price_per_night")] public decimal PricePerNight { get; set; } = 0;

    [MaxLength(10)] [Column("currency")] public string Currency { get; set; } = "USD";

    [Column("is_featured")] public bool IsFeatured { get; set; }

    [MaxLength(50)] [Column("phone")] public string? Phone { get; set; }

    [MaxLength(200)] [Column("email")] public string? Email { get; set; }

    [Column("website")] public string? Website { get; set; }

    [Column("check_in_time")] public TimeSpan CheckInTime { get; set; } = new(14, 0, 0); // 14:00

    [Column("check_out_time")] public TimeSpan CheckOutTime { get; set; } = new(11, 0, 0); // 11:00

    [Column("cancellation_policy")] public string? CancellationPolicy { get; set; }

    [Column("is_active")] public bool IsActive { get; set; } = true;

    [Column("created_by")] public Guid? CreatedBy { get; set; }

    [Column("updated_by")] public Guid? UpdatedBy { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     房型实体模型
/// </summary>
[Table("room_types")]
public class RoomType : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required] [Column("hotel_id")] public Guid HotelId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")] public string? Description { get; set; }

    [Column("max_occupancy")] public int MaxOccupancy { get; set; } = 2;

    [Column("size")] public decimal Size { get; set; } = 25.0m;

    [MaxLength(50)] [Column("bed_type")] public string BedType { get; set; } = "Queen";

    [Required] [Column("price_per_night")] public decimal PricePerNight { get; set; }

    [MaxLength(10)] [Column("currency")] public string Currency { get; set; } = "USD";

    [Column("available_rooms")] public int AvailableRooms { get; set; } = 0;

    [Column("amenities")] public string[]? Amenities { get; set; }

    [Column("images")] public string[]? Images { get; set; }

    [Column("is_available")] public bool IsAvailable { get; set; } = true;

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     酒店预订实体模型
/// </summary>
[Table("hotel_bookings")]
public class HotelBooking : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required] [Column("hotel_id")] public Guid HotelId { get; set; }

    [Required] [Column("room_type_id")] public Guid RoomTypeId { get; set; }

    [Required] [Column("user_id")] public Guid UserId { get; set; }

    [Required] [Column("check_in_date")] public DateTime CheckInDate { get; set; }

    [Required] [Column("check_out_date")] public DateTime CheckOutDate { get; set; }

    [Column("number_of_rooms")] public int NumberOfRooms { get; set; } = 1;

    [Column("number_of_guests")] public int NumberOfGuests { get; set; } = 1;

    [Required] [Column("total_price")] public decimal TotalPrice { get; set; }

    [MaxLength(10)] [Column("currency")] public string Currency { get; set; } = "USD";

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending"; // pending, confirmed, cancelled, completed, no-show

    [MaxLength(20)]
    [Column("payment_status")]
    public string PaymentStatus { get; set; } = "pending"; // pending, paid, refunded

    [Column("special_requests")] public string? SpecialRequests { get; set; }

    [MaxLength(200)]
    [Column("guest_name")]
    public string? GuestName { get; set; }

    [MaxLength(200)]
    [Column("guest_email")]
    public string? GuestEmail { get; set; }

    [MaxLength(50)]
    [Column("guest_phone")]
    public string? GuestPhone { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
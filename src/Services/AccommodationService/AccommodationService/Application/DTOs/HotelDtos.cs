using System.ComponentModel.DataAnnotations;

namespace AccommodationService.Application.DTOs;

/// <summary>
///     创建酒店请求DTO
/// </summary>
public class CreateHotelRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    public string? CityId { get; set; }
    
    public string? CityName { get; set; }
    
    public string? Country { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }

    [Range(0, 100000)]
    public decimal PricePerNight { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    // 数字游民功能
    public int? WifiSpeed { get; set; }
    public bool HasWifi { get; set; }
    public bool HasWorkDesk { get; set; }
    public bool HasCoworkingSpace { get; set; }
    public bool HasAirConditioning { get; set; }
    public bool HasKitchen { get; set; }
    public bool HasLaundry { get; set; }
    public bool HasParking { get; set; }
    public bool HasPool { get; set; }
    public bool HasGym { get; set; }
    public bool Has24HReception { get; set; }
    public bool HasLongStayDiscount { get; set; }
    public bool IsPetFriendly { get; set; }

    [Range(0, 100)]
    public decimal? LongStayDiscountPercent { get; set; }

    public string[]? Images { get; set; }

    /// <summary>
    ///     房型列表（创建酒店时可选）
    /// </summary>
    public List<CreateRoomTypeRequest>? RoomTypes { get; set; }
}

/// <summary>
///     更新酒店请求DTO
/// </summary>
public class UpdateHotelRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }

    [Range(0, 100000)]
    public decimal? PricePerNight { get; set; }

    // 数字游民功能
    public int? WifiSpeed { get; set; }
    public bool? HasWifi { get; set; }
    public bool? HasWorkDesk { get; set; }
    public bool? HasCoworkingSpace { get; set; }
    public bool? HasAirConditioning { get; set; }
    public bool? HasKitchen { get; set; }
    public bool? HasLaundry { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasPool { get; set; }
    public bool? HasGym { get; set; }
    public bool? Has24HReception { get; set; }
    public bool? HasLongStayDiscount { get; set; }
    public bool? IsPetFriendly { get; set; }

    [Range(0, 100)]
    public decimal? LongStayDiscountPercent { get; set; }

    public string[]? Images { get; set; }

    /// <summary>
    ///     房型列表（更新酒店时可选）
    /// </summary>
    public List<CreateRoomTypeRequest>? RoomTypes { get; set; }
}

/// <summary>
///     酒店响应DTO
/// </summary>
public class HotelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public Guid? CityId { get; set; }
    public string? CityName { get; set; }
    public string? Country { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public string[]? Images { get; set; }
    public string Category { get; set; } = "mid-range";
    public int? StarRating { get; set; }
    public decimal PricePerNight { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsFeatured { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // 数字游民功能
    public int? WifiSpeed { get; set; }
    public bool HasWifi { get; set; }
    public bool HasWorkDesk { get; set; }
    public bool HasCoworkingSpace { get; set; }
    public bool HasAirConditioning { get; set; }
    public bool HasKitchen { get; set; }
    public bool HasLaundry { get; set; }
    public bool HasParking { get; set; }
    public bool HasPool { get; set; }
    public bool HasGym { get; set; }
    public bool Has24HReception { get; set; }
    public bool HasLongStayDiscount { get; set; }
    public decimal? LongStayDiscountPercent { get; set; }
    public bool IsPetFriendly { get; set; }

    // 计算字段
    public int NomadScore { get; set; }

    // 房型列表
    public List<RoomTypeDto> RoomTypes { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

/// <summary>
///     酒店列表响应DTO
/// </summary>
public class HotelListResponse
{
    public List<HotelDto> Hotels { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
///     酒店查询参数
/// </summary>
public class HotelQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? CityId { get; set; }
    public string? Search { get; set; }
    public bool? HasWifi { get; set; }
    public bool? HasCoworkingSpace { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}

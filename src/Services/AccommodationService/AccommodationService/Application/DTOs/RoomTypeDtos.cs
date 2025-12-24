using System.ComponentModel.DataAnnotations;

namespace AccommodationService.Application.DTOs;

/// <summary>
///     创建房型请求DTO
/// </summary>
public class CreateRoomTypeRequest
{
    /// <summary>
    ///     房型ID（更新时提供，新建时为空）
    /// </summary>
    public string? Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 20)]
    public int MaxOccupancy { get; set; } = 2;

    [Range(5, 1000)]
    public decimal? RoomSize { get; set; } = 25;

    [MaxLength(50)]
    public string? BedType { get; set; } = "Double";

    [Range(0, 100000)]
    public decimal PricePerNight { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    [Range(0, 1000)]
    public int AvailableRooms { get; set; } = 1;

    public string[]? Amenities { get; set; }

    public string[]? Images { get; set; }

    public bool IsAvailable { get; set; } = true;

    // 房间设施
    public bool HasWifi { get; set; }
    public bool HasAirConditioning { get; set; }
    public bool HasPrivateBathroom { get; set; }
    public bool HasKitchen { get; set; }
    public bool HasBalcony { get; set; }
}

/// <summary>
///     更新房型请求DTO
/// </summary>
public class UpdateRoomTypeRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 20)]
    public int? MaxOccupancy { get; set; }

    [Range(5, 1000)]
    public decimal? Size { get; set; }

    [MaxLength(50)]
    public string? BedType { get; set; }

    [Range(0, 100000)]
    public decimal? PricePerNight { get; set; }

    [Range(0, 1000)]
    public int? AvailableRooms { get; set; }

    public string[]? Amenities { get; set; }

    public string[]? Images { get; set; }

    public bool? IsAvailable { get; set; }
}

/// <summary>
///     房型响应DTO
/// </summary>
public class RoomTypeDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxOccupancy { get; set; }
    public decimal Size { get; set; }
    public string BedType { get; set; } = "Double";
    public decimal PricePerNight { get; set; }
    public string Currency { get; set; } = "USD";
    public int AvailableRooms { get; set; }
    public string[]? Amenities { get; set; }
    public string[]? Images { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
}

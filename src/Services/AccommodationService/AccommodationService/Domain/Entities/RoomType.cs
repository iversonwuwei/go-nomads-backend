using Postgrest.Attributes;
using Postgrest.Models;

namespace AccommodationService.Domain.Entities;

/// <summary>
///     RoomType 实体 - 酒店房型
/// </summary>
[Table("room_types")]
public class RoomType : BaseModel
{
    public RoomType() { }

    [PrimaryKey("id")] 
    public Guid Id { get; set; }

    [Column("hotel_id")] 
    public Guid HotelId { get; set; }

    [Column("name")] 
    public string Name { get; set; } = string.Empty;

    [Column("description")] 
    public string? Description { get; set; }

    [Column("max_occupancy")] 
    public int MaxOccupancy { get; set; } = 2;

    [Column("size")] 
    public decimal Size { get; set; }

    [Column("bed_type")] 
    public string BedType { get; set; } = "Double";

    [Column("price_per_night")] 
    public decimal PricePerNight { get; set; }

    [Column("currency")] 
    public string Currency { get; set; } = "USD";

    [Column("available_rooms")] 
    public int AvailableRooms { get; set; }

    [Column("amenities")] 
    public string[]? Amenities { get; set; }

    [Column("images")] 
    public string[]? Images { get; set; }

    [Column("is_available")] 
    public bool IsAvailable { get; set; } = true;

    [Column("created_at")] 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] 
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     工厂方法 - 创建房型
    /// </summary>
    public static RoomType Create(
        Guid hotelId,
        string name,
        string? description = null,
        int maxOccupancy = 2,
        decimal size = 25,
        string bedType = "Double",
        decimal pricePerNight = 0,
        string currency = "USD",
        int availableRooms = 1,
        string[]? amenities = null,
        string[]? images = null)
    {
        return new RoomType
        {
            Id = Guid.NewGuid(),
            HotelId = hotelId,
            Name = name,
            Description = description,
            MaxOccupancy = maxOccupancy,
            Size = size,
            BedType = bedType,
            PricePerNight = pricePerNight,
            Currency = currency,
            AvailableRooms = availableRooms,
            Amenities = amenities,
            Images = images,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     更新房型信息
    /// </summary>
    public void Update(
        string? name = null,
        string? description = null,
        int? maxOccupancy = null,
        decimal? pricePerNight = null,
        string[]? amenities = null,
        string[]? images = null,
        string? bedType = null,
        decimal? roomSize = null)
    {
        if (name != null) Name = name;
        if (description != null) Description = description;
        if (maxOccupancy.HasValue) MaxOccupancy = maxOccupancy.Value;
        if (pricePerNight.HasValue) PricePerNight = pricePerNight.Value;
        if (amenities != null) Amenities = amenities;
        if (images != null) Images = images;
        if (bedType != null) BedType = bedType;
        if (roomSize.HasValue) Size = roomSize.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}

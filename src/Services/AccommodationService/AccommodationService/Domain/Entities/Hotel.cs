using Postgrest.Attributes;
using Postgrest.Models;

namespace AccommodationService.Domain.Entities;

/// <summary>
///     Hotel 聚合根 - 酒店领域实体
///     封装业务规则和领域逻辑，支持数字游民友好功能
/// </summary>
[Table("hotels")]
public class Hotel : BaseModel
{
    /// <summary>
    ///     公共无参构造函数 (ORM 需要)
    /// </summary>
    public Hotel()
    {
    }

    [PrimaryKey("id")] public Guid Id { get; set; }

    [Column("name")] public string Name { get; set; } = string.Empty;

    [Column("city_id")] public Guid? CityId { get; set; }
    
    [Column("city_name")] public string? CityName { get; set; }
    
    [Column("country")] public string? Country { get; set; }

    [Column("address")] public string Address { get; set; } = string.Empty;

    [Column("description")] public string? Description { get; set; }

    [Column("location")] public string? Location { get; set; }

    [Column("latitude")] public decimal? Latitude { get; set; }

    [Column("longitude")] public decimal? Longitude { get; set; }

    [Column("rating")] public decimal Rating { get; set; }

    [Column("review_count")] public int ReviewCount { get; set; }

    [Column("images")] public string[]? Images { get; set; }

    [Column("amenities")] public string[]? Amenities { get; set; }

    [Column("category")] public string Category { get; set; } = "mid-range";

    [Column("star_rating")] public int? StarRating { get; set; }

    [Column("price_per_night")] public decimal PricePerNight { get; set; }

    [Column("currency")] public string Currency { get; set; } = "USD";

    [Column("is_featured")] public bool IsFeatured { get; set; }

    [Column("phone")] public string? Phone { get; set; }

    [Column("email")] public string? Email { get; set; }

    [Column("website")] public string? Website { get; set; }

    [Column("check_in_time")] public TimeSpan CheckInTime { get; set; } = new(14, 0, 0);

    [Column("check_out_time")] public TimeSpan CheckOutTime { get; set; } = new(11, 0, 0);

    [Column("cancellation_policy")] public string? CancellationPolicy { get; set; }

    // ============================================================
    // 数字游民友好功能字段
    // ============================================================

    [Column("wifi_speed")] public int? WifiSpeed { get; set; }

    [Column("has_wifi")] public bool HasWifi { get; set; }

    [Column("has_work_desk")] public bool HasWorkDesk { get; set; }

    [Column("has_coworking_space")] public bool HasCoworkingSpace { get; set; }

    [Column("has_air_conditioning")] public bool HasAirConditioning { get; set; }

    [Column("has_kitchen")] public bool HasKitchen { get; set; }

    [Column("has_laundry")] public bool HasLaundry { get; set; }

    [Column("has_parking")] public bool HasParking { get; set; }

    [Column("has_pool")] public bool HasPool { get; set; }

    [Column("has_gym")] public bool HasGym { get; set; }

    [Column("has_24h_reception")] public bool Has24HReception { get; set; }

    [Column("has_long_stay_discount")] public bool HasLongStayDiscount { get; set; }

    [Column("long_stay_discount_percent")] public decimal? LongStayDiscountPercent { get; set; }

    [Column("is_pet_friendly")] public bool IsPetFriendly { get; set; }

    // ============================================================
    // 状态和审计字段
    // ============================================================

    [Column("is_active")] public bool IsActive { get; set; } = true;

    [Column("created_by")] public Guid? CreatedBy { get; set; }

    [Column("updated_by")] public Guid? UpdatedBy { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ============================================================
    // 工厂方法
    // ============================================================

    /// <summary>
    ///     工厂方法 - 创建新的酒店
    /// </summary>
    public static Hotel Create(
        string name,
        string address,
        decimal? latitude,
        decimal? longitude,
        Guid? cityId = null,
        string? cityName = null,
        string? country = null,
        string? description = null,
        string[]? images = null,
        decimal pricePerNight = 0,
        string currency = "USD",
        string? phone = null,
        string? email = null,
        string? website = null,
        int? wifiSpeed = null,
        bool hasWifi = false,
        bool hasWorkDesk = false,
        bool hasCoworkingSpace = false,
        bool hasAirConditioning = false,
        bool hasKitchen = false,
        bool hasLaundry = false,
        bool hasParking = false,
        bool hasPool = false,
        bool hasGym = false,
        bool has24HReception = false,
        bool hasLongStayDiscount = false,
        decimal? longStayDiscountPercent = null,
        bool isPetFriendly = false,
        Guid? createdBy = null)
    {
        var hotel = new Hotel
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            CityId = cityId,
            CityName = cityName,
            Country = country,
            Description = description,
            Images = images,
            PricePerNight = pricePerNight,
            Currency = currency,
            Phone = phone,
            Email = email,
            Website = website,
            WifiSpeed = wifiSpeed,
            HasWifi = hasWifi,
            HasWorkDesk = hasWorkDesk,
            HasCoworkingSpace = hasCoworkingSpace,
            HasAirConditioning = hasAirConditioning,
            HasKitchen = hasKitchen,
            HasLaundry = hasLaundry,
            HasParking = hasParking,
            HasPool = hasPool,
            HasGym = hasGym,
            Has24HReception = has24HReception,
            HasLongStayDiscount = hasLongStayDiscount,
            LongStayDiscountPercent = longStayDiscountPercent,
            IsPetFriendly = isPetFriendly,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // 设置 PostGIS location
        if (latitude.HasValue && longitude.HasValue)
        {
            hotel.Location = $"POINT({longitude.Value} {latitude.Value})";
        }

        return hotel;
    }

    // ============================================================
    // 业务方法
    // ============================================================

    /// <summary>
    ///     更新酒店信息
    /// </summary>
    public void Update(
        string? name = null,
        string? address = null,
        string? description = null,
        decimal? latitude = null,
        decimal? longitude = null,
        decimal? pricePerNight = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string[]? images = null,
        int? wifiSpeed = null,
        bool? hasWifi = null,
        bool? hasWorkDesk = null,
        bool? hasCoworkingSpace = null,
        bool? hasAirConditioning = null,
        bool? hasKitchen = null,
        bool? hasLaundry = null,
        bool? hasParking = null,
        bool? hasPool = null,
        bool? hasGym = null,
        bool? has24HReception = null,
        bool? hasLongStayDiscount = null,
        decimal? longStayDiscountPercent = null,
        bool? isPetFriendly = null,
        Guid? updatedBy = null)
    {
        if (!string.IsNullOrEmpty(name)) Name = name;
        if (!string.IsNullOrEmpty(address)) Address = address;
        if (description != null) Description = description;
        if (pricePerNight.HasValue) PricePerNight = pricePerNight.Value;
        if (phone != null) Phone = phone;
        if (email != null) Email = email;
        if (website != null) Website = website;
        if (images != null) Images = images;

        // 数字游民功能
        if (wifiSpeed.HasValue) WifiSpeed = wifiSpeed;
        if (hasWifi.HasValue) HasWifi = hasWifi.Value;
        if (hasWorkDesk.HasValue) HasWorkDesk = hasWorkDesk.Value;
        if (hasCoworkingSpace.HasValue) HasCoworkingSpace = hasCoworkingSpace.Value;
        if (hasAirConditioning.HasValue) HasAirConditioning = hasAirConditioning.Value;
        if (hasKitchen.HasValue) HasKitchen = hasKitchen.Value;
        if (hasLaundry.HasValue) HasLaundry = hasLaundry.Value;
        if (hasParking.HasValue) HasParking = hasParking.Value;
        if (hasPool.HasValue) HasPool = hasPool.Value;
        if (hasGym.HasValue) HasGym = hasGym.Value;
        if (has24HReception.HasValue) Has24HReception = has24HReception.Value;
        if (hasLongStayDiscount.HasValue) HasLongStayDiscount = hasLongStayDiscount.Value;
        if (longStayDiscountPercent.HasValue) LongStayDiscountPercent = longStayDiscountPercent;
        if (isPetFriendly.HasValue) IsPetFriendly = isPetFriendly.Value;

        // 更新坐标
        if (latitude.HasValue && longitude.HasValue)
        {
            Latitude = latitude;
            Longitude = longitude;
            Location = $"POINT({longitude.Value} {latitude.Value})";
        }

        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     计算数字游民友好评分 (0-100)
    /// </summary>
    public int CalculateNomadScore()
    {
        var score = 0;

        // WiFi 是最重要的 (30分)
        if (HasWifi)
        {
            score += 10;
            if (WifiSpeed.HasValue)
            {
                if (WifiSpeed >= 100) score += 20;
                else if (WifiSpeed >= 50) score += 15;
                else if (WifiSpeed >= 20) score += 10;
                else score += 5;
            }
        }

        // 工作环境 (25分)
        if (HasWorkDesk) score += 15;
        if (HasCoworkingSpace) score += 10;

        // 生活便利性 (25分)
        if (HasAirConditioning) score += 5;
        if (HasKitchen) score += 10;
        if (HasLaundry) score += 5;
        if (Has24HReception) score += 5;

        // 长住支持 (10分)
        if (HasLongStayDiscount) score += 10;

        // 其他设施 (10分)
        if (HasGym) score += 3;
        if (HasPool) score += 3;
        if (HasParking) score += 2;
        if (IsPetFriendly) score += 2;

        return Math.Min(score, 100);
    }

    /// <summary>
    ///     激活酒店
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     停用酒店
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

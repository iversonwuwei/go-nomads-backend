using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CoworkingService.Domain.Entities;

/// <summary>
/// CoworkingSpace 聚合根 - 共享办公空间领域实体
/// 封装业务规则和领域逻辑
/// </summary>
[Table("coworking_spaces")]
public class CoworkingSpace : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; private set; }

    [Column("name")]
    public string Name { get; private set; } = string.Empty;

    [Column("city_id")]
    public Guid? CityId { get; private set; }

    [Column("address")]
    public string Address { get; private set; } = string.Empty;

    [Column("description")]
    public string? Description { get; private set; }

    [Column("image_url")]
    public string? ImageUrl { get; private set; }

    [Column("images")]
    public string[]? Images { get; private set; }

    [Column("price_per_day")]
    public decimal? PricePerDay { get; private set; }

    [Column("price_per_month")]
    public decimal? PricePerMonth { get; private set; }

    [Column("price_per_hour")]
    public decimal? PricePerHour { get; private set; }

    [Column("currency")]
    public string Currency { get; private set; } = "USD";

    [Column("rating")]
    public decimal Rating { get; private set; } = 0;

    [Column("review_count")]
    public int ReviewCount { get; private set; } = 0;

    [Column("wifi_speed")]
    public decimal? WifiSpeed { get; private set; }

    [Column("has_meeting_room")]
    public bool HasMeetingRoom { get; private set; }

    [Column("has_coffee")]
    public bool HasCoffee { get; private set; }

    [Column("has_parking")]
    public bool HasParking { get; private set; }

    [Column("has_24_7_access")]
    public bool Has247Access { get; private set; }

    [Column("amenities")]
    public string[]? Amenities { get; private set; }

    [Column("capacity")]
    public int? Capacity { get; private set; }

    [Column("location")]
    public string? Location { get; private set; }

    [Column("latitude")]
    public decimal? Latitude { get; private set; }

    [Column("longitude")]
    public decimal? Longitude { get; private set; }

    [Column("phone")]
    public string? Phone { get; private set; }

    [Column("email")]
    public string? Email { get; private set; }

    [Column("website")]
    public string? Website { get; private set; }

    [Column("opening_hours")]
    public string? OpeningHours { get; private set; }

    [Column("is_active")]
    public bool IsActive { get; private set; } = true;

    [Column("created_by")]
    public Guid? CreatedBy { get; private set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; private set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// 公共无参构造函数 (ORM 需要)
    /// </summary>
    public CoworkingSpace() { }

    /// <summary>
    /// 工厂方法 - 创建新的共享办公空间
    /// </summary>
    public static CoworkingSpace Create(
        string name,
        string address,
        decimal? latitude,
        decimal? longitude,
        Guid? cityId = null,
        string? description = null,
        string? imageUrl = null,
        string[]? images = null,
        decimal? pricePerDay = null,
        decimal? pricePerMonth = null,
        decimal? pricePerHour = null,
        string currency = "USD",
        decimal? wifiSpeed = null,
        bool hasMeetingRoom = false,
        bool hasCoffee = false,
        bool hasParking = false,
        bool has247Access = false,
        string[]? amenities = null,
        int? capacity = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string? openingHours = null,
        Guid? createdBy = null)
    {
        // 业务规则验证
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("地址不能为空", nameof(address));

        if (pricePerDay.HasValue && pricePerDay.Value < 0)
            throw new ArgumentException("日价格不能为负数", nameof(pricePerDay));

        if (pricePerMonth.HasValue && pricePerMonth.Value < 0)
            throw new ArgumentException("月价格不能为负数", nameof(pricePerMonth));

        if (pricePerHour.HasValue && pricePerHour.Value < 0)
            throw new ArgumentException("时价格不能为负数", nameof(pricePerHour));

        if (capacity.HasValue && capacity.Value <= 0)
            throw new ArgumentException("容量必须大于 0", nameof(capacity));

        return new CoworkingSpace
        {
            Id = Guid.NewGuid(),
            Name = name,
            CityId = cityId,
            Address = address,
            Description = description,
            ImageUrl = imageUrl,
            Images = images,
            PricePerDay = pricePerDay,
            PricePerMonth = pricePerMonth,
            PricePerHour = pricePerHour,
            Currency = currency,
            Rating = 0,
            ReviewCount = 0,
            WifiSpeed = wifiSpeed,
            HasMeetingRoom = hasMeetingRoom,
            HasCoffee = hasCoffee,
            HasParking = hasParking,
            Has247Access = has247Access,
            Amenities = amenities,
            Capacity = capacity,
            Latitude = latitude,
            Longitude = longitude,
            Phone = phone,
            Email = email,
            Website = website,
            OpeningHours = openingHours,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 领域方法 - 更新共享办公空间信息
    /// </summary>
    public void Update(
        string name,
        string address,
        decimal? latitude,
        decimal? longitude,
        Guid? cityId = null,
        string? description = null,
        string? imageUrl = null,
        string[]? images = null,
        decimal? pricePerDay = null,
        decimal? pricePerMonth = null,
        decimal? pricePerHour = null,
        string currency = "USD",
        decimal? wifiSpeed = null,
        bool hasMeetingRoom = false,
        bool hasCoffee = false,
        bool hasParking = false,
        bool has247Access = false,
        string[]? amenities = null,
        int? capacity = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string? openingHours = null,
        Guid? updatedBy = null)
    {
        // 业务规则验证
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名称不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("地址不能为空", nameof(address));

        if (pricePerDay.HasValue && pricePerDay.Value < 0)
            throw new ArgumentException("日价格不能为负数", nameof(pricePerDay));

        if (pricePerMonth.HasValue && pricePerMonth.Value < 0)
            throw new ArgumentException("月价格不能为负数", nameof(pricePerMonth));

        if (pricePerHour.HasValue && pricePerHour.Value < 0)
            throw new ArgumentException("时价格不能为负数", nameof(pricePerHour));

        if (capacity.HasValue && capacity.Value <= 0)
            throw new ArgumentException("容量必须大于 0", nameof(capacity));

        // 更新字段
        Name = name;
        CityId = cityId;
        Address = address;
        Description = description;
        ImageUrl = imageUrl;
        Images = images;
        PricePerDay = pricePerDay;
        PricePerMonth = pricePerMonth;
        PricePerHour = pricePerHour;
        Currency = currency;
        WifiSpeed = wifiSpeed;
        HasMeetingRoom = hasMeetingRoom;
        HasCoffee = hasCoffee;
        HasParking = hasParking;
        Has247Access = has247Access;
        Amenities = amenities;
        Capacity = capacity;
        Latitude = latitude;
        Longitude = longitude;
        Phone = phone;
        Email = email;
        Website = website;
        OpeningHours = openingHours;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 领域方法 - 添加评分
    /// </summary>
    public void AddReview(decimal rating)
    {
        if (rating < 0 || rating > 5)
            throw new ArgumentException("评分必须在 0-5 之间", nameof(rating));

        // 计算新的平均评分
        var totalRating = Rating * ReviewCount + rating;
        ReviewCount++;
        Rating = totalRating / ReviewCount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 领域方法 - 停用空间
    /// </summary>
    public void Deactivate(Guid? updatedBy = null)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 领域方法 - 激活空间
    /// </summary>
    public void Activate(Guid? updatedBy = null)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 领域查询 - 检查是否可预订
    /// </summary>
    public bool CanBook()
    {
        return IsActive && 
               (PricePerDay.HasValue || PricePerMonth.HasValue || PricePerHour.HasValue);
    }

    /// <summary>
    /// 领域查询 - 检查是否有特定设施
    /// </summary>
    public bool HasAmenity(string amenity)
    {
        return Amenities != null && Amenities.Contains(amenity, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 领域查询 - 计算指定类型的价格
    /// </summary>
    public decimal? GetPrice(string priceType)
    {
        return priceType.ToLower() switch
        {
            "day" => PricePerDay,
            "month" => PricePerMonth,
            "hour" => PricePerHour,
            _ => throw new ArgumentException("无效的价格类型", nameof(priceType))
        };
    }
}

using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     旅行历史实体 - 记录用户的旅行历史
/// </summary>
[Table("travel_history")]
public class TravelHistory : BaseModel
{
    // 公共无参构造函数 (ORM 需要)
    public TravelHistory()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    ///     国家名称
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column("country")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    ///     国家代码 (ISO 3166-1 alpha-2)，用于统计
    /// </summary>
    [StringLength(2)]
    [Column("country_code")]
    public string? CountryCode { get; set; }

    /// <summary>
    ///     纬度
    /// </summary>
    [Column("latitude")]
    public double? Latitude { get; set; }

    /// <summary>
    ///     经度
    /// </summary>
    [Column("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    ///     到达时间
    /// </summary>
    [Required]
    [Column("arrival_time")]
    public DateTime ArrivalTime { get; set; }

    /// <summary>
    ///     离开时间
    /// </summary>
    [Column("departure_time")]
    public DateTime? DepartureTime { get; set; }

    /// <summary>
    ///     是否已确认（用户确认后为 true，自动检测未确认为 false）
    /// </summary>
    [Column("is_confirmed")]
    public bool IsConfirmed { get; set; } = false;

    /// <summary>
    ///     旅行评价/回顾
    /// </summary>
    [Column("review")]
    public string? Review { get; set; }

    /// <summary>
    ///     评分 (1-5 星)
    /// </summary>
    [Column("rating")]
    public double? Rating { get; set; }

    /// <summary>
    ///     照片 URL 列表 (JSON 数组)
    /// </summary>
    [Column("photos")]
    public string? Photos { get; set; }

    /// <summary>
    ///     关联的城市 ID（可选，用于链接到城市详情）
    /// </summary>
    [Column("city_id")]
    public string? CityId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region 工厂方法

    /// <summary>
    ///     创建新的旅行历史记录
    /// </summary>
    public static TravelHistory Create(
        string userId,
        string city,
        string country,
        DateTime arrivalTime,
        DateTime? departureTime = null,
        double? latitude = null,
        double? longitude = null,
        bool isConfirmed = false,
        string? cityId = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("城市名称不能为空", nameof(city));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("国家名称不能为空", nameof(country));

        return new TravelHistory
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            City = city,
            Country = country,
            Latitude = latitude,
            Longitude = longitude,
            ArrivalTime = arrivalTime,
            DepartureTime = departureTime,
            IsConfirmed = isConfirmed,
            CityId = cityId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     从自动检测的候选行程创建
    /// </summary>
    public static TravelHistory CreateFromDetection(
        string userId,
        string city,
        string country,
        DateTime arrivalTime,
        DateTime departureTime,
        double latitude,
        double longitude)
    {
        return new TravelHistory
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            City = city,
            Country = country,
            Latitude = latitude,
            Longitude = longitude,
            ArrivalTime = arrivalTime,
            DepartureTime = departureTime,
            IsConfirmed = false, // 自动检测的需要用户确认
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region 领域方法

    /// <summary>
    ///     确认旅行记录
    /// </summary>
    public void Confirm()
    {
        IsConfirmed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     取消确认
    /// </summary>
    public void Unconfirm()
    {
        IsConfirmed = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     更新旅行记录
    /// </summary>
    public void Update(
        string? city = null,
        string? country = null,
        DateTime? arrivalTime = null,
        DateTime? departureTime = null,
        double? latitude = null,
        double? longitude = null,
        string? review = null,
        double? rating = null,
        string? photos = null,
        string? cityId = null)
    {
        if (!string.IsNullOrWhiteSpace(city))
            City = city;

        if (!string.IsNullOrWhiteSpace(country))
            Country = country;

        if (arrivalTime.HasValue)
            ArrivalTime = arrivalTime.Value;

        if (departureTime.HasValue)
            DepartureTime = departureTime.Value;

        if (latitude.HasValue)
            Latitude = latitude.Value;

        if (longitude.HasValue)
            Longitude = longitude.Value;

        if (review != null)
            Review = review;

        if (rating.HasValue)
            Rating = rating.Value;

        if (photos != null)
            Photos = photos;

        if (!string.IsNullOrWhiteSpace(cityId))
            CityId = cityId;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     添加评价
    /// </summary>
    public void AddReview(string review, double rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("评分必须在1-5之间", nameof(rating));

        Review = review;
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     获取停留天数
    /// </summary>
    public int? GetDurationDays()
    {
        if (DepartureTime == null)
            return null;

        return (DepartureTime.Value - ArrivalTime).Days;
    }

    /// <summary>
    ///     是否正在进行中（没有离开时间）
    /// </summary>
    public bool IsOngoing => DepartureTime == null;

    #endregion
}

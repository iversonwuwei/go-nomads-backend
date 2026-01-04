using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     访问地点实体 - 记录用户在旅行中访问过的具体地点（停留40分钟以上）
/// </summary>
[Table("visited_places")]
public class VisitedPlace : BaseModel
{
    // 公共无参构造函数 (ORM 需要)
    public VisitedPlace()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     关联的旅行历史 ID
    /// </summary>
    [Required]
    [Column("travel_history_id")]
    public string TravelHistoryId { get; set; } = string.Empty;

    /// <summary>
    ///     用户 ID
    /// </summary>
    [Required]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    ///     纬度
    /// </summary>
    [Required]
    [Column("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    ///     经度
    /// </summary>
    [Required]
    [Column("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    ///     地点名称（通过逆地理编码获取）
    /// </summary>
    [StringLength(200)]
    [Column("place_name")]
    public string? PlaceName { get; set; }

    /// <summary>
    ///     地点类型（餐厅、咖啡馆、景点、酒店等）
    /// </summary>
    [StringLength(50)]
    [Column("place_type")]
    public string? PlaceType { get; set; }

    /// <summary>
    ///     详细地址
    /// </summary>
    [StringLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    /// <summary>
    ///     到达时间
    /// </summary>
    [Required]
    [Column("arrival_time")]
    public DateTime ArrivalTime { get; set; }

    /// <summary>
    ///     离开时间
    /// </summary>
    [Required]
    [Column("departure_time")]
    public DateTime DepartureTime { get; set; }

    /// <summary>
    ///     停留时长（分钟）
    /// </summary>
    [Column("duration_minutes")]
    public int DurationMinutes { get; set; }

    /// <summary>
    ///     地点照片 URL（可选）
    /// </summary>
    [Column("photo_url")]
    public string? PhotoUrl { get; set; }

    /// <summary>
    ///     用户备注（可选）
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    ///     是否为精选地点（用户标记的重要地点）
    /// </summary>
    [Column("is_highlight")]
    public bool IsHighlight { get; set; } = false;

    /// <summary>
    ///     Google Place ID（可选，用于获取更多地点信息）
    /// </summary>
    [Column("google_place_id")]
    public string? GooglePlaceId { get; set; }

    /// <summary>
    ///     客户端生成的唯一标识（用于同步去重）
    /// </summary>
    [Column("client_id")]
    public string? ClientId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region 工厂方法

    /// <summary>
    ///     创建新的访问地点记录
    /// </summary>
    public static VisitedPlace Create(
        string travelHistoryId,
        string userId,
        double latitude,
        double longitude,
        DateTime arrivalTime,
        DateTime departureTime,
        string? placeName = null,
        string? placeType = null,
        string? address = null,
        string? clientId = null)
    {
        if (string.IsNullOrWhiteSpace(travelHistoryId))
            throw new ArgumentException("旅行历史ID不能为空", nameof(travelHistoryId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        var durationMinutes = (int)(departureTime - arrivalTime).TotalMinutes;

        return new VisitedPlace
        {
            Id = Guid.NewGuid().ToString(),
            TravelHistoryId = travelHistoryId,
            UserId = userId,
            Latitude = latitude,
            Longitude = longitude,
            ArrivalTime = arrivalTime,
            DepartureTime = departureTime,
            DurationMinutes = durationMinutes,
            PlaceName = placeName,
            PlaceType = placeType,
            Address = address,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region 领域方法

    /// <summary>
    ///     标记为精选地点
    /// </summary>
    public void MarkAsHighlight()
    {
        IsHighlight = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     取消精选标记
    /// </summary>
    public void UnmarkAsHighlight()
    {
        IsHighlight = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     更新地点信息
    /// </summary>
    public void UpdatePlaceInfo(string? placeName, string? placeType, string? address)
    {
        PlaceName = placeName;
        PlaceType = placeType;
        Address = address;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     添加备注
    /// </summary>
    public void AddNotes(string notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     更新照片
    /// </summary>
    public void UpdatePhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region 计算属性

    /// <summary>
    ///     获取格式化的停留时长
    /// </summary>
    public string FormattedDuration
    {
        get
        {
            var hours = DurationMinutes / 60;
            var minutes = DurationMinutes % 60;
            if (hours > 0)
            {
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            }
            return $"{minutes}m";
        }
    }

    /// <summary>
    ///     停留时长（小时）
    /// </summary>
    public double DurationHours => DurationMinutes / 60.0;

    /// <summary>
    ///     获取地点图标类型
    /// </summary>
    public string IconType
    {
        get
        {
            return PlaceType?.ToLower() switch
            {
                "restaurant" or "food" or "cafe" or "coffee" => "food",
                "hotel" or "lodging" or "accommodation" => "hotel",
                "park" or "nature" or "outdoor" => "nature",
                "shopping" or "store" or "mall" => "shopping",
                "museum" or "art" or "culture" => "culture",
                "coworking" or "office" or "work" => "work",
                "entertainment" or "bar" or "nightlife" => "entertainment",
                _ => "place"
            };
        }
    }

    #endregion
}

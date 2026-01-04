namespace UserService.Application.DTOs;

/// <summary>
///     访问地点 DTO
/// </summary>
public class VisitedPlaceDto
{
    public string Id { get; set; } = string.Empty;
    public string TravelHistoryId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? PlaceName { get; set; }
    public string? PlaceType { get; set; }
    public string? Address { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime DepartureTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsHighlight { get; set; }
    public string? GooglePlaceId { get; set; }
    public string? ClientId { get; set; }
    public string FormattedDuration { get; set; } = string.Empty;
    public string IconType { get; set; } = "place";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     创建访问地点请求 DTO
/// </summary>
public class CreateVisitedPlaceDto
{
    /// <summary>
    ///     关联的旅行历史 ID
    /// </summary>
    public string TravelHistoryId { get; set; } = string.Empty;

    /// <summary>
    ///     纬度
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    ///     经度
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    ///     地点名称
    /// </summary>
    public string? PlaceName { get; set; }

    /// <summary>
    ///     地点类型
    /// </summary>
    public string? PlaceType { get; set; }

    /// <summary>
    ///     详细地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    ///     到达时间
    /// </summary>
    public DateTime ArrivalTime { get; set; }

    /// <summary>
    ///     离开时间
    /// </summary>
    public DateTime DepartureTime { get; set; }

    /// <summary>
    ///     地点照片 URL
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    ///     用户备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    ///     是否为精选地点
    /// </summary>
    public bool IsHighlight { get; set; } = false;

    /// <summary>
    ///     Google Place ID
    /// </summary>
    public string? GooglePlaceId { get; set; }

    /// <summary>
    ///     客户端生成的唯一标识（用于同步去重）
    /// </summary>
    public string? ClientId { get; set; }
}

/// <summary>
///     更新访问地点请求 DTO
/// </summary>
public class UpdateVisitedPlaceDto
{
    public string? PlaceName { get; set; }
    public string? PlaceType { get; set; }
    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public bool? IsHighlight { get; set; }
    public string? GooglePlaceId { get; set; }
}

/// <summary>
///     批量创建访问地点请求 DTO（用于同步自动检测的停留点）
/// </summary>
public class BatchCreateVisitedPlaceDto
{
    /// <summary>
    ///     关联的旅行历史 ID
    /// </summary>
    public string TravelHistoryId { get; set; } = string.Empty;

    /// <summary>
    ///     访问地点列表
    /// </summary>
    public List<CreateVisitedPlaceDto> Items { get; set; } = new();
}

/// <summary>
///     访问地点简要 DTO（用于列表展示）
/// </summary>
public class VisitedPlaceSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string? PlaceName { get; set; }
    public string? PlaceType { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public string FormattedDuration { get; set; } = string.Empty;
    public string IconType { get; set; } = "place";
    public bool IsHighlight { get; set; }
}

/// <summary>
///     旅行访问地点统计 DTO
/// </summary>
public class TravelVisitedPlaceStatsDto
{
    /// <summary>
    ///     旅行历史 ID
    /// </summary>
    public string TravelHistoryId { get; set; } = string.Empty;

    /// <summary>
    ///     总访问地点数
    /// </summary>
    public int TotalPlaces { get; set; }

    /// <summary>
    ///     精选地点数
    /// </summary>
    public int HighlightPlaces { get; set; }

    /// <summary>
    ///     总停留时间（分钟）
    /// </summary>
    public int TotalDurationMinutes { get; set; }

    /// <summary>
    ///     地点类型分布
    /// </summary>
    public Dictionary<string, int> PlaceTypeDistribution { get; set; } = new();
}

/// <summary>
///     城市访问摘要 DTO - 用于 Visited Places 页面头部信息
/// </summary>
public class VisitedPlacesCitySummaryDto
{
    /// <summary>
    ///     城市 ID
    /// </summary>
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称（本地语言）
    /// </summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     城市英文名称
    /// </summary>
    public string? CityNameEn { get; set; }

    /// <summary>
    ///     国家名称
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    ///     城市图片 URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    ///     旅行日期（第一次到达）
    /// </summary>
    public DateTime? TravelDate { get; set; }

    /// <summary>
    ///     最后访问日期
    /// </summary>
    public DateTime? LastVisitDate { get; set; }

    /// <summary>
    ///     总停留天数
    /// </summary>
    public int TotalDurationDays { get; set; }

    /// <summary>
    ///     当前天气信息
    /// </summary>
    public CityWeatherDto? Weather { get; set; }

    /// <summary>
    ///     城市综合评分 (0-5)
    /// </summary>
    public decimal? OverallScore { get; set; }

    /// <summary>
    ///     平均每月花费（美元）
    /// </summary>
    public decimal? AverageMonthlyCost { get; set; }

    /// <summary>
    ///     共享办公空间数量
    /// </summary>
    public int CoworkingSpaceCount { get; set; }

    /// <summary>
    ///     访问地点分页列表
    /// </summary>
    public PaginatedVisitedPlacesDto VisitedPlaces { get; set; } = new();
}

/// <summary>
///     城市天气 DTO
/// </summary>
public class CityWeatherDto
{
    /// <summary>
    ///     当前温度（摄氏度）
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    ///     体感温度
    /// </summary>
    public double? FeelsLike { get; set; }

    /// <summary>
    ///     天气状况描述
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    ///     天气图标代码
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    ///     湿度 (%)
    /// </summary>
    public int? Humidity { get; set; }

    /// <summary>
    ///     风速
    /// </summary>
    public string? WindSpeed { get; set; }
}

/// <summary>
///     分页访问地点列表 DTO
/// </summary>
public class PaginatedVisitedPlacesDto
{
    /// <summary>
    ///     访问地点列表
    /// </summary>
    public List<VisitedPlaceDto> Items { get; set; } = new();

    /// <summary>
    ///     总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    ///     当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    ///     每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    ///     是否有更多数据
    /// </summary>
    public bool HasMore => Page * PageSize < TotalCount;
}

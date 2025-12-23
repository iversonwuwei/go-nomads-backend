namespace UserService.Application.DTOs;

/// <summary>
///     旅行历史 DTO
/// </summary>
public class TravelHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime? DepartureTime { get; set; }
    public bool IsConfirmed { get; set; }
    public string? Review { get; set; }
    public double? Rating { get; set; }
    public List<string>? Photos { get; set; }
    public string? CityId { get; set; }
    public int? DurationDays { get; set; }
    public bool IsOngoing { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     创建旅行历史请求 DTO
/// </summary>
public class CreateTravelHistoryDto
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime ArrivalTime { get; set; }
    public DateTime? DepartureTime { get; set; }
    public bool IsConfirmed { get; set; } = true;
    public string? Review { get; set; }
    public double? Rating { get; set; }
    public List<string>? Photos { get; set; }
    public string? CityId { get; set; }
}

/// <summary>
///     更新旅行历史请求 DTO
/// </summary>
public class UpdateTravelHistoryDto
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public DateTime? DepartureTime { get; set; }
    public bool? IsConfirmed { get; set; }
    public string? Review { get; set; }
    public double? Rating { get; set; }
    public List<string>? Photos { get; set; }
    public string? CityId { get; set; }
}

/// <summary>
///     批量创建旅行历史请求 DTO（用于同步自动检测的行程）
/// </summary>
public class BatchCreateTravelHistoryDto
{
    public List<CreateTravelHistoryDto> Items { get; set; } = new();
}

/// <summary>
///     旅行历史简要 DTO（用于列表展示）
/// </summary>
public class TravelHistorySummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime ArrivalTime { get; set; }
    public DateTime? DepartureTime { get; set; }
    public int? DurationDays { get; set; }
    public bool IsConfirmed { get; set; }
    public double? Rating { get; set; }
}

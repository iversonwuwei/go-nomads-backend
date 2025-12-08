namespace CityService.Application.DTOs;

/// <summary>
///     附近城市DTO
/// </summary>
public class NearbyCityDto
{
    public string Id { get; set; } = string.Empty;
    public string SourceCityId { get; set; } = string.Empty;
    public string? TargetCityId { get; set; }
    public string TargetCityName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public string TransportationType { get; set; } = string.Empty;
    public int TravelTimeMinutes { get; set; }
    public List<string> Highlights { get; set; } = new();
    public NearbyCityNomadFeaturesDto NomadFeatures { get; set; } = new();
    public string? ImageUrl { get; set; }
    public double? OverallScore { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsAIGenerated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     附近城市的数字游民相关特色DTO
/// </summary>
public class NearbyCityNomadFeaturesDto
{
    /// <summary>
    ///     预计月生活成本 (美元)
    /// </summary>
    public double? MonthlyCostUsd { get; set; }

    /// <summary>
    ///     网络速度 (Mbps)
    /// </summary>
    public int? InternetSpeedMbps { get; set; }

    /// <summary>
    ///     联合办公空间数量
    /// </summary>
    public int? CoworkingSpaces { get; set; }

    /// <summary>
    ///     签证便利性描述
    /// </summary>
    public string? VisaInfo { get; set; }

    /// <summary>
    ///     安全评分 (1-5)
    /// </summary>
    public double? SafetyScore { get; set; }

    /// <summary>
    ///     生活质量描述
    /// </summary>
    public string? QualityOfLife { get; set; }
}

/// <summary>
///     保存附近城市请求DTO
/// </summary>
public class SaveNearbyCitiesRequest
{
    public string SourceCityId { get; set; } = string.Empty;
    public List<NearbyCityItemDto> NearbyCities { get; set; } = new();
}

/// <summary>
///     附近城市项目DTO（用于保存请求）
/// </summary>
public class NearbyCityItemDto
{
    public string? TargetCityId { get; set; }
    public string TargetCityName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public string TransportationType { get; set; } = string.Empty;
    public int TravelTimeMinutes { get; set; }
    public List<string> Highlights { get; set; } = new();
    public NearbyCityNomadFeaturesDto NomadFeatures { get; set; } = new();
    public string? ImageUrl { get; set; }
    public double? OverallScore { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsAIGenerated { get; set; } = true;
}

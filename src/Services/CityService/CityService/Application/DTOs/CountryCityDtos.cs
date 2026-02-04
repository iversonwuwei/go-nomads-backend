namespace CityService.Application.DTOs;

public class CountryCitiesDto
{
    public string Country { get; set; } = string.Empty;
    public List<CitySummaryDto> Cities { get; set; } = new();
}

public class CitySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Region { get; set; }
}

/// <summary>
/// 城市聚合数据 DTO - 用于异步加载城市统计信息
/// </summary>
public class CityCountsDto
{
    public Guid CityId { get; set; }
    public int MeetupCount { get; set; }
    public int CoworkingCount { get; set; }
    public int ReviewCount { get; set; }
    public decimal AverageCost { get; set; }
}

/// <summary>
/// 城市列表项 DTO - 用于城市列表页面的轻量级数据
/// 不包含实时天气信息（性能考虑），但包含对数字游民有价值的静态评分数据
/// </summary>
public class CityListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Country { get; set; }
    public Guid? CountryId { get; set; }
    public string? Region { get; set; }
    public string? ImageUrl { get; set; }
    public string? PortraitImageUrl { get; set; }
    public List<string>? LandscapeImageUrls { get; set; }

    // 城市简介
    public string? Description { get; set; }

    // 时区信息 - 远程工作需考虑时差
    public string? TimeZone { get; set; }

    // 货币
    public string? Currency { get; set; }

    // 综合评分
    public decimal? OverallScore { get; set; }

    // 数字游民核心关注指标
    public decimal? InternetQualityScore { get; set; }  // 互联网质量评分 - 最重要
    public decimal? SafetyScore { get; set; }           // 安全评分
    public decimal? CostScore { get; set; }             // 成本评分
    public decimal? CommunityScore { get; set; }        // 社区活跃度评分
    public decimal? WeatherScore { get; set; }          // 天气评分（静态评分，非实时天气）

    // 标签 - 快速了解城市特点
    public List<string>? Tags { get; set; }

    // 用户交互
    public bool IsFavorite { get; set; }
    public int ReviewCount { get; set; }
    public decimal AverageCost { get; set; }

    // 社区活跃度指标
    public int MeetupCount { get; set; }
    public int CoworkingCount { get; set; }

    // 版主信息
    public Guid? ModeratorId { get; set; }
    public ModeratorDto? Moderator { get; set; }

    // 当前用户权限
    public bool IsCurrentUserModerator { get; set; }
    public bool IsCurrentUserAdmin { get; set; }

    // 地理位置
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>
    /// 创建当前对象的深拷贝
    /// </summary>
    public CityListItemDto Clone()
    {
        return new CityListItemDto
        {
            Id = Id,
            Name = Name,
            NameEn = NameEn,
            Country = Country,
            CountryId = CountryId,
            Region = Region,
            ImageUrl = ImageUrl,
            PortraitImageUrl = PortraitImageUrl,
            LandscapeImageUrls = LandscapeImageUrls?.ToList(),
            Description = Description,
            TimeZone = TimeZone,
            Currency = Currency,
            OverallScore = OverallScore,
            InternetQualityScore = InternetQualityScore,
            SafetyScore = SafetyScore,
            CostScore = CostScore,
            CommunityScore = CommunityScore,
            WeatherScore = WeatherScore,
            Tags = Tags?.ToList(),
            IsFavorite = IsFavorite,
            ReviewCount = ReviewCount,
            AverageCost = AverageCost,
            MeetupCount = MeetupCount,
            CoworkingCount = CoworkingCount,
            ModeratorId = ModeratorId,
            Moderator = Moderator,
            IsCurrentUserModerator = IsCurrentUserModerator,
            IsCurrentUserAdmin = IsCurrentUserAdmin,
            Latitude = Latitude,
            Longitude = Longitude
        };
    }
}
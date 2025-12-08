namespace AIService.Application.DTOs;

/// <summary>
///     对话响应
/// </summary>
public class ConversationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokens { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
///     消息响应
/// </summary>
public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public string? ModelName { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    public int? ResponseTimeMs { get; set; }
    public string? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsError { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     AI 聊天响应
/// </summary>
public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "assistant";
    public string? ModelName { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? FinishReason { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
    public MessageResponse? UserMessage { get; set; }
    public MessageResponse? AssistantMessage { get; set; }
}

/// <summary>
///     分页响应
/// </summary>
public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

/// <summary>
///     用户统计响应
/// </summary>
public class UserStatsResponse
{
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokens { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

/// <summary>
///     流式响应块
/// </summary>
public class StreamResponse
{
    public string Delta { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public string? FinishReason { get; set; }
    public int? TokenCount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
///     旅行计划响应
/// </summary>
public class TravelPlanResponse
{
    public string Id { get; set; } = string.Empty;
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string CityImage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Duration { get; set; }
    public string Budget { get; set; } = string.Empty;
    public string TravelStyle { get; set; } = string.Empty;
    public List<string> Interests { get; set; } = new();
    public string? DepartureLocation { get; set; } // 出发地
    public DateTime? DepartureDate { get; set; } // 出发日期
    public TransportationPlanDto Transportation { get; set; } = new();
    public AccommodationPlanDto Accommodation { get; set; } = new();
    public List<DailyItineraryDto> DailyItineraries { get; set; } = new();
    public List<AttractionDto> Attractions { get; set; } = new();
    public List<RestaurantDto> Restaurants { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public BudgetBreakdownDto BudgetBreakdown { get; set; } = new();
}

public class TransportationPlanDto
{
    public string ArrivalMethod { get; set; } = string.Empty;
    public string ArrivalDetails { get; set; } = string.Empty;
    public double EstimatedCost { get; set; }
    public string LocalTransport { get; set; } = string.Empty; // 存储逗号分隔的交通方式
    public string LocalTransportDetails { get; set; } = string.Empty;
    public double DailyTransportCost { get; set; }
}

public class AccommodationPlanDto
{
    public string Type { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public double PricePerNight { get; set; }
    public List<string> Amenities { get; set; } = new();
    public string BookingTips { get; set; } = string.Empty;
}

public class DailyItineraryDto
{
    public int Day { get; set; }
    public string Theme { get; set; } = string.Empty;
    public List<ActivityDto> Activities { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class ActivityDto
{
    public string Time { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double EstimatedCost { get; set; }
    public int Duration { get; set; }
}

public class AttractionDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Location { get; set; } = string.Empty;
    public double EntryFee { get; set; }
    public string BestTime { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}

public class RestaurantDto
{
    public string Name { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string PriceRange { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}

public class BudgetBreakdownDto
{
    public double Transportation { get; set; }
    public double Accommodation { get; set; }
    public double Food { get; set; }
    public double Activities { get; set; }
    public double Miscellaneous { get; set; }
    public double Total { get; set; }
    public string Currency { get; set; } = "USD";
}

/// <summary>
///     数字游民旅游指南响应
/// </summary>
public class TravelGuideResponse
{
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public VisaInfoDto VisaInfo { get; set; } = new();
    public List<BestAreaDto> BestAreas { get; set; } = new();
    public List<string> WorkspaceRecommendations { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public Dictionary<string, string> EssentialInfo { get; set; } = new();
}

/// <summary>
///     附近城市响应
/// </summary>
public class NearbyCitiesResponse
{
    public string SourceCityId { get; set; } = string.Empty;
    public string SourceCityName { get; set; } = string.Empty;
    public List<NearbyCityItemResponse> Cities { get; set; } = new();
}

/// <summary>
///     附近城市项目响应
/// </summary>
public class NearbyCityItemResponse
{
    /// <summary>
    ///     城市名称
    /// </summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     所属国家
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    ///     距离（公里）
    /// </summary>
    public double DistanceKm { get; set; }

    /// <summary>
    ///     主要交通方式 (train/bus/car/flight/ferry)
    /// </summary>
    public string TransportationType { get; set; } = string.Empty;

    /// <summary>
    ///     预计旅行时间（分钟）
    /// </summary>
    public int TravelTimeMinutes { get; set; }

    /// <summary>
    ///     城市亮点/特色
    /// </summary>
    public List<string> Highlights { get; set; } = new();

    /// <summary>
    ///     数字游民相关特色
    /// </summary>
    public NearbyCityNomadFeaturesResponse NomadFeatures { get; set; } = new();

    /// <summary>
    ///     纬度
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    ///     经度
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    ///     综合评分 (1-5)
    /// </summary>
    public double? OverallScore { get; set; }

    /// <summary>
    ///     城市图片 URL
    /// </summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
///     附近城市的数字游民相关特色响应
/// </summary>
public class NearbyCityNomadFeaturesResponse
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

public class BestAreaDto
{
    /// <summary>
    ///     区域名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     区域描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     娱乐评分 (1-5)
    /// </summary>
    public double EntertainmentScore { get; set; }

    /// <summary>
    ///     娱乐设施说明
    /// </summary>
    public string EntertainmentDescription { get; set; } = string.Empty;

    /// <summary>
    ///     旅游评分 (1-5)
    /// </summary>
    public double TourismScore { get; set; }

    /// <summary>
    ///     旅游景点说明
    /// </summary>
    public string TourismDescription { get; set; } = string.Empty;

    /// <summary>
    ///     经济评分 (1-5,越低越便宜)
    /// </summary>
    public double EconomyScore { get; set; }

    /// <summary>
    ///     经济情况说明(生活成本)
    /// </summary>
    public string EconomyDescription { get; set; } = string.Empty;

    /// <summary>
    ///     文化评分 (1-5)
    /// </summary>
    public double CultureScore { get; set; }

    /// <summary>
    ///     文化特色说明
    /// </summary>
    public string CultureDescription { get; set; } = string.Empty;
}

public class VisaInfoDto
{
    public string Type { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Requirements { get; set; } = string.Empty;
    public double Cost { get; set; }
    public string Process { get; set; } = string.Empty;
}

/// <summary>
///     图片生成响应
/// </summary>
public class GenerateImageResponse
{
    /// <summary>
    ///     生成的图片信息列表
    /// </summary>
    public List<GeneratedImageInfo> Images { get; set; } = new();

    /// <summary>
    ///     任务ID（通义万象返回）
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    ///     生成耗时（毫秒）
    /// </summary>
    public int GenerationTimeMs { get; set; }

    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
///     生成的图片信息
/// </summary>
public class GeneratedImageInfo
{
    /// <summary>
    ///     Supabase Storage 中的公开访问 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    ///     存储路径
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    ///     原始 URL（通义万象返回的临时 URL）
    /// </summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>
    ///     文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }
}

/// <summary>
///     城市图片批量生成响应
/// </summary>
public class GenerateCityImagesResponse
{
    /// <summary>
    ///     城市ID
    /// </summary>
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     竖屏封面图（720*1280）
    /// </summary>
    public GeneratedImageInfo? PortraitImage { get; set; }

    /// <summary>
    ///     横屏图片列表（1280*720）
    /// </summary>
    public List<GeneratedImageInfo> LandscapeImages { get; set; } = new();

    /// <summary>
    ///     生成耗时（毫秒）
    /// </summary>
    public int GenerationTimeMs { get; set; }

    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
///     旅行计划摘要（用于列表显示）
/// </summary>
public class AiTravelPlanSummary
{
    /// <summary>
    ///     计划ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     城市ID
    /// </summary>
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称
    /// </summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     城市图片
    /// </summary>
    public string? CityImage { get; set; }

    /// <summary>
    ///     旅行天数
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    ///     预算级别
    /// </summary>
    public string BudgetLevel { get; set; } = string.Empty;

    /// <summary>
    ///     旅行风格
    /// </summary>
    public string TravelStyle { get; set; } = string.Empty;

    /// <summary>
    ///     状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    ///     创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
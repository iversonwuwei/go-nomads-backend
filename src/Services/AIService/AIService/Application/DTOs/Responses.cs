namespace AIService.Application.DTOs;

/// <summary>
/// 对话响应
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
/// 消息响应
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
/// AI 聊天响应
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
/// 分页响应
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
/// 用户统计响应
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
/// 流式响应块
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
/// 旅行计划响应
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
/// 数字游民旅游指南响应
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

public class BestAreaDto
{
    /// <summary>
    /// 区域名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 区域描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 娱乐评分 (1-5)
    /// </summary>
    public double EntertainmentScore { get; set; }
    
    /// <summary>
    /// 娱乐设施说明
    /// </summary>
    public string EntertainmentDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 旅游评分 (1-5)
    /// </summary>
    public double TourismScore { get; set; }
    
    /// <summary>
    /// 旅游景点说明
    /// </summary>
    public string TourismDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 经济评分 (1-5,越低越便宜)
    /// </summary>
    public double EconomyScore { get; set; }
    
    /// <summary>
    /// 经济情况说明(生活成本)
    /// </summary>
    public string EconomyDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// 文化评分 (1-5)
    /// </summary>
    public double CultureScore { get; set; }
    
    /// <summary>
    /// 文化特色说明
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
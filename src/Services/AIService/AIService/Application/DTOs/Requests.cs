using System.ComponentModel.DataAnnotations;

namespace AIService.Application.DTOs;

/// <summary>
///     创建对话请求
/// </summary>
public class CreateConversationRequest
{
    [Required(ErrorMessage = "对话标题不能为空")]
    [StringLength(200, ErrorMessage = "对话标题不能超过200个字符")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "系统提示不能超过1000个字符")]
    public string? SystemPrompt { get; set; }

    [StringLength(50, ErrorMessage = "模型名称不能超过50个字符")]
    public string ModelName { get; set; } = "qwen-plus";
}

/// <summary>
///     发送消息请求
/// </summary>
public class SendMessageRequest
{
    [Required(ErrorMessage = "消息内容不能为空")]
    [StringLength(50000, ErrorMessage = "消息内容不能超过50000个字符")]
    public string Content { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "模型名称不能超过50个字符")]
    public string? ModelName { get; set; }

    /// <summary>
    ///     是否流式响应
    /// </summary>
    public bool Stream { get; set; } = false;

    /// <summary>
    ///     温度参数 (0.0-2.0)
    /// </summary>
    [Range(0.0, 2.0, ErrorMessage = "温度参数必须在0.0-2.0之间")]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    ///     最大输出token数
    /// </summary>
    [Range(1, 8000, ErrorMessage = "最大输出token数必须在1-8000之间")]
    public int MaxTokens { get; set; } = 2000;
}

/// <summary>
///     更新对话请求
/// </summary>
public class UpdateConversationRequest
{
    [StringLength(200, ErrorMessage = "对话标题不能超过200个字符")]
    public string? Title { get; set; }

    [StringLength(1000, ErrorMessage = "系统提示不能超过1000个字符")]
    public string? SystemPrompt { get; set; }
}

/// <summary>
///     对话查询请求
/// </summary>
public class GetConversationsRequest
{
    public string? Status { get; set; } // active, archived, all

    [Range(1, 100, ErrorMessage = "页码必须在1-100之间")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "每页数量必须在1-50之间")]
    public int PageSize { get; set; } = 20;
}

/// <summary>
///     消息查询请求
/// </summary>
public class GetMessagesRequest
{
    [Range(1, 100, ErrorMessage = "页码必须在1-100之间")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "每页数量必须在1-100之间")]
    public int PageSize { get; set; } = 50;

    public bool IncludeSystem { get; set; } = false;
}

/// <summary>
///     生成旅行计划请求
/// </summary>
public class GenerateTravelPlanRequest
{
    [Required(ErrorMessage = "城市ID不能为空")] public string CityId { get; set; } = string.Empty;

    [Required(ErrorMessage = "城市名称不能为空")] public string CityName { get; set; } = string.Empty;

    public string? CityImage { get; set; }

    [Range(1, 30, ErrorMessage = "旅行天数必须在1-30天之间")]
    public int Duration { get; set; } = 7;

    [Required(ErrorMessage = "预算等级不能为空")]
    [RegularExpression("^(low|medium|high)$", ErrorMessage = "预算等级必须是 low, medium 或 high")]
    public string Budget { get; set; } = "medium";

    [Required(ErrorMessage = "旅行风格不能为空")]
    [RegularExpression("^(adventure|relaxation|culture|nightlife)$",
        ErrorMessage = "旅行风格必须是 adventure, relaxation, culture 或 nightlife")]
    public string TravelStyle { get; set; } = "culture";

    public List<string> Interests { get; set; } = new();

    public string? DepartureLocation { get; set; }

    public DateTime? DepartureDate { get; set; }

    public string? CustomBudget { get; set; }

    public string? Currency { get; set; } = "USD";

    public List<string>? SelectedAttractions { get; set; }
}

/// <summary>
///     生成数字游民旅游指南请求
/// </summary>
public class GenerateTravelGuideRequest
{
    [Required(ErrorMessage = "城市ID不能为空")] public string CityId { get; set; } = string.Empty;

    [Required(ErrorMessage = "城市名称不能为空")] public string CityName { get; set; } = string.Empty;
}

/// <summary>
///     生成附近城市请求
/// </summary>
public class GenerateNearbyCitiesRequest
{
    [Required(ErrorMessage = "城市ID不能为空")]
    public string CityId { get; set; } = string.Empty;

    [Required(ErrorMessage = "城市名称不能为空")]
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     城市所在国家
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    ///     搜索半径（公里），默认100公里
    /// </summary>
    public int RadiusKm { get; set; } = 100;

    /// <summary>
    ///     返回城市数量，默认4个
    /// </summary>
    public int Count { get; set; } = 4;
}

/// <summary>
///     生成图片请求 (通义万象) - 单张图片
/// </summary>
public class GenerateImageRequest
{
    /// <summary>
    ///     正向提示词，描述期望生成的图像内容
    /// </summary>
    [Required(ErrorMessage = "提示词不能为空")]
    [StringLength(800, ErrorMessage = "提示词不能超过800个字符")]
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    ///     反向提示词，描述不希望出现的元素（可选）
    /// </summary>
    [StringLength(800, ErrorMessage = "反向提示词不能超过800个字符")]
    public string? NegativePrompt { get; set; }

    /// <summary>
    ///     输出图像的风格
    ///     可选值: auto, photography, portrait, 3d cartoon, anime, oil painting, watercolor, sketch, chinese painting, flat illustration
    /// </summary>
    public string Style { get; set; } = "<auto>";

    /// <summary>
    ///     输出图像的分辨率
    ///     可选值: 1024*1024, 720*1280, 1280*720
    /// </summary>
    public string Size { get; set; } = "1024*1024";

    /// <summary>
    ///     生成图片的数量 (1-4)
    /// </summary>
    [Range(1, 4, ErrorMessage = "生成数量必须在1-4之间")]
    public int Count { get; set; } = 1;

    /// <summary>
    ///     Supabase Storage 中的存储桶名称
    /// </summary>
    public string Bucket { get; set; } = "city-photos";

    /// <summary>
    ///     存储路径前缀（可选，例如 "city-covers" 或 "avatars"）
    /// </summary>
    public string? PathPrefix { get; set; }
}

/// <summary>
///     批量生成城市图片请求（1张竖屏 + 4张横屏）
/// </summary>
public class GenerateCityImagesRequest
{
    /// <summary>
    ///     城市ID
    /// </summary>
    [Required(ErrorMessage = "城市ID不能为空")]
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称（用于生成提示词）
    /// </summary>
    [Required(ErrorMessage = "城市名称不能为空")]
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     城市所在国家（用于生成提示词）
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    ///     竖屏封面图的提示词（可选，如果不提供则自动生成）
    /// </summary>
    public string? PortraitPrompt { get; set; }

    /// <summary>
    ///     横屏图片的提示词（可选，如果不提供则自动生成）
    /// </summary>
    public string? LandscapePrompt { get; set; }

    /// <summary>
    ///     反向提示词
    /// </summary>
    public string? NegativePrompt { get; set; }

    /// <summary>
    ///     输出图像的风格
    /// </summary>
    public string Style { get; set; } = "<photography>";

    /// <summary>
    ///     Supabase Storage 中的存储桶名称
    /// </summary>
    public string Bucket { get; set; } = "city-photos";

    /// <summary>
    ///     用户ID（用于推送通知，由服务端传递）
    /// </summary>
    public string? UserId { get; set; }
}
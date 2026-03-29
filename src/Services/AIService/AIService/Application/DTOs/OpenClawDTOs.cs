using System.Text.Json.Serialization;

namespace AIService.Application.DTOs;

// ============================================================
// OpenClaw 请求 DTOs
// ============================================================

/// <summary>
///     执行自然语言指令请求
/// </summary>
public class OpenClawExecuteRequest
{
    /// <summary>
    ///     用户指令文本
    /// </summary>
    public string Command { get; set; } = "";

    /// <summary>
    ///     会话 ID（可选，用于关联上下文）
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
///     设置提醒请求
/// </summary>
public class OpenClawReminderRequest
{
    /// <summary>
    ///     提醒文本
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    ///     触发时间
    /// </summary>
    public DateTime TriggerTime { get; set; }
}

/// <summary>
///     设置签证提醒请求
/// </summary>
public class OpenClawVisaReminderRequest
{
    /// <summary>
    ///     国家名称
    /// </summary>
    public string Country { get; set; } = "";

    /// <summary>
    ///     签证到期日期
    /// </summary>
    public DateTime ExpiryDate { get; set; }
}

/// <summary>
///     旅行规划预研究请求
/// </summary>
public class OpenClawResearchRequest
{
    /// <summary>
    ///     目的地城市
    /// </summary>
    public string CityName { get; set; } = "";

    /// <summary>
    ///     行程天数
    /// </summary>
    public int Duration { get; set; } = 7;

    /// <summary>
    ///     预算档位
    /// </summary>
    public string Budget { get; set; } = "";

    /// <summary>
    ///     旅行风格
    /// </summary>
    public string TravelStyle { get; set; } = "";

    /// <summary>
    ///     规划模式
    /// </summary>
    public string PlanningMode { get; set; } = "";

    /// <summary>
    ///     规划目标
    /// </summary>
    public string PlanningObjective { get; set; } = "";

    /// <summary>
    ///     研究信号
    /// </summary>
    public List<string> ResearchSignals { get; set; } = new();

    /// <summary>
    ///     兴趣点
    /// </summary>
    public List<string> Interests { get; set; } = new();

    /// <summary>
    ///     出发地
    /// </summary>
    public string? DepartureLocation { get; set; }

    /// <summary>
    ///     出发日期
    /// </summary>
    public DateTime? DepartureDate { get; set; }

    /// <summary>
    ///     会话 ID（可选）
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
///     旅行规划预研究结果
/// </summary>
public class OpenClawResearchResponse
{
    /// <summary>
    ///     后端生成的 session key
    /// </summary>
    public string SessionKey { get; set; } = "";

    /// <summary>
    ///     研究摘要
    /// </summary>
    public string Summary { get; set; } = "";

    /// <summary>
    ///     关键洞察
    /// </summary>
    public List<string> Insights { get; set; } = new();

    /// <summary>
    ///     建议核查项
    /// </summary>
    public List<string> Checks { get; set; } = new();

    /// <summary>
    ///     OpenClaw 原始响应
    /// </summary>
    public string RawResponse { get; set; } = "";
}

// ============================================================
// OpenClaw 内部通信 DTOs（与 OpenClaw Gateway 交互）
// ============================================================

/// <summary>
///     OpenClaw Gateway Chat Completion 请求
/// </summary>
public class OpenClawChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "openclaw:main";

    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("messages")]
    public List<OpenClawChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2000;
}

/// <summary>
///     OpenClaw Gateway Chat 消息
/// </summary>
public class OpenClawChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

/// <summary>
///     OpenClaw Gateway Chat Completion 响应
/// </summary>
public class OpenClawChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("object")]
    public string Object { get; set; } = "";

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenClawChoice> Choices { get; set; } = new();
}

/// <summary>
///     OpenClaw Gateway Choice
/// </summary>
public class OpenClawChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public OpenClawChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = "";
}

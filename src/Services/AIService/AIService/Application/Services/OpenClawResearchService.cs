using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIService.Application.DTOs;
using Microsoft.Extensions.Options;

namespace AIService.Application.Services;

public class OpenClawOptions
{
    public const string SectionName = "OpenClaw";

    public bool Enabled { get; set; } = true;
    public string GatewayUrl { get; set; } = "wss://api.go-nomads.com/openclaw";
    public string GatewayToken { get; set; } = string.Empty;
    public string ClientId { get; set; } = "gonomads-backend";
    public int ConnectTimeoutSeconds { get; set; } = 15;
    public int RequestTimeoutSeconds { get; set; } = 70;
    public int HistoryTimeoutSeconds { get; set; } = 35;
    public int PollIntervalMs { get; set; } = 1500;
}

public class OpenClawResearchService : IOpenClawResearchService
{
    private static readonly Regex SessionKeyRegex = new("[^a-z0-9]+", RegexOptions.Compiled);
    private readonly ILogger<OpenClawResearchService> _logger;
    private readonly OpenClawOptions _options;

    public OpenClawResearchService(IOptions<OpenClawOptions> options, ILogger<OpenClawResearchService> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public bool IsConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.GatewayUrl) &&
        !string.IsNullOrWhiteSpace(_options.GatewayToken);

    public async Task<OpenClawResearchResponse?> ResearchTravelPlanAsync(
        OpenClawResearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenClaw research skipped: gateway is not configured");
            return null;
        }

        var normalizedCity = SessionKeyRegex.Replace(request.CityName.ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(normalizedCity))
            normalizedCity = "city";

        var sessionKey = $"travel-plan-{normalizedCity}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var requestId = $"travel-plan-research-{Guid.NewGuid():N}";

        try
        {
            await using var client = new OpenClawGatewayRpcClient(_options, _logger);
            await client.ConnectAsync(cancellationToken);

            await client.RequestAsync(
                "chat.send",
                new
                {
                    sessionKey,
                    message = BuildPrompt(request),
                    thinking = request.PlanningMode == "research" ? "high" : "medium",
                    deliver = false,
                    timeoutMs = 60000,
                    idempotencyKey = requestId
                },
                cancellationToken);

            var assistantReply = await PollAssistantReplyAsync(client, sessionKey, cancellationToken);
            if (string.IsNullOrWhiteSpace(assistantReply))
                return null;

            return ParseBrief(sessionKey, assistantReply);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenClaw research failed for city {CityName}", request.CityName);
            return null;
        }
    }

    private async Task<string?> PollAssistantReplyAsync(
        OpenClawGatewayRpcClient client,
        string sessionKey,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.HistoryTimeoutSeconds);

        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            var history = await client.RequestAsync(
                "chat.history",
                new
                {
                    sessionKey,
                    limit = 12
                },
                cancellationToken);

            var assistantReply = ExtractLatestAssistantReply(history);
            if (!string.IsNullOrWhiteSpace(assistantReply))
                return assistantReply;

            await Task.Delay(_options.PollIntervalMs, cancellationToken);
        }

        return null;
    }

    private static string? ExtractLatestAssistantReply(JsonElement? history)
    {
        if (history is not { ValueKind: JsonValueKind.Object } historyValue)
            return null;

        if (!historyValue.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var message in messages.EnumerateArray().Reverse())
        {
            if (!message.TryGetProperty("role", out var role) || role.GetString() != "assistant")
                continue;

            if (!message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;

            var buffer = new StringBuilder();
            foreach (var part in content.EnumerateArray())
            {
                if (!part.TryGetProperty("type", out var type) || type.GetString() != "text")
                    continue;

                if (!part.TryGetProperty("text", out var textNode))
                    continue;

                var text = textNode.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (buffer.Length > 0)
                    buffer.Append('\n');
                buffer.Append(text);
            }

            if (buffer.Length > 0)
                return buffer.ToString();
        }

        return null;
    }

    private static OpenClawResearchResponse ParseBrief(string sessionKey, string rawReply)
    {
        var normalized = StripMarkdownFence(rawReply).Trim();

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var summary = document.RootElement.TryGetProperty("summary", out var summaryNode)
                    ? summaryNode.GetString()?.Trim() ?? string.Empty
                    : string.Empty;

                return new OpenClawResearchResponse
                {
                    SessionKey = sessionKey,
                    Summary = string.IsNullOrWhiteSpace(summary) ? "OpenClaw 已完成研究预处理。" : summary,
                    Insights = ReadStringList(document.RootElement, "insights"),
                    Checks = ReadStringList(document.RootElement, "checks"),
                    RawResponse = rawReply
                };
            }
        }
        catch
        {
        }

        return new OpenClawResearchResponse
        {
            SessionKey = sessionKey,
            Summary = normalized,
            Insights = new List<string>(),
            Checks = new List<string>(),
            RawResponse = rawReply
        };
    }

    private static List<string> ReadStringList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            return new List<string>();

        return property.EnumerateArray()
            .Select(item => Regex.Replace(item.ToString(), "\\s+", " ").Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(3)
            .ToList();
    }

    private static string StripMarkdownFence(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        trimmed = Regex.Replace(trimmed, "^```[a-zA-Z0-9_-]*\\s*", string.Empty);
        trimmed = Regex.Replace(trimmed, "\\s*```$", string.Empty);
        return trimmed;
    }

    private static string BuildPrompt(OpenClawResearchRequest request)
    {
        var normalizedSignals = request.ResearchSignals.Any()
            ? request.ResearchSignals
            : DefaultSignalsForMode(request.PlanningMode);
        var cleanedInterests = request.Interests.Where(item => !item.StartsWith("openclaw_")).ToList();

        var buffer = new StringBuilder()
            .AppendLine("你是 Go Nomads 的 OpenClaw 旅行研究层。")
            .AppendLine("目标不是直接输出完整 itinerary，而是给下游 AI 旅行规划生成一个高质量、可执行的研究摘要。")
            .AppendLine("请只返回 JSON，不要 Markdown，不要代码块，不要额外解释。")
            .AppendLine("你的职责是先做策略判断，再给出压缩后的研究信号，供后续 itinerary 生成模型直接消费。")
            .AppendLine("JSON schema:")
            .AppendLine("{")
            .AppendLine("  \"summary\": \"一句中文摘要，80字内，必须体现模式和目标\",")
            .AppendLine("  \"insights\": [\"3条以内的关键信号，每条32字内，必须具体，不要空话\"],")
            .AppendLine("  \"checks\": [\"3条以内的落地核对项，每条32字内，必须可执行\"]")
            .AppendLine("}")
            .AppendLine()
            .AppendLine("用户输入:")
            .AppendLine($"- 城市: {request.CityName}")
            .AppendLine($"- 天数: {request.Duration}")
            .AppendLine($"- 预算: {request.Budget}")
            .AppendLine($"- 风格: {request.TravelStyle}")
            .AppendLine($"- 规划模式: {request.PlanningMode}")
            .AppendLine($"- 规划目标: {request.PlanningObjective}")
            .AppendLine($"- 研究信号: {string.Join('、', normalizedSignals)}")
            .AppendLine($"- 兴趣偏好: {(cleanedInterests.Count == 0 ? "无" : string.Join('、', cleanedInterests))}")
            .AppendLine($"- 出发地: {TrimToNull(request.DepartureLocation) ?? "未提供"}")
            .AppendLine($"- 出发日期: {request.DepartureDate?.ToString("O") ?? "未提供"}")
            .AppendLine()
            .AppendLine("模式约束:")
            .AppendLine(PlanningModeInstruction(request.PlanningMode))
            .AppendLine()
            .AppendLine("目标约束:")
            .AppendLine(PlanningObjectiveInstruction(request.PlanningObjective))
            .AppendLine()
            .AppendLine("信号优先级:")
            .AppendLine(ResearchSignalInstruction(normalizedSignals))
            .AppendLine()
            .AppendLine("输出规则:")
            .AppendLine("1. summary 必须先说清楚这一版路线应该偏向什么，不要重复输入字段。")
            .AppendLine("2. insights 必须给下游 itinerary 模型真正有用的约束，例如节奏、区域、时段、预算风险、天气风险、办公可行性。")
            .AppendLine("3. checks 必须是出行前或排程时要核对的动作，不要写成泛泛建议。")
            .AppendLine("4. 如果模式是 quick，就少写核对项，优先保留方向判断。")
            .AppendLine("5. 如果模式是 research，就把实时信号和不确定性写清楚，让下游模型显式做兜底。")
            .AppendLine("6. 不要输出景点长名单，不要写完整行程，不要解释推理过程。")
            .AppendLine("7. 输出必须可直接给另一个 AI 作为规划前置信号。");

        return buffer.ToString();
    }

    private static List<string> DefaultSignalsForMode(string planningMode) => planningMode switch
    {
        "quick" => new List<string> { "weather", "transit" },
        "research" => new List<string> { "weather", "events", "coworking", "transit", "budget" },
        _ => new List<string> { "weather", "events", "budget" }
    };

    private static string PlanningModeInstruction(string planningMode) => planningMode switch
    {
        "quick" => "你在 quick 模式下要优先给出方向判断和高风险提醒，减少展开，不做过重研究；结论要利于快速生成首稿。",
        "research" => "你在 research 模式下要更重视实时信号、潜在冲突、备选方案和排程不确定性；输出要像严谨的前置 briefing。",
        _ => "你在 balanced 模式下要在效率、体验、预算与执行可行性之间折中，不偏向极端保守或极端激进。"
    };

    private static string PlanningObjectiveInstruction(string planningObjective) => planningObjective switch
    {
        "work" => "重点审视共享办公质量、安静时段、通勤切换成本、白天专注窗口，以及娱乐安排是否打断工作节奏。",
        "explore" => "重点审视街区漫游、城市代表性体验、本地活动密度、天气对探索体验的影响，以及是否过度模板化。",
        _ => "同时兼顾工作可持续性与城市体验，避免两者互相挤压到不可执行。"
    };

    private static string ResearchSignalInstruction(IEnumerable<string> signals)
    {
        var descriptions = signals.Select(signal => signal switch
        {
            "weather" => "天气: 判断户外活动是否需要备选和时段调整",
            "events" => "活动: 判断是否值得插入本地活动或避开拥挤时段",
            "coworking" => "办公: 判断远程工作落点是否稳定",
            "transit" => "交通: 判断跨区切换和日内动线是否过重",
            "visa" => "签证: 判断入境或停留限制是否影响路线安排",
            "budget" => "预算: 判断当前预算与天数/体验级别是否失衡",
            _ => $"{signal}: 若相关则纳入判断"
        });

        return string.Join('；', descriptions);
    }

    private static string? TrimToNull(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
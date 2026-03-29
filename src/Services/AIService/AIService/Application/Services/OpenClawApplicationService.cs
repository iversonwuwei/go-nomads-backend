using System.Text.Json;
using System.Net.Http.Json;
using AIService.Application.DTOs;

namespace AIService.Application.Services;

/// <summary>
///     OpenClaw 自动化服务实现
///     负责与 OpenClaw Gateway 通信，执行自然语言指令和预设自动化场景
/// </summary>
public class OpenClawApplicationService : IOpenClawService
{
    private static readonly string[] SensitiveKeywords = ["支付", "转账", "付款", "银行卡", "密码", "pay", "transfer", "password"];

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenClawApplicationService> _logger;

    public OpenClawApplicationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenClawApplicationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenClawClient");
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ExecuteCommandAsync(string userCommand, string? sessionId = null)
    {
        _logger.LogInformation("执行 OpenClaw 指令: {Command}, SessionId={SessionId}", userCommand, sessionId);

        // 敏感操作检测
        if (SensitiveKeywords.Any(k => userCommand.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("检测到敏感操作关键词: {Command}", userCommand);
            return "⚠️ 检测到敏感操作，请手动完成后再记录结果";
        }

        var content = await SendChatCompletionAsync(
            new List<OpenClawChatMessage>
            {
                new() { Role = "user", Content = userCommand }
            },
            sessionId);

        _logger.LogInformation("OpenClaw 指令执行成功: {Result}", content);
        return content;
    }

    /// <inheritdoc />
    public async Task<string> SetReminderAsync(string reminderText, DateTime triggerTime, string? sessionId = null)
    {
        var command = $"在 {triggerTime:yyyy-MM-dd HH:mm} 提醒我：{reminderText}";
        return await ExecuteCommandAsync(command, sessionId);
    }

    /// <inheritdoc />
    public async Task<string> SetVisaReminderAsync(string country, DateTime expiryDate, string? sessionId = null)
    {
        var daysRemaining = (expiryDate - DateTime.UtcNow).Days;
        var command = $"{country}签证还有 {daysRemaining} 天到期，帮我设提醒（提前7天、提前3天、当天）";
        return await ExecuteCommandAsync(command, sessionId);
    }

    /// <inheritdoc />
    public async Task<string> RunAutomationAsync(string scenarioName, Dictionary<string, string> parameters, string? sessionId = null)
    {
        var prompt = BuildScenarioPrompt(scenarioName, parameters);
        return await ExecuteCommandAsync(prompt, sessionId);
    }

    /// <inheritdoc />
    public async Task<OpenClawResearchResponse> ResearchTravelPlanAsync(OpenClawResearchRequest request, string? sessionId = null)
    {
        _logger.LogInformation(
            "执行 OpenClaw 旅行研究: City={CityName}, Duration={Duration}, SessionId={SessionId}",
            request.CityName,
            request.Duration,
            sessionId);

        var rawResponse = await SendChatCompletionAsync(
            new List<OpenClawChatMessage>
            {
                new()
                {
                    Role = "system",
                    Content = "你是 GoNomads 的旅行研究代理。只输出一个 JSON 对象，不要 Markdown，不要解释。JSON 必须包含 summary、insights、checks 三个字段。summary 是一句简洁摘要；insights 和 checks 都是最多 3 条的字符串数组。"
                },
                new()
                {
                    Role = "user",
                    Content = BuildTravelResearchPrompt(request)
                }
            },
            sessionId,
            1200);

        var parsed = ParseResearchResponse(rawResponse, sessionId);

        _logger.LogInformation(
            "OpenClaw 旅行研究完成: City={CityName}, Summary={Summary}",
            request.CityName,
            parsed.Summary);

        return parsed;
    }

    private static string BuildScenarioPrompt(string scenario, Dictionary<string, string> param)
    {
        return scenario.ToLower() switch
        {
            "flight_checkin" => $"帮我值机{param.GetValueOrDefault("flight")}航班",
            "calendar_sync" => $"把{param.GetValueOrDefault("source")}的订单同步到系统日历",
            "expense_record" => $"记录一笔{param.GetValueOrDefault("amount")}元的{param.GetValueOrDefault("category")}，备注：{param.GetValueOrDefault("note", "")}",
            "visa_reminder" => $"{param.GetValueOrDefault("country")}签证还有{param.GetValueOrDefault("days")}天到期，帮我设提醒",
            "form_fill" => $"帮我填好{param.GetValueOrDefault("form_name")}的个人信息",
            "work_mode" => "帮我开启工作模式：关闭抖音、打开飞书、设置免打扰",
            "meeting_prep" => $"帮我准备{param.GetValueOrDefault("meeting_name")}会议",
            _ => scenario
        };
    }

    private async Task<string> SendChatCompletionAsync(
        List<OpenClawChatMessage> messages,
        string? sessionId = null,
        int maxTokens = 2000)
    {
        var baseUrl = _configuration["OpenClaw:BaseUrl"] ?? "http://localhost:8080";

        var request = new OpenClawChatRequest
        {
            Model = "openclaw:main",
            SessionId = sessionId,
            Messages = messages,
            MaxTokens = maxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{baseUrl}/v1/chat/completions",
            request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenClawChatResponse>();
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "执行完成";
    }

    private static string BuildTravelResearchPrompt(OpenClawResearchRequest request)
    {
        var lines = new List<string>
        {
            "请根据以下旅行规划信息做一份出行前研究。",
            $"目的地: {request.CityName}",
            $"行程时长: {request.Duration} 天",
            $"预算档位: {FallbackValue(request.Budget)}",
            $"旅行风格: {FallbackValue(request.TravelStyle)}",
            $"规划模式: {FallbackValue(request.PlanningMode)}",
            $"规划目标: {FallbackValue(request.PlanningObjective)}"
        };

        if (!string.IsNullOrWhiteSpace(request.DepartureLocation))
            lines.Add($"出发地: {request.DepartureLocation}");

        if (request.DepartureDate.HasValue)
            lines.Add($"出发日期: {request.DepartureDate:yyyy-MM-dd}");

        if (request.ResearchSignals.Count != 0)
            lines.Add($"重点研究信号: {string.Join("、", request.ResearchSignals.Where(static item => !string.IsNullOrWhiteSpace(item)))}");

        if (request.Interests.Count != 0)
            lines.Add($"已知兴趣偏好: {string.Join("、", request.Interests.Where(static item => !string.IsNullOrWhiteSpace(item)))}");

        lines.Add("请输出 JSON，包含：summary（1 句摘要）、insights（1-3 条关键发现）、checks（1-3 条出发前要核查的事项）。不要输出其他文本。");

        return string.Join(Environment.NewLine, lines);
    }

    private static OpenClawResearchResponse ParseResearchResponse(string rawResponse, string? sessionId)
    {
        if (TryParseResearchJson(rawResponse, out var response))
        {
            response.SessionKey = sessionId ?? string.Empty;
            response.RawResponse = rawResponse;
            return response;
        }

        return new OpenClawResearchResponse
        {
            SessionKey = sessionId ?? string.Empty,
            Summary = BuildFallbackSummary(rawResponse),
            Insights = new List<string>(),
            Checks = new List<string>(),
            RawResponse = rawResponse
        };
    }

    private static bool TryParseResearchJson(string rawResponse, out OpenClawResearchResponse response)
    {
        response = new OpenClawResearchResponse();

        var json = ExtractJsonObject(rawResponse);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var summary = root.TryGetProperty("summary", out var summaryElement)
                ? summaryElement.GetString()?.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(summary))
                return false;

            response = new OpenClawResearchResponse
            {
                Summary = summary,
                Insights = ReadStringArray(root, "insights"),
                Checks = ReadStringArray(root, "checks")
            };

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static List<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
            return new List<string>();

        return element
            .EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.String)
            .Select(static item => item.GetString()?.Trim())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Take(3)
            .Cast<string>()
            .ToList();
    }

    private static string ExtractJsonObject(string rawResponse)
    {
        var trimmed = rawResponse.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');

        if (start < 0 || end <= start)
            return string.Empty;

        return trimmed[start..(end + 1)];
    }

    private static string BuildFallbackSummary(string rawResponse)
    {
        var collapsed = string.Join(
            ' ',
            rawResponse
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(static line => line.Trim())
                .Where(static line => !string.IsNullOrWhiteSpace(line)));

        if (string.IsNullOrWhiteSpace(collapsed))
            return "OpenClaw 已完成研究，但暂时无法结构化解析结果";

        return collapsed.Length <= 160 ? collapsed : collapsed[..160];
    }

    private static string FallbackValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "未指定" : value.Trim();
    }
}

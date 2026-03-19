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
        _logger.LogInformation("执行 OpenClaw 指令: {Command}", userCommand);

        // 敏感操作检测
        if (SensitiveKeywords.Any(k => userCommand.Contains(k, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("检测到敏感操作关键词: {Command}", userCommand);
            return "⚠️ 检测到敏感操作，请手动完成后再记录结果";
        }

        var baseUrl = _configuration["OpenClaw:BaseUrl"] ?? "http://localhost:8080";

        var request = new OpenClawChatRequest
        {
            Model = "openclaw:main",
            Messages = new List<OpenClawChatMessage>
            {
                new() { Role = "user", Content = userCommand }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{baseUrl}/v1/chat/completions",
            request);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenClawChatResponse>();
        var content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "执行完成";

        _logger.LogInformation("OpenClaw 指令执行成功: {Result}", content);
        return content;
    }

    /// <inheritdoc />
    public async Task<string> SetReminderAsync(string reminderText, DateTime triggerTime)
    {
        var command = $"在 {triggerTime:yyyy-MM-dd HH:mm} 提醒我：{reminderText}";
        return await ExecuteCommandAsync(command);
    }

    /// <inheritdoc />
    public async Task<string> SetVisaReminderAsync(string country, DateTime expiryDate)
    {
        var daysRemaining = (expiryDate - DateTime.UtcNow).Days;
        var command = $"{country}签证还有 {daysRemaining} 天到期，帮我设提醒（提前7天、提前3天、当天）";
        return await ExecuteCommandAsync(command);
    }

    /// <inheritdoc />
    public async Task<string> RunAutomationAsync(string scenarioName, Dictionary<string, string> parameters)
    {
        var prompt = BuildScenarioPrompt(scenarioName, parameters);
        return await ExecuteCommandAsync(prompt);
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
}

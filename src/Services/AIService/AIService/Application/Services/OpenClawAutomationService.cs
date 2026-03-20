using System.Text;
using System.Text.Json;
using AIService.Application.DTOs;
using Microsoft.Extensions.Options;

namespace AIService.Application.Services;

public class OpenClawAutomationService : IOpenClawAutomationService
{
    private readonly ILogger<OpenClawAutomationService> _logger;
    private readonly OpenClawOptions _options;

    public OpenClawAutomationService(IOptions<OpenClawOptions> options, ILogger<OpenClawAutomationService> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    private bool IsConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.GatewayUrl) &&
        !string.IsNullOrWhiteSpace(_options.GatewayToken);

    public async Task<OpenClawAutomationResponse> ExecuteCommandAsync(
        OpenClawExecuteRequest request, CancellationToken cancellationToken = default)
    {
        var sessionKey = request.SessionId ?? $"execute-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var prompt = new StringBuilder()
            .AppendLine("你是 Go Nomads 的 OpenClaw 自动化执行层。")
            .AppendLine("用户通过自然语言发出指令，你需要理解意图并执行。")
            .AppendLine("返回 JSON: {\"status\": \"done|pending|failed\", \"result\": \"执行结果摘要\"}")
            .AppendLine()
            .AppendLine($"用户指令: {request.Command}")
            .ToString();

        return await SendToGatewayAsync(sessionKey, prompt, cancellationToken);
    }

    public async Task<OpenClawAutomationResponse> SetReminderAsync(
        OpenClawReminderRequest request, CancellationToken cancellationToken = default)
    {
        var sessionKey = $"reminder-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var prompt = new StringBuilder()
            .AppendLine("你是 Go Nomads 的 OpenClaw 提醒管理层。")
            .AppendLine("请为用户设置一个提醒任务。")
            .AppendLine("返回 JSON: {\"status\": \"done|pending|failed\", \"result\": \"提醒设置结果\"}")
            .AppendLine()
            .AppendLine($"提醒内容: {request.Text}")
            .AppendLine($"触发时间: {request.TriggerTime:O}")
            .ToString();

        return await SendToGatewayAsync(sessionKey, prompt, cancellationToken);
    }

    public async Task<OpenClawAutomationResponse> SetVisaReminderAsync(
        OpenClawVisaReminderRequest request, CancellationToken cancellationToken = default)
    {
        var sessionKey = $"visa-reminder-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var prompt = new StringBuilder()
            .AppendLine("你是 Go Nomads 的 OpenClaw 签证合规层。")
            .AppendLine("请为用户设置签证到期提醒，提前 30 天、14 天、7 天分别触发。")
            .AppendLine("返回 JSON: {\"status\": \"done|pending|failed\", \"result\": \"签证提醒设置结果\"}")
            .AppendLine()
            .AppendLine($"国家: {request.Country}")
            .AppendLine($"签证到期日: {request.ExpiryDate:O}")
            .ToString();

        return await SendToGatewayAsync(sessionKey, prompt, cancellationToken);
    }

    public async Task<OpenClawAutomationResponse> RunAutomationAsync(
        string scenario, OpenClawAutomationRequest request, CancellationToken cancellationToken = default)
    {
        var sessionKey = $"automation-{scenario}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var paramsDescription = request.Params.Count > 0
            ? string.Join('\n', request.Params.Select(kv => $"  - {kv.Key}: {kv.Value}"))
            : "  无额外参数";

        var prompt = new StringBuilder()
            .AppendLine("你是 Go Nomads 的 OpenClaw 预设自动化执行层。")
            .AppendLine($"用户触发了预设场景「{scenario}」，请根据场景语义和参数执行自动化。")
            .AppendLine("返回 JSON: {\"status\": \"done|pending|failed\", \"result\": \"执行结果摘要\"}")
            .AppendLine()
            .AppendLine($"场景: {scenario}")
            .AppendLine("参数:")
            .AppendLine(paramsDescription)
            .ToString();

        return await SendToGatewayAsync(sessionKey, prompt, cancellationToken);
    }

    public async Task<OpenClawAutomationResponse> OrganizeInvoicesAsync(
        OpenClawInvoiceOrganizeRequest request, CancellationToken cancellationToken = default)
    {
        var sessionKey = $"invoice-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var prompt = new StringBuilder()
            .AppendLine("你是 Go Nomads 的 OpenClaw 记账报销层。")
            .AppendLine("请整理用户的发票信息，按日期和类别分类，然后发送到指定邮箱。")
            .AppendLine("返回 JSON: {\"status\": \"done|pending|failed\", \"result\": \"发票整理结果\"}")
            .AppendLine()
            .AppendLine($"接收邮箱: {request.Email}")
            .ToString();

        return await SendToGatewayAsync(sessionKey, prompt, cancellationToken);
    }

    public async Task<OpenClawAutomationResponse> CreateScriptAsync(
        OpenClawScriptRequest request, CancellationToken cancellationToken = default)
    {
        var sessionKey = $"script-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        var prompt = new StringBuilder()
            .AppendLine("你是 Go Nomads 的 OpenClaw 脚本编排层。")
            .AppendLine("请为用户创建一个定时自动化脚本。")
            .AppendLine("返回 JSON: {\"status\": \"done|pending|failed\", \"result\": \"脚本创建结果\", \"scriptId\": \"生成的脚本ID\"}")
            .AppendLine()
            .AppendLine($"脚本指令: {request.Command}")
            .AppendLine($"调度计划: {request.Schedule ?? "手动触发"}")
            .ToString();

        return await SendToGatewayAsync(sessionKey, prompt, cancellationToken);
    }

    /// <summary>
    ///     统一的 OpenClaw Gateway 通信方法
    /// </summary>
    private async Task<OpenClawAutomationResponse> SendToGatewayAsync(
        string sessionKey, string prompt, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("OpenClaw automation skipped: gateway is not configured");
            return new OpenClawAutomationResponse
            {
                Success = false,
                Error = "OpenClaw 服务未配置"
            };
        }

        try
        {
            await using var client = new OpenClawGatewayRpcClient(_options, _logger);
            await client.ConnectAsync(cancellationToken);

            var requestId = $"automation-{Guid.NewGuid():N}";

            await client.RequestAsync(
                "chat.send",
                new
                {
                    sessionKey,
                    message = prompt,
                    thinking = "medium",
                    deliver = false,
                    timeoutMs = 60000,
                    idempotencyKey = requestId
                },
                cancellationToken);

            var assistantReply = await PollAssistantReplyAsync(client, sessionKey, cancellationToken);

            if (string.IsNullOrWhiteSpace(assistantReply))
            {
                return new OpenClawAutomationResponse
                {
                    Success = false,
                    Error = "OpenClaw 未返回结果，请稍后重试"
                };
            }

            return new OpenClawAutomationResponse
            {
                Success = true,
                Data = assistantReply,
                Message = "执行完成"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenClaw automation failed for session {SessionKey}", sessionKey);
            return new OpenClawAutomationResponse
            {
                Success = false,
                Error = "OpenClaw 当前不可用，请稍后重试"
            };
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
                new { sessionKey, limit = 12 },
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
}

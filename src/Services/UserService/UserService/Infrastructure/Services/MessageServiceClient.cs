using System.Net.Http.Json;

namespace UserService.Infrastructure.Services;

/// <summary>
///     消息服务客户端接口 - 用于调用 MessageService API
/// </summary>
public interface IMessageServiceClient
{
    /// <summary>
    ///     向所有管理员发送通知
    /// </summary>
    Task SendNotificationToAdminsAsync(
        string title,
        string message,
        string type,
        string? relatedId = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     消息服务客户端 - 通过 HTTP 调用 MessageService 的通知 API
/// </summary>
public class MessageServiceClient : IMessageServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MessageServiceClient> _logger;

    public MessageServiceClient(HttpClient httpClient, ILogger<MessageServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendNotificationToAdminsAsync(
        string title,
        string message,
        string type,
        string? relatedId = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📤 向管理员发送通知: Title={Title}, Type={Type}", title, type);

        try
        {
            var payload = new
            {
                title,
                message,
                type,
                relatedId,
                metadata
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/notifications/admins", payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ 管理员通知发送成功");
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("⚠️ 管理员通知发送失败: StatusCode={StatusCode}, Body={Body}",
                    response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 调用 MessageService 通知 API 异常");
            // 不抛出异常，通知失败不影响主流程
        }
    }
}

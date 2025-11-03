using AIService.Application.DTOs;

namespace AIService.API.Models;

/// <summary>
/// 数字游民指南生成任务消息
/// </summary>
public class DigitalNomadGuideTaskMessage
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// 生成请求
    /// </summary>
    public required GenerateTravelGuideRequest Request { get; set; }

    /// <summary>
    /// SignalR 连接ID (用于推送通知)
    /// </summary>
    public string? ConnectionId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

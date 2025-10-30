using AIService.Application.DTOs;

namespace AIService.API.Models;

/// <summary>
/// 旅行计划生成任务消息
/// </summary>
public class TravelPlanTaskMessage
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
    public required GenerateTravelPlanRequest Request { get; set; }

    /// <summary>
    /// SignalR 连接ID (用于推送通知)
    /// </summary>
    public string? ConnectionId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 任务状态
/// </summary>
public class TaskStatus
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// 状态: queued, processing, completed, failed
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// 生成的旅行计划ID (状态为completed时)
    /// </summary>
    public string? PlanId { get; set; }

    /// <summary>
    /// 错误信息 (状态为failed时)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 进度消息
    /// </summary>
    public string? ProgressMessage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 创建任务响应
/// </summary>
public class CreateTaskResponse
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// 预计完成时间(秒)
    /// </summary>
    public int EstimatedTimeSeconds { get; set; } = 120;

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = "任务已创建,正在队列中等待处理";
}

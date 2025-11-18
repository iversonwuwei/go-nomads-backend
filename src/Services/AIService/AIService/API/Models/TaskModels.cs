namespace AIService.API.Models;

/// <summary>
///     任务创建响应
/// </summary>
public class CreateTaskResponse
{
    /// <summary>
    ///     任务ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    ///     任务状态
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    ///     预计处理时间(秒)
    /// </summary>
    public int EstimatedTimeSeconds { get; set; }

    /// <summary>
    ///     提示消息
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
///     任务状态
/// </summary>
public class TaskStatus
{
    /// <summary>
    ///     任务ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    ///     状态: queued, processing, completed, failed
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    ///     进度 (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    ///     进度消息
    /// </summary>
    public string? ProgressMessage { get; set; }

    /// <summary>
    ///     旅行计划ID (仅在 completed 状态时有值)
    /// </summary>
    public string? PlanId { get; set; }

    /// <summary>
    ///     指南ID (仅在 completed 状态时有值)
    /// </summary>
    public string? GuideId { get; set; }

    /// <summary>
    ///     结果数据 (仅在 completed 状态时有值)
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    ///     错误消息 (仅在 failed 状态时有值)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    ///     完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
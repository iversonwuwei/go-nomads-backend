namespace Shared.Messages;

/// <summary>
/// AI 任务进度消息（用于实时进度推送）
/// </summary>
public class AIProgressMessage
{
    /// <summary>
    /// 任务 ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public required int Progress { get; set; }

    /// <summary>
    /// 进度消息
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// 任务类型：travel-plan, digital-nomad-guide
    /// </summary>
    public required string TaskType { get; set; }

    /// <summary>
    /// 当前阶段
    /// </summary>
    public string? CurrentStage { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

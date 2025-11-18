namespace Shared.Messages;

/// <summary>
///     AI 任务失败消息
/// </summary>
public class AITaskFailedMessage
{
    /// <summary>
    ///     任务 ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    ///     用户 ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    ///     任务类型：travel-plan, digital-nomad-guide
    /// </summary>
    public required string TaskType { get; set; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public required string ErrorMessage { get; set; }

    /// <summary>
    ///     错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    ///     堆栈跟踪（仅开发环境）
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    ///     失败时间
    /// </summary>
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}
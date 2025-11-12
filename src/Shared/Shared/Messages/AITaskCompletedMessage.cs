namespace Shared.Messages;

/// <summary>
/// AI 任务完成消息
/// </summary>
public class AITaskCompletedMessage
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
    /// 任务类型：travel-plan, digital-nomad-guide
    /// </summary>
    public required string TaskType { get; set; }

    /// <summary>
    /// 结果 ID（PlanId 或 GuideId）
    /// </summary>
    public required string ResultId { get; set; }

    /// <summary>
    /// 结果数据（JSON 序列化）
    /// </summary>
    public required object Result { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 总耗时（秒）
    /// </summary>
    public int DurationSeconds { get; set; }
}

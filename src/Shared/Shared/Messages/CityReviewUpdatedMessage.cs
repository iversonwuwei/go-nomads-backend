namespace Shared.Messages;

/// <summary>
///     城市评论更新消息
///     当用户创建或删除评论时发布此消息
///     MessageService 订阅此消息并通过 SignalR 广播给客户端
/// </summary>
public class CityReviewUpdatedMessage
{
    /// <summary>
    ///     城市 ID
    /// </summary>
    public required string CityId { get; set; }

    /// <summary>
    ///     城市名称（中文）
    /// </summary>
    public string? CityName { get; set; }

    /// <summary>
    ///     城市名称（英文）
    /// </summary>
    public string? CityNameEn { get; set; }

    /// <summary>
    ///     评论 ID（创建或删除的评论）
    /// </summary>
    public string? ReviewId { get; set; }

    /// <summary>
    ///     变更类型: created, updated, deleted
    /// </summary>
    public required string ChangeType { get; set; }

    /// <summary>
    ///     新的综合评分（基于评论的平均评分）
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    ///     评论总数
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    ///     触发更新的用户 ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    ///     更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

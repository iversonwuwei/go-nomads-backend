namespace Shared.Messages;

/// <summary>
///     城市评分更新消息
///     当用户提交或更新城市评分时发布此消息
///     MessageService 订阅此消息并通过 SignalR 广播给客户端
/// </summary>
public class CityRatingUpdatedMessage
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
    ///     新的综合评分
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    ///     评论/评分总数
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    ///     触发更新的用户 ID（可选）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    ///     更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

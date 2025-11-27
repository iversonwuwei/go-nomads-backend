namespace Shared.Messages;

/// <summary>
///     城市图片生成完成消息
/// </summary>
public class CityImageGeneratedMessage
{
    /// <summary>
    ///     任务 ID
    /// </summary>
    public required string TaskId { get; set; }

    /// <summary>
    ///     城市 ID
    /// </summary>
    public required string CityId { get; set; }

    /// <summary>
    ///     城市名称
    /// </summary>
    public required string CityName { get; set; }

    /// <summary>
    ///     用户 ID (发起请求的用户)
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    ///     竖屏图片 URL
    /// </summary>
    public string? PortraitImageUrl { get; set; }

    /// <summary>
    ///     横屏图片 URL 列表
    /// </summary>
    public List<string>? LandscapeImageUrls { get; set; }

    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     完成时间
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     总耗时（秒）
    /// </summary>
    public int DurationSeconds { get; set; }
}

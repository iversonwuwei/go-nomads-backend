namespace Shared.Messages;

/// <summary>
/// 城市删除消息
/// </summary>
public class CityDeletedMessage
{
    /// <summary>
    /// 城市 ID
    /// </summary>
    public Guid CityId { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 共享办公空间更新消息
/// </summary>
public class CoworkingUpdatedMessage
{
    /// <summary>
    /// 共享办公空间 ID
    /// </summary>
    public Guid CoworkingId { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 城市ID
    /// </summary>
    public Guid? CityId { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 共享办公空间删除消息
/// </summary>
public class CoworkingDeletedMessage
{
    /// <summary>
    /// 共享办公空间 ID
    /// </summary>
    public Guid CoworkingId { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}

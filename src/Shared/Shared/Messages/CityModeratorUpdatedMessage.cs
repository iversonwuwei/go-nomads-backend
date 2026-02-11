namespace Shared.Messages;

/// <summary>
///     城市版主变更消息
///     当版主被指派、申请被批准或版主被撤销时发布此消息
///     MessageService 订阅此消息并通过 SignalR 广播给客户端
/// </summary>
public class CityModeratorUpdatedMessage
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
    ///     变更类型: assigned（指派）, approved（申请通过）, revoked（撤销）
    /// </summary>
    public required string ChangeType { get; set; }

    /// <summary>
    ///     相关用户 ID（被指派/批准/撤销的版主）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    ///     更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

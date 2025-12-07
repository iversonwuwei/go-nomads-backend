namespace Shared.Messages;

/// <summary>
///     Coworking 验证人数变化消息（用于实时验证人数推送）
/// </summary>
public class CoworkingVerificationVotesMessage
{
    /// <summary>
    ///     Coworking 空间 ID
    /// </summary>
    public required string CoworkingId { get; set; }

    /// <summary>
    ///     当前验证人数
    /// </summary>
    public int VerificationVotes { get; set; }

    /// <summary>
    ///     是否已认证
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    ///     消息时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

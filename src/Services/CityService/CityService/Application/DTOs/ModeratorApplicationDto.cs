namespace CityService.Application.DTOs;

/// <summary>
///     版主申请请求 DTO
/// </summary>
public class ApplyModeratorRequest
{
    /// <summary>
    ///     申请的城市ID
    /// </summary>
    public Guid CityId { get; set; }

    /// <summary>
    ///     申请原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
///     处理申请请求 DTO
/// </summary>
public class HandleModeratorApplicationRequest
{
    /// <summary>
    ///     申请ID
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    ///     操作: approve 或 reject
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     拒绝原因（如果 action 是 reject）
    /// </summary>
    public string? RejectionReason { get; set; }
}

/// <summary>
///     版主申请响应 DTO
/// </summary>
public class ModeratorApplicationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserAvatar { get; set; } = string.Empty;
    public Guid CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ProcessedBy { get; set; }
    public string? ProcessedByName { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

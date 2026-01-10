namespace CityService.Application.DTOs;

/// <summary>
///     版主转让请求 DTO
/// </summary>
public class InitiateModeratorTransferRequest
{
    /// <summary>
    ///     城市ID
    /// </summary>
    public Guid CityId { get; set; }

    /// <summary>
    ///     接收转让的目标用户ID
    /// </summary>
    public Guid ToUserId { get; set; }

    /// <summary>
    ///     转让说明/消息（可选）
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
///     响应转让请求 DTO
/// </summary>
public class RespondToTransferRequest
{
    /// <summary>
    ///     转让请求ID
    /// </summary>
    public Guid TransferId { get; set; }

    /// <summary>
    ///     操作: accept 或 reject
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     回复消息（可选）
    /// </summary>
    public string? ResponseMessage { get; set; }
}

/// <summary>
///     版主转让响应 DTO
/// </summary>
public class ModeratorTransferResponse
{
    public Guid Id { get; set; }
    public Guid CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public Guid FromUserId { get; set; }
    public string FromUserName { get; set; } = string.Empty;
    public string FromUserAvatar { get; set; } = string.Empty;
    public Guid ToUserId { get; set; }
    public string ToUserName { get; set; } = string.Empty;
    public string ToUserAvatar { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ResponseMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

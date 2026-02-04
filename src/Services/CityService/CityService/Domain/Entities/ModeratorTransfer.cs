using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     版主转让实体
///     用于现任版主将版主权限转让给另一个用户
/// </summary>
[Table("moderator_transfers")]
public class ModeratorTransfer : BaseModel
{
    /// <summary>
    ///     转让ID
    /// </summary>
    [PrimaryKey("id")]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    ///     城市ID
    /// </summary>
    [Column("city_id")]
    public Guid CityId { get; set; }

    /// <summary>
    ///     发起转让的版主用户ID
    /// </summary>
    [Column("from_user_id")]
    public Guid FromUserId { get; set; }

    /// <summary>
    ///     接收转让的目标用户ID
    /// </summary>
    [Column("to_user_id")]
    public Guid ToUserId { get; set; }

    /// <summary>
    ///     转让状态: pending, accepted, rejected, cancelled, expired
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    ///     转让说明/消息
    /// </summary>
    [Column("message")]
    public string? Message { get; set; }

    /// <summary>
    ///     接收方的回复消息
    /// </summary>
    [Column("response_message")]
    public string? ResponseMessage { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     响应时间
    /// </summary>
    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    ///     过期时间（默认7天后过期）
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    /// <summary>
    ///     更新时间
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     创建转让请求 - 工厂方法
    /// </summary>
    public static ModeratorTransfer Create(Guid fromUserId, Guid toUserId, Guid cityId, string? message = null)
    {
        return new ModeratorTransfer
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            CityId = cityId,
            Message = message,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     接受转让
    /// </summary>
    public void Accept(string? responseMessage = null)
    {
        Status = "accepted";
        ResponseMessage = responseMessage;
        RespondedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     拒绝转让
    /// </summary>
    public void Reject(string? responseMessage = null)
    {
        Status = "rejected";
        ResponseMessage = responseMessage;
        RespondedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     取消转让
    /// </summary>
    public void Cancel()
    {
        Status = "cancelled";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     检查是否已过期
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    ///     检查是否可以响应（待处理且未过期）
    /// </summary>
    public bool CanRespond => Status == "pending" && !IsExpired;
}

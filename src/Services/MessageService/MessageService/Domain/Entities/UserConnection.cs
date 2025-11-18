namespace MessageService.Domain.Entities;

/// <summary>
///     用户 SignalR 连接信息
/// </summary>
public class UserConnection
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string HubName { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public bool IsActive { get; set; }
}
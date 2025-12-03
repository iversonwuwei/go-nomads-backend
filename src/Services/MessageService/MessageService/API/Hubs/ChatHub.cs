using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using AppMessageAttachmentDto = MessageService.Application.Services.MessageAttachmentDto;

namespace MessageService.API.Hubs;

/// <summary>
///     聊天室 Hub - 处理实时聊天通信
/// </summary>
[AllowAnonymous]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IChatService _chatService;

    // 存储用户连接信息
    private static readonly Dictionary<string, UserConnectionInfo> _connections = new();
    private static readonly object _lock = new();

    public ChatHub(ILogger<ChatHub> logger, IChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
    }

    #region 连接生命周期

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端连接到 ChatHub, ConnectionId: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // 获取用户信息并移除
        UserConnectionInfo? info = null;
        List<string> joinedRooms = new();
        
        lock (_lock)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out info))
            {
                joinedRooms = info.JoinedRooms.ToList();
                _connections.Remove(Context.ConnectionId);
            }
        }

        // 在 lock 外部进行异步操作
        if (info != null)
        {
            foreach (var roomId in joinedRooms)
            {
                await Clients.Group(roomId).SendAsync("UserLeft", new
                {
                    UserId = info.UserId,
                    UserName = info.UserName,
                    RoomId = roomId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        _logger.LogInformation("客户端断开 ChatHub, ConnectionId: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region 用户认证

    /// <summary>
    ///     用户认证并初始化连接
    /// </summary>
    public async Task Authenticate(string userId, string userName, string? userAvatar)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Authenticate 失败：userId 为空, ConnectionId: {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("AuthenticateFailed", "用户ID不能为空");
            return;
        }

        lock (_lock)
        {
            _connections[Context.ConnectionId] = new UserConnectionInfo
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                UserName = userName,
                UserAvatar = userAvatar,
                ConnectedAt = DateTime.UtcNow,
                JoinedRooms = new HashSet<string>()
            };
        }

        // 加入用户个人频道（用于私信）
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        _logger.LogInformation("用户 {UserId} ({UserName}) 认证成功, ConnectionId: {ConnectionId}",
            userId, userName, Context.ConnectionId);

        await Clients.Caller.SendAsync("Authenticated", new
        {
            Success = true,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region 聊天室操作

    /// <summary>
    ///     加入聊天室
    /// </summary>
    public async Task JoinRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            await Clients.Caller.SendAsync("Error", "聊天室ID不能为空");
            return;
        }

        UserConnectionInfo? userInfo;
        lock (_lock)
        {
            if (!_connections.TryGetValue(Context.ConnectionId, out userInfo))
            {
                // 发送错误消息在 lock 外部进行
            }
            else
            {
                userInfo.JoinedRooms.Add(roomId);
            }
        }

        if (userInfo == null)
        {
            await Clients.Caller.SendAsync("Error", "请先进行身份认证");
            return;
        }

        // 添加到 SignalR 组
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // 通知其他用户有人加入
        await Clients.Group(roomId).SendAsync("UserJoined", new
        {
            UserId = userInfo.UserId,
            UserName = userInfo.UserName,
            UserAvatar = userInfo.UserAvatar,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });

        // 获取聊天室信息和在线用户
        var onlineUsers = GetOnlineUsersInRoom(roomId);

        _logger.LogInformation("用户 {UserId} 加入聊天室 {RoomId}, 当前在线: {OnlineCount}",
            userInfo.UserId, roomId, onlineUsers.Count);

        // 返回确认
        await Clients.Caller.SendAsync("JoinedRoom", new
        {
            RoomId = roomId,
            OnlineUsers = onlineUsers,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    ///     离开聊天室
    /// </summary>
    public async Task LeaveRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            return;
        }

        UserConnectionInfo? userInfo;
        lock (_lock)
        {
            if (!_connections.TryGetValue(Context.ConnectionId, out userInfo))
            {
                return;
            }
            userInfo.JoinedRooms.Remove(roomId);
        }

        // 从 SignalR 组移除
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

        // 通知其他用户
        await Clients.Group(roomId).SendAsync("UserLeft", new
        {
            UserId = userInfo.UserId,
            UserName = userInfo.UserName,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });

        _logger.LogInformation("用户 {UserId} 离开聊天室 {RoomId}", userInfo.UserId, roomId);

        await Clients.Caller.SendAsync("LeftRoom", new
        {
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region 消息操作

    /// <summary>
    ///     发送消息
    /// </summary>
    public async Task SendMessage(SendMessageRequest request)
    {
        if (string.IsNullOrEmpty(request.RoomId) || string.IsNullOrEmpty(request.Message))
        {
            await Clients.Caller.SendAsync("Error", "消息内容不能为空");
            return;
        }

        UserConnectionInfo? userInfo;
        lock (_lock)
        {
            if (!_connections.TryGetValue(Context.ConnectionId, out userInfo))
            {
                // 在 lock 外部处理
            }
        }

        if (userInfo == null)
        {
            await Clients.Caller.SendAsync("Error", "请先进行身份认证");
            return;
        }

        try
        {
            // 转换附件 DTO
            AppMessageAttachmentDto? attachmentDto = null;
            if (request.Attachment != null)
            {
                attachmentDto = new AppMessageAttachmentDto
                {
                    Url = request.Attachment.Url,
                    FileName = request.Attachment.FileName,
                    FileSize = request.Attachment.FileSize,
                    MimeType = request.Attachment.MimeType,
                    Latitude = request.Attachment.Latitude,
                    Longitude = request.Attachment.Longitude,
                    LocationName = request.Attachment.LocationName,
                    Duration = request.Attachment.Duration,
                    Width = request.Attachment.Width,
                    Height = request.Attachment.Height
                };
            }

            // 保存消息到数据库
            var savedMessage = await _chatService.SaveMessageAsync(new SaveMessageDto
            {
                RoomId = request.RoomId,
                UserId = userInfo.UserId,
                UserName = userInfo.UserName,
                UserAvatar = userInfo.UserAvatar,
                Message = request.Message,
                MessageType = request.MessageType ?? "text",
                ReplyToId = request.ReplyToId,
                Mentions = request.Mentions,
                Attachment = attachmentDto
            });

            // 构建消息响应
            var messageResponse = new
            {
                Id = savedMessage.Id,
                RoomId = request.RoomId,
                Author = new
                {
                    UserId = userInfo.UserId,
                    UserName = userInfo.UserName,
                    UserAvatar = userInfo.UserAvatar
                },
                Message = request.Message,
                MessageType = request.MessageType ?? "text",
                ReplyTo = savedMessage.ReplyTo,
                Mentions = request.Mentions ?? new List<string>(),
                Attachment = request.Attachment,
                Timestamp = savedMessage.Timestamp
            };

            // 广播消息到聊天室
            await Clients.Group(request.RoomId).SendAsync("NewMessage", messageResponse);

            _logger.LogInformation("用户 {UserId} 在聊天室 {RoomId} 发送消息", 
                userInfo.UserId, request.RoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存消息失败");
            await Clients.Caller.SendAsync("Error", "发送消息失败，请重试");
        }
    }

    /// <summary>
    ///     删除消息
    /// </summary>
    public async Task DeleteMessage(string roomId, string messageId)
    {
        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(messageId))
        {
            return;
        }

        UserConnectionInfo? userInfo;
        lock (_lock)
        {
            if (!_connections.TryGetValue(Context.ConnectionId, out userInfo))
            {
                // 在 lock 外部处理
            }
        }

        if (userInfo == null)
        {
            await Clients.Caller.SendAsync("Error", "请先进行身份认证");
            return;
        }

        try
        {
            // 从数据库删除消息
            var success = await _chatService.DeleteMessageAsync(messageId, userInfo.UserId);

            if (success)
            {
                // 广播删除事件
                await Clients.Group(roomId).SendAsync("MessageDeleted", new
                {
                    RoomId = roomId,
                    MessageId = messageId,
                    DeletedBy = userInfo.UserId,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "删除消息失败，您可能没有权限");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除消息失败");
            await Clients.Caller.SendAsync("Error", "删除消息失败，请重试");
        }
    }

    /// <summary>
    ///     发送正在输入状态
    /// </summary>
    public async Task SendTyping(string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            return;
        }

        UserConnectionInfo? userInfo;
        lock (_lock)
        {
            if (!_connections.TryGetValue(Context.ConnectionId, out userInfo))
            {
                return;
            }
        }

        // 通知其他用户（排除自己）
        await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync("UserTyping", new
        {
            UserId = userInfo.UserId,
            UserName = userInfo.UserName,
            RoomId = roomId
        });
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     获取聊天室在线用户列表
    /// </summary>
    private List<OnlineUserDto> GetOnlineUsersInRoom(string roomId)
    {
        lock (_lock)
        {
            return _connections.Values
                .Where(c => c.JoinedRooms.Contains(roomId))
                .Select(c => new OnlineUserDto
                {
                    UserId = c.UserId,
                    UserName = c.UserName,
                    UserAvatar = c.UserAvatar,
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow
                })
                .ToList();
        }
    }

    /// <summary>
    ///     获取聊天室在线人数
    /// </summary>
    public Task<int> GetOnlineCount(string roomId)
    {
        lock (_lock)
        {
            var count = _connections.Values.Count(c => c.JoinedRooms.Contains(roomId));
            return Task.FromResult(count);
        }
    }

    #endregion
}

#region DTOs

/// <summary>
///     用户连接信息
/// </summary>
public class UserConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public DateTime ConnectedAt { get; set; }
    public HashSet<string> JoinedRooms { get; set; } = new();
}

/// <summary>
///     发送消息请求
/// </summary>
public class SendMessageRequest
{
    public string RoomId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MessageType { get; set; } = "text"; // text, image, file, location, voice, video
    public string? ReplyToId { get; set; }
    public List<string>? Mentions { get; set; }
    public MessageAttachmentDto? Attachment { get; set; }
}

/// <summary>
///     消息附件
/// </summary>
public class MessageAttachmentDto
{
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }
    public int? Duration { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

/// <summary>
///     在线用户 DTO
/// </summary>
public class OnlineUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
}

#endregion

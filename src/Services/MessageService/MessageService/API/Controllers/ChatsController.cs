using GoNomads.Shared.Middleware;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

/// <summary>
///     聊天室 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/chats")]
[AllowAnonymous]
public class ChatsController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatsController> _logger;

    public ChatsController(IChatService chatService, ILogger<ChatsController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    #region 私有辅助方法

    /// <summary>
    ///     从 UserContext 中获取当前用户ID
    /// </summary>
    private string? GetCurrentUserId()
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
        {
            return userContext.UserId;
        }
        return null;
    }

    /// <summary>
    ///     从 UserContext 中获取当前用户 Email（作为用户名）
    /// </summary>
    private string? GetCurrentUserEmail()
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        return userContext?.Email;
    }

    #endregion

    #region 聊天室管理

    /// <summary>
    ///     获取公开聊天室列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPublicRooms([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var rooms = await _chatService.GetPublicRoomsAsync(page, pageSize);
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取公开聊天室列表失败");
            return StatusCode(500, new { error = "获取聊天室列表失败" });
        }
    }

    /// <summary>
    ///     获取聊天室详情
    /// </summary>
    [HttpGet("{roomId}")]
    public async Task<IActionResult> GetRoomById(string roomId)
    {
        try
        {
            // 如果是 meetup_ 格式的 roomId，尝试获取或创建聊天室
            if (roomId.StartsWith("meetup_"))
            {
                var meetupIdStr = roomId.Substring(7); // 移除 "meetup_" 前缀
                if (Guid.TryParse(meetupIdStr, out var meetupId))
                {
                    var meetupRoom = await _chatService.GetOrCreateMeetupRoomAsync(meetupId, "Meetup Chat", null);
                    return Ok(meetupRoom);
                }
            }
            
            var room = await _chatService.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                return NotFound(new { error = "聊天室不存在" });
            }
            return Ok(room);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天室详情失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "获取聊天室详情失败" });
        }
    }

    /// <summary>
    ///     获取或创建 Meetup 聊天室
    /// </summary>
    [HttpPost("meetup")]
    public async Task<IActionResult> GetOrCreateMeetupRoom([FromBody] CreateMeetupRoomRequest request)
    {
        try
        {
            if (!Guid.TryParse(request.MeetupId, out var meetupGuid))
            {
                return BadRequest(new { error = "无效的 MeetupId" });
            }

            // 优先从 UserContext 获取组织者信息（Gateway 传递）
            var organizerId = GetCurrentUserId();
            var organizerName = GetCurrentUserEmail() ?? request.OrganizerName;
            var organizerAvatar = request.OrganizerAvatar;
            
            // 如果 UserContext 中没有用户信息，尝试从请求体获取
            if (string.IsNullOrEmpty(organizerId) && !string.IsNullOrEmpty(request.OrganizerId))
            {
                organizerId = request.OrganizerId;
            }
            if (!string.IsNullOrEmpty(request.OrganizerName))
            {
                organizerName = request.OrganizerName;
            }
            
            _logger.LogInformation("GetOrCreateMeetupRoom: MeetupId={MeetupId}, OrganizerId={OrganizerId}", 
                request.MeetupId, organizerId);

            var room = await _chatService.GetOrCreateMeetupRoomAsync(
                meetupGuid, 
                request.MeetupTitle, 
                request.MeetupType,
                organizerId,
                organizerName,
                organizerAvatar);

            return Ok(room);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Meetup 聊天室失败: MeetupId={MeetupId}", request.MeetupId);
            return StatusCode(500, new { error = "创建聊天室失败" });
        }
    }

    /// <summary>
    ///     获取用户加入的聊天室列表
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserRooms(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var rooms = await _chatService.GetUserRoomsAsync(userId, page, pageSize);
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户聊天室失败: UserId={UserId}", userId);
            return StatusCode(500, new { error = "获取聊天室列表失败" });
        }
    }

    /// <summary>
    ///     加入聊天室
    /// </summary>
    [HttpPost("{roomId}/join")]
    public async Task<IActionResult> JoinRoom(string roomId, [FromBody] JoinRoomRequest? request)
    {
        try
        {
            // 优先从 UserContext 获取用户信息（Gateway 传递）
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserEmail() ?? "User";
            var userAvatar = request?.UserAvatar;
            
            // 如果 UserContext 中没有用户信息，尝试从请求体获取
            if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(request?.UserId))
            {
                userId = request.UserId;
            }
            if (!string.IsNullOrEmpty(request?.UserName))
            {
                userName = request.UserName;
            }
            
            _logger.LogInformation("JoinRoom 请求: RoomId={RoomId}, UserId={UserId}, UserName={UserName}", 
                roomId, userId, userName);
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "用户ID不能为空，请确保已登录" });
            }
            
            // 处理 meetup_ 格式的 roomId
            var actualRoomId = roomId;
            if (roomId.StartsWith("meetup_"))
            {
                var meetupIdStr = roomId.Substring(7);
                if (Guid.TryParse(meetupIdStr, out var meetupId))
                {
                    // 确保 meetup 聊天室存在
                    var meetupRoom = await _chatService.GetOrCreateMeetupRoomAsync(meetupId, "Meetup Chat", null);
                    actualRoomId = meetupRoom.Id;
                    _logger.LogInformation("Meetup 聊天室已获取/创建: MeetupId={MeetupId}, RoomId={RoomId}", 
                        meetupId, actualRoomId);
                }
            }
            
            var success = await _chatService.JoinRoomAsync(
                actualRoomId, 
                userId, 
                userName, 
                userAvatar);

            if (success)
            {
                return Ok(new { message = "加入成功", roomId = actualRoomId, userId, userName });
            }
            return BadRequest(new { error = "加入失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加入聊天室失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "加入聊天室失败" });
        }
    }

    /// <summary>
    ///     离开聊天室
    /// </summary>
    [HttpPost("{roomId}/leave")]
    public async Task<IActionResult> LeaveRoom(string roomId, [FromBody] LeaveRoomRequest? request)
    {
        try
        {
            // 优先从 UserContext 获取用户信息
            var userId = GetCurrentUserId();
            
            // 如果 UserContext 中没有，尝试从请求体获取
            if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(request?.UserId))
            {
                userId = request.UserId;
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "用户ID不能为空，请确保已登录" });
            }
            
            var success = await _chatService.LeaveRoomAsync(roomId, userId);

            if (success)
            {
                return Ok(new { message = "离开成功" });
            }
            return BadRequest(new { error = "离开失败" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "离开聊天室失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "离开聊天室失败" });
        }
    }

    #endregion

    #region 消息管理

    /// <summary>
    ///     获取聊天室消息列表
    /// </summary>
    [HttpGet("{roomId}/messages")]
    public async Task<IActionResult> GetMessages(string roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            // 处理 meetup_ 格式的 roomId
            var actualRoomId = roomId;
            if (roomId.StartsWith("meetup_"))
            {
                var meetupIdStr = roomId.Substring(7);
                if (Guid.TryParse(meetupIdStr, out var meetupId))
                {
                    var meetupRoom = await _chatService.GetOrCreateMeetupRoomAsync(meetupId, "Meetup Chat", null);
                    actualRoomId = meetupRoom.Id;
                }
            }
            
            var messages = await _chatService.GetMessagesAsync(actualRoomId, page, pageSize);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取消息列表失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "获取消息列表失败" });
        }
    }

    /// <summary>
    ///     发送消息（REST API 方式，建议使用 SignalR）
    /// </summary>
    [HttpPost("{roomId}/messages")]
    public async Task<IActionResult> SendMessage(string roomId, [FromBody] SendMessageApiRequest request)
    {
        try
        {
            // 处理 meetup_ 格式的 roomId，获取实际的数据库 roomId
            var actualRoomId = roomId;
            if (roomId.StartsWith("meetup_"))
            {
                var meetupIdStr = roomId.Substring(7);
                if (Guid.TryParse(meetupIdStr, out var meetupId))
                {
                    var meetupRoom = await _chatService.GetOrCreateMeetupRoomAsync(meetupId, "Meetup Chat", null);
                    actualRoomId = meetupRoom.Id;
                }
            }

            var savedMessage = await _chatService.SaveMessageAsync(new SaveMessageDto
            {
                RoomId = actualRoomId,
                UserId = request.UserId,
                UserName = request.UserName,
                UserAvatar = request.UserAvatar,
                Message = request.Message,
                MessageType = request.MessageType ?? "text",
                ReplyToId = request.ReplyToId,
                Mentions = request.Mentions
            });

            return Ok(savedMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "发送消息失败" });
        }
    }

    /// <summary>
    ///     删除消息
    /// </summary>
    [HttpDelete("{roomId}/messages/{messageId}")]
    public async Task<IActionResult> DeleteMessage(string roomId, string messageId, [FromQuery] string userId)
    {
        try
        {
            var success = await _chatService.DeleteMessageAsync(messageId, userId);

            if (success)
            {
                return Ok(new { message = "删除成功" });
            }
            return BadRequest(new { error = "删除失败，您可能没有权限" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除消息失败: MessageId={MessageId}", messageId);
            return StatusCode(500, new { error = "删除消息失败" });
        }
    }

    #endregion

    #region 成员管理

    /// <summary>
    ///     获取聊天室成员列表
    /// </summary>
    [HttpGet("{roomId}/members")]
    public async Task<IActionResult> GetMembers(string roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var members = await _chatService.GetMembersAsync(roomId, page, pageSize);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取成员列表失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "获取成员列表失败" });
        }
    }

    /// <summary>
    ///     获取在线成员列表
    /// </summary>
    [HttpGet("{roomId}/members/online")]
    public async Task<IActionResult> GetOnlineMembers(string roomId)
    {
        try
        {
            var members = await _chatService.GetOnlineMembersAsync(roomId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取在线成员失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "获取在线成员失败" });
        }
    }

    /// <summary>
    ///     获取聊天室参与者列表（兼容前端 API）
    /// </summary>
    [HttpGet("{roomId}/participants")]
    public async Task<IActionResult> GetParticipants(string roomId, [FromQuery] bool onlineOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("GetParticipants 请求: RoomId={RoomId}, OnlineOnly={OnlineOnly}", roomId, onlineOnly);
            
            // 处理 meetup_ 格式的 roomId
            var actualRoomId = roomId;
            if (roomId.StartsWith("meetup_"))
            {
                var meetupIdStr = roomId.Substring(7);
                if (Guid.TryParse(meetupIdStr, out var meetupId))
                {
                    var meetupRoom = await _chatService.GetOrCreateMeetupRoomAsync(meetupId, "Meetup Chat", null);
                    actualRoomId = meetupRoom.Id;
                    _logger.LogInformation("Meetup 聊天室已解析: MeetupId={MeetupId}, ActualRoomId={ActualRoomId}", 
                        meetupId, actualRoomId);
                }
            }
            
            if (onlineOnly)
            {
                var onlineMembers = await _chatService.GetOnlineMembersAsync(actualRoomId);
                _logger.LogInformation("返回在线成员: Count={Count}", onlineMembers?.Count() ?? 0);
                return Ok(onlineMembers);
            }
            else
            {
                var members = await _chatService.GetMembersAsync(actualRoomId, page, pageSize);
                _logger.LogInformation("返回所有成员: Count={Count}", members?.Count() ?? 0);
                return Ok(members);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取参与者列表失败: RoomId={RoomId}", roomId);
            return StatusCode(500, new { error = "获取参与者列表失败" });
        }
    }

    #endregion
}

#region Request DTOs

public class CreateMeetupRoomRequest
{
    public string MeetupId { get; set; } = string.Empty;
    public string MeetupTitle { get; set; } = string.Empty;
    public string? MeetupType { get; set; }
    public string? OrganizerId { get; set; }
    public string? OrganizerName { get; set; }
    public string? OrganizerAvatar { get; set; }
}

public class JoinRoomRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
}

public class LeaveRoomRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class SendMessageApiRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageType { get; set; }
    public string? ReplyToId { get; set; }
    public List<string>? Mentions { get; set; }
}

#endregion

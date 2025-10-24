using Microsoft.AspNetCore.Mvc;
using EventService.Application.Services;
using EventService.Application.DTOs;
using GoNomads.Shared.Middleware;

namespace EventService.API.Controllers;

/// <summary>
/// Events API - RESTful endpoints for event management
/// </summary>
[ApiController]
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// 创建 Event
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var organizerId = Guid.Parse(userContext.UserId);
            var response = await _eventService.CreateEventAsync(request, organizerId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功创建 Event {EventId}", organizerId, response.Id);
            return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取 Event 详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息（可选，用于判断关注/参与状态）
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            Guid? userId = null;
            
            if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
            {
                userId = Guid.Parse(userContext.UserId);
            }

            var response = await _eventService.GetEventAsync(id, userId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取 Event 列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] Guid? cityId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = "upcoming",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var (events, total) = await _eventService.GetEventsAsync(cityId, category, status, page, pageSize);
            return Ok(new { data = events, page, pageSize, total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Event 列表失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新 Event
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.UpdateEventAsync(id, request, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功更新 Event {EventId}", userId, id);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 参加 Event
    /// </summary>
    [HttpPost("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinEvent(Guid id, [FromBody] JoinEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.JoinEventAsync(id, userId, request);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功参加 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "成功加入 Event", participant = response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "参加 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 取消参加 Event
    /// </summary>
    [HttpDelete("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.LeaveEventAsync(id, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功取消参加 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "已取消参加" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消参加 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 关注 Event
    /// </summary>
    [HttpPost("{id}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FollowEvent(Guid id, [FromBody] FollowEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.FollowEventAsync(id, userId, request);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功关注 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "成功关注 Event", follower = response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关注 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 取消关注 Event
    /// </summary>
    [HttpDelete("{id}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnfollowEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.UnfollowEventAsync(id, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功取消关注 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "已取消关注" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消关注 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取 Event 参与者列表
    /// </summary>
    [HttpGet("{id}/participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventParticipants(Guid id)
    {
        try
        {
            var participants = await _eventService.GetParticipantsAsync(id);
            return Ok(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取参与者列表失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取 Event 关注者列表
    /// </summary>
    [HttpGet("{id}/followers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventFollowers(Guid id)
    {
        try
        {
            var followers = await _eventService.GetFollowersAsync(id);
            return Ok(followers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取关注者列表失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户创建的 Event 列表
    /// </summary>
    [HttpGet("me/created")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyCreatedEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserCreatedEventsAsync(userId);
            
            _logger.LogInformation("✅ 获取用户 {UserId} 创建的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户创建的 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户参加的 Event 列表
    /// </summary>
    [HttpGet("me/joined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyJoinedEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserJoinedEventsAsync(userId);
            
            _logger.LogInformation("✅ 获取用户 {UserId} 参加的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户参加的 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户关注的 Event 列表
    /// </summary>
    [HttpGet("me/following")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyFollowingEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserFollowingEventsAsync(userId);
            
            _logger.LogInformation("✅ 获取用户 {UserId} 关注的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户关注的 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using EventService.Application.Services;
using EventService.Application.DTOs;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using System.Collections.Generic;
using System.Linq;

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
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var organizerId = Guid.Parse(userContext.UserId);
            var response = await _eventService.CreateEventAsync(request, organizerId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功创建 Event {EventId}", organizerId, response.Id);
            return CreatedAtAction(
                nameof(GetEvent),
                new { id = response.Id },
                new ApiResponse<EventResponse>
                {
                    Success = true,
                    Message = "Event 创建成功",
                    Data = response
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Event 失败");
            return StatusCode(500, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = "创建 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取 Event 详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> GetEvent(Guid id)
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
            return Ok(new ApiResponse<EventResponse>
            {
                Success = true,
                Message = "Event 获取成功",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Event 失败");
            return StatusCode(500, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = "获取 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取 Event 列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EventResponse>>>> GetEvents(
        [FromQuery] Guid? cityId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = "upcoming",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var (events, total) = await _eventService.GetEventsAsync(cityId, category, status, page, pageSize);
            return Ok(new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = true,
                Message = "Event 列表获取成功",
                Data = new PaginatedResponse<EventResponse>
                {
                    Items = events.ToList(),
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Event 列表失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = false,
                Message = "获取 Event 列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 更新 Event
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.UpdateEventAsync(id, request, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功更新 Event {EventId}", userId, id);
            return Ok(new ApiResponse<EventResponse>
            {
                Success = true,
                Message = "Event 更新成功",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Event 失败");
            return StatusCode(500, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = "更新 Event 失败",
                Errors = new List<string> { ex.Message }
            });
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
    public async Task<ActionResult<ApiResponse<ParticipantResponse>>> JoinEvent(Guid id, [FromBody] JoinEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<ParticipantResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.JoinEventAsync(id, userId, request);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功参加 Event {EventId}", userId, id);
            return Ok(new ApiResponse<ParticipantResponse>
            {
                Success = true,
                Message = "成功加入 Event",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ParticipantResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ParticipantResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "参加 Event 失败");
            return StatusCode(500, new ApiResponse<ParticipantResponse>
            {
                Success = false,
                Message = "参加 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 取消参加 Event
    /// </summary>
    [HttpDelete("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> LeaveEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.LeaveEventAsync(id, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功取消参加 Event {EventId}", userId, id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "已取消参加",
                Data = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消参加 Event 失败");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "取消参加 Event 失败",
                Errors = new List<string> { ex.Message }
            });
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
    public async Task<ActionResult<ApiResponse<FollowerResponse>>> FollowEvent(Guid id, [FromBody] FollowEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<FollowerResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.FollowEventAsync(id, userId, request);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功关注 Event {EventId}", userId, id);
            return Ok(new ApiResponse<FollowerResponse>
            {
                Success = true,
                Message = "成功关注 Event",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<FollowerResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<FollowerResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关注 Event 失败");
            return StatusCode(500, new ApiResponse<FollowerResponse>
            {
                Success = false,
                Message = "关注 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 取消关注 Event
    /// </summary>
    [HttpDelete("{id}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UnfollowEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.UnfollowEventAsync(id, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功取消关注 Event {EventId}", userId, id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "已取消关注",
                Data = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消关注 Event 失败");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "取消关注 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取 Event 参与者列表
    /// </summary>
    [HttpGet("{id}/participants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ParticipantResponse>>>> GetEventParticipants(Guid id)
    {
        try
        {
            var participants = await _eventService.GetParticipantsAsync(id);
            return Ok(new ApiResponse<IEnumerable<ParticipantResponse>>
            {
                Success = true,
                Message = "参与者列表获取成功",
                Data = participants
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取参与者列表失败");
            return StatusCode(500, new ApiResponse<IEnumerable<ParticipantResponse>>
            {
                Success = false,
                Message = "获取参与者列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取 Event 关注者列表
    /// </summary>
    [HttpGet("{id}/followers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerResponse>>>> GetEventFollowers(Guid id)
    {
        try
        {
            var followers = await _eventService.GetFollowersAsync(id);
            return Ok(new ApiResponse<IEnumerable<FollowerResponse>>
            {
                Success = true,
                Message = "关注者列表获取成功",
                Data = followers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取关注者列表失败");
            return StatusCode(500, new ApiResponse<IEnumerable<FollowerResponse>>
            {
                Success = false,
                Message = "获取关注者列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取当前用户创建的 Event 列表
    /// </summary>
    [HttpGet("me/created")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventResponse>>>> GetMyCreatedEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<IEnumerable<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserCreatedEventsAsync(userId);
            
            _logger.LogInformation("✅ 获取用户 {UserId} 创建的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = true,
                Message = "获取创建的 Event 列表成功",
                Data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户创建的 Event 失败");
            return StatusCode(500, new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = false,
                Message = "获取用户创建的 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取当前用户参加的 Event 列表
    /// </summary>
    [HttpGet("me/joined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventResponse>>>> GetMyJoinedEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<IEnumerable<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserJoinedEventsAsync(userId);
            
            _logger.LogInformation("✅ 获取用户 {UserId} 参加的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = true,
                Message = "获取参加的 Event 列表成功",
                Data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户参加的 Event 失败");
            return StatusCode(500, new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = false,
                Message = "获取用户参加的 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取当前用户关注的 Event 列表
    /// </summary>
    [HttpGet("me/following")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventResponse>>>> GetMyFollowingEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new ApiResponse<IEnumerable<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });
            }

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserFollowingEventsAsync(userId);
            
            _logger.LogInformation("✅ 获取用户 {UserId} 关注的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = true,
                Message = "获取关注的 Event 列表成功",
                Data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户关注的 Event 失败");
            return StatusCode(500, new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = false,
                Message = "获取用户关注的 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}

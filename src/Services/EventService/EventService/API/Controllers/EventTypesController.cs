using EventService.Application.DTOs;
using EventService.Application.Services;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.API.Controllers;

/// <summary>
///     聚会类型 API - RESTful endpoints for event types management
/// </summary>
[ApiController]
[Route("api/v1/event-types")]
public class EventTypesController : ControllerBase
{
    private readonly IEventTypeService _eventTypeService;
    private readonly ILogger<EventTypesController> _logger;

    public EventTypesController(IEventTypeService eventTypeService, ILogger<EventTypesController> logger)
    {
        _eventTypeService = eventTypeService;
        _logger = logger;
    }

    /// <summary>
    ///     获取所有启用的聚会类型
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EventTypeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<EventTypeDto>>>> GetAllActiveTypes()
    {
        try
        {
            _logger.LogInformation("📋 收到获取聚会类型列表请求");

            var types = await _eventTypeService.GetAllActiveTypesAsync();

            _logger.LogInformation("✅ 成功返回 {Count} 个聚会类型", types.Count);
            return Ok(new ApiResponse<List<EventTypeDto>>
            {
                Success = true,
                Message = "获取聚会类型列表成功",
                Data = types
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取聚会类型列表失败");
            return StatusCode(500, new ApiResponse<List<EventTypeDto>>
            {
                Success = false,
                Message = "获取聚会类型列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取所有聚会类型（包括禁用的）- 仅管理员
    /// </summary>
    [HttpGet("all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<EventTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<EventTypeDto>>>> GetAllTypes()
    {
        try
        {
            _logger.LogInformation("📋 收到获取所有聚会类型请求（包括禁用的）");

            // TODO: 添加管理员权限检查
            // var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            // if (!userContext.IsAdmin) return Forbid();

            var types = await _eventTypeService.GetAllTypesAsync();

            _logger.LogInformation("✅ 成功返回 {Count} 个聚会类型", types.Count);
            return Ok(new ApiResponse<List<EventTypeDto>>
            {
                Success = true,
                Message = "获取所有聚会类型成功",
                Data = types
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取所有聚会类型失败");
            return StatusCode(500, new ApiResponse<List<EventTypeDto>>
            {
                Success = false,
                Message = "获取所有聚会类型失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     根据 ID 获取聚会类型
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventTypeDto>>> GetTypeById(Guid id)
    {
        try
        {
            _logger.LogInformation("🔍 收到获取聚会类型请求: {Id}", id);

            var type = await _eventTypeService.GetTypeByIdAsync(id);
            if (type == null)
            {
                _logger.LogWarning("⚠️ 聚会类型不存在: {Id}", id);
                return NotFound(new ApiResponse<EventTypeDto>
                {
                    Success = false,
                    Message = "聚会类型不存在",
                    Errors = new List<string> { $"聚会类型 {id} 不存在" }
                });
            }

            _logger.LogInformation("✅ 成功返回聚会类型: {Name}", type.Name);
            return Ok(new ApiResponse<EventTypeDto>
            {
                Success = true,
                Message = "获取聚会类型成功",
                Data = type
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取聚会类型失败: {Id}", id);
            return StatusCode(500, new ApiResponse<EventTypeDto>
            {
                Success = false,
                Message = "获取聚会类型失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     创建聚会类型 - 仅管理员
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EventTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EventTypeDto>>> CreateType([FromBody] CreateEventTypeRequest request)
    {
        try
        {
            _logger.LogInformation("➕ 收到创建聚会类型请求: {Name} ({EnName})", request.Name, request.EnName);

            // TODO: 添加管理员权限检查
            // var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            // if (!userContext.IsAdmin) return Forbid();

            var type = await _eventTypeService.CreateTypeAsync(request);

            _logger.LogInformation("✅ 成功创建聚会类型: {Id} - {Name}", type.Id, type.Name);
            return CreatedAtAction(
                nameof(GetTypeById),
                new { id = type.Id },
                new ApiResponse<EventTypeDto>
                {
                    Success = true,
                    Message = "创建聚会类型成功",
                    Data = type
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("⚠️ 创建聚会类型失败: {Message}", ex.Message);
            return BadRequest(new ApiResponse<EventTypeDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建聚会类型失败");
            return StatusCode(500, new ApiResponse<EventTypeDto>
            {
                Success = false,
                Message = "创建聚会类型失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     更新聚会类型 - 仅管理员
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EventTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventTypeDto>>> UpdateType(Guid id, [FromBody] UpdateEventTypeRequest request)
    {
        try
        {
            _logger.LogInformation("📝 收到更新聚会类型请求: {Id}", id);

            // TODO: 添加管理员权限检查
            // var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            // if (!userContext.IsAdmin) return Forbid();

            var type = await _eventTypeService.UpdateTypeAsync(id, request);

            _logger.LogInformation("✅ 成功更新聚会类型: {Id}", id);
            return Ok(new ApiResponse<EventTypeDto>
            {
                Success = true,
                Message = "更新聚会类型成功",
                Data = type
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("⚠️ 更新聚会类型失败: {Message}", ex.Message);
            return BadRequest(new ApiResponse<EventTypeDto>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新聚会类型失败: {Id}", id);
            return StatusCode(500, new ApiResponse<EventTypeDto>
            {
                Success = false,
                Message = "更新聚会类型失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     删除聚会类型（软删除）- 仅管理员
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteType(Guid id)
    {
        try
        {
            _logger.LogInformation("🗑️ 收到删除聚会类型请求: {Id}", id);

            // TODO: 添加管理员权限检查
            // var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            // if (!userContext.IsAdmin) return Forbid();

            await _eventTypeService.DeleteTypeAsync(id);

            _logger.LogInformation("✅ 成功删除聚会类型: {Id}", id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "删除聚会类型成功"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("⚠️ 删除聚会类型失败: {Message}", ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除聚会类型失败: {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除聚会类型失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}

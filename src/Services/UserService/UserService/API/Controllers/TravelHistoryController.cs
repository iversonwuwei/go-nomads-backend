using System.ComponentModel.DataAnnotations;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Repositories;

namespace UserService.API.Controllers;

/// <summary>
///     Travel History API - RESTful endpoints for travel history management
/// </summary>
[ApiController]
[Route("api/v1/travel-history")]
[Authorize]
public class TravelHistoryController : ControllerBase
{
    private readonly ILogger<TravelHistoryController> _logger;
    private readonly ITravelHistoryService _travelHistoryService;

    public TravelHistoryController(
        ITravelHistoryService travelHistoryService,
        ILogger<TravelHistoryController> logger)
    {
        _travelHistoryService = travelHistoryService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的旅行历史（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<TravelHistoryDto>>>> GetMyTravelHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isConfirmed = null,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<PaginatedResponse<TravelHistoryDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📋 GetMyTravelHistory - UserId: {UserId}, Page: {Page}, IsConfirmed: {IsConfirmed}",
            userContext.UserId, page, isConfirmed);

        try
        {
            var (items, total) = await _travelHistoryService.GetUserTravelHistoryAsync(
                userContext.UserId!, page, pageSize, isConfirmed, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<TravelHistoryDto>>
            {
                Success = true,
                Message = "获取旅行历史成功",
                Data = new PaginatedResponse<TravelHistoryDto>
                {
                    Items = items,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取旅行历史失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<TravelHistoryDto>>
            {
                Success = false,
                Message = "获取旅行历史失败"
            });
        }
    }

    /// <summary>
    ///     获取当前用户已确认的旅行历史（用于 profile 展示）
    /// </summary>
    [HttpGet("confirmed")]
    public async Task<ActionResult<ApiResponse<List<TravelHistoryDto>>>> GetConfirmedTravelHistory(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<List<TravelHistoryDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📋 GetConfirmedTravelHistory - UserId: {UserId}", userContext.UserId);

        try
        {
            var items = await _travelHistoryService.GetConfirmedTravelHistoryAsync(
                userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<List<TravelHistoryDto>>
            {
                Success = true,
                Message = "获取已确认旅行历史成功",
                Data = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取已确认旅行历史失败");
            return StatusCode(500, new ApiResponse<List<TravelHistoryDto>>
            {
                Success = false,
                Message = "获取已确认旅行历史失败"
            });
        }
    }

    /// <summary>
    ///     获取当前用户未确认的旅行历史（待确认的自动检测行程）
    /// </summary>
    [HttpGet("unconfirmed")]
    public async Task<ActionResult<ApiResponse<List<TravelHistoryDto>>>> GetUnconfirmedTravelHistory(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<List<TravelHistoryDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📋 GetUnconfirmedTravelHistory - UserId: {UserId}", userContext.UserId);

        try
        {
            var items = await _travelHistoryService.GetUnconfirmedTravelHistoryAsync(
                userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<List<TravelHistoryDto>>
            {
                Success = true,
                Message = "获取未确认旅行历史成功",
                Data = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取未确认旅行历史失败");
            return StatusCode(500, new ApiResponse<List<TravelHistoryDto>>
            {
                Success = false,
                Message = "获取未确认旅行历史失败"
            });
        }
    }

    /// <summary>
    ///     获取旅行历史详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TravelHistoryDto>>> GetTravelHistoryById(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("🔍 GetTravelHistoryById - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            var item = await _travelHistoryService.GetTravelHistoryByIdAsync(id, cancellationToken);

            if (item == null)
                return NotFound(new ApiResponse<TravelHistoryDto>
                {
                    Success = false,
                    Message = "旅行历史记录不存在"
                });

            // 验证所有权
            if (item.UserId != userContext.UserId)
                return Forbid();

            return Ok(new ApiResponse<TravelHistoryDto>
            {
                Success = true,
                Message = "获取旅行历史详情成功",
                Data = item
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取旅行历史详情失败: {Id}", id);
            return StatusCode(500, new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = "获取旅行历史详情失败"
            });
        }
    }

    /// <summary>
    ///     创建旅行历史记录
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TravelHistoryDto>>> CreateTravelHistory(
        [FromBody] CreateTravelHistoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📝 CreateTravelHistory - UserId: {UserId}, City: {City}", userContext.UserId, dto.City);

        try
        {
            var created = await _travelHistoryService.CreateTravelHistoryAsync(
                userContext.UserId!, dto, cancellationToken);

            return CreatedAtAction(
                nameof(GetTravelHistoryById),
                new { id = created.Id },
                new ApiResponse<TravelHistoryDto>
                {
                    Success = true,
                    Message = "创建旅行历史记录成功",
                    Data = created
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 创建旅行历史记录失败: {Message}", ex.Message);
            return BadRequest(new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建旅行历史记录失败");
            return StatusCode(500, new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = "创建旅行历史记录失败"
            });
        }
    }

    /// <summary>
    ///     批量创建旅行历史记录（用于同步自动检测的行程）
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<List<TravelHistoryDto>>>> CreateBatchTravelHistory(
        [FromBody] BatchCreateTravelHistoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<List<TravelHistoryDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📝 CreateBatchTravelHistory - UserId: {UserId}, Count: {Count}",
            userContext.UserId, dto.Items.Count);

        try
        {
            var created = await _travelHistoryService.CreateBatchTravelHistoryAsync(
                userContext.UserId!, dto, cancellationToken);

            return Ok(new ApiResponse<List<TravelHistoryDto>>
            {
                Success = true,
                Message = $"成功创建 {created.Count} 条旅行历史记录",
                Data = created
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建旅行历史记录失败");
            return StatusCode(500, new ApiResponse<List<TravelHistoryDto>>
            {
                Success = false,
                Message = "批量创建旅行历史记录失败"
            });
        }
    }

    /// <summary>
    ///     更新旅行历史记录
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TravelHistoryDto>>> UpdateTravelHistory(
        [FromRoute] string id,
        [FromBody] UpdateTravelHistoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📝 UpdateTravelHistory - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            // 验证所有权
            var existing = await _travelHistoryService.GetTravelHistoryByIdAsync(id, cancellationToken);
            if (existing == null)
                return NotFound(new ApiResponse<TravelHistoryDto>
                {
                    Success = false,
                    Message = "旅行历史记录不存在"
                });

            if (existing.UserId != userContext.UserId)
                return Forbid();

            var updated = await _travelHistoryService.UpdateTravelHistoryAsync(id, dto, cancellationToken);

            return Ok(new ApiResponse<TravelHistoryDto>
            {
                Success = true,
                Message = "更新旅行历史记录成功",
                Data = updated
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 更新旅行历史记录失败: {Message}", ex.Message);
            return BadRequest(new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新旅行历史记录失败: {Id}", id);
            return StatusCode(500, new ApiResponse<TravelHistoryDto>
            {
                Success = false,
                Message = "更新旅行历史记录失败"
            });
        }
    }

    /// <summary>
    ///     删除旅行历史记录
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTravelHistory(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<bool>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("🗑️ DeleteTravelHistory - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            // 验证所有权
            var existing = await _travelHistoryService.GetTravelHistoryByIdAsync(id, cancellationToken);
            if (existing == null)
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "旅行历史记录不存在"
                });

            if (existing.UserId != userContext.UserId)
                return Forbid();

            var result = await _travelHistoryService.DeleteTravelHistoryAsync(id, cancellationToken);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "删除旅行历史记录成功",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除旅行历史记录失败: {Id}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "删除旅行历史记录失败"
            });
        }
    }

    /// <summary>
    ///     确认旅行历史记录
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmTravelHistory(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<bool>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("✅ ConfirmTravelHistory - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            // 验证所有权
            var existing = await _travelHistoryService.GetTravelHistoryByIdAsync(id, cancellationToken);
            if (existing == null)
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "旅行历史记录不存在"
                });

            if (existing.UserId != userContext.UserId)
                return Forbid();

            var result = await _travelHistoryService.ConfirmTravelHistoryAsync(id, cancellationToken);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "确认旅行历史记录成功",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认旅行历史记录失败: {Id}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "确认旅行历史记录失败"
            });
        }
    }

    /// <summary>
    ///     批量确认旅行历史记录
    /// </summary>
    [HttpPost("confirm/batch")]
    public async Task<ActionResult<ApiResponse<int>>> ConfirmBatchTravelHistory(
        [FromBody] List<string> ids,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<int>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("✅ ConfirmBatchTravelHistory - UserId: {UserId}, Count: {Count}",
            userContext.UserId, ids.Count);

        try
        {
            // 验证所有权（简化版：只验证第一条，生产环境应验证所有）
            foreach (var id in ids)
            {
                var existing = await _travelHistoryService.GetTravelHistoryByIdAsync(id, cancellationToken);
                if (existing != null && existing.UserId != userContext.UserId)
                    return Forbid();
            }

            var count = await _travelHistoryService.ConfirmBatchTravelHistoryAsync(ids, cancellationToken);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Message = $"成功确认 {count} 条旅行历史记录",
                Data = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量确认旅行历史记录失败");
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Message = "批量确认旅行历史记录失败"
            });
        }
    }

    /// <summary>
    ///     获取当前用户的旅行统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<TravelHistoryStats>>> GetMyTravelStats(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<TravelHistoryStats>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📊 GetMyTravelStats - UserId: {UserId}", userContext.UserId);

        try
        {
            var stats = await _travelHistoryService.GetUserTravelStatsAsync(
                userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<TravelHistoryStats>
            {
                Success = true,
                Message = "获取旅行统计成功",
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取旅行统计失败");
            return StatusCode(500, new ApiResponse<TravelHistoryStats>
            {
                Success = false,
                Message = "获取旅行统计失败"
            });
        }
    }

    /// <summary>
    ///     获取指定用户的已确认旅行历史（公开接口，用于查看他人 profile）
    /// </summary>
    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<TravelHistorySummaryDto>>>> GetUserTravelHistory(
        [FromRoute] string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 GetUserTravelHistory - UserId: {UserId}", userId);

        try
        {
            var items = await _travelHistoryService.GetConfirmedTravelHistoryAsync(userId, cancellationToken);

            // 转换为简要 DTO（只包含公开信息）
            var summaries = items.Select(item => new TravelHistorySummaryDto
            {
                Id = item.Id,
                City = item.City,
                Country = item.Country,
                ArrivalTime = item.ArrivalTime,
                DepartureTime = item.DepartureTime,
                DurationDays = item.DurationDays,
                IsConfirmed = item.IsConfirmed,
                Rating = item.Rating
            }).ToList();

            return Ok(new ApiResponse<List<TravelHistorySummaryDto>>
            {
                Success = true,
                Message = "获取用户旅行历史成功",
                Data = summaries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户旅行历史失败: {UserId}", userId);
            return StatusCode(500, new ApiResponse<List<TravelHistorySummaryDto>>
            {
                Success = false,
                Message = "获取用户旅行历史失败"
            });
        }
    }
}

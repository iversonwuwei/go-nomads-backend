using CityService.Application.DTOs;
using CityService.Application.Services;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

/// <summary>
///     版主转让管理控制器
/// </summary>
[ApiController]
[Route("api/v1/cities/moderator/transfers")]
public class ModeratorTransferController : ControllerBase
{
    private readonly ILogger<ModeratorTransferController> _logger;
    private readonly IModeratorTransferService _service;

    public ModeratorTransferController(
        IModeratorTransferService service,
        ILogger<ModeratorTransferController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    ///     发起版主转让请求
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ModeratorTransferResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ModeratorTransferResponse>>> InitiateTransfer(
        [FromBody] InitiateModeratorTransferRequest request)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<ModeratorTransferResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            var result = await _service.InitiateTransferAsync(userId, request);

            return Ok(new ApiResponse<ModeratorTransferResponse>
            {
                Success = true,
                Message = "转让请求已发送，等待对方响应",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ModeratorTransferResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ModeratorTransferResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发起版主转让失败");
            return StatusCode(500, new ApiResponse<ModeratorTransferResponse>
            {
                Success = false,
                Message = "发起转让失败"
            });
        }
    }

    /// <summary>
    ///     响应转让请求（接受或拒绝）
    /// </summary>
    [HttpPost("{transferId}/respond")]
    [ProducesResponseType(typeof(ApiResponse<ModeratorTransferResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ModeratorTransferResponse>>> RespondToTransfer(
        [FromRoute] Guid transferId,
        [FromBody] RespondToTransferBodyRequest body)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<ModeratorTransferResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            var request = new RespondToTransferRequest
            {
                TransferId = transferId,
                Action = body.Action,
                ResponseMessage = body.ResponseMessage
            };

            var result = await _service.RespondToTransferAsync(userId, request);

            var message = body.Action.ToLower() == "accept"
                ? "已接受版主转让"
                : "已拒绝版主转让";

            return Ok(new ApiResponse<ModeratorTransferResponse>
            {
                Success = true,
                Message = message,
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ModeratorTransferResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ModeratorTransferResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "响应版主转让失败");
            return StatusCode(500, new ApiResponse<ModeratorTransferResponse>
            {
                Success = false,
                Message = "响应转让失败"
            });
        }
    }

    /// <summary>
    ///     取消转让请求
    /// </summary>
    [HttpPost("{transferId}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> CancelTransfer([FromRoute] Guid transferId)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            await _service.CancelTransferAsync(userId, transferId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "转让请求已取消"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消版主转让失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "取消转让失败"
            });
        }
    }

    /// <summary>
    ///     获取我发起的转让请求
    /// </summary>
    [HttpGet("initiated")]
    [ProducesResponseType(typeof(ApiResponse<List<ModeratorTransferResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ModeratorTransferResponse>>>> GetInitiatedTransfers()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<List<ModeratorTransferResponse>>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            var transfers = await _service.GetInitiatedTransfersAsync(userId);

            return Ok(new ApiResponse<List<ModeratorTransferResponse>>
            {
                Success = true,
                Message = "获取成功",
                Data = transfers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取发起的转让请求列表失败");
            return StatusCode(500, new ApiResponse<List<ModeratorTransferResponse>>
            {
                Success = false,
                Message = "获取转让列表失败"
            });
        }
    }

    /// <summary>
    ///     获取我收到的转让请求
    /// </summary>
    [HttpGet("received")]
    [ProducesResponseType(typeof(ApiResponse<List<ModeratorTransferResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ModeratorTransferResponse>>>> GetReceivedTransfers()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<List<ModeratorTransferResponse>>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            var transfers = await _service.GetReceivedTransfersAsync(userId);

            return Ok(new ApiResponse<List<ModeratorTransferResponse>>
            {
                Success = true,
                Message = "获取成功",
                Data = transfers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收到的转让请求列表失败");
            return StatusCode(500, new ApiResponse<List<ModeratorTransferResponse>>
            {
                Success = false,
                Message = "获取转让列表失败"
            });
        }
    }

    /// <summary>
    ///     获取待处理的转让请求（我收到的）
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(ApiResponse<List<ModeratorTransferResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ModeratorTransferResponse>>>> GetPendingTransfers()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<List<ModeratorTransferResponse>>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            var transfers = await _service.GetPendingTransfersAsync(userId);

            return Ok(new ApiResponse<List<ModeratorTransferResponse>>
            {
                Success = true,
                Message = "获取成功",
                Data = transfers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待处理转让请求列表失败");
            return StatusCode(500, new ApiResponse<List<ModeratorTransferResponse>>
            {
                Success = false,
                Message = "获取转让列表失败"
            });
        }
    }

    /// <summary>
    ///     获取转让请求详情
    /// </summary>
    [HttpGet("{transferId}")]
    [ProducesResponseType(typeof(ApiResponse<ModeratorTransferResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ModeratorTransferResponse>>> GetTransferById([FromRoute] Guid transferId)
    {
        try
        {
            var transfer = await _service.GetTransferByIdAsync(transferId);
            if (transfer == null)
            {
                return NotFound(new ApiResponse<ModeratorTransferResponse>
                {
                    Success = false,
                    Message = "转让请求不存在"
                });
            }

            return Ok(new ApiResponse<ModeratorTransferResponse>
            {
                Success = true,
                Message = "获取成功",
                Data = transfer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取转让请求详情失败");
            return StatusCode(500, new ApiResponse<ModeratorTransferResponse>
            {
                Success = false,
                Message = "获取转让详情失败"
            });
        }
    }
}

/// <summary>
///     响应转让请求体 DTO（不包含 transferId，因为从路由获取）
/// </summary>
public class RespondToTransferBodyRequest
{
    /// <summary>
    ///     操作: accept 或 reject
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     回复消息（可选）
    /// </summary>
    public string? ResponseMessage { get; set; }
}

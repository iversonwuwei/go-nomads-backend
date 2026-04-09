using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

/// <summary>
///     Budget Center API
/// </summary>
[ApiController]
[Route("api/v1/budgets")]
[Produces("application/json")]
public class BudgetCenterController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BudgetCenterController> _logger;
    private readonly IMigrationCentersService _migrationCentersService;

    public BudgetCenterController(
        IMigrationCentersService migrationCentersService,
        ICurrentUserService currentUserService,
        ILogger<BudgetCenterController> logger)
    {
        _migrationCentersService = migrationCentersService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的 Budget Center 聚合数据
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<BudgetCenterResponse>>> GetCurrentBudget()
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _migrationCentersService.GetBudgetCenterAsync(userId);

            return Ok(new ApiResponse<BudgetCenterResponse>
            {
                Success = true,
                Message = "Budget center 获取成功",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "获取 Budget Center 时用户未认证");
            return Unauthorized(new ApiResponse<BudgetCenterResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Budget Center 失败");
            return StatusCode(500, new ApiResponse<BudgetCenterResponse>
            {
                Success = false,
                Message = "获取 Budget Center 失败"
            });
        }
    }

    [HttpPost("plans/{planId:guid}")]
    public async Task<ActionResult<ApiResponse<BudgetCenterResponse>>> SaveBudgetPlan(
        Guid planId,
        [FromBody] SaveBudgetPlanRequest request)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _migrationCentersService.SaveBudgetPlanAsync(userId, planId, request);

            return Ok(new ApiResponse<BudgetCenterResponse>
            {
                Success = true,
                Message = "Budget baseline 保存成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<BudgetCenterResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<BudgetCenterResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "保存 Budget baseline 时用户未认证");
            return Unauthorized(new ApiResponse<BudgetCenterResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存 Budget baseline 失败");
            return StatusCode(500, new ApiResponse<BudgetCenterResponse>
            {
                Success = false,
                Message = "保存 Budget baseline 失败"
            });
        }
    }
}
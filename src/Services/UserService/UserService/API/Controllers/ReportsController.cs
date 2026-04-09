using GoNomads.Shared.Models;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ReportsController> _logger;
    private readonly IReportService _reportService;

    public ReportsController(
        IReportService reportService,
        ICurrentUserService currentUser,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<List<ReportDto>>>> GetMyReports(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        try
        {
            var reports = await _reportService.GetReportsAsync(cancellationToken);
            return Ok(new ApiResponse<List<ReportDto>>
            {
                Success = true,
                Message = "OK",
                Data = reports
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取举报列表失败");
            return StatusCode(500, new ApiResponse<List<ReportDto>>
            {
                Success = false,
                Message = "获取举报列表失败"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        try
        {
            var report = await _reportService.GetReportByIdAsync(id, cancellationToken);
            if (report == null)
                return NotFound(new ApiResponse<ReportDto>
                {
                    Success = false,
                    Message = "举报不存在"
                });

            return Ok(new ApiResponse<ReportDto>
            {
                Success = true,
                Message = "OK",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取举报详情失败: {ReportId}", id);
            return StatusCode(500, new ApiResponse<ReportDto>
            {
                Success = false,
                Message = "获取举报详情失败"
            });
        }
    }

    [HttpPost("{id}/{action}")]
    public async Task<ActionResult<ApiResponse<ReportActionResponse>>> ApplyAction(
        string id,
        string action,
        [FromBody] ReportActionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        try
        {
            var currentUserId = _currentUser.GetUserIdString();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(new ApiResponse<ReportActionResponse>
                {
                    Success = false,
                    Message = "未认证用户"
                });

            var result = await _reportService.ApplyActionAsync(id, action, currentUserId, request.Note, cancellationToken);
            return Ok(new ApiResponse<ReportActionResponse>
            {
                Success = true,
                Message = "Report action submitted",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ReportActionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ReportActionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 举报动作执行失败: {ReportId}, {Action}", id, action);
            return StatusCode(500, new ApiResponse<ReportActionResponse>
            {
                Success = false,
                Message = "举报动作执行失败"
            });
        }
    }
}
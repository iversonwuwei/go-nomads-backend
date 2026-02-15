using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

/// <summary>
///     举报 API - 用户举报 RESTful 端点
/// </summary>
[ApiController]
[Route("api/v1/reports")]
public class ReportController : ControllerBase
{
    private readonly ILogger<ReportController> _logger;
    private readonly IReportService _reportService;

    public ReportController(
        IReportService reportService,
        ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    ///     提交举报
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Create(
        [FromBody] CreateReportDto dto,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<ReportDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📝 CreateReport - UserId: {UserId}, ContentType: {ContentType}, TargetId: {TargetId}",
            userContext.UserId, dto.ContentType, dto.TargetId);

        try
        {
            var report = await _reportService.CreateReportAsync(
                userContext.UserId!,
                null,
                dto,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = report.Id }, new ApiResponse<ReportDto>
            {
                Success = true,
                Message = "举报提交成功",
                Data = report
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<ReportDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 提交举报失败");
            return StatusCode(500, new ApiResponse<ReportDto>
            {
                Success = false,
                Message = "提交举报失败"
            });
        }
    }

    /// <summary>
    ///     获取举报详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReportDto>>> GetById(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<ReportDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("🔍 GetReportById - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            var report = await _reportService.GetReportByIdAsync(id, cancellationToken);
            if (report == null)
                return NotFound(new ApiResponse<ReportDto>
                {
                    Success = false,
                    Message = "举报记录不存在"
                });

            return Ok(new ApiResponse<ReportDto>
            {
                Success = true,
                Message = "获取举报详情成功",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取举报详情失败");
            return StatusCode(500, new ApiResponse<ReportDto>
            {
                Success = false,
                Message = "获取举报详情失败"
            });
        }
    }

    /// <summary>
    ///     获取当前用户提交的举报记录
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<List<ReportDto>>>> GetMyReports(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<List<ReportDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📋 GetMyReports - UserId: {UserId}", userContext.UserId);

        try
        {
            var reports = await _reportService.GetMyReportsAsync(userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<List<ReportDto>>
            {
                Success = true,
                Message = "获取举报记录成功",
                Data = reports
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取举报记录失败");
            return StatusCode(500, new ApiResponse<List<ReportDto>>
            {
                Success = false,
                Message = "获取举报记录失败"
            });
        }
    }
}

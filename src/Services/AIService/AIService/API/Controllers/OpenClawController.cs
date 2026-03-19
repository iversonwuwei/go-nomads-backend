using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

/// <summary>
///     OpenClaw 自动化控制器
///     提供自然语言指令执行、提醒设置、签证提醒和预设自动化场景
/// </summary>
[ApiController]
[Route("api/v1/ai/openclaw")]
[Produces("application/json")]
public class OpenClawController : ControllerBase
{
    private readonly IOpenClawService _openClawService;
    private readonly ILogger<OpenClawController> _logger;

    public OpenClawController(
        IOpenClawService openClawService,
        ILogger<OpenClawController> logger)
    {
        _openClawService = openClawService;
        _logger = logger;
    }

    /// <summary>
    ///     执行自然语言指令
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<string>>> Execute(
        [FromBody] OpenClawExecuteRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.ErrorResponse("用户未认证"));

            if (string.IsNullOrWhiteSpace(request.Command))
                return BadRequest(ApiResponse<string>.ErrorResponse("指令不能为空"));

            _logger.LogInformation("用户 {UserId} 执行 OpenClaw 指令: {Command}",
                userId, request.Command);

            var result = await _openClawService.ExecuteCommandAsync(
                request.Command,
                request.SessionId);

            return Ok(ApiResponse<string>.SuccessResponse(result, "指令执行成功"));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenClaw Gateway 通信失败");
            return StatusCode(502, ApiResponse<string>.ErrorResponse("OpenClaw 服务暂不可用"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行 OpenClaw 指令失败");
            return StatusCode(500, ApiResponse<string>.ErrorResponse("指令执行失败"));
        }
    }

    /// <summary>
    ///     设置单次提醒
    /// </summary>
    [HttpPost("reminder")]
    public async Task<ActionResult<ApiResponse<string>>> SetReminder(
        [FromBody] OpenClawReminderRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.ErrorResponse("用户未认证"));

            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(ApiResponse<string>.ErrorResponse("提醒内容不能为空"));

            _logger.LogInformation("用户 {UserId} 设置提醒: {Text} @ {Time}",
                userId, request.Text, request.TriggerTime);

            var result = await _openClawService.SetReminderAsync(
                request.Text,
                request.TriggerTime);

            return Ok(ApiResponse<string>.SuccessResponse(result, "提醒设置成功"));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenClaw Gateway 通信失败");
            return StatusCode(502, ApiResponse<string>.ErrorResponse("OpenClaw 服务暂不可用"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置提醒失败");
            return StatusCode(500, ApiResponse<string>.ErrorResponse("提醒设置失败"));
        }
    }

    /// <summary>
    ///     设置签证到期提醒（自动多次提醒）
    /// </summary>
    [HttpPost("visa-reminder")]
    public async Task<ActionResult<ApiResponse<string>>> SetVisaReminder(
        [FromBody] OpenClawVisaReminderRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.ErrorResponse("用户未认证"));

            if (string.IsNullOrWhiteSpace(request.Country))
                return BadRequest(ApiResponse<string>.ErrorResponse("国家名称不能为空"));

            _logger.LogInformation("用户 {UserId} 设置签证提醒: {Country} 到期 {Date}",
                userId, request.Country, request.ExpiryDate);

            var result = await _openClawService.SetVisaReminderAsync(
                request.Country,
                request.ExpiryDate);

            return Ok(ApiResponse<string>.SuccessResponse(result, "签证提醒设置成功"));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenClaw Gateway 通信失败");
            return StatusCode(502, ApiResponse<string>.ErrorResponse("OpenClaw 服务暂不可用"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置签证提醒失败");
            return StatusCode(500, ApiResponse<string>.ErrorResponse("签证提醒设置失败"));
        }
    }

    /// <summary>
    ///     执行预设自动化场景
    /// </summary>
    [HttpPost("automation/{scenario}")]
    public async Task<ActionResult<ApiResponse<string>>> RunAutomation(
        string scenario,
        [FromBody] Dictionary<string, string> parameters)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiResponse<string>.ErrorResponse("用户未认证"));

            if (string.IsNullOrWhiteSpace(scenario))
                return BadRequest(ApiResponse<string>.ErrorResponse("场景名称不能为空"));

            _logger.LogInformation("用户 {UserId} 执行自动化场景: {Scenario}",
                userId, scenario);

            var result = await _openClawService.RunAutomationAsync(
                scenario,
                parameters);

            return Ok(ApiResponse<string>.SuccessResponse(result, "自动化场景执行成功"));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenClaw Gateway 通信失败");
            return StatusCode(502, ApiResponse<string>.ErrorResponse("OpenClaw 服务暂不可用"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行自动化场景失败: {Scenario}", scenario);
            return StatusCode(500, ApiResponse<string>.ErrorResponse("自动化场景执行失败"));
        }
    }

    private Guid GetUserId()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
            Guid.TryParse(userIdHeader.ToString(), out var userId))
            return userId;

        return Guid.Empty;
    }
}

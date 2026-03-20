using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

[ApiController]
[Route("api/v1/ai/openclaw")]
[Produces("application/json")]
public class OpenClawController : ControllerBase
{
    private readonly ILogger<OpenClawController> _logger;
    private readonly IMembershipAccessService _membershipAccessService;
    private readonly IOpenClawResearchService _openClawResearchService;
    private readonly IOpenClawAutomationService _openClawAutomationService;

    public OpenClawController(
        IOpenClawResearchService openClawResearchService,
        IOpenClawAutomationService openClawAutomationService,
        IMembershipAccessService membershipAccessService,
        ILogger<OpenClawController> logger)
    {
        _openClawResearchService = openClawResearchService;
        _openClawAutomationService = openClawAutomationService;
        _membershipAccessService = membershipAccessService;
        _logger = logger;
    }

    [HttpPost("research")]
    public async Task<ActionResult<ApiResponse<OpenClawResearchResponse?>>> ResearchTravelPlan(
        [FromBody] OpenClawResearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var access = await _membershipAccessService.EnsurePaidMembershipAsync(cancellationToken);
            if (!access.Allowed)
            {
                return StatusCode(access.StatusCode, new ApiResponse<OpenClawResearchResponse?>
                {
                    Success = false,
                    Message = access.Message,
                    Data = null
                });
            }

            var result = await _openClawResearchService.ResearchTravelPlanAsync(request, cancellationToken);

            if (result == null)
            {
                return Ok(new ApiResponse<OpenClawResearchResponse?>
                {
                    Success = true,
                    Message = "OpenClaw 当前不可用，已回退到常规 AI 规划",
                    Data = null
                });
            }

            return Ok(new ApiResponse<OpenClawResearchResponse?>
            {
                Success = true,
                Message = "OpenClaw 研究完成",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenClaw research endpoint failed");
            return Ok(new ApiResponse<OpenClawResearchResponse?>
            {
                Success = true,
                Message = "OpenClaw 当前不可用，已回退到常规 AI 规划",
                Data = null
            });
        }
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ApiResponse<OpenClawAutomationResponse>>> ExecuteCommand(
        [FromBody] OpenClawExecuteRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _membershipAccessService.EnsurePaidMembershipAsync(cancellationToken);
        if (!access.Allowed)
            return StatusCode(access.StatusCode, new ApiResponse<OpenClawAutomationResponse>
            {
                Success = false,
                Message = access.Message
            });

        var result = await _openClawAutomationService.ExecuteCommandAsync(request, cancellationToken);
        return Ok(new ApiResponse<OpenClawAutomationResponse>
        {
            Success = result.Success,
            Message = result.Success ? "指令执行完成" : result.Error ?? "执行失败",
            Data = result
        });
    }

    [HttpPost("reminder")]
    public async Task<ActionResult<ApiResponse<OpenClawAutomationResponse>>> SetReminder(
        [FromBody] OpenClawReminderRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _membershipAccessService.EnsurePaidMembershipAsync(cancellationToken);
        if (!access.Allowed)
            return StatusCode(access.StatusCode, new ApiResponse<OpenClawAutomationResponse>
            {
                Success = false,
                Message = access.Message
            });

        var result = await _openClawAutomationService.SetReminderAsync(request, cancellationToken);
        return Ok(new ApiResponse<OpenClawAutomationResponse>
        {
            Success = result.Success,
            Message = result.Success ? "提醒设置成功" : result.Error ?? "设置失败",
            Data = result
        });
    }

    [HttpPost("visa-reminder")]
    public async Task<ActionResult<ApiResponse<OpenClawAutomationResponse>>> SetVisaReminder(
        [FromBody] OpenClawVisaReminderRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _membershipAccessService.EnsurePaidMembershipAsync(cancellationToken);
        if (!access.Allowed)
            return StatusCode(access.StatusCode, new ApiResponse<OpenClawAutomationResponse>
            {
                Success = false,
                Message = access.Message
            });

        var result = await _openClawAutomationService.SetVisaReminderAsync(request, cancellationToken);
        return Ok(new ApiResponse<OpenClawAutomationResponse>
        {
            Success = result.Success,
            Message = result.Success ? "签证提醒设置成功" : result.Error ?? "设置失败",
            Data = result
        });
    }

    [HttpPost("automation/{scenario}")]
    public async Task<ActionResult<ApiResponse<OpenClawAutomationResponse>>> RunAutomation(
        [FromRoute] string scenario,
        [FromBody] OpenClawAutomationRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _membershipAccessService.EnsurePaidMembershipAsync(cancellationToken);
        if (!access.Allowed)
            return StatusCode(access.StatusCode, new ApiResponse<OpenClawAutomationResponse>
            {
                Success = false,
                Message = access.Message
            });

        var result = await _openClawAutomationService.RunAutomationAsync(scenario, request, cancellationToken);
        return Ok(new ApiResponse<OpenClawAutomationResponse>
        {
            Success = result.Success,
            Message = result.Success ? $"场景 {scenario} 执行完成" : result.Error ?? "执行失败",
            Data = result
        });
    }

    [HttpPost("invoice-organize")]
    public async Task<ActionResult<ApiResponse<OpenClawAutomationResponse>>> OrganizeInvoices(
        [FromBody] OpenClawInvoiceOrganizeRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _membershipAccessService.EnsurePaidMembershipAsync(cancellationToken);
        if (!access.Allowed)
            return StatusCode(access.StatusCode, new ApiResponse<OpenClawAutomationResponse>
            {
                Success = false,
                Message = access.Message
            });

        var result = await _openClawAutomationService.OrganizeInvoicesAsync(request, cancellationToken);
        return Ok(new ApiResponse<OpenClawAutomationResponse>
        {
            Success = result.Success,
            Message = result.Success ? "发票整理完成" : result.Error ?? "整理失败",
            Data = result
        });
    }

    [HttpPost("script")]
    public async Task<ActionResult<ApiResponse<OpenClawAutomationResponse>>> CreateScript(
        [FromBody] OpenClawScriptRequest request,
        CancellationToken cancellationToken)
    {
        var access = await _membershipAccessService.EnsurePaidMembershipAsync(cancellationToken);
        if (!access.Allowed)
            return StatusCode(access.StatusCode, new ApiResponse<OpenClawAutomationResponse>
            {
                Success = false,
                Message = access.Message
            });

        var result = await _openClawAutomationService.CreateScriptAsync(request, cancellationToken);
        return Ok(new ApiResponse<OpenClawAutomationResponse>
        {
            Success = result.Success,
            Message = result.Success ? "脚本创建成功" : result.Error ?? "创建失败",
            Data = result
        });
    }
}
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

    public OpenClawController(
        IOpenClawResearchService openClawResearchService,
        IMembershipAccessService membershipAccessService,
        ILogger<OpenClawController> logger)
    {
        _openClawResearchService = openClawResearchService;
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
}
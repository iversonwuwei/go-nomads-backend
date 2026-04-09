using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

[ApiController]
[Route("api/v1/community")]
[Produces("application/json")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityQaService _communityQaService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(
        ICommunityQaService communityQaService,
        ICurrentUserService currentUserService,
        ILogger<CommunityController> logger)
    {
        _communityQaService = communityQaService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("questions")]
    public async Task<ActionResult<ApiResponse<CommunityQuestionResponse>>> CreateQuestion(
        [FromBody] CreateCommunityQuestionRequest request)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _communityQaService.CreateQuestionAsync(userId, request);

            return Ok(new ApiResponse<CommunityQuestionResponse>
            {
                Success = true,
                Message = "问题发布成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<CommunityQuestionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "创建 Community question 时用户未认证");
            return Unauthorized(new ApiResponse<CommunityQuestionResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Community question 失败");
            return StatusCode(500, new ApiResponse<CommunityQuestionResponse>
            {
                Success = false,
                Message = "创建问题失败"
            });
        }
    }

    [HttpPost("questions/{questionId:guid}/answers")]
    public async Task<ActionResult<ApiResponse<CommunityAnswerResponse>>> CreateAnswer(
        Guid questionId,
        [FromBody] CreateCommunityAnswerRequest request)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _communityQaService.CreateAnswerAsync(userId, questionId, request);

            return Ok(new ApiResponse<CommunityAnswerResponse>
            {
                Success = true,
                Message = "回答发布成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<CommunityAnswerResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<CommunityAnswerResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "创建 Community answer 时用户未认证");
            return Unauthorized(new ApiResponse<CommunityAnswerResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Community answer 失败");
            return StatusCode(500, new ApiResponse<CommunityAnswerResponse>
            {
                Success = false,
                Message = "创建回答失败"
            });
        }
    }

    [HttpPost("questions/{questionId:guid}/upvote")]
    public async Task<ActionResult<ApiResponse<CommunityQuestionResponse>>> ToggleQuestionUpvote(Guid questionId)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _communityQaService.ToggleQuestionUpvoteAsync(userId, questionId);

            return Ok(new ApiResponse<CommunityQuestionResponse>
            {
                Success = true,
                Message = "问题点赞状态已更新",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<CommunityQuestionResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "点赞 Community question 时用户未认证");
            return Unauthorized(new ApiResponse<CommunityQuestionResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Community question 点赞失败");
            return StatusCode(500, new ApiResponse<CommunityQuestionResponse>
            {
                Success = false,
                Message = "更新问题点赞失败"
            });
        }
    }

    [HttpPost("answers/{answerId:guid}/upvote")]
    public async Task<ActionResult<ApiResponse<CommunityAnswerResponse>>> ToggleAnswerUpvote(Guid answerId)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _communityQaService.ToggleAnswerUpvoteAsync(userId, answerId);

            return Ok(new ApiResponse<CommunityAnswerResponse>
            {
                Success = true,
                Message = "回答点赞状态已更新",
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<CommunityAnswerResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "点赞 Community answer 时用户未认证");
            return Unauthorized(new ApiResponse<CommunityAnswerResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Community answer 点赞失败");
            return StatusCode(500, new ApiResponse<CommunityAnswerResponse>
            {
                Success = false,
                Message = "更新回答点赞失败"
            });
        }
    }
}
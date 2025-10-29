using AIService.Application.DTOs;
using AIService.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Extensions;
using System.Security.Claims;

namespace AIService.API.Controllers;

/// <summary>
/// AI 聊天控制器
/// </summary>
[ApiController]
[Route("api/v1/ai")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IAIChatService _aiChatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IAIChatService aiChatService, ILogger<ChatController> logger)
    {
        _aiChatService = aiChatService;
        _logger = logger;
    }

    /// <summary>
    /// 创建新对话
    /// </summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.CreateConversationAsync(request, userId);
            return Ok(ApiResponse.Success(result, "对话创建成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建对话失败");
            return StatusCode(500, ApiResponse.Fail("创建对话失败"));
        }
    }

    /// <summary>
    /// 获取用户的对话列表
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] GetConversationsRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.GetConversationsAsync(request, userId);
            return Ok(ApiResponse.Success(result, "获取对话列表成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对话列表失败");
            return StatusCode(500, ApiResponse.Fail("获取对话列表失败"));
        }
    }

    /// <summary>
    /// 根据ID获取对话详情
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> GetConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.GetConversationAsync(conversationId, userId);
            return Ok(ApiResponse.Success(result, "获取对话详情成功"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对话详情失败");
            return StatusCode(500, ApiResponse.Fail("获取对话详情失败"));
        }
    }

    /// <summary>
    /// 更新对话
    /// </summary>
    [HttpPut("conversations/{conversationId:guid}")]
    public async Task<IActionResult> UpdateConversation(Guid conversationId, [FromBody] UpdateConversationRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.UpdateConversationAsync(conversationId, request, userId);
            return Ok(ApiResponse.Success(result, "对话更新成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新对话失败");
            return StatusCode(500, ApiResponse.Fail("更新对话失败"));
        }
    }

    /// <summary>
    /// 删除对话
    /// </summary>
    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            await _aiChatService.DeleteConversationAsync(conversationId, userId);
            return Ok(ApiResponse.Success(new { }, "对话删除成功"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除对话失败");
            return StatusCode(500, ApiResponse.Fail("删除对话失败"));
        }
    }

    /// <summary>
    /// 归档对话
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/archive")]
    public async Task<IActionResult> ArchiveConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.ArchiveConversationAsync(conversationId, userId);
            return Ok(ApiResponse.Success(result, "对话归档成功"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "归档对话失败");
            return StatusCode(500, ApiResponse.Fail("归档对话失败"));
        }
    }

    /// <summary>
    /// 激活对话
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/activate")]
    public async Task<IActionResult> ActivateConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.ActivateConversationAsync(conversationId, userId);
            return Ok(ApiResponse.Success(result, "对话激活成功"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活对话失败");
            return StatusCode(500, ApiResponse.Fail("激活对话失败"));
        }
    }

    /// <summary>
    /// 发送消息并获取AI回复
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.SendMessageAsync(conversationId, request, userId);
            return Ok(ApiResponse.Success(result, "消息发送成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息失败");
            return StatusCode(500, ApiResponse.Fail("发送消息失败"));
        }
    }

    /// <summary>
    /// 发送消息并获取流式AI回复
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages/stream")]
    public async IAsyncEnumerable<string> SendMessageStream(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var userId = this.GetUserId();
        if (userId == Guid.Empty)
        {
            yield return ApiResponse.Fail("用户未认证").ToString() ?? "";
            yield break;
        }

        await foreach (var chunk in _aiChatService.SendMessageStreamAsync(conversationId, request, userId))
        {
            yield return System.Text.Json.JsonSerializer.Serialize(chunk);
        }
    }

    /// <summary>
    /// 获取对话的消息历史
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId, [FromQuery] GetMessagesRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.GetMessagesAsync(conversationId, request, userId);
            return Ok(ApiResponse.Success(result, "获取消息历史成功"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取消息历史失败");
            return StatusCode(500, ApiResponse.Fail("获取消息历史失败"));
        }
    }

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetUserStats()
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse.Fail("用户未认证"));
            }

            var result = await _aiChatService.GetUserStatsAsync(userId);
            return Ok(ApiResponse.Success(result, "获取用户统计成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计失败");
            return StatusCode(500, ApiResponse.Fail("获取用户统计失败"));
        }
    }

    /// <summary>
    /// AI服务健康检查
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            var isHealthy = await _aiChatService.HealthCheckAsync();
            
            if (isHealthy)
            {
                return Ok(ApiResponse.Success(new { status = "healthy", timestamp = DateTime.UtcNow }, "AI服务运行正常"));
            }
            else
            {
                return StatusCode(503, ApiResponse.Fail("AI服务连接异常"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return StatusCode(503, ApiResponse.Fail("健康检查失败"));
        }
    }

    /// <summary>
    /// 从JWT令牌中获取用户ID
    /// </summary>
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

/// <summary>
/// API 标准响应格式
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// API 响应工厂
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<T> Success<T>(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<object> Fail(string message, List<string>? errors = null)
    {
        return new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}
using AIService.Application.DTOs;
using AIService.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Extensions;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.DTOs;
using System.Text.Json;

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
    public async Task<ActionResult<ApiResponse<ConversationResponse>>> CreateConversation([FromBody] CreateConversationRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<ConversationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            var result = await _aiChatService.CreateConversationAsync(request, userId);
            return Ok(new ApiResponse<ConversationResponse>
            {
                Success = true,
                Message = "对话创建成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<ConversationResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建对话失败");
            return StatusCode(500, new ApiResponse<ConversationResponse>
            {
                Success = false,
                Message = "创建对话失败"
            });
        }
    }

    /// <summary>
    /// 获取用户的对话列表
    /// </summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ConversationResponse>>>> GetConversations([FromQuery] GetConversationsRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<PagedResponse<ConversationResponse>>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            var result = await _aiChatService.GetConversationsAsync(request, userId);
            return Ok(new ApiResponse<PagedResponse<ConversationResponse>>
            {
                Success = true,
                Message = "获取对话列表成功",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对话列表失败");
            return StatusCode(500, new ApiResponse<PagedResponse<ConversationResponse>>
            {
                Success = false,
                Message = "获取对话列表失败"
            });
        }
    }

    /// <summary>
    /// 根据ID获取对话详情
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<ConversationResponse>>> GetConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<ConversationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            var result = await _aiChatService.GetConversationAsync(conversationId, userId);
            return Ok(new ApiResponse<ConversationResponse>
            {
                Success = true,
                Message = "获取对话详情成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<ConversationResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<ConversationResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取对话详情失败");
            return StatusCode(500, new ApiResponse<ConversationResponse>
            {
                Success = false,
                Message = "获取对话详情失败"
            });
        }
    }

    /// <summary>
    /// 更新对话
    /// </summary>
    [HttpPut("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateConversation(Guid conversationId, [FromBody] UpdateConversationRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });
            }

            var result = await _aiChatService.UpdateConversationAsync(conversationId, request, userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "对话更新成功", Data = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新对话失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "更新对话失败" });
        }
    }

    /// <summary>
    /// 删除对话
    /// </summary>
    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });
            }

            await _aiChatService.DeleteConversationAsync(conversationId, userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "对话删除成功", Data = new { } });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除对话失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "删除对话失败" });
        }
    }

    /// <summary>
    /// 归档对话
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/archive")]
    public async Task<ActionResult<ApiResponse<object>>> ArchiveConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });
            }

            var result = await _aiChatService.ArchiveConversationAsync(conversationId, userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "对话归档成功", Data = result });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "归档对话失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "归档对话失败" });
        }
    }

    /// <summary>
    /// 激活对话
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/activate")]
    public async Task<ActionResult<ApiResponse<object>>> ActivateConversation(Guid conversationId)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });
            }

            var result = await _aiChatService.ActivateConversationAsync(conversationId, userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "对话激活成功", Data = result });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活对话失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "激活对话失败" });
        }
    }

    /// <summary>
    /// 发送消息并获取AI回复
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<ApiResponse<object>>> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });
            }

            var result = await _aiChatService.SendMessageAsync(conversationId, request, userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "消息发送成功", Data = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "发送消息失败" });
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
            yield return ApiResponse<object>.ErrorResponse("用户未认证").ToString() ?? "";
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
    public async Task<ActionResult<ApiResponse<object>>> GetMessages(Guid conversationId, [FromQuery] GetMessagesRequest request)
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });
            }

            var result = await _aiChatService.GetMessagesAsync(conversationId, request, userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "获取消息历史成功", Data = result });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<object> { Success = false, Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取消息历史失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "获取消息历史失败" });
        }
    }

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<object>>> GetUserStats()
    {
        try
        {
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });
            }

            var result = await _aiChatService.GetUserStatsAsync(userId);
            return Ok(new ApiResponse<object> { Success = true, Message = "获取用户统计成功", Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "获取用户统计失败" });
        }
    }

    /// <summary>
    /// AI服务健康检查
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<object>>> HealthCheck()
    {
        try
        {
            var isHealthy = await _aiChatService.HealthCheckAsync();
            
            if (isHealthy)
            {
                return Ok(ApiResponse<object>.SuccessResponse(new { status = "healthy", timestamp = DateTime.UtcNow }, "AI服务运行正常"));
            }
            else
            {
                return StatusCode(503, new ApiResponse<object> { Success = false, Message = "AI服务连接异常" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return StatusCode(503, new ApiResponse<object> { Success = false, Message = "健康检查失败" });
        }
    }

    /// <summary>
    /// <summary>
    /// 生成AI旅行计划
    /// </summary>
    /// <param name="request">旅行计划生成请求</param>
    /// <returns>包含完整行程安排的旅行计划</returns>
    [HttpPost("travel-plan")]
    public async Task<ActionResult<ApiResponse<TravelPlanResponse>>> GenerateTravelPlan([FromBody] GenerateTravelPlanRequest request)
    {
        try
        {
            // 获取当前用户ID(可选,AIService 不强制要求认证)
            var userId = this.GetUserId();
            
            // 如果没有用户上下文,使用匿名用户ID
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // 匿名用户
                _logger.LogInformation("ℹ️ 匿名用户生成旅行计划");
            }

            _logger.LogInformation("🗺️ 开始生成旅行计划 - 城市: {CityName}, 天数: {Duration}, 预算: {Budget}, 风格: {TravelStyle}, 用户: {UserId}", 
                request.CityName, request.Duration, request.Budget, request.TravelStyle, userId);

            // 调用AI服务生成旅行计划
            var result = await _aiChatService.GenerateTravelPlanAsync(request, userId);
            
            _logger.LogInformation("✅ 旅行计划生成成功 - 计划ID: {PlanId}, 包含 {DayCount} 天行程", 
                result.Id, result.DailyItineraries?.Count ?? 0);
            
            return Ok(new ApiResponse<TravelPlanResponse>
            {
                Success = true,
                Message = "旅行计划生成成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "⚠️ 生成旅行计划参数错误: {Message}", ex.Message);
            return BadRequest(new ApiResponse<TravelPlanResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "❌ AI响应解析失败: {Message}", ex.Message);
            return StatusCode(500, new ApiResponse<TravelPlanResponse>
            {
                Success = false,
                Message = "AI服务返回格式错误,请稍后重试"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON解析失败: {Message}", ex.Message);
            return StatusCode(500, new ApiResponse<TravelPlanResponse>
            {
                Success = false,
                Message = "数据解析失败,请稍后重试"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成旅行计划失败");
            return StatusCode(500, new ApiResponse<TravelPlanResponse>
            {
                Success = false,
                Message = "生成旅行计划失败,请稍后重试"
            });
        }
    }

    /// <summary>
    /// 流式生成AI旅行计划 - 支持进度更新
    /// </summary>
    /// <param name="request">旅行计划生成请求</param>
    /// <returns>Server-Sent Events 流</returns>
    [HttpPost("travel-plan/stream")]
    public async Task GenerateTravelPlanStream([FromBody] GenerateTravelPlanRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            // 获取当前用户ID
            var userId = this.GetUserId();
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            }

            _logger.LogInformation("🗺️ [流式] 开始生成旅行计划 - 城市: {CityName}", request.CityName);

            // 发送开始事件
            await SendProgressEvent("start", new { message = "开始生成旅行计划...", progress = 0 });
            await Response.Body.FlushAsync();

            // 分析请求
            await SendProgressEvent("analyzing", new { message = "正在分析您的需求...", progress = 10 });
            await Response.Body.FlushAsync();
            await Task.Delay(500); // 模拟分析时间

            // 调用 AI 服务
            await SendProgressEvent("generating", new { message = "AI 正在生成行程安排...", progress = 30 });
            await Response.Body.FlushAsync();

            var result = await _aiChatService.GenerateTravelPlanAsync(request, userId);

            // 发送成功事件
            await SendProgressEvent("success", new 
            { 
                message = "旅行计划生成成功!", 
                progress = 100,
                data = result 
            });
            await Response.Body.FlushAsync();

            _logger.LogInformation("✅ [流式] 旅行计划生成成功 - 计划ID: {PlanId}", result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [流式] 生成旅行计划失败");
            await SendProgressEvent("error", new { message = $"生成失败: {ex.Message}", progress = 0 });
            await Response.Body.FlushAsync();
        }
    }

    /// <summary>
    /// 发送 SSE 进度事件
    /// </summary>
    private async Task SendProgressEvent(string eventType, object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = eventType,
            timestamp = DateTime.UtcNow,
            payload = data
        });

        var message = $"data: {json}\n\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
        await Response.Body.WriteAsync(bytes);
    }

    /// <summary>
    /// 从 UserContext 中获取用户 ID
    /// </summary>
    private Guid GetUserId()
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Guid.Empty;
        }

        return Guid.TryParse(userContext.UserId, out var userId) ? userId : Guid.Empty;
    }
}
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AIService.API.Models;
using AIService.Application.DTOs;
using AIService.Application.Services;
using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using AIService.Infrastructure.Cache;
using AIService.Infrastructure.GrpcClients;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Middleware;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Messages;
using TaskStatus = AIService.API.Models.TaskStatus;

namespace AIService.API.Controllers;

/// <summary>
///     AI 聊天控制器
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
    ///     创建新对话
    /// </summary>
    [HttpPost("conversations")]
    public async Task<ActionResult<ApiResponse<ConversationResponse>>> CreateConversation(
        [FromBody] CreateConversationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<ConversationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });

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
    ///     获取用户的对话列表
    /// </summary>
    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ConversationResponse>>>> GetConversations(
        [FromQuery] GetConversationsRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<PagedResponse<ConversationResponse>>
                {
                    Success = false,
                    Message = "用户未认证"
                });

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
    ///     根据ID获取对话详情
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<ConversationResponse>>> GetConversation(Guid conversationId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<ConversationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });

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
    ///     更新对话
    /// </summary>
    [HttpPut("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateConversation(Guid conversationId,
        [FromBody] UpdateConversationRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

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
    ///     删除对话
    /// </summary>
    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteConversation(Guid conversationId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

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
    ///     归档对话
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/archive")]
    public async Task<ActionResult<ApiResponse<object>>> ArchiveConversation(Guid conversationId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

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
    ///     激活对话
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/activate")]
    public async Task<ActionResult<ApiResponse<object>>> ActivateConversation(Guid conversationId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

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
    ///     发送消息并获取AI回复
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<ApiResponse<object>>> SendMessage(Guid conversationId,
        [FromBody] SendMessageRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

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
    ///     发送消息并获取流式AI回复
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages/stream")]
    public async IAsyncEnumerable<string> SendMessageStream(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            yield return ApiResponse<object>.ErrorResponse("用户未认证").ToString() ?? "";
            yield break;
        }

        await foreach (var chunk in _aiChatService.SendMessageStreamAsync(conversationId, request, userId))
            yield return JsonSerializer.Serialize(chunk);
    }

    /// <summary>
    ///     发送消息并通过 SignalR 获取流式AI回复（推荐）
    ///     响应将通过 SignalR 的 AIChatChunk 事件推送到客户端
    /// </summary>
    [HttpPost("conversations/{conversationId:guid}/messages/signalr-stream")]
    public async Task<ActionResult<ApiResponse<object>>> SendMessageWithSignalR(
        Guid conversationId,
        [FromBody] SendMessageWithSignalRRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

            // 生成请求 ID（用于关联 SignalR 响应）
            var requestId = request.RequestId ?? Guid.NewGuid().ToString();

            var userMessage = await _aiChatService.SendMessageWithSignalRStreamAsync(
                conversationId,
                new SendMessageRequest { Content = request.Content, Temperature = request.Temperature, MaxTokens = request.MaxTokens },
                userId,
                requestId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "消息已接收，AI 响应将通过 SignalR 推送",
                Data = new
                {
                    RequestId = requestId,
                    UserMessage = userMessage,
                    SignalREvent = "AIChatChunk"
                }
            });
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
            _logger.LogError(ex, "发送 SignalR 流式消息失败");
            return StatusCode(500, new ApiResponse<object> { Success = false, Message = "发送消息失败" });
        }
    }

    /// <summary>
    ///     获取对话的消息历史
    /// </summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<ApiResponse<object>>> GetMessages(Guid conversationId,
        [FromQuery] GetMessagesRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

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
    ///     获取用户统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<object>>> GetUserStats()
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "用户未认证" });

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
    ///     AI服务健康检查
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<object>>> HealthCheck()
    {
        try
        {
            var isHealthy = await _aiChatService.HealthCheckAsync();

            if (isHealthy)
                return Ok(ApiResponse<object>.SuccessResponse(new { status = "healthy", timestamp = DateTime.UtcNow },
                    "AI服务运行正常"));

            return StatusCode(503, new ApiResponse<object> { Success = false, Message = "AI服务连接异常" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return StatusCode(503, new ApiResponse<object> { Success = false, Message = "健康检查失败" });
        }
    }

    /// <summary>
    ///     <summary>
    ///         生成AI旅行计划
    ///     </summary>
    ///     <param name="request">旅行计划生成请求</param>
    ///     <returns>包含完整行程安排的旅行计划</returns>
    [HttpPost("travel-plan")]
    public async Task<ActionResult<ApiResponse<TravelPlanResponse>>> GenerateTravelPlan(
        [FromBody] GenerateTravelPlanRequest request)
    {
        try
        {
            // 获取当前用户ID(可选,AIService 不强制要求认证)
            var userId = GetUserId();

            // 如果没有用户上下文,使用匿名用户ID
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // 匿名用户
                _logger.LogInformation("ℹ️ 匿名用户生成旅行计划");
            }

            _logger.LogInformation(
                "🗺️ 开始生成旅行计划 - 城市: {CityName}, 天数: {Duration}, 预算: {Budget}, 风格: {TravelStyle}, 用户: {UserId}",
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
    ///     流式生成AI旅行计划 - 支持进度更新
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
            var userId = GetUserId();
            if (userId == Guid.Empty) userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

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
    ///     流式生成AI旅行计划 - 像流水一样逐步输出内容
    ///     模拟 ChatGPT 的逐字输出效果
    /// </summary>
    /// <param name="request">旅行计划生成请求</param>
    /// <returns>Server-Sent Events 流</returns>
    [HttpPost("travel-plan/stream-text")]
    public async Task GenerateTravelPlanStreamText([FromBody] GenerateTravelPlanRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("🌊 [流式文本-{RequestId}] 开始生成旅行计划 - 城市: {CityName}, Duration: {Duration}",
            requestId, request.CityName, request.Duration);

        // 设置SSE响应头 - 必须在发送任何内容前设置
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // 禁用 Nginx 缓冲

        // 立即发送一个初始化消息,建立SSE连接
        var initMessage = "data: {\"type\":\"init\",\"message\":\"连接已建立\"}\n\n";
        var initBytes = Encoding.UTF8.GetBytes(initMessage);
        await Response.Body.WriteAsync(initBytes);
        await Response.Body.FlushAsync();
        _logger.LogDebug("[{RequestId}] ✅ SSE连接已建立", requestId);

        try
        {
            // 获取当前用户ID
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                _logger.LogDebug("[{RequestId}] 使用默认用户ID", requestId);
            }

            _logger.LogInformation("🌊 [流式文本-{RequestId}] 用户ID: {UserId}", requestId, userId);

            // 发送开始提示
            _logger.LogDebug("[{RequestId}] 发送开始提示", requestId);
            await StreamText("🚀 开始为您生成旅行计划...\n\n");
            await Task.Delay(300);

            await StreamText($"📍 目的地: {request.CityName}\n");
            await Task.Delay(200);
            await StreamText($"⏱️ 行程天数: {request.Duration} 天\n");
            await Task.Delay(200);
            await StreamText($"💰 预算级别: {request.Budget}\n");
            await Task.Delay(200);
            await StreamText($"🎨 旅行风格: {request.TravelStyle}\n\n");
            await Task.Delay(500);

            // 调用 AI 服务生成
            _logger.LogInformation("[{RequestId}] 开始调用 AI 服务", requestId);
            await StreamText("🤖 AI 正在为您规划行程，请稍候...\n\n");
            await Task.Delay(800);

            var result = await _aiChatService.GenerateTravelPlanAsync(request, userId);
            _logger.LogInformation("✅ [{RequestId}] AI 服务返回结果,计划ID: {PlanId}", requestId, result.Id);

            // 逐步输出旅行计划
            _logger.LogDebug("[{RequestId}] 开始流式输出内容", requestId);
            await StreamText(new string('=', 50) + "\n");
            await StreamText($"✨ {result.CityName} {result.Duration}天旅行计划\n");
            await StreamText(new string('=', 50) + "\n\n");
            await Task.Delay(300);

            // 输出概览
            await StreamText("📋 行程概览\n");
            await StreamText($"  城市: {result.CityName}\n");
            await StreamText($"  时长: {result.Duration} 天\n");
            await StreamText($"  预算: {result.Budget}\n");
            await StreamText($"  风格: {result.TravelStyle}\n\n");
            await Task.Delay(400);

            // 逐天输出行程
            for (var i = 0; i < result.DailyItineraries.Count; i++)
            {
                var day = result.DailyItineraries[i];

                await StreamText($"\n📅 第 {day.Day} 天\n");
                await StreamText($"   主题: {day.Theme}\n\n");
                await Task.Delay(300);

                // 输出该天的活动
                foreach (var activity in day.Activities)
                {
                    await StreamText($"   ⏰ {activity.Time}\n");
                    await StreamText($"      📍 {activity.Name}\n");
                    await StreamText($"      � {activity.Description}\n");
                    await StreamText($"      � 位置: {activity.Location}\n");

                    if (activity.EstimatedCost > 0) await StreamText($"      💰 预计费用: ¥{activity.EstimatedCost:F2}\n");

                    if (activity.Duration > 0) await StreamText($"      ⏱️  预计时长: {activity.Duration} 分钟\n");

                    await StreamText("\n");
                    await Task.Delay(150); // 每个活动间隔
                }

                // 输出当天备注
                if (!string.IsNullOrEmpty(day.Notes)) await StreamText($"   📝 备注: {day.Notes}\n\n");

                await Task.Delay(200); // 每天之间的间隔
            }

            // 输出交通建议
            if (result.Transportation != null)
            {
                await StreamText("\n🚗 交通建议\n");
                await StreamText($"   到达方式: {result.Transportation.ArrivalMethod}\n");
                await StreamText($"   详情: {result.Transportation.ArrivalDetails}\n");
                await StreamText($"   预计费用: ¥{result.Transportation.EstimatedCost:F2}\n");
                await StreamText($"   市内交通: {result.Transportation.LocalTransport}\n");
                await StreamText($"   每日交通费: ¥{result.Transportation.DailyTransportCost:F2}\n\n");
                await Task.Delay(300);
            }

            // 输出住宿建议
            if (result.Accommodation != null)
            {
                await StreamText("\n🏨 住宿建议\n");
                await StreamText($"   类型: {result.Accommodation.Type}\n");
                await StreamText($"   推荐: {result.Accommodation.Recommendation}\n");
                await StreamText($"   区域: {result.Accommodation.Area}\n");
                await StreamText($"   价格: ¥{result.Accommodation.PricePerNight:F2}/晚\n");

                if (result.Accommodation.Amenities?.Any() == true)
                    await StreamText($"   设施: {string.Join(", ", result.Accommodation.Amenities)}\n");

                if (!string.IsNullOrEmpty(result.Accommodation.BookingTips))
                    await StreamText($"   预订提示: {result.Accommodation.BookingTips}\n");

                await StreamText("\n");
                await Task.Delay(300);
            }

            // 输出推荐景点
            if (result.Attractions?.Any() == true)
            {
                await StreamText("\n🎯 推荐景点 TOP 5\n");
                for (var i = 0; i < Math.Min(5, result.Attractions.Count); i++)
                {
                    var attraction = result.Attractions[i];
                    await StreamText($"   {i + 1}. {attraction.Name}\n");
                    await StreamText($"      {attraction.Description}\n");
                    await StreamText($"      类别: {attraction.Category} | 评分: {attraction.Rating}⭐\n");

                    if (attraction.EntryFee > 0) await StreamText($"      门票: ¥{attraction.EntryFee:F2}\n");

                    if (!string.IsNullOrEmpty(attraction.BestTime))
                        await StreamText($"      最佳游览时间: {attraction.BestTime}\n");

                    await Task.Delay(150);
                    await StreamText("\n");
                }
            }

            // 输出推荐餐厅
            if (result.Restaurants?.Any() == true)
            {
                await StreamText("\n🍜 推荐餐厅 TOP 5\n");
                for (var i = 0; i < Math.Min(5, result.Restaurants.Count); i++)
                {
                    var restaurant = result.Restaurants[i];
                    await StreamText($"   {i + 1}. {restaurant.Name} - {restaurant.Cuisine}\n");
                    await StreamText($"      {restaurant.Description}\n");
                    await StreamText($"      评分: {restaurant.Rating}⭐ | 价格: {restaurant.PriceRange}\n");

                    if (!string.IsNullOrEmpty(restaurant.Specialty))
                        await StreamText($"      招牌菜: {restaurant.Specialty}\n");

                    await Task.Delay(150);
                    await StreamText("\n");
                }
            }

            // 输出预算明细
            if (result.BudgetBreakdown != null)
            {
                await StreamText("\n💰 预算明细\n");
                await StreamText($"   交通: ¥{result.BudgetBreakdown.Transportation:F2}\n");
                await StreamText($"   住宿: ¥{result.BudgetBreakdown.Accommodation:F2}\n");
                await StreamText($"   餐饮: ¥{result.BudgetBreakdown.Food:F2}\n");
                await StreamText($"   活动: ¥{result.BudgetBreakdown.Activities:F2}\n");
                await StreamText($"   其他: ¥{result.BudgetBreakdown.Miscellaneous:F2}\n");
                await StreamText("   ───────────────\n");
                await StreamText($"   总计: ¥{result.BudgetBreakdown.Total:F2}\n\n");
                await Task.Delay(300);
            }

            // 输出旅行贴士
            if (result.Tips?.Any() == true)
            {
                await StreamText("\n💡 旅行贴士\n");
                for (var i = 0; i < result.Tips.Count; i++)
                {
                    await StreamText($"   {i + 1}. {result.Tips[i]}\n");
                    await Task.Delay(100);
                }

                await StreamText("\n");
            }

            // 输出总结
            await StreamText("\n" + new string('=', 50) + "\n");
            await StreamText("✅ 旅行计划生成完成!\n");
            await StreamText(new string('=', 50) + "\n\n");

            _logger.LogInformation("📤 [{RequestId}] 准备发送 complete 事件", requestId);
            // 发送完成事件(包含完整数据供客户端使用)
            await SendProgressEvent("complete", new
            {
                message = "流式输出完成",
                data = result
            });
            await Response.Body.FlushAsync(HttpContext.RequestAborted);

            _logger.LogInformation("✅ [流式文本-{RequestId}] 旅行计划输出完成 - 计划ID: {PlanId}", requestId, result.Id);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "⚠️ [流式文本-{RequestId}] 请求被取消 - 客户端可能断开连接", requestId);
            // 不抛出异常,正常结束
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "❌ [流式文本-{RequestId}] IO异常 - 网络连接问题: {Message}", requestId, ex.Message);
            try
            {
                await StreamText($"\n\n❌ 网络连接错误: {ex.Message}\n");
                await Response.Body.FlushAsync();
            }
            catch
            {
                // 忽略写入失败
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [流式文本-{RequestId}] 生成旅行计划失败: {ExceptionType}, {Message}, StackTrace: {StackTrace}",
                requestId, ex.GetType().Name, ex.Message, ex.StackTrace);
            try
            {
                await StreamText($"\n\n❌ 生成失败: {ex.Message}\n");
                await Response.Body.FlushAsync();
            }
            catch
            {
                // 忽略写入失败
            }
        }
    }

    /// <summary>
    ///     流式输出文本 - 像打字机一样逐字输出
    /// </summary>
    private async Task StreamText(string text)
    {
        try
        {
            // 检查连接状态
            if (HttpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning("⚠️ [StreamText] 客户端已断开连接,停止写入");
                return;
            }

            // 使用与前端一致的 SSE 格式
            var eventData = new
            {
                type = "text",
                payload = new { text }
            };

            var json = JsonSerializer.Serialize(eventData);
            var message = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(message);

            _logger.LogTrace("📤 [StreamText] 准备写入 {ByteCount} 字节", bytes.Length);

            await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);

            _logger.LogTrace("✅ [StreamText] 写入并刷新完成");

            // 可选: 添加控制台输出,方便调试
            Console.Write(text);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "⚠️ [StreamText] 操作被取消 - 客户端可能断开连接");
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "❌ [StreamText] IO异常 - 连接可能已断开");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [StreamText] 写入失败: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     流式输出文本 - 逐字输出(更慢,更像打字机效果)
    /// </summary>
    private async Task StreamTextCharByChar(string text, int delayMs = 30)
    {
        foreach (var c in text)
        {
            var eventData = new
            {
                type = "char",
                content = c.ToString(),
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(eventData);
            var message = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(message);

            await Response.Body.WriteAsync(bytes);
            await Response.Body.FlushAsync();

            Console.Write(c);

            // 添加延迟,模拟打字机效果
            await Task.Delay(delayMs);
        }
    }

    /// <summary>
    ///     发送 SSE 进度事件
    /// </summary>
    private async Task SendProgressEvent(string eventType, object data)
    {
        try
        {
            // 检查连接状态
            if (HttpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning("⚠️ [SendProgressEvent] 客户端已断开连接,停止写入事件: {EventType}", eventType);
                return;
            }

            var json = JsonSerializer.Serialize(new
            {
                type = eventType,
                timestamp = DateTime.UtcNow,
                payload = data
            });

            var message = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(message);

            _logger.LogDebug("📤 [SendProgressEvent] 发送事件: {EventType}, 大小: {ByteCount} 字节", eventType, bytes.Length);

            await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);

            _logger.LogDebug("✅ [SendProgressEvent] 事件发送完成: {EventType}", eventType);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "⚠️ [SendProgressEvent] 操作被取消,事件类型: {EventType}", eventType);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "❌ [SendProgressEvent] IO异常,事件类型: {EventType}", eventType);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SendProgressEvent] 发送事件失败: {EventType}, 错误: {Message}", eventType, ex.Message);
            throw;
        }
    }

    /// <summary>
    ///     从 UserContext 中获取用户 ID
    /// </summary>
    private Guid GetUserId()
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true) return Guid.Empty;

        return Guid.TryParse(userContext.UserId, out var userId) ? userId : Guid.Empty;
    }

    #region 异步任务 API

    /// <summary>
    ///     创建旅行计划生成任务(异步)
    /// </summary>
    [HttpPost("travel-plan/async")]
    [AllowAnonymous] // 测试用,生产环境应移除
    public async Task<ActionResult<ApiResponse<CreateTaskResponse>>> CreateTravelPlanTaskAsync(
        [FromBody] GenerateTravelPlanRequest request,
        [FromServices] IPublishEndpoint publishEndpoint,
        [FromServices] IRedisCache cache,
        [FromServices] IAIChatService chatService,
        [FromServices] ITravelPlanRepository travelPlanRepository,
        [FromServices] ICityGrpcClient cityGrpcClient,
        [FromServices] IImageGenerationService imageGenerationService)
    {
        try
        {
            _logger.LogInformation(
                "📥 收到异步旅行计划请求: CityId={CityId}, CityName={CityName}, Duration={Duration}, Budget={Budget}, TravelStyle={TravelStyle}",
                request.CityId, request.CityName, request.Duration, request.Budget, request.TravelStyle);

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("⚠️ 请求验证失败: {Errors}", errors);
                return BadRequest(new ApiResponse<CreateTaskResponse>
                {
                    Success = false,
                    Message = $"请求验证失败: {errors}"
                });
            }

            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                // 测试用:使用固定的测试用户ID (生产环境应移除)
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                _logger.LogWarning("⚠️ 用户未认证,使用测试用户ID: {UserId}", userId);
            }

            // 生成任务ID
            var taskId = Guid.NewGuid().ToString("N");
            var startTime = DateTime.UtcNow;

            // 初始化任务状态
            var taskStatus = new TaskStatus
            {
                TaskId = taskId,
                Status = "queued",
                Progress = 0,
                ProgressMessage = "任务已创建,正在开始处理...",
                CreatedAt = startTime,
                UpdatedAt = startTime
            };

            // 保存到 Redis (24小时过期)
            await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

            // 在后台线程中处理任务
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 开始处理旅行计划任务: TaskId={TaskId}", taskId);

                    // 调用 AI 生成服务，传递进度回调
                    var travelPlan = await chatService.GenerateTravelPlanAsync(
                        request,
                        userId,
                        async (progress, message) =>
                        {
                            // 发送进度消息到 MessageService
                            await publishEndpoint.Publish(new AIProgressMessage
                            {
                                TaskId = taskId,
                                UserId = userId.ToString(),
                                Progress = progress,
                                Message = message,
                                TaskType = "travel-plan",
                                CurrentStage = message,
                                Status = progress >= 100 ? "completed" : "processing", // 进度100%时标记为 completed
                                Timestamp = DateTime.UtcNow
                            });

                            _logger.LogInformation("📊 进度: {Progress}% - {Message}", progress, message);
                        });

                    // 保存结果到 Redis
                    var planId = travelPlan.Id;
                    
                    // 生成旅行计划封面图片
                    string? cityImage = null;
                    try
                    {
                        // 发送图片生成进度
                        await publishEndpoint.Publish(new AIProgressMessage
                        {
                            TaskId = taskId,
                            UserId = userId.ToString(),
                            Progress = 88,
                            Message = "正在生成旅行计划封面图片...",
                            TaskType = "travel-plan",
                            CurrentStage = "正在生成旅行计划封面图片...",
                            Status = "processing",
                            Timestamp = DateTime.UtcNow
                        });
                        
                        _logger.LogInformation("🎨 开始生成旅行计划封面图片: PlanId={PlanId}, CityName={CityName}", planId, request.CityName);
                        
                        // 构建图片生成提示词
                        var imagePrompt = $"A stunning travel photography of {request.CityName}, showcasing the most iconic landmarks and atmosphere of this destination, " +
                                         $"beautiful lighting, professional travel magazine quality, vibrant colors, inviting atmosphere for travelers, " +
                                         $"4K ultra high definition, cinematic composition";
                        
                        var imageRequest = new GenerateImageRequest
                        {
                            Prompt = imagePrompt,
                            NegativePrompt = "blurry, low quality, watermark, text, logo, cartoon, anime, illustration, painting",
                            Style = "<photography>",
                            Size = "1280*720", // 横屏适合卡片展示
                            Count = 1,
                            Bucket = "city-photos",
                            PathPrefix = $"travel-plans/{planId}"
                        };
                        
                        var imageResult = await imageGenerationService.GenerateImageAsync(imageRequest, userId);
                        
                        if (imageResult.Success && imageResult.Images?.Count > 0)
                        {
                            cityImage = imageResult.Images.First().Url;
                            travelPlan.CityImage = cityImage;
                            _logger.LogInformation("✅ 旅行计划封面图片生成成功: PlanId={PlanId}, ImageUrl={ImageUrl}", planId, cityImage);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ 旅行计划封面图片生成失败，尝试获取城市默认图片: PlanId={PlanId}, Error={Error}", 
                                planId, imageResult.ErrorMessage);
                            
                            // 降级：尝试从 CityService 获取城市默认图片
                            if (!string.IsNullOrEmpty(request.CityId) && Guid.TryParse(request.CityId, out var cityIdGuid))
                            {
                                cityImage = await cityGrpcClient.GetCityImageAsync(cityIdGuid);
                                if (!string.IsNullOrEmpty(cityImage))
                                {
                                    travelPlan.CityImage = cityImage;
                                    _logger.LogInformation("✅ 使用城市默认图片: CityId={CityId}, ImageUrl={ImageUrl}", request.CityId, cityImage);
                                }
                            }
                        }
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, "⚠️ 生成旅行计划封面图片异常（不影响主流程）: PlanId={PlanId}", planId);
                        
                        // 降级：尝试从 CityService 获取城市默认图片
                        try
                        {
                            if (!string.IsNullOrEmpty(request.CityId) && Guid.TryParse(request.CityId, out var cityIdGuid))
                            {
                                cityImage = await cityGrpcClient.GetCityImageAsync(cityIdGuid);
                                if (!string.IsNullOrEmpty(cityImage))
                                {
                                    travelPlan.CityImage = cityImage;
                                }
                            }
                        }
                        catch
                        {
                            // 忽略降级失败
                        }
                    }
                    
                    // 更新 JSON 包含图片 URL
                    var planJson = JsonSerializer.Serialize(travelPlan);
                    await cache.SetStringAsync($"plan:{planId}", planJson, TimeSpan.FromHours(24));

                    // 保存到数据库 (持久化存储)
                    try
                    {
                        var dbPlan = new AiTravelPlan
                        {
                            Id = Guid.Parse(planId),
                            UserId = userId,
                            CityId = request.CityId,
                            CityName = request.CityName,
                            CityImage = cityImage,
                            Duration = request.Duration,
                            BudgetLevel = request.Budget,
                            TravelStyle = request.TravelStyle,
                            Interests = request.Interests?.ToArray(),
                            DepartureLocation = request.DepartureLocation,
                            DepartureDate = request.DepartureDate,
                            PlanData = planJson,
                            Status = "published",
                            IsPublic = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await travelPlanRepository.SaveAsync(dbPlan);
                        _logger.LogInformation("💾 旅行计划已保存到数据库: PlanId={PlanId}, UserId={UserId}, CityImage={CityImage}", planId, userId, cityImage ?? "null");
                    }
                    catch (Exception dbEx)
                    {
                        _logger.LogWarning(dbEx, "⚠️ 保存旅行计划到数据库失败（不影响主流程）: PlanId={PlanId}", planId);
                    }

                    // 更新任务状态
                    taskStatus.Status = "completed";
                    taskStatus.Progress = 100;
                    taskStatus.PlanId = planId;
                    taskStatus.Result = travelPlan;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    // 发送完成消息到 MessageService
                    await publishEndpoint.Publish(new AITaskCompletedMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        TaskType = "travel-plan",
                        ResultId = planId,
                        Result = travelPlan,
                        CompletedAt = DateTime.UtcNow,
                        DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds
                    });

                    _logger.LogInformation("✅ 旅行计划生成完成: TaskId={TaskId}, PlanId={PlanId}", taskId, planId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 处理旅行计划任务失败: TaskId={TaskId}", taskId);

                    // 更新任务状态为失败
                    taskStatus.Status = "failed";
                    taskStatus.Error = ex.Message;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    // 发送失败消息到 MessageService
                    await publishEndpoint.Publish(new AITaskFailedMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        TaskType = "travel-plan",
                        ErrorMessage = ex.Message,
                        ErrorCode = "GENERATION_FAILED",
                        StackTrace = ex.StackTrace,
                        FailedAt = DateTime.UtcNow
                    });
                }
            });

            _logger.LogInformation("✅ 任务已创建: {TaskId}, UserId: {UserId}", taskId, userId);

            return Ok(new ApiResponse<CreateTaskResponse>
            {
                Success = true,
                Message = "任务已创建",
                Data = new CreateTaskResponse
                {
                    TaskId = taskId,
                    Status = "queued",
                    EstimatedTimeSeconds = 120,
                    Message = "任务已创建,正在处理中。请通过 SignalR 连接 MessageService 接收实时进度。"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建任务失败");
            return StatusCode(500, new ApiResponse<CreateTaskResponse>
            {
                Success = false,
                Message = $"创建任务失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     查询任务状态
    /// </summary>
    [HttpGet("travel-plan/tasks/{taskId}")]
    [AllowAnonymous] // 测试用,生产环境应移除
    public async Task<ActionResult<ApiResponse<TaskStatus>>> GetTaskStatusAsync(
        string taskId,
        [FromServices] IRedisCache cache)
    {
        try
        {
            var taskStatus = await cache.GetAsync<TaskStatus>($"task:{taskId}");

            if (taskStatus == null)
                return NotFound(new ApiResponse<TaskStatus>
                {
                    Success = false,
                    Message = "任务不存在或已过期"
                });

            return Ok(new ApiResponse<TaskStatus>
            {
                Success = true,
                Message = "查询成功",
                Data = taskStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询任务状态失败: {TaskId}", taskId);
            return StatusCode(500, new ApiResponse<TaskStatus>
            {
                Success = false,
                Message = $"查询失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     根据 planId 获取旅行计划详情
    /// </summary>
    [HttpGet("travel-plans/{planId}")]
    [AllowAnonymous] // 测试用,生产环境应移除
    public async Task<ActionResult<ApiResponse<TravelPlanResponse>>> GetTravelPlanByIdAsync(
        string planId,
        [FromServices] IRedisCache cache)
    {
        try
        {
            _logger.LogInformation("📥 收到获取旅行计划请求: PlanId={PlanId}", planId);

            // 从 Redis 读取计划数据 (AIWorkerService 保存时的键名格式)
            var planContent = await cache.GetStringAsync($"plan:{planId}");

            if (string.IsNullOrEmpty(planContent))
            {
                _logger.LogWarning("⚠️ 计划不存在或已过期: {PlanId}", planId);
                return NotFound(new ApiResponse<TravelPlanResponse>
                {
                    Success = false,
                    Message = "旅行计划不存在或已过期 (24小时有效期)"
                });
            }

            _logger.LogInformation("📄 找到计划数据,长度: {Length}", planContent.Length);

            // 解析 JSON 为 TravelPlanResponse
            try
            {
                // 使用与 GenerateTravelPlanAsync 相同的解析逻辑
                var travelPlan = JsonSerializer.Deserialize<TravelPlanResponse>(
                    planContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                if (travelPlan == null) throw new InvalidOperationException("解析结果为 null");

                _logger.LogInformation("✅ 旅行计划解析成功: {PlanId}, Duration={Duration}", planId, travelPlan.Duration);

                return Ok(new ApiResponse<TravelPlanResponse>
                {
                    Success = true,
                    Message = "获取成功",
                    Data = travelPlan
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ JSON 解析失败: {PlanId}", planId);
                return StatusCode(500, new ApiResponse<TravelPlanResponse>
                {
                    Success = false,
                    Message = $"计划数据解析失败: {ex.Message}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取旅行计划失败: {PlanId}", planId);
            return StatusCode(500, new ApiResponse<TravelPlanResponse>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     根据 ID 获取旅行计划详情（从数据库）
    /// </summary>
    /// <param name="planId">旅行计划ID</param>
    /// <returns>旅行计划详情</returns>
    [HttpGet("travel-plans/{planId:guid}/detail")]
    public async Task<ActionResult<ApiResponse<TravelPlanResponse>>> GetTravelPlanDetailAsync(
        Guid planId,
        [FromServices] ITravelPlanRepository travelPlanRepository = null!)
    {
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("📥 获取旅行计划详情: PlanId={PlanId}, UserId={UserId}", planId, userId);

            var plan = await travelPlanRepository.GetByIdAsync(planId);
            if (plan == null)
            {
                return NotFound(new ApiResponse<TravelPlanResponse>
                {
                    Success = false,
                    Message = "旅行计划不存在"
                });
            }

            // 验证计划所有权（只有所有者或公开计划可以访问）
            if (userId != Guid.Empty && plan.UserId != userId && !plan.IsPublic)
            {
                return StatusCode(403, new ApiResponse<TravelPlanResponse>
                {
                    Success = false,
                    Message = "无权访问该旅行计划"
                });
            }

            // 解析 PlanData 为 TravelPlanResponse
            if (string.IsNullOrEmpty(plan.PlanData))
            {
                return StatusCode(500, new ApiResponse<TravelPlanResponse>
                {
                    Success = false,
                    Message = "旅行计划数据为空"
                });
            }

            var travelPlan = JsonSerializer.Deserialize<TravelPlanResponse>(
                plan.PlanData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (travelPlan == null)
            {
                return StatusCode(500, new ApiResponse<TravelPlanResponse>
                {
                    Success = false,
                    Message = "旅行计划数据解析失败"
                });
            }

            // 补充数据库中存储的出发地和出发日期信息
            travelPlan.DepartureLocation = plan.DepartureLocation;
            travelPlan.DepartureDate = plan.DepartureDate;

            _logger.LogInformation("✅ 获取旅行计划详情成功: PlanId={PlanId}", planId);

            return Ok(new ApiResponse<TravelPlanResponse>
            {
                Success = true,
                Message = "获取成功",
                Data = travelPlan
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取旅行计划详情失败: PlanId={PlanId}", planId);
            return StatusCode(500, new ApiResponse<TravelPlanResponse>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     获取当前用户的旅行计划列表
    /// </summary>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认20</param>
    /// <returns>用户的旅行计划列表</returns>
    [HttpGet("travel-plans")]
    public async Task<ActionResult<ApiResponse<List<AiTravelPlanSummary>>>> GetUserTravelPlansAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ITravelPlanRepository travelPlanRepository = null!)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ApiResponse<List<AiTravelPlanSummary>>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            _logger.LogInformation("📥 获取用户旅行计划列表: UserId={UserId}, Page={Page}, PageSize={PageSize}",
                userId, page, pageSize);

            var plans = await travelPlanRepository.GetByUserIdAsync(userId, page, pageSize);

            // 转换为摘要DTO（不包含完整的 PlanData）
            var summaries = plans.Select(p => new AiTravelPlanSummary
            {
                Id = p.Id,
                CityId = p.CityId,
                CityName = p.CityName,
                CityImage = p.CityImage,
                Duration = p.Duration,
                BudgetLevel = p.BudgetLevel,
                TravelStyle = p.TravelStyle,
                Status = p.Status,
                DepartureDate = p.DepartureDate,
                CreatedAt = p.CreatedAt
            }).ToList();

            _logger.LogInformation("✅ 获取到 {Count} 个旅行计划", summaries.Count);

            return Ok(new ApiResponse<List<AiTravelPlanSummary>>
            {
                Success = true,
                Message = $"获取成功，共 {summaries.Count} 个旅行计划",
                Data = summaries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户旅行计划列表失败");
            return StatusCode(500, new ApiResponse<List<AiTravelPlanSummary>>
            {
                Success = false,
                Message = $"获取失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     生成数字游民旅游指南
    /// </summary>
    [HttpPost("travel-guide")]
    public async Task<ActionResult<ApiResponse<TravelGuideResponse>>> GenerateTravelGuide(
        [FromBody] GenerateTravelGuideRequest request)
    {
        try
        {
            // 获取当前用户ID(可选,AIService 不强制要求认证)
            var userId = GetUserId();

            // 如果没有用户上下文,使用匿名用户ID
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // 匿名用户
                _logger.LogInformation("ℹ️ 匿名用户生成旅游指南");
            }

            _logger.LogInformation("📖 开始生成数字游民旅游指南 - 城市: {CityName}, 用户: {UserId}",
                request.CityName, userId);

            // 调用AI服务生成旅游指南
            var result = await _aiChatService.GenerateTravelGuideAsync(request, userId);

            _logger.LogInformation("✅ 旅游指南生成成功 - 城市: {CityName}", request.CityName);

            return Ok(new ApiResponse<TravelGuideResponse>
            {
                Success = true,
                Message = "旅游指南生成成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "⚠️ 生成旅游指南参数错误: {Message}", ex.Message);
            return BadRequest(new ApiResponse<TravelGuideResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "❌ AI响应解析失败: {Message}", ex.Message);
            return StatusCode(500, new ApiResponse<TravelGuideResponse>
            {
                Success = false,
                Message = "AI服务返回格式错误,请稍后重试"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON解析失败: {Message}", ex.Message);
            return StatusCode(500, new ApiResponse<TravelGuideResponse>
            {
                Success = false,
                Message = "数据解析失败,请稍后重试"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成旅游指南失败");
            return StatusCode(500, new ApiResponse<TravelGuideResponse>
            {
                Success = false,
                Message = "生成旅游指南失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     流式生成数字游民旅游指南 - 带进度条
    /// </summary>
    [HttpPost("travel-guide/stream")]
    public async Task GenerateTravelGuideStream([FromBody] GenerateTravelGuideRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            // 获取当前用户ID
            var userId = GetUserId();
            if (userId == Guid.Empty) userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            _logger.LogInformation("📖 [流式] 开始生成数字游民旅游指南 - 城市: {CityName}", request.CityName);

            // 发送开始事件
            await SendProgressEvent("start", new { message = "开始生成旅游指南...", progress = 0 });
            await Response.Body.FlushAsync();

            // 调用 AI 服务,传入进度回调
            var result = await _aiChatService.GenerateTravelGuideAsync(
                request,
                userId,
                async (progress, message) =>
                {
                    await SendProgressEvent("progress", new { message, progress });
                    await Response.Body.FlushAsync();
                });

            // 发送成功事件
            await SendProgressEvent("success", new
            {
                message = "旅游指南生成成功!",
                progress = 100,
                data = result
            });
            await Response.Body.FlushAsync();

            _logger.LogInformation("✅ [流式] 旅游指南生成成功 - 城市: {CityName}", request.CityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [流式] 生成旅游指南失败");
            await SendProgressEvent("error", new { message = $"生成失败: {ex.Message}", progress = 0 });
            await Response.Body.FlushAsync();
        }
    }

    /// <summary>
    ///     创建数字游民指南生成任务(异步)
    /// </summary>
    [HttpPost("guide/async")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CreateTaskResponse>>> CreateDigitalNomadGuideTaskAsync(
        [FromBody] GenerateTravelGuideRequest request,
        [FromServices] IPublishEndpoint publishEndpoint,
        [FromServices] IRedisCache cache,
        [FromServices] IAIChatService chatService,
        [FromServices] IHttpClientFactory httpClientFactory)
    {
        try
        {
            _logger.LogInformation("📥 收到异步数字游民指南请求: CityId={CityId}, CityName={CityName}",
                request.CityId, request.CityName);

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("⚠️ 请求验证失败: {Errors}", errors);
                return BadRequest(new ApiResponse<CreateTaskResponse>
                {
                    Success = false,
                    Message = $"请求验证失败: {errors}"
                });
            }

            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                _logger.LogWarning("⚠️ 用户未认证,使用测试用户ID: {UserId}", userId);
            }

            // 生成任务ID
            var taskId = Guid.NewGuid().ToString("N");
            var startTime = DateTime.UtcNow;

            // 初始化任务状态
            var taskStatus = new TaskStatus
            {
                TaskId = taskId,
                Status = "queued",
                Progress = 0,
                ProgressMessage = "指南任务已创建,正在开始处理...",
                CreatedAt = startTime,
                UpdatedAt = startTime
            };

            // 保存到 Redis (24小时过期)
            await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

            // 在后台线程中处理任务
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 开始处理数字游民指南任务: TaskId={TaskId}", taskId);

                    // 调用 AI 生成服务，传递进度回调
                    var guide = await chatService.GenerateTravelGuideAsync(
                        request,
                        userId,
                        async (progress, message) =>
                        {
                            // 发送进度消息到 MessageService
                            await publishEndpoint.Publish(new AIProgressMessage
                            {
                                TaskId = taskId,
                                UserId = userId.ToString(),
                                Progress = progress,
                                Message = message,
                                TaskType = "digital-nomad-guide",
                                CurrentStage = message,
                                Status = progress >= 100 ? "completed" : "processing", // 进度100%时标记为 completed
                                Timestamp = DateTime.UtcNow
                            });

                            _logger.LogInformation("📊 指南进度: {Progress}% - {Message}", progress, message);
                        });

                    // 保存结果到 CityService（通过 HttpClient）
                    try
                    {
                        var saveRequest = new
                        {
                            request.CityId,
                            request.CityName,
                            guide.Overview,
                            VisaInfo = new
                            {
                                guide.VisaInfo.Type,
                                guide.VisaInfo.Duration,
                                guide.VisaInfo.Requirements,
                                guide.VisaInfo.Cost,
                                guide.VisaInfo.Process
                            },
                            BestAreas = guide.BestAreas.Select(a => new
                            {
                                a.Name,
                                a.Description,
                                a.EntertainmentScore,
                                a.EntertainmentDescription,
                                a.TourismScore,
                                a.TourismDescription,
                                a.EconomyScore,
                                a.EconomyDescription,
                                a.CultureScore,
                                a.CultureDescription
                            }).ToList(),
                            guide.WorkspaceRecommendations,
                            guide.Tips,
                            guide.EssentialInfo
                        };

                        await httpClientFactory.CreateClient("city-service").PostAsJsonAsync(
                            $"api/v1/cities/{request.CityId}/guide",
                            saveRequest);

                        _logger.LogInformation("✅ 指南已保存到 CityService: CityId={CityId}", request.CityId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ 保存指南到 CityService 失败,但不影响任务完成");
                    }

                    // 保存结果到 Redis
                    var guideId = $"guide_{request.CityId}_{Guid.NewGuid():N}";
                    var guideJson = JsonSerializer.Serialize(guide);
                    await cache.SetStringAsync($"guide:{guideId}", guideJson, TimeSpan.FromHours(24));

                    // 更新任务状态
                    taskStatus.Status = "completed";
                    taskStatus.Progress = 100;
                    taskStatus.GuideId = guideId;
                    taskStatus.Result = guide;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    // 发送完成消息到 MessageService
                    await publishEndpoint.Publish(new AITaskCompletedMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        TaskType = "digital-nomad-guide",
                        ResultId = guideId,
                        Result = guide,
                        CompletedAt = DateTime.UtcNow,
                        DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds
                    });

                    _logger.LogInformation("✅ 数字游民指南生成完成: TaskId={TaskId}, GuideId={GuideId}", taskId, guideId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 处理数字游民指南任务失败: TaskId={TaskId}", taskId);

                    // 更新任务状态为失败
                    taskStatus.Status = "failed";
                    taskStatus.Error = ex.Message;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    // 发送失败消息到 MessageService
                    await publishEndpoint.Publish(new AITaskFailedMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        TaskType = "digital-nomad-guide",
                        ErrorMessage = ex.Message,
                        ErrorCode = "GENERATION_FAILED",
                        StackTrace = ex.StackTrace,
                        FailedAt = DateTime.UtcNow
                    });
                }
            });

            _logger.LogInformation("✅ 指南任务已创建: {TaskId}, UserId: {UserId}", taskId, userId);

            return Ok(new ApiResponse<CreateTaskResponse>
            {
                Success = true,
                Message = "任务创建成功",
                Data = new CreateTaskResponse
                {
                    TaskId = taskId,
                    Status = "queued",
                    EstimatedTimeSeconds = 120,
                    Message = "数字游民指南生成任务已创建,正在处理中。请通过 SignalR 连接 MessageService 接收实时进度。"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建指南任务失败");
            return StatusCode(500, new ApiResponse<CreateTaskResponse>
            {
                Success = false,
                Message = $"创建任务失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     生成附近城市信息
    /// </summary>
    [HttpPost("nearby-cities")]
    public async Task<ActionResult<ApiResponse<NearbyCitiesResponse>>> GenerateNearbyCities(
        [FromBody] GenerateNearbyCitiesRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                _logger.LogInformation("ℹ️ 匿名用户生成附近城市");
            }

            _logger.LogInformation("🌍 开始生成附近城市信息 - 城市: {CityName}", request.CityName);

            var result = await _aiChatService.GenerateNearbyCitiesAsync(request, userId);

            _logger.LogInformation("✅ 附近城市信息生成成功 - 城市: {CityName}, 数量: {Count}",
                request.CityName, result.Cities.Count);

            return Ok(new ApiResponse<NearbyCitiesResponse>
            {
                Success = true,
                Message = "附近城市信息生成成功",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成附近城市信息失败");
            return StatusCode(500, new ApiResponse<NearbyCitiesResponse>
            {
                Success = false,
                Message = "生成附近城市信息失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     流式生成附近城市信息 - 带进度条
    /// </summary>
    [HttpPost("nearby-cities/stream")]
    public async Task GenerateNearbyCitiesStream([FromBody] GenerateNearbyCitiesRequest request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            _logger.LogInformation("🌍 [流式] 开始生成附近城市信息 - 城市: {CityName}", request.CityName);

            await SendProgressEvent("start", new { message = "开始生成附近城市信息...", progress = 0 });
            await Response.Body.FlushAsync();

            var result = await _aiChatService.GenerateNearbyCitiesAsync(
                request,
                userId,
                async (progress, message) =>
                {
                    await SendProgressEvent("progress", new { message, progress });
                    await Response.Body.FlushAsync();
                });

            await SendProgressEvent("success", new
            {
                message = "附近城市信息生成成功!",
                progress = 100,
                data = result
            });
            await Response.Body.FlushAsync();

            _logger.LogInformation("✅ [流式] 附近城市信息生成成功 - 城市: {CityName}", request.CityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [流式] 生成附近城市信息失败");
            await SendProgressEvent("error", new { message = $"生成失败: {ex.Message}", progress = 0 });
            await Response.Body.FlushAsync();
        }
    }

    /// <summary>
    ///     创建附近城市生成任务(异步)
    /// </summary>
    [HttpPost("nearby-cities/async")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CreateTaskResponse>>> CreateNearbyCitiesTaskAsync(
        [FromBody] GenerateNearbyCitiesRequest request,
        [FromServices] IPublishEndpoint publishEndpoint,
        [FromServices] IRedisCache cache,
        [FromServices] IAIChatService chatService,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IImageGenerationService imageService)
    {
        try
        {
            _logger.LogInformation("📥 收到异步附近城市生成请求: CityId={CityId}, CityName={CityName}",
                request.CityId, request.CityName);

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(new ApiResponse<CreateTaskResponse>
                {
                    Success = false,
                    Message = $"请求验证失败: {errors}"
                });
            }

            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            }

            var taskId = Guid.NewGuid().ToString("N");
            var startTime = DateTime.UtcNow;

            var taskStatus = new TaskStatus
            {
                TaskId = taskId,
                Status = "queued",
                Progress = 0,
                ProgressMessage = "附近城市生成任务已创建,正在开始处理...",
                CreatedAt = startTime,
                UpdatedAt = startTime
            };

            await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

            // 后台处理任务
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 开始处理附近城市生成任务: TaskId={TaskId}", taskId);

                    var nearbyCities = await chatService.GenerateNearbyCitiesAsync(
                        request,
                        userId,
                        async (progress, message) =>
                        {
                            await publishEndpoint.Publish(new AIProgressMessage
                            {
                                TaskId = taskId,
                                UserId = userId.ToString(),
                                Progress = progress,
                                Message = message,
                                TaskType = "nearby-cities",
                                CurrentStage = message,
                                Status = progress >= 100 ? "completed" : "processing",
                                Timestamp = DateTime.UtcNow
                            });

                            _logger.LogInformation("📊 附近城市进度: {Progress}% - {Message}", progress, message);
                        });

                    // 为每个附近城市生成图片
                    await publishEndpoint.Publish(new AIProgressMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        Progress = 75,
                        Message = "正在生成城市图片...",
                        TaskType = "nearby-cities",
                        CurrentStage = "正在生成城市图片...",
                        Status = "processing",
                        Timestamp = DateTime.UtcNow
                    });

                    var citiesWithImages = new List<NearbyCityItemResponse>();
                    var totalCities = nearbyCities.Cities.Count;
                    var processedCount = 0;
                    var successCount = 0;
                    var failedCount = 0;

                    foreach (var city in nearbyCities.Cities)
                    {
                        processedCount++;

                        // 先发送进度：开始生成当前城市图片
                        var imageProgress = 75 + (int)((processedCount - 1) * 20.0 / totalCities);
                        await publishEndpoint.Publish(new AIProgressMessage
                        {
                            TaskId = taskId,
                            UserId = userId.ToString(),
                            Progress = imageProgress,
                            Message = $"正在生成图片 ({processedCount}/{totalCities}): {city.CityName}",
                            TaskType = "nearby-cities",
                            CurrentStage = $"🖼️ 正在为 {city.CityName} 生成图片...",
                            Status = "processing",
                            Timestamp = DateTime.UtcNow
                        });

                        try
                        {
                            _logger.LogInformation("🖼️ 开始为附近城市生成图片: {CityName}", city.CityName);

                            // 提取英文城市名（如果有中英文格式如"北戴河/Beidaihe"，只取英文部分）
                            var cityNameForPrompt = city.CityName;
                            if (cityNameForPrompt.Contains('/'))
                            {
                                cityNameForPrompt = cityNameForPrompt.Split('/').Last().Trim();
                            }

                            // 生成安全的城市ID（只保留字母数字和下划线，移除中文和特殊字符）
                            var safeCityName = System.Text.RegularExpressions.Regex.Replace(
                                city.CityName, @"[^a-zA-Z0-9_]", "_");
                            var nearbyCityId = $"nearby_{request.CityId}_{safeCityName}_{Guid.NewGuid():N}";

                            // 构建 prompt（融合美食、旅游和现代化元素）
                            var cityDesc = string.IsNullOrEmpty(city.Country)
                                ? cityNameForPrompt
                                : $"{cityNameForPrompt}, {city.Country}";
                            var prompt = $"Panoramic travel photograph of {cityDesc}, featuring local food culture, famous scenic spots, modern architecture, vibrant street life, colorful composition, professional photography, high resolution, vivid colors";

                            // 只生成一张横屏图片
                            var imageRequest = new GenerateImageRequest
                            {
                                Prompt = prompt,
                                NegativePrompt = "blurry, low quality, distorted, watermark, text, logo, ugly, deformed, dull colors",
                                Style = "<photography>",
                                Size = "1280*720", // 横屏尺寸
                                Count = 1,
                                Bucket = "city-photos",
                                PathPrefix = $"nearby/{nearbyCityId}"
                            };

                            var imageResult = await imageService.GenerateImageAsync(imageRequest, Guid.Empty);

                            if (imageResult.Success && imageResult.Images.Count > 0)
                            {
                                city.ImageUrl = imageResult.Images[0].Url;
                                successCount++;
                                _logger.LogInformation("✅ 附近城市图片生成成功: {CityName} -> {Url}",
                                    city.CityName, city.ImageUrl);

                                // 发送成功状态
                                await publishEndpoint.Publish(new AIProgressMessage
                                {
                                    TaskId = taskId,
                                    UserId = userId.ToString(),
                                    Progress = 75 + (int)(processedCount * 20.0 / totalCities),
                                    Message = $"✅ {city.CityName} 图片生成成功 ({processedCount}/{totalCities})",
                                    TaskType = "nearby-cities",
                                    CurrentStage = $"✅ {city.CityName} 图片已生成",
                                    Status = "processing",
                                    Timestamp = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                failedCount++;
                                _logger.LogWarning("⚠️ 附近城市图片生成失败: {CityName}, 错误: {Error}",
                                    city.CityName, imageResult.ErrorMessage);

                                // 发送失败状态（但继续处理）
                                await publishEndpoint.Publish(new AIProgressMessage
                                {
                                    TaskId = taskId,
                                    UserId = userId.ToString(),
                                    Progress = 75 + (int)(processedCount * 20.0 / totalCities),
                                    Message = $"⚠️ {city.CityName} 图片生成失败 ({processedCount}/{totalCities})",
                                    TaskType = "nearby-cities",
                                    CurrentStage = $"⚠️ {city.CityName} 图片生成失败，继续处理下一个...",
                                    Status = "processing",
                                    Timestamp = DateTime.UtcNow
                                });
                            }
                        }
                        catch (Exception imgEx)
                        {
                            failedCount++;
                            _logger.LogWarning(imgEx, "⚠️ 生成附近城市图片异常: {CityName}", city.CityName);

                            // 发送异常状态
                            await publishEndpoint.Publish(new AIProgressMessage
                            {
                                TaskId = taskId,
                                UserId = userId.ToString(),
                                Progress = 75 + (int)(processedCount * 20.0 / totalCities),
                                Message = $"❌ {city.CityName} 图片生成异常 ({processedCount}/{totalCities})",
                                TaskType = "nearby-cities",
                                CurrentStage = $"❌ {city.CityName} 发生错误，继续处理下一个...",
                                Status = "processing",
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        citiesWithImages.Add(city);
                    }

                    // 发送图片生成汇总状态
                    await publishEndpoint.Publish(new AIProgressMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        Progress = 95,
                        Message = $"图片生成完成: 成功 {successCount} 个, 失败 {failedCount} 个",
                        TaskType = "nearby-cities",
                        CurrentStage = $"📊 图片生成汇总: ✅ {successCount} 成功, ⚠️ {failedCount} 失败",
                        Status = "processing",
                        Timestamp = DateTime.UtcNow
                    });

                    // 更新结果中的城市列表
                    nearbyCities.Cities = citiesWithImages;

                    // 保存结果到 CityService（通过 HttpClient）
                    try
                    {
                        var saveRequest = new
                        {
                            SourceCityId = request.CityId,
                            NearbyCities = nearbyCities.Cities.Select(c => new
                            {
                                TargetCityName = c.CityName,
                                c.Country,
                                c.DistanceKm,
                                c.TransportationType,
                                c.TravelTimeMinutes,
                                c.Highlights,
                                NomadFeatures = new
                                {
                                    c.NomadFeatures.MonthlyCostUsd,
                                    c.NomadFeatures.InternetSpeedMbps,
                                    c.NomadFeatures.CoworkingSpaces,
                                    c.NomadFeatures.VisaInfo,
                                    c.NomadFeatures.SafetyScore,
                                    c.NomadFeatures.QualityOfLife
                                },
                                c.ImageUrl,
                                c.OverallScore,
                                c.Latitude,
                                c.Longitude,
                                IsAIGenerated = true
                            }).ToList()
                        };

                        await httpClientFactory.CreateClient("city-service").PostAsJsonAsync(
                            $"api/v1/cities/{request.CityId}/nearby",
                            saveRequest);

                        _logger.LogInformation("✅ 附近城市已保存到 CityService: CityId={CityId}", request.CityId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ 保存附近城市到 CityService 失败,但不影响任务完成");
                    }

                    // 保存结果到 Redis
                    var resultId = $"nearby_{request.CityId}_{Guid.NewGuid():N}";
                    var resultJson = JsonSerializer.Serialize(nearbyCities);
                    await cache.SetStringAsync($"nearby:{resultId}", resultJson, TimeSpan.FromHours(24));

                    // 更新任务状态
                    taskStatus.Status = "completed";
                    taskStatus.Progress = 100;
                    taskStatus.GuideId = resultId;
                    taskStatus.Result = nearbyCities;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    // 发送完成消息
                    await publishEndpoint.Publish(new AITaskCompletedMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        TaskType = "nearby-cities",
                        ResultId = resultId,
                        Result = nearbyCities,
                        CompletedAt = DateTime.UtcNow,
                        DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds
                    });

                    _logger.LogInformation("✅ 附近城市生成完成: TaskId={TaskId}", taskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 处理附近城市生成任务失败: TaskId={TaskId}", taskId);

                    taskStatus.Status = "failed";
                    taskStatus.Error = ex.Message;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    await publishEndpoint.Publish(new AITaskFailedMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        TaskType = "nearby-cities",
                        ErrorMessage = ex.Message,
                        ErrorCode = "GENERATION_FAILED",
                        StackTrace = ex.StackTrace,
                        FailedAt = DateTime.UtcNow
                    });
                }
            });

            _logger.LogInformation("✅ 附近城市任务已创建: {TaskId}, UserId: {UserId}", taskId, userId);

            return Ok(new ApiResponse<CreateTaskResponse>
            {
                Success = true,
                Message = "任务创建成功",
                Data = new CreateTaskResponse
                {
                    TaskId = taskId,
                    Status = "queued",
                    EstimatedTimeSeconds = 60,
                    Message = "附近城市生成任务已创建,正在处理中。请通过 SignalR 连接 MessageService 接收实时进度。"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建附近城市任务失败");
            return StatusCode(500, new ApiResponse<CreateTaskResponse>
            {
                Success = false,
                Message = $"创建任务失败: {ex.Message}"
            });
        }
    }

    #endregion

    #region 图片生成 API (通义万象)

    /// <summary>
    ///     生成图片（通义万象）并上传到 Supabase Storage
    /// </summary>
    /// <remarks>
    ///     使用通义万象 wanx-v1 模型生成图片，自动下载并上传到 Supabase Storage。
    ///     支持多种风格：摄影、人像写真、3D卡通、动画、油画、水彩、素描、中国画、扁平插画等。
    /// </remarks>
    /// <param name="request">图片生成请求</param>
    /// <returns>生成的图片信息，包含 Supabase Storage 公开访问 URL</returns>
    [AllowAnonymous]
    [HttpPost("images/generate")]
    public async Task<ActionResult<ApiResponse<GenerateImageResponse>>> GenerateImage(
        [FromBody] GenerateImageRequest request,
        [FromServices] IImageGenerationService imageService)
    {
        try
        {
            // 不需要验证用户，使用系统用户ID
            var userId = Guid.Empty;

            _logger.LogInformation("收到图片生成请求，提示词: {Prompt}", request.Prompt);

            var result = await imageService.GenerateImageAsync(request, userId);

            if (!result.Success)
                return BadRequest(new ApiResponse<GenerateImageResponse>
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "图片生成失败",
                    Data = result
                });

            return Ok(new ApiResponse<GenerateImageResponse>
            {
                Success = true,
                Message = $"成功生成 {result.Images.Count} 张图片",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成图片失败");
            return StatusCode(500, new ApiResponse<GenerateImageResponse>
            {
                Success = false,
                Message = $"生成图片失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     批量生成城市图片（1张竖屏封面 + 4张横屏）
    /// </summary>
    /// <remarks>
    ///     为指定城市生成一组图片：
    ///     - 1张竖屏封面图 (720*1280)，存储路径：portrait/{cityId}/
    ///     - 4张横屏图片 (1280*720)，存储路径：landscape/{cityId}/
    /// </remarks>
    /// <param name="request">城市图片生成请求</param>
    /// <returns>生成的图片信息</returns>
    [AllowAnonymous]
    [HttpPost("images/city")]
    public async Task<ActionResult<ApiResponse<GenerateCityImagesResponse>>> GenerateCityImages(
        [FromBody] GenerateCityImagesRequest request,
        [FromServices] IImageGenerationService imageService)
    {
        try
        {
            _logger.LogInformation("收到城市图片批量生成请求，城市: {CityName} ({CityId})", request.CityName, request.CityId);

            var result = await imageService.GenerateCityImagesAsync(request);

            if (!result.Success)
                return BadRequest(new ApiResponse<GenerateCityImagesResponse>
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "城市图片生成失败",
                    Data = result
                });

            return Ok(new ApiResponse<GenerateCityImagesResponse>
            {
                Success = true,
                Message = $"成功生成城市图片：竖屏 {(result.PortraitImage != null ? 1 : 0)} 张，横屏 {result.LandscapeImages.Count} 张",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "城市图片批量生成失败，城市: {CityId}", request.CityId);
            return StatusCode(500, new ApiResponse<GenerateCityImagesResponse>
            {
                Success = false,
                Message = $"生成城市图片失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     异步生成城市图片（后台处理）
    /// </summary>
    /// <remarks>
    ///     为指定城市异步生成一组图片，立即返回任务ID。
    ///     生成完成后通过 MassTransit 发送消息通知 CityService 和 MessageService。
    ///     - 1张竖屏封面图 (720*1280)，存储路径：portrait/{cityId}/
    ///     - 4张横屏图片 (1280*720)，存储路径：landscape/{cityId}/
    /// </remarks>
    /// <param name="request">城市图片生成请求</param>
    /// <returns>任务创建结果</returns>
    [AllowAnonymous]
    [HttpPost("images/city/async")]
    public async Task<ActionResult<ApiResponse<CreateTaskResponse>>> GenerateCityImagesAsync(
        [FromBody] GenerateCityImagesRequest request,
        [FromServices] IImageGenerationService imageService,
        [FromServices] IPublishEndpoint publishEndpoint,
        [FromServices] IRedisCache cache)
    {
        try
        {
            _logger.LogInformation("📥 收到异步城市图片生成请求: CityId={CityId}, CityName={CityName}",
                request.CityId, request.CityName);

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("⚠️ 请求验证失败: {Errors}", errors);
                return BadRequest(new ApiResponse<CreateTaskResponse>
                {
                    Success = false,
                    Message = $"请求验证失败: {errors}"
                });
            }

            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                // 优先从请求体获取 userId（服务端调用会传递此参数）
                if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var requestUserId))
                {
                    userId = requestUserId;
                    _logger.LogInformation("📥 使用请求体中的 UserId: {UserId}", userId);
                }
                else
                {
                    // 测试用：使用固定的测试用户ID
                    userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                    _logger.LogWarning("⚠️ 用户未认证且未提供 UserId，使用测试用户ID: {UserId}", userId);
                }
            }

            // 生成任务ID
            var taskId = Guid.NewGuid().ToString("N");
            var startTime = DateTime.UtcNow;

            // 初始化任务状态
            var taskStatus = new TaskStatus
            {
                TaskId = taskId,
                Status = "queued",
                Progress = 0,
                ProgressMessage = "图片生成任务已创建，正在处理...",
                CreatedAt = startTime,
                UpdatedAt = startTime
            };

            // 保存到 Redis (24小时过期)
            await cache.SetAsync($"task:image:{taskId}", taskStatus, TimeSpan.FromHours(24));

            // 在后台线程中处理任务
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("🚀 开始处理城市图片生成任务: TaskId={TaskId}, CityId={CityId}",
                        taskId, request.CityId);

                    // 发送开始处理的进度消息
                    await publishEndpoint.Publish(new AIProgressMessage
                    {
                        TaskId = taskId,
                        UserId = userId.ToString(),
                        Progress = 10,
                        Message = "正在生成城市图片...",
                        TaskType = "city-image",
                        CurrentStage = "generating",
                        Status = "processing",
                        Timestamp = DateTime.UtcNow
                    });

                    // 调用图片生成服务
                    var result = await imageService.GenerateCityImagesAsync(request);

                    // 更新任务状态
                    taskStatus.Status = result.Success ? "completed" : "failed";
                    taskStatus.Progress = 100;
                    taskStatus.Result = result;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:image:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    // 发送城市图片生成完成消息
                    await publishEndpoint.Publish(new CityImageGeneratedMessage
                    {
                        TaskId = taskId,
                        CityId = request.CityId,
                        CityName = request.CityName,
                        UserId = userId.ToString(),
                        PortraitImageUrl = result.PortraitImage?.Url,
                        LandscapeImageUrls = result.LandscapeImages?.Select(img => img.Url).ToList(),
                        Success = result.Success,
                        ErrorMessage = result.ErrorMessage,
                        CompletedAt = DateTime.UtcNow,
                        DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds
                    });

                    _logger.LogInformation("✅ 城市图片生成完成: TaskId={TaskId}, CityId={CityId}, Success={Success}",
                        taskId, request.CityId, result.Success);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 处理城市图片生成任务失败: TaskId={TaskId}, CityId={CityId}",
                        taskId, request.CityId);

                    // 更新任务状态为失败
                    taskStatus.Status = "failed";
                    taskStatus.Error = ex.Message;
                    taskStatus.CompletedAt = DateTime.UtcNow;
                    await cache.SetAsync($"task:image:{taskId}", taskStatus, TimeSpan.FromHours(24));

                    // 发送失败消息
                    await publishEndpoint.Publish(new CityImageGeneratedMessage
                    {
                        TaskId = taskId,
                        CityId = request.CityId,
                        CityName = request.CityName,
                        UserId = userId.ToString(),
                        Success = false,
                        ErrorMessage = ex.Message,
                        CompletedAt = DateTime.UtcNow,
                        DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds
                    });
                }
            });

            _logger.LogInformation("✅ 城市图片生成任务已创建: TaskId={TaskId}, CityId={CityId}",
                taskId, request.CityId);

            return Ok(new ApiResponse<CreateTaskResponse>
            {
                Success = true,
                Message = "图片生成任务已创建",
                Data = new CreateTaskResponse
                {
                    TaskId = taskId,
                    Status = "queued",
                    EstimatedTimeSeconds = 180, // 预计3分钟
                    Message = "图片生成任务已创建，生成完成后将通过 SignalR 通知。"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建城市图片生成任务失败: CityId={CityId}", request.CityId);
            return StatusCode(500, new ApiResponse<CreateTaskResponse>
            {
                Success = false,
                Message = $"创建任务失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    ///     查询图片生成任务状态
    /// </summary>
    /// <param name="taskId">通义万象任务ID</param>
    /// <returns>任务状态和结果</returns>
    [AllowAnonymous]
    [HttpGet("images/tasks/{taskId}")]
    public async Task<ActionResult<ApiResponse<ImageTaskStatusResponse>>> GetImageTaskStatus(
        string taskId,
        [FromServices] IImageGenerationService imageService)
    {
        try
        {
            var result = await imageService.GetTaskStatusAsync(taskId);

            return Ok(new ApiResponse<ImageTaskStatusResponse>
            {
                Success = true,
                Message = $"任务状态: {result.Status}",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询图片任务状态失败，TaskId: {TaskId}", taskId);
            return StatusCode(500, new ApiResponse<ImageTaskStatusResponse>
            {
                Success = false,
                Message = $"查询任务状态失败: {ex.Message}"
            });
        }
    }

    #endregion
}
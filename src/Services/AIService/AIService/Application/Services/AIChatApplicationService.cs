using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AIService.Application.DTOs;
using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AIService.Application.Services;

/// <summary>
/// AI 聊天应用服务实现
/// </summary>
public class AIChatApplicationService : IAIChatService
{
    private readonly IAIConversationRepository _conversationRepository;
    private readonly IAIMessageRepository _messageRepository;
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<AIChatApplicationService> _logger;

    public AIChatApplicationService(
        IAIConversationRepository conversationRepository,
        IAIMessageRepository messageRepository,
        Kernel kernel,
        ILogger<AIChatApplicationService> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _kernel = kernel;
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
    }

    public async Task<ConversationResponse> CreateConversationAsync(CreateConversationRequest request, Guid userId)
    {
        try
        {
            _logger.LogInformation("创建新对话，用户ID: {UserId}, 标题: {Title}", userId, request.Title);

            var conversation = AIConversation.Create(
                userId, 
                request.Title, 
                request.SystemPrompt, 
                request.ModelName);

            var createdConversation = await _conversationRepository.CreateAsync(conversation);

            // 如果有系统提示，创建系统消息
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                var systemMessage = AIMessage.CreateSystemMessage(createdConversation.Id, request.SystemPrompt);
                await _messageRepository.CreateAsync(systemMessage);
            }

            _logger.LogInformation("✅ 成功创建对话，ID: {ConversationId}", createdConversation.Id);

            return MapToConversationResponse(createdConversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建对话失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<PagedResponse<ConversationResponse>> GetConversationsAsync(GetConversationsRequest request, Guid userId)
    {
        try
        {
            var (conversations, total) = await _conversationRepository.GetByUserIdAsync(
                userId, 
                request.Status, 
                request.Page, 
                request.PageSize);

            var conversationResponses = conversations.Select(MapToConversationResponse).ToList();

            return new PagedResponse<ConversationResponse>
            {
                Data = conversationResponses,
                Total = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取对话列表失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<ConversationResponse> GetConversationAsync(Guid conversationId, Guid userId)
    {
        var conversation = await GetConversationWithPermissionCheck(conversationId, userId);
        return MapToConversationResponse(conversation);
    }

    public async Task<ConversationResponse> UpdateConversationAsync(Guid conversationId, UpdateConversationRequest request, Guid userId)
    {
        try
        {
            var conversation = await GetConversationWithPermissionCheck(conversationId, userId);

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                conversation.UpdateTitle(request.Title);
            }

            var updatedConversation = await _conversationRepository.UpdateAsync(conversation);
            
            _logger.LogInformation("✅ 成功更新对话，ID: {ConversationId}", conversationId);

            return MapToConversationResponse(updatedConversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新对话失败，ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task DeleteConversationAsync(Guid conversationId, Guid userId)
    {
        try
        {
            var conversation = await GetConversationWithPermissionCheck(conversationId, userId);
            
            conversation.Delete();
            await _conversationRepository.UpdateAsync(conversation);

            // 删除相关消息
            await _messageRepository.DeleteByConversationIdAsync(conversationId);

            _logger.LogInformation("✅ 成功删除对话，ID: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除对话失败，ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<ConversationResponse> ArchiveConversationAsync(Guid conversationId, Guid userId)
    {
        try
        {
            var conversation = await GetConversationWithPermissionCheck(conversationId, userId);
            
            conversation.Archive();
            var updatedConversation = await _conversationRepository.UpdateAsync(conversation);

            _logger.LogInformation("✅ 成功归档对话，ID: {ConversationId}", conversationId);

            return MapToConversationResponse(updatedConversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 归档对话失败，ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<ConversationResponse> ActivateConversationAsync(Guid conversationId, Guid userId)
    {
        try
        {
            var conversation = await GetConversationWithPermissionCheck(conversationId, userId);
            
            conversation.Activate();
            var updatedConversation = await _conversationRepository.UpdateAsync(conversation);

            _logger.LogInformation("✅ 成功激活对话，ID: {ConversationId}", conversationId);

            return MapToConversationResponse(updatedConversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 激活对话失败，ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<ChatResponse> SendMessageAsync(Guid conversationId, SendMessageRequest request, Guid userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("发送消息到对话，ID: {ConversationId}, 用户ID: {UserId}", conversationId, userId);

            var conversation = await GetConversationWithPermissionCheck(conversationId, userId);

            if (!conversation.CanAddMessage())
            {
                throw new InvalidOperationException("当前对话状态不允许添加消息");
            }

            // 创建用户消息
            var userMessage = AIMessage.CreateUserMessage(conversationId, request.Content);
            var savedUserMessage = await _messageRepository.CreateAsync(userMessage);

            // 获取上下文消息
            var contextMessages = await _messageRepository.GetContextMessagesAsync(conversationId, 20);
            
            // 构建 ChatHistory
            var chatHistory = new ChatHistory();
            
            foreach (var msg in contextMessages.OrderBy(m => m.CreatedAt))
            {
                if (msg.IsSystemMessage())
                {
                    chatHistory.AddSystemMessage(msg.Content);
                }
                else if (msg.IsUserMessage())
                {
                    chatHistory.AddUserMessage(msg.Content);
                }
                else if (msg.IsAssistantMessage() && !msg.IsError)
                {
                    chatHistory.AddAssistantMessage(msg.Content);
                }
            }

            // 配置执行设置
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                ModelId = request.ModelName ?? conversation.ModelName
            };

            // 调用千问大模型
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory, 
                executionSettings, 
                _kernel);

            stopwatch.Stop();

            // 创建助手消息
            var assistantMessage = AIMessage.CreateAssistantMessage(
                conversationId,
                response.Content ?? "",
                executionSettings.ModelId,
                response.Metadata?.TryGetValue("Usage.PromptTokens", out var promptTokens) == true ? (int?)promptTokens : null,
                response.Metadata?.TryGetValue("Usage.CompletionTokens", out var completionTokens) == true ? (int?)completionTokens : null,
                (int)stopwatch.ElapsedMilliseconds);

            var savedAssistantMessage = await _messageRepository.CreateAsync(assistantMessage);

            // 更新对话统计
            var totalTokens = (assistantMessage.PromptTokens ?? 0) + (assistantMessage.CompletionTokens ?? 0);
            conversation.AddMessage(totalTokens);
            await _conversationRepository.UpdateAsync(conversation);

            _logger.LogInformation("✅ 成功处理消息，对话ID: {ConversationId}, 响应时间: {ResponseTime}ms", 
                conversationId, stopwatch.ElapsedMilliseconds);

            return new ChatResponse
            {
                Content = response.Content ?? "",
                Role = "assistant",
                ModelName = executionSettings.ModelId,
                PromptTokens = assistantMessage.PromptTokens,
                CompletionTokens = assistantMessage.CompletionTokens,
                TotalTokens = assistantMessage.TotalTokens,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                FinishReason = response.Metadata?.TryGetValue("FinishReason", out var finishReason) == true ? finishReason?.ToString() : null,
                UserMessage = MapToMessageResponse(savedUserMessage),
                AssistantMessage = MapToMessageResponse(savedAssistantMessage)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ 处理消息失败，对话ID: {ConversationId}", conversationId);

            // 创建错误消息
            var errorMessage = AIMessage.CreateErrorMessage(conversationId, ex.Message, request.Content);
            await _messageRepository.CreateAsync(errorMessage);

            return new ChatResponse
            {
                Content = "抱歉，处理您的请求时发生了错误。请稍后重试。",
                Role = "assistant",
                IsError = true,
                ErrorMessage = ex.Message,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async IAsyncEnumerable<StreamResponse> SendMessageStreamAsync(
        Guid conversationId, 
        SendMessageRequest request, 
        Guid userId)
    {
        // 流式实现（简化版本，实际需要根据千问API的流式支持）
        var response = await SendMessageAsync(conversationId, request, userId);
        
        if (response.IsError)
        {
            yield return new StreamResponse
            {
                Delta = response.Content,
                IsComplete = true,
                Error = response.ErrorMessage
            };
            yield break;
        }

        // 模拟流式响应（实际实现需要真正的流式API）
        var words = response.Content.Split(' ');
        foreach (var word in words)
        {
            yield return new StreamResponse
            {
                Delta = word + " ",
                IsComplete = false
            };
            
            await Task.Delay(50); // 模拟延迟
        }

        yield return new StreamResponse
        {
            Delta = "",
            IsComplete = true,
            FinishReason = response.FinishReason,
            TokenCount = response.TotalTokens
        };
    }

    public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid conversationId, GetMessagesRequest request, Guid userId)
    {
        try
        {
            await GetConversationWithPermissionCheck(conversationId, userId);

            var messages = await _messageRepository.GetByConversationIdAsync(
                conversationId, 
                request.Page, 
                request.PageSize, 
                request.IncludeSystem);

            var messageResponses = messages.Select(MapToMessageResponse).ToList();

            // 获取总数（简化实现）
            var (_, totalTokens) = await _messageRepository.GetConversationStatsAsync(conversationId);

            return new PagedResponse<MessageResponse>
            {
                Data = messageResponses,
                Total = messages.Count,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取消息列表失败，对话ID: {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task<UserStatsResponse> GetUserStatsAsync(Guid userId)
    {
        try
        {
            var (totalConversations, activeConversations, totalMessages) = 
                await _conversationRepository.GetUserStatsAsync(userId);

            return new UserStatsResponse
            {
                TotalConversations = totalConversations,
                ActiveConversations = activeConversations,
                TotalMessages = totalMessages,
                LastActivityAt = DateTime.UtcNow // 简化实现
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户统计失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            // 测试千问API连接
            var testMessage = new ChatHistory();
            testMessage.AddUserMessage("Hello");

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.1,
                MaxTokens = 10
            };

            await _chatCompletionService.GetChatMessageContentAsync(testMessage, executionSettings, _kernel);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ AI服务健康检查失败");
            return false;
        }
    }

    // 私有辅助方法

    private async Task<AIConversation> GetConversationWithPermissionCheck(Guid conversationId, Guid userId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        
        if (conversation == null)
        {
            throw new ArgumentException($"对话不存在，ID: {conversationId}");
        }

        if (conversation.UserId != userId)
        {
            throw new UnauthorizedAccessException("无权限访问此对话");
        }

        return conversation;
    }

    private static ConversationResponse MapToConversationResponse(AIConversation conversation)
    {
        return new ConversationResponse
        {
            Id = conversation.Id,
            Title = conversation.Title,
            UserId = conversation.UserId,
            Status = conversation.Status,
            ModelName = conversation.ModelName,
            SystemPrompt = conversation.SystemPrompt,
            TotalMessages = conversation.TotalMessages,
            TotalTokens = conversation.TotalTokens,
            LastMessageAt = conversation.LastMessageAt,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    private static MessageResponse MapToMessageResponse(AIMessage message)
    {
        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Role = message.Role,
            Content = message.Content,
            TokenCount = message.TokenCount,
            ModelName = message.ModelName,
            PromptTokens = message.PromptTokens,
            CompletionTokens = message.CompletionTokens,
            TotalTokens = message.TotalTokens,
            ResponseTimeMs = message.ResponseTimeMs,
            Metadata = message.Metadata,
            ErrorMessage = message.ErrorMessage,
            IsError = message.IsError,
            CreatedAt = message.CreatedAt
        };
    }
}
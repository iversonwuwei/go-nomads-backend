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

            // 调用 DeepSeek 大模型
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
            // 测试 DeepSeek API 连接
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

#pragma warning disable SKEXP0010 // ResponseFormat is experimental
    public async Task<TravelPlanResponse> GenerateTravelPlanAsync(GenerateTravelPlanRequest request, Guid userId)
    {
        try
        {
            _logger.LogInformation("🗺️ 开始生成旅行计划，城市: {CityName}, 用户ID: {UserId}", request.CityName, userId);

            // 构建 AI 提示词
            var prompt = BuildTravelPlanPrompt(request);
            
            _logger.LogDebug("AI 提示词: {Prompt}", prompt);

            // 创建聊天历史
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("你是一个专业的旅行规划助手，擅长根据用户需求制定详细的旅行计划。请以 JSON 格式返回旅行计划。");
            chatHistory.AddUserMessage(prompt);

            // 设置执行参数
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
                MaxTokens = 4000,
                ResponseFormat = "json_object"
            };

            var stopwatch = Stopwatch.StartNew();

            // 获取 AI 响应
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);

            stopwatch.Stop();

            _logger.LogInformation("✅ AI 响应完成，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            // 解析 JSON 响应
            var aiContent = response.Content ?? string.Empty;
            _logger.LogDebug("AI 响应内容: {Content}", aiContent);

            var travelPlan = ParseTravelPlanFromAI(aiContent, request);

            _logger.LogInformation("✅ 旅行计划生成成功，ID: {PlanId}", travelPlan.Id);

            return travelPlan;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ 解析 AI 响应 JSON 失败");
            throw new InvalidOperationException("AI 响应格式错误，无法生成旅行计划", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成旅行计划失败，城市: {CityName}", request.CityName);
            throw;
        }
    }
#pragma warning restore SKEXP0010

    private string BuildTravelPlanPrompt(GenerateTravelPlanRequest request)
    {
        var budgetDescription = request.Budget switch
        {
            "low" => "经济型预算（每天50-100美元）",
            "medium" => "中等预算（每天100-200美元）",
            "high" => "豪华预算（每天200美元以上）",
            _ => "中等预算"
        };

        var styleDescription = request.TravelStyle switch
        {
            "adventure" => "冒险探索，喜欢户外活动和刺激体验",
            "relaxation" => "休闲放松，注重舒适和享受",
            "culture" => "文化探索，关注历史和艺术",
            "nightlife" => "夜生活娱乐，喜欢酒吧和夜间活动",
            _ => "文化探索"
        };

        var interestsText = request.Interests.Any() 
            ? string.Join("、", request.Interests) 
            : "无特定偏好";

        var departureInfo = !string.IsNullOrWhiteSpace(request.DepartureLocation)
            ? $"从 {request.DepartureLocation} 出发，"
            : "";

        return $@"请为我制定一个详细的 {request.CityName} 旅行计划。

旅行信息：
- 目的地：{request.CityName}
- 旅行天数：{request.Duration} 天
- 预算等级：{budgetDescription}
- 旅行风格：{styleDescription}
- 兴趣偏好：{interestsText}
{(string.IsNullOrWhiteSpace(departureInfo) ? "" : $"- 出发地：{request.DepartureLocation}")}
{(request.CustomBudget != null ? $"- 自定义预算：{request.CustomBudget} {request.Currency}" : "")}

请以 JSON 格式返回完整的旅行计划，包含以下内容：

1. transportation（交通计划）：
   - arrivalMethod: 到达方式（飞机/火车/汽车）
   - arrivalDetails: 到达详情（航班推荐、车站信息等）
   - estimatedCost: 预估费用
   - localTransport: 当地交通方式
   - localTransportDetails: 当地交通详情
   - dailyTransportCost: 每日交通费用

2. accommodation（住宿计划）：
   - type: 住宿类型（hotel/hostel/apartment）
   - recommendation: 推荐说明
   - area: 推荐区域
   - pricePerNight: 每晚价格
   - amenities: 设施列表
   - bookingTips: 预订建议

3. dailyItineraries（每日行程）：数组，每天包含：
   - day: 第几天
   - theme: 当天主题
   - activities: 活动列表（时间、名称、描述、地点、费用、时长分钟）
   - notes: 注意事项

4. attractions（推荐景点）：数组，每个景点包含：
   - name: 景点名称
   - description: 描述
   - category: 类别
   - rating: 评分（1-5）
   - location: 位置
   - entryFee: 门票费用
   - bestTime: 最佳游览时间
   - image: 图片URL（可以是占位符）

5. restaurants（推荐餐厅）：数组，每个餐厅包含：
   - name: 餐厅名称
   - cuisine: 菜系
   - description: 描述
   - rating: 评分（1-5）
   - priceRange: 价格区间（$/$$/$$$/$$$$）
   - location: 位置
   - specialty: 招牌菜
   - image: 图片URL（可以是占位符）

6. tips（旅行建议）：字符串数组，包含实用建议

7. budgetBreakdown（预算明细）：
   - transportation: 交通费用
   - accommodation: 住宿费用
   - food: 餐饮费用
   - activities: 活动费用
   - miscellaneous: 其他费用
   - total: 总费用
   - currency: 货币单位

请确保返回的是有效的 JSON 格式，所有数字字段使用数字类型，不要使用字符串。";
    }

    private TravelPlanResponse ParseTravelPlanFromAI(string aiContent, GenerateTravelPlanRequest request)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonDoc = JsonDocument.Parse(aiContent);
            var root = jsonDoc.RootElement;

            return new TravelPlanResponse
            {
                Id = Guid.NewGuid().ToString(),
                CityId = request.CityId,
                CityName = request.CityName,
                CityImage = request.CityImage ?? "",
                CreatedAt = DateTime.UtcNow,
                Duration = request.Duration,
                Budget = request.Budget,
                TravelStyle = request.TravelStyle,
                Interests = request.Interests,
                Transportation = ParseTransportation(root.GetProperty("transportation")),
                Accommodation = ParseAccommodation(root.GetProperty("accommodation")),
                DailyItineraries = ParseDailyItineraries(root.GetProperty("dailyItineraries")),
                Attractions = ParseAttractions(root.GetProperty("attractions")),
                Restaurants = ParseRestaurants(root.GetProperty("restaurants")),
                Tips = ParseStringArray(root.GetProperty("tips")),
                BudgetBreakdown = ParseBudgetBreakdown(root.GetProperty("budgetBreakdown"))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 解析旅行计划 JSON 失败: {Content}", aiContent);
            throw new JsonException("无法解析 AI 生成的旅行计划", ex);
        }
    }

    private TransportationPlanDto ParseTransportation(JsonElement element)
    {
        return new TransportationPlanDto
        {
            ArrivalMethod = element.GetProperty("arrivalMethod").GetString() ?? "",
            ArrivalDetails = element.GetProperty("arrivalDetails").GetString() ?? "",
            EstimatedCost = element.GetProperty("estimatedCost").GetDouble(),
            LocalTransport = element.GetProperty("localTransport").GetString() ?? "",
            LocalTransportDetails = element.GetProperty("localTransportDetails").GetString() ?? "",
            DailyTransportCost = element.GetProperty("dailyTransportCost").GetDouble()
        };
    }

    private AccommodationPlanDto ParseAccommodation(JsonElement element)
    {
        return new AccommodationPlanDto
        {
            Type = element.GetProperty("type").GetString() ?? "",
            Recommendation = element.GetProperty("recommendation").GetString() ?? "",
            Area = element.GetProperty("area").GetString() ?? "",
            PricePerNight = element.GetProperty("pricePerNight").GetDouble(),
            Amenities = ParseStringArray(element.GetProperty("amenities")),
            BookingTips = element.GetProperty("bookingTips").GetString() ?? ""
        };
    }

    private List<DailyItineraryDto> ParseDailyItineraries(JsonElement element)
    {
        var itineraries = new List<DailyItineraryDto>();
        foreach (var item in element.EnumerateArray())
        {
            itineraries.Add(new DailyItineraryDto
            {
                Day = item.GetProperty("day").GetInt32(),
                Theme = item.GetProperty("theme").GetString() ?? "",
                Activities = ParseActivities(item.GetProperty("activities")),
                Notes = item.GetProperty("notes").GetString() ?? ""
            });
        }
        return itineraries;
    }

    private List<ActivityDto> ParseActivities(JsonElement element)
    {
        var activities = new List<ActivityDto>();
        foreach (var item in element.EnumerateArray())
        {
            activities.Add(new ActivityDto
            {
                Time = item.GetProperty("time").GetString() ?? "",
                Name = item.GetProperty("name").GetString() ?? "",
                Description = item.GetProperty("description").GetString() ?? "",
                Location = item.GetProperty("location").GetString() ?? "",
                EstimatedCost = item.GetProperty("estimatedCost").GetDouble(),
                Duration = item.GetProperty("duration").GetInt32()
            });
        }
        return activities;
    }

    private List<AttractionDto> ParseAttractions(JsonElement element)
    {
        var attractions = new List<AttractionDto>();
        foreach (var item in element.EnumerateArray())
        {
            attractions.Add(new AttractionDto
            {
                Name = item.GetProperty("name").GetString() ?? "",
                Description = item.GetProperty("description").GetString() ?? "",
                Category = item.GetProperty("category").GetString() ?? "",
                Rating = item.GetProperty("rating").GetDouble(),
                Location = item.GetProperty("location").GetString() ?? "",
                EntryFee = item.GetProperty("entryFee").GetDouble(),
                BestTime = item.GetProperty("bestTime").GetString() ?? "",
                Image = item.GetProperty("image").GetString() ?? ""
            });
        }
        return attractions;
    }

    private List<RestaurantDto> ParseRestaurants(JsonElement element)
    {
        var restaurants = new List<RestaurantDto>();
        foreach (var item in element.EnumerateArray())
        {
            restaurants.Add(new RestaurantDto
            {
                Name = item.GetProperty("name").GetString() ?? "",
                Cuisine = item.GetProperty("cuisine").GetString() ?? "",
                Description = item.GetProperty("description").GetString() ?? "",
                Rating = item.GetProperty("rating").GetDouble(),
                PriceRange = item.GetProperty("priceRange").GetString() ?? "",
                Location = item.GetProperty("location").GetString() ?? "",
                Specialty = item.GetProperty("specialty").GetString() ?? "",
                Image = item.GetProperty("image").GetString() ?? ""
            });
        }
        return restaurants;
    }

    private BudgetBreakdownDto ParseBudgetBreakdown(JsonElement element)
    {
        return new BudgetBreakdownDto
        {
            Transportation = element.GetProperty("transportation").GetDouble(),
            Accommodation = element.GetProperty("accommodation").GetDouble(),
            Food = element.GetProperty("food").GetDouble(),
            Activities = element.GetProperty("activities").GetDouble(),
            Miscellaneous = element.GetProperty("miscellaneous").GetDouble(),
            Total = element.GetProperty("total").GetDouble(),
            Currency = element.TryGetProperty("currency", out var currency) ? currency.GetString() ?? "USD" : "USD"
        };
    }

    private List<string> ParseStringArray(JsonElement element)
    {
        var result = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Add(value);
            }
        }
        return result;
    }
}
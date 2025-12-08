using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AIService.Application.DTOs;
using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AIService.Application.Services;

/// <summary>
///     AI 聊天应用服务实现
/// </summary>
public class AIChatApplicationService : IAIChatService
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IConfiguration _configuration;
    private readonly IAIConversationRepository _conversationRepository;
    private readonly Kernel _kernel;
    private readonly ILogger<AIChatApplicationService> _logger;
    private readonly IAIMessageRepository _messageRepository;

    public AIChatApplicationService(
        IAIConversationRepository conversationRepository,
        IAIMessageRepository messageRepository,
        Kernel kernel,
        ILogger<AIChatApplicationService> logger,
        IConfiguration configuration)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _kernel = kernel;
        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
        _configuration = configuration;
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

    public async Task<PagedResponse<ConversationResponse>> GetConversationsAsync(GetConversationsRequest request,
        Guid userId)
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

    public async Task<ConversationResponse> UpdateConversationAsync(Guid conversationId,
        UpdateConversationRequest request, Guid userId)
    {
        try
        {
            var conversation = await GetConversationWithPermissionCheck(conversationId, userId);

            if (!string.IsNullOrWhiteSpace(request.Title)) conversation.UpdateTitle(request.Title);

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

            if (!conversation.CanAddMessage()) throw new InvalidOperationException("当前对话状态不允许添加消息");

            // 创建用户消息
            var userMessage = AIMessage.CreateUserMessage(conversationId, request.Content);
            var savedUserMessage = await _messageRepository.CreateAsync(userMessage);

            // 获取上下文消息
            var contextMessages = await _messageRepository.GetContextMessagesAsync(conversationId);

            // 构建 ChatHistory
            var chatHistory = new ChatHistory();

            foreach (var msg in contextMessages.OrderBy(m => m.CreatedAt))
                if (msg.IsSystemMessage())
                    chatHistory.AddSystemMessage(msg.Content);
                else if (msg.IsUserMessage())
                    chatHistory.AddUserMessage(msg.Content);
                else if (msg.IsAssistantMessage() && !msg.IsError) chatHistory.AddAssistantMessage(msg.Content);

            // 配置执行设置
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                ModelId = request.ModelName ?? conversation.ModelName
            };

            // 调用 Qwen 大模型
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
                response.Metadata?.TryGetValue("Usage.PromptTokens", out var promptTokens) == true
                    ? (int?)promptTokens
                    : null,
                response.Metadata?.TryGetValue("Usage.CompletionTokens", out var completionTokens) == true
                    ? (int?)completionTokens
                    : null,
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
                FinishReason = response.Metadata?.TryGetValue("FinishReason", out var finishReason) == true
                    ? finishReason?.ToString()
                    : null,
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

    public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid conversationId, GetMessagesRequest request,
        Guid userId)
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
            // 测试 Qwen API 连接
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

#pragma warning disable SKEXP0010 // ResponseFormat is experimental
    public async Task<TravelPlanResponse> GenerateTravelPlanAsync(
        GenerateTravelPlanRequest request,
        Guid userId,
        Func<int, string, Task>? onProgress = null)
    {
        try
        {
            _logger.LogInformation("� 开始分段生成旅行计划 - 城市: {CityName}, 天数: {Duration}, 用户ID: {UserId}",
                request.CityName, request.Duration, userId);

            var planId = Guid.NewGuid().ToString();

            // 第一步：生成基础信息（交通和住宿）- 15-25%
            _logger.LogInformation("📍 步骤 1/4: 生成交通和住宿计划...");
            if (onProgress != null) await onProgress(15, "正在生成交通和住宿方案...");
            var (transportation, accommodation) = await GenerateBasicInfoAsync(request);
            if (onProgress != null) await onProgress(25, "交通和住宿方案生成完成");

            // 第二步：生成每日行程 - 25-60%
            _logger.LogInformation("📍 步骤 2/4: 生成每日行程...");
            if (onProgress != null) await onProgress(30, $"正在规划 {request.Duration} 天的详细行程...");
            var dailyItineraries = await GenerateDailyItinerariesAsync(request, async (day, totalDays) =>
            {
                // 每天的进度：30% + (day / totalDays * 30%)
                var dayProgress = 30 + (int)((double)day / totalDays * 30);
                if (onProgress != null)
                    await onProgress(dayProgress, $"正在规划第 {day}/{totalDays} 天的行程...");
            });
            if (onProgress != null) await onProgress(60, "每日行程规划完成");

            // 第三步：生成景点和餐厅推荐 - 60-75%
            _logger.LogInformation("📍 步骤 3/4: 生成景点和餐厅推荐...");
            if (onProgress != null) await onProgress(65, "正在推荐必游景点和美食...");
            var (attractions, restaurants) = await GenerateAttractionsAndRestaurantsAsync(request);
            if (onProgress != null) await onProgress(75, "景点和餐厅推荐完成");

            // 第四步：生成预算和建议 - 75-85%
            _logger.LogInformation("📍 步骤 4/4: 生成预算明细和旅行建议...");
            if (onProgress != null) await onProgress(80, "正在计算预算和准备旅行贴士...");
            var (budgetBreakdown, tips) =
                await GenerateBudgetAndTipsAsync(request, transportation, accommodation, dailyItineraries);
            if (onProgress != null) await onProgress(85, "预算和建议生成完成");

            var travelPlan = new TravelPlanResponse
            {
                Id = planId,
                CityId = request.CityId,
                CityName = request.CityName,
                CityImage = request.CityImage ?? "",
                CreatedAt = DateTime.UtcNow,
                Duration = request.Duration,
                Budget = request.Budget,
                TravelStyle = request.TravelStyle,
                Interests = request.Interests,
                Transportation = transportation,
                Accommodation = accommodation,
                DailyItineraries = dailyItineraries,
                Attractions = attractions,
                Restaurants = restaurants,
                Tips = tips,
                BudgetBreakdown = budgetBreakdown
            };

            _logger.LogInformation("✅ 旅行计划分段生成完成，ID: {PlanId}", planId);
            return travelPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成旅行计划失败，城市: {CityName}", request.CityName);
            throw;
        }
    }
#pragma warning restore SKEXP0010

    /// <summary>
    ///     生成数字游民旅游指南（拆分为多个小请求）
    /// </summary>
    public async Task<TravelGuideResponse> GenerateTravelGuideAsync(
        GenerateTravelGuideRequest request,
        Guid userId,
        Func<int, string, Task>? onProgress = null)
    {
        try
        {
            _logger.LogInformation("📖 开始生成数字游民旅游指南（拆分请求模式）- 城市: {CityName}, 用户ID: {UserId}",
                request.CityName, userId);

            // 初始化结果对象
            var guide = new TravelGuideResponse
            {
                CityId = request.CityId,
                CityName = request.CityName
            };

            // 第 1 部分: 概述 + 签证信息 (15% - 40%)
            if (onProgress != null) await onProgress(15, "正在生成城市概述和签证信息...");
            await GenerateBasicInfoAsync(request, guide, onProgress);

            // 第 2 部分: 推荐区域 (40% - 70%)
            if (onProgress != null) await onProgress(40, "正在分析推荐区域...");
            await GenerateBestAreasAsync(request, guide, onProgress);

            // 第 3 部分: 工作空间 + 实用建议 + 基本信息 (70% - 100%)
            if (onProgress != null) await onProgress(70, "正在整理工作空间和实用建议...");
            await GeneratePracticalInfoAsync(request, guide, onProgress);

            if (onProgress != null) await onProgress(100, "旅游指南生成完成!");

            _logger.LogInformation("✅ 数字游民旅游指南生成成功 - 城市: {CityName}", request.CityName);
            return guide;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成数字游民旅游指南失败，城市: {CityName}", request.CityName);
            throw;
        }
    }

    /// <summary>
    ///     生成附近城市信息
    /// </summary>
    public async Task<NearbyCitiesResponse> GenerateNearbyCitiesAsync(
        GenerateNearbyCitiesRequest request,
        Guid userId,
        Func<int, string, Task>? onProgress = null)
    {
        try
        {
            _logger.LogInformation("🌍 开始生成附近城市信息 - 城市: {CityName}, 半径: {Radius}km, 数量: {Count}",
                request.CityName, request.RadiusKm, request.Count);

            if (onProgress != null) await onProgress(10, "正在分析周边城市...");

            var response = new NearbyCitiesResponse
            {
                SourceCityId = request.CityId,
                SourceCityName = request.CityName
            };

            // 生成附近城市列表
            var cities = await GenerateNearbyCitiesListAsync(request, onProgress);
            response.Cities = cities;

            if (onProgress != null) await onProgress(100, "附近城市信息生成完成!");

            _logger.LogInformation("✅ 附近城市信息生成成功 - 城市: {CityName}, 找到 {Count} 个附近城市",
                request.CityName, cities.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 生成附近城市信息失败，城市: {CityName}", request.CityName);
            throw;
        }
    }

    /// <summary>
    ///     生成附近城市列表
    /// </summary>
    private async Task<List<NearbyCityItemResponse>> GenerateNearbyCitiesListAsync(
        GenerateNearbyCitiesRequest request,
        Func<int, string, Task>? onProgress)
    {
        var countryInfo = string.IsNullOrEmpty(request.Country) ? "" : $"（{request.Country}）";
        var prompt = $@"请为 {request.CityName}{countryInfo} 推荐 {request.Count} 个车程在 {request.RadiusKm} 公里范围内的相邻城市。

这些城市应该是适合数字游民短途旅行或周末游的目的地。

请以 JSON 格式返回：

{{
  ""cities"": [
    {{
      ""cityName"": ""城市名称（中英文皆可，如：苏州/Suzhou）"",
      ""country"": ""所属国家"",
      ""distanceKm"": 距离公里数（数字）,
      ""transportationType"": ""主要交通方式（train/bus/car之一）"",
      ""travelTimeMinutes"": 预计旅行时间分钟数（数字）,
      ""highlights"": [""亮点1"", ""亮点2"", ""亮点3""],
      ""nomadFeatures"": {{
        ""monthlyCostUsd"": 预计月生活成本美元（数字，可选）,
        ""internetSpeedMbps"": 网络速度Mbps（数字，可选）,
        ""coworkingSpaces"": 联合办公空间数量（数字，可选）,
        ""visaInfo"": ""签证便利性描述（可选）"",
        ""safetyScore"": 安全评分1-5（数字，可选）,
        ""qualityOfLife"": ""生活质量描述（可选）""
      }},
      ""latitude"": 纬度（数字，可选）,
      ""longitude"": 经度（数字，可选）,
      ""overallScore"": 综合评分1-5（数字）
    }}
  ]
}}

要求：
1. 只推荐真实存在的城市，距离和交通信息要准确
2. 优先推荐对数字游民友好的城市
3. 亮点要简洁有特色，3个左右
4. 必须返回严格的 JSON 格式
5. 【重要】绝对不能包含 {request.CityName} 本身，只返回周边其他城市";

        if (onProgress != null) await onProgress(30, "正在调用 AI 分析周边城市...");

        _logger.LogInformation("🤖 调用 AI 生成附近城市列表...");
        var aiResponse = await CallAIAsync(prompt, 2000);

        if (onProgress != null) await onProgress(70, "正在解析附近城市信息...");

        try
        {
            var jsonContent = ExtractJsonFromAIResponse(aiResponse);
            jsonContent = TryFixIncompleteJson(jsonContent);

            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            var cities = new List<NearbyCityItemResponse>();

            if (root.TryGetProperty("cities", out var citiesElement) && citiesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var cityElement in citiesElement.EnumerateArray())
                {
                    var city = new NearbyCityItemResponse
                    {
                        CityName = cityElement.TryGetProperty("cityName", out var name) ? name.GetString() ?? "" : "",
                        Country = cityElement.TryGetProperty("country", out var country) ? country.GetString() ?? "" : "",
                        DistanceKm = cityElement.TryGetProperty("distanceKm", out var distance) ? distance.GetDouble() : 0,
                        TransportationType = cityElement.TryGetProperty("transportationType", out var transport) ? transport.GetString() ?? "car" : "car",
                        TravelTimeMinutes = cityElement.TryGetProperty("travelTimeMinutes", out var time) ? time.GetInt32() : 0,
                        Latitude = cityElement.TryGetProperty("latitude", out var lat) && lat.ValueKind == JsonValueKind.Number ? lat.GetDouble() : null,
                        Longitude = cityElement.TryGetProperty("longitude", out var lng) && lng.ValueKind == JsonValueKind.Number ? lng.GetDouble() : null,
                        OverallScore = cityElement.TryGetProperty("overallScore", out var score) && score.ValueKind == JsonValueKind.Number ? score.GetDouble() : null
                    };

                    // 解析 highlights
                    if (cityElement.TryGetProperty("highlights", out var highlightsElement) && highlightsElement.ValueKind == JsonValueKind.Array)
                    {
                        city.Highlights = highlightsElement.EnumerateArray()
                            .Select(h => h.GetString() ?? "")
                            .Where(h => !string.IsNullOrEmpty(h))
                            .ToList();
                    }

                    // 解析 nomadFeatures
                    if (cityElement.TryGetProperty("nomadFeatures", out var featuresElement) && featuresElement.ValueKind == JsonValueKind.Object)
                    {
                        city.NomadFeatures = new NearbyCityNomadFeaturesResponse
                        {
                            MonthlyCostUsd = featuresElement.TryGetProperty("monthlyCostUsd", out var cost) && cost.ValueKind == JsonValueKind.Number ? cost.GetDouble() : null,
                            InternetSpeedMbps = featuresElement.TryGetProperty("internetSpeedMbps", out var speed) && speed.ValueKind == JsonValueKind.Number ? speed.GetInt32() : null,
                            CoworkingSpaces = featuresElement.TryGetProperty("coworkingSpaces", out var spaces) && spaces.ValueKind == JsonValueKind.Number ? spaces.GetInt32() : null,
                            VisaInfo = featuresElement.TryGetProperty("visaInfo", out var visa) ? visa.GetString() : null,
                            SafetyScore = featuresElement.TryGetProperty("safetyScore", out var safety) && safety.ValueKind == JsonValueKind.Number ? safety.GetDouble() : null,
                            QualityOfLife = featuresElement.TryGetProperty("qualityOfLife", out var quality) ? quality.GetString() : null
                        };
                    }

                    if (!string.IsNullOrEmpty(city.CityName))
                    {
                        // 过滤掉源城市本身（防止 AI 返回源城市）
                        var sourceCityName = request.CityName.ToLowerInvariant();
                        var targetCityName = city.CityName.ToLowerInvariant();

                        // 检查是否包含源城市名（考虑中英文格式如 "大同/Datong"）
                        var isSourceCity = targetCityName.Contains(sourceCityName) ||
                                           sourceCityName.Contains(targetCityName) ||
                                           targetCityName.Split('/').Any(n => sourceCityName.Split('/').Any(s =>
                                               n.Trim().Equals(s.Trim(), StringComparison.OrdinalIgnoreCase)));

                        if (isSourceCity)
                        {
                            _logger.LogWarning("⚠️ 过滤掉源城市: {CityName}", city.CityName);
                            continue;
                        }

                        cities.Add(city);
                    }
                }
            }

            _logger.LogInformation("✅ 成功解析 {Count} 个附近城市", cities.Count);
            return cities;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON 解析失败，原始响应: {Response}",
                aiResponse.Length > 500 ? aiResponse.Substring(0, 500) + "..." : aiResponse);

            // 返回空列表
            return new List<NearbyCityItemResponse>();
        }
    }

    // 私有辅助方法

    private async Task<AIConversation> GetConversationWithPermissionCheck(Guid conversationId, Guid userId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);

        if (conversation == null) throw new ArgumentException($"对话不存在，ID: {conversationId}");

        if (conversation.UserId != userId) throw new UnauthorizedAccessException("无权限访问此对话");

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

            // 提取 JSON 内容（处理可能被代码块包裹的情况）
            var jsonContent = ExtractJsonFromAIResponse(aiContent);
            _logger.LogInformation("🔍 提取的 JSON 内容: {JsonContent}", jsonContent);

            var jsonDoc = JsonDocument.Parse(jsonContent);
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
                Transportation = root.TryGetProperty("transportation", out var trans)
                    ? ParseTransportation(trans)
                    : new TransportationPlanDto(),
                Accommodation = root.TryGetProperty("accommodation", out var acc)
                    ? ParseAccommodation(acc)
                    : new AccommodationPlanDto(),
                DailyItineraries = root.TryGetProperty("dailyItineraries", out var daily)
                    ? ParseDailyItineraries(daily)
                    : new List<DailyItineraryDto>(),
                Attractions = root.TryGetProperty("attractions", out var attr)
                    ? ParseAttractions(attr)
                    : new List<AttractionDto>(),
                Restaurants = root.TryGetProperty("restaurants", out var rest)
                    ? ParseRestaurants(rest)
                    : new List<RestaurantDto>(),
                Tips = root.TryGetProperty("tips", out var tips) ? ParseStringArray(tips) : new List<string>(),
                BudgetBreakdown = root.TryGetProperty("budgetBreakdown", out var budget)
                    ? ParseBudgetBreakdown(budget)
                    : new BudgetBreakdownDto()
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
            ArrivalMethod = element.TryGetProperty("arrivalMethod", out var am) ? am.GetString() ?? "" : "",
            ArrivalDetails = element.TryGetProperty("arrivalDetails", out var ad) ? ad.GetString() ?? "" : "",
            EstimatedCost = element.TryGetProperty("estimatedCost", out var ec) ? ec.GetDouble() : 0,
            LocalTransport = element.TryGetProperty("localTransport", out var lt) ? lt.GetString() ?? "" : "",
            LocalTransportDetails =
                element.TryGetProperty("localTransportDetails", out var ltd) ? ltd.GetString() ?? "" : "",
            DailyTransportCost = element.TryGetProperty("dailyTransportCost", out var dtc) ? dtc.GetDouble() : 0
        };
    }

    private AccommodationPlanDto ParseAccommodation(JsonElement element)
    {
        return new AccommodationPlanDto
        {
            Type = element.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
            Recommendation = element.TryGetProperty("recommendation", out var r) ? r.GetString() ?? "" : "",
            Area = element.TryGetProperty("area", out var a) ? a.GetString() ?? "" : "",
            PricePerNight = element.TryGetProperty("pricePerNight", out var ppn) ? ppn.GetDouble() : 0,
            Amenities = element.TryGetProperty("amenities", out var amenities)
                ? ParseStringArray(amenities)
                : new List<string>(),
            BookingTips = element.TryGetProperty("bookingTips", out var bt) ? bt.GetString() ?? "" : ""
        };
    }

    private List<DailyItineraryDto> ParseDailyItineraries(JsonElement element)
    {
        var itineraries = new List<DailyItineraryDto>();
        foreach (var item in element.EnumerateArray())
            itineraries.Add(new DailyItineraryDto
            {
                Day = item.TryGetProperty("day", out var day) ? day.GetInt32() : 0,
                Theme = item.TryGetProperty("theme", out var theme) ? theme.GetString() ?? "" : "",
                Activities = item.TryGetProperty("activities", out var acts)
                    ? ParseActivities(acts)
                    : new List<ActivityDto>(),
                Notes = item.TryGetProperty("notes", out var notes) ? notes.GetString() ?? "" : ""
            });
        return itineraries;
    }

    private List<ActivityDto> ParseActivities(JsonElement element)
    {
        var activities = new List<ActivityDto>();
        foreach (var item in element.EnumerateArray())
            activities.Add(new ActivityDto
            {
                Time = item.TryGetProperty("time", out var timeEl) ? timeEl.GetString() ?? "" : "",
                Name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "",
                Description = item.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "",
                Location = item.TryGetProperty("location", out var locEl) ? locEl.GetString() ?? "" : "",
                EstimatedCost = item.TryGetProperty("estimatedCost", out var costEl) ? costEl.GetDouble() : 0,
                Duration = item.TryGetProperty("duration", out var durEl) ? durEl.GetInt32() : 0
            });
        return activities;
    }

    private List<AttractionDto> ParseAttractions(JsonElement element)
    {
        var attractions = new List<AttractionDto>();
        foreach (var item in element.EnumerateArray())
            attractions.Add(new AttractionDto
            {
                Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Category = item.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                Rating = item.TryGetProperty("rating", out var rating) ? rating.GetDouble() : 0,
                Location = item.TryGetProperty("location", out var loc) ? loc.GetString() ?? "" : "",
                EntryFee = item.TryGetProperty("entryFee", out var fee) ? fee.GetDouble() : 0,
                BestTime = item.TryGetProperty("bestTime", out var bt) ? bt.GetString() ?? "" : "",
                Image = item.TryGetProperty("image", out var img) ? img.GetString() ?? "" : ""
            });
        return attractions;
    }

    private List<RestaurantDto> ParseRestaurants(JsonElement element)
    {
        var restaurants = new List<RestaurantDto>();
        foreach (var item in element.EnumerateArray())
            restaurants.Add(new RestaurantDto
            {
                Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Cuisine = item.TryGetProperty("cuisine", out var cuisine) ? cuisine.GetString() ?? "" : "",
                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Rating = item.TryGetProperty("rating", out var rating) ? rating.GetDouble() : 0,
                PriceRange = item.TryGetProperty("priceRange", out var pr) ? pr.GetString() ?? "" : "",
                Location = item.TryGetProperty("location", out var loc) ? loc.GetString() ?? "" : "",
                Specialty = item.TryGetProperty("specialty", out var spec) ? spec.GetString() ?? "" : "",
                Image = item.TryGetProperty("image", out var img) ? img.GetString() ?? "" : ""
            });
        return restaurants;
    }

    private BudgetBreakdownDto ParseBudgetBreakdown(JsonElement element)
    {
        return new BudgetBreakdownDto
        {
            Transportation = element.TryGetProperty("transportation", out var trans) ? trans.GetDouble() : 0,
            Accommodation = element.TryGetProperty("accommodation", out var acc) ? acc.GetDouble() : 0,
            Food = element.TryGetProperty("food", out var food) ? food.GetDouble() : 0,
            Activities = element.TryGetProperty("activities", out var acts) ? acts.GetDouble() : 0,
            Miscellaneous = element.TryGetProperty("miscellaneous", out var misc) ? misc.GetDouble() : 0,
            Total = element.TryGetProperty("total", out var total) ? total.GetDouble() : 0,
            Currency = element.TryGetProperty("currency", out var currency) ? currency.GetString() ?? "USD" : "USD"
        };
    }

    private List<string> ParseStringArray(JsonElement element)
    {
        var result = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value)) result.Add(value);
        }

        return result;
    }

    /// <summary>
    ///     从 AI 响应中提取 JSON 内容
    ///     处理以下情况：
    ///     1. 纯 JSON
    ///     2. JSON 被代码块包裹
    ///     3. JSON 前后有文字说明
    /// </summary>
    private string ExtractJsonFromAIResponse(string aiContent)
    {
        if (string.IsNullOrWhiteSpace(aiContent)) throw new ArgumentException("AI 响应内容为空", nameof(aiContent));

        var content = aiContent.Trim();
        const string codeBlockMarker = "```";
        const string jsonCodeBlock = "```json";

        // 情况1: 检查是否被代码块包裹
        if (content.Contains(jsonCodeBlock) || content.Contains(codeBlockMarker))
        {
            _logger.LogInformation("📝 检测到代码块格式，开始提取 JSON");

            // 提取代码块之间的内容
            var startMarker = content.Contains(jsonCodeBlock) ? jsonCodeBlock : codeBlockMarker;
            var startIndex = content.IndexOf(startMarker);

            if (startIndex >= 0)
            {
                startIndex += startMarker.Length;
                var endIndex = content.IndexOf(codeBlockMarker, startIndex);
                if (endIndex > startIndex)
                {
                    content = content.Substring(startIndex, endIndex - startIndex).Trim();
                    _logger.LogInformation("✅ 从代码块中提取 JSON 成功");
                }
            }
        }

        // 情况2: 查找 JSON 对象的开始和结束
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            content = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            _logger.LogInformation("✅ 提取 JSON 对象成功，长度: {Length}", content.Length);
        }
        else
        {
            _logger.LogWarning("⚠️ 未找到有效的 JSON 对象标记");
        }

        return content;
    }

    /// <summary>
    ///     尝试修复不完整的 JSON（处理未闭合的对象/数组）
    /// </summary>
    private string TryFixIncompleteJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        try
        {
            // 先尝试验证 JSON 是否有效
            JsonDocument.Parse(json);
            return json; // JSON 有效，直接返回
        }
        catch (JsonException)
        {
            _logger.LogWarning("⚠️ JSON 格式不完整，尝试自动修复...");

            // 统计括号和方括号
            var braceCount = 0;
            var bracketCount = 0;
            var inString = false;
            var prevChar = '\0';

            foreach (var c in json)
            {
                if (c == '"' && prevChar != '\\')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{') braceCount++;
                    else if (c == '}') braceCount--;
                    else if (c == '[') bracketCount++;
                    else if (c == ']') bracketCount--;
                }

                prevChar = c;
            }

            // 补全缺失的闭合符号
            var fixedJson = json;

            // 先闭合数组
            for (var i = 0; i < bracketCount; i++) fixedJson += "\n]";

            // 再闭合对象
            for (var i = 0; i < braceCount; i++) fixedJson += "\n}";

            _logger.LogInformation("🔧 JSON 修复完成，补全了 {Brackets} 个方括号和 {Braces} 个花括号", bracketCount, braceCount);

            // 再次验证修复后的 JSON
            try
            {
                JsonDocument.Parse(fixedJson);
                return fixedJson;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ JSON 修复后仍然无效");
                return json; // 修复失败，返回原始内容
            }
        }
    }

    /// <summary>
    ///     第 1 部分：生成基本信息（概述 + 签证）
    /// </summary>
    private async Task GenerateBasicInfoAsync(
        GenerateTravelGuideRequest request,
        TravelGuideResponse guide,
        Func<int, string, Task>? onProgress)
    {
        var prompt = $@"请为 {request.CityName} 生成数字游民指南的基本信息部分。

请以 JSON 格式返回：

{{
  ""overview"": ""城市概述（适合数字游民的整体评价，包括工作环境、生活成本、社区氛围等，200-300字）"",
  ""visaInfo"": {{
    ""type"": ""签证类型（如：旅游签证、数字游民签证、落地签等）"",
    ""duration"": 签证有效天数（数字），
    ""requirements"": ""签证申请要求（详细说明所需材料和条件）"",
    ""cost"": 签证费用（数字，美元），
    ""process"": ""申请流程（详细步骤说明）""
  }}
}}

要求：信息要准确、实用、最新，使用中文，必须返回严格的 JSON 格式。";

        _logger.LogInformation("🤖 [1/3] 调用 AI 生成基本信息...");
        var aiResponse = await CallAIAsync(prompt, 800);

        if (onProgress != null) await onProgress(35, "正在解析基本信息...");

        try
        {
            var jsonContent = ExtractJsonFromAIResponse(aiResponse);
            jsonContent = TryFixIncompleteJson(jsonContent);

            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            guide.Overview = root.TryGetProperty("overview", out var overview) ? overview.GetString() ?? "" : "";
            guide.VisaInfo = root.TryGetProperty("visaInfo", out var visaInfo)
                ? ParseVisaInfo(visaInfo)
                : new VisaInfoDto();

            _logger.LogInformation("✅ [1/3] 基本信息生成完成");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON 解析失败，原始响应: {Response}",
                aiResponse.Length > 500 ? aiResponse.Substring(0, 500) + "..." : aiResponse);

            // 提供默认值
            guide.Overview = $"{request.CityName} 是一个适合数字游民工作和生活的城市。";
            guide.VisaInfo = new VisaInfoDto();
            _logger.LogWarning("⚠️ 使用默认基本信息继续");
        }
    }

    /// <summary>
    ///     第 2 部分：生成推荐区域
    /// </summary>
    private async Task GenerateBestAreasAsync(
        GenerateTravelGuideRequest request,
        TravelGuideResponse guide,
        Func<int, string, Task>? onProgress)
    {
        var prompt = $@"请为 {request.CityName} 推荐5个最适合数字游民居住和工作的区域。

请以 JSON 格式返回：

{{
  ""bestAreas"": [
    {{
      ""name"": ""区域名称1"",
      ""description"": ""区域整体描述（100-150字）"",
      ""entertainmentScore"": 娱乐评分（1-5的数字），
      ""entertainmentDescription"": ""娱乐设施说明（酒吧、餐厅、夜生活等）"",
      ""tourismScore"": 旅游评分（1-5的数字），
      ""tourismDescription"": ""旅游景点说明（附近景点、文化地标等）"",
      ""economyScore"": 经济评分（1-5的数字，1最便宜，5最贵），
      ""economyDescription"": ""生活成本说明（住宿、餐饮、日常开销等）"",
      ""cultureScore"": 文化评分（1-5的数字），
      ""cultureDescription"": ""文化特色说明（当地文化、艺术氛围、历史底蕴等）""
    }},
    {{
      ""name"": ""区域名称2"",
      ""description"": ""区域整体描述"",
      ""entertainmentScore"": 娱乐评分（1-5），
      ""entertainmentDescription"": ""娱乐设施说明"",
      ""tourismScore"": 旅游评分（1-5），
      ""tourismDescription"": ""旅游景点说明"",
      ""economyScore"": 经济评分（1-5），
      ""economyDescription"": ""生活成本说明"",
      ""cultureScore"": 文化评分（1-5），
      ""cultureDescription"": ""文化特色说明""
    }},
    {{
      ""name"": ""区域名称3"",
      ""description"": ""区域整体描述"",
      ""entertainmentScore"": 娱乐评分（1-5），
      ""entertainmentDescription"": ""娱乐设施说明"",
      ""tourismScore"": 旅游评分（1-5），
      ""tourismDescription"": ""旅游景点说明"",
      ""economyScore"": 经济评分（1-5），
      ""economyDescription"": ""生活成本说明"",
      ""cultureScore"": 文化评分（1-5），
      ""cultureDescription"": ""文化特色说明""
    }},
    {{
      ""name"": ""区域名称4"",
      ""description"": ""区域整体描述"",
      ""entertainmentScore"": 娱乐评分（1-5），
      ""entertainmentDescription"": ""娱乐设施说明"",
      ""tourismScore"": 旅游评分（1-5），
      ""tourismDescription"": ""旅游景点说明"",
      ""economyScore"": 经济评分（1-5），
      ""economyDescription"": ""生活成本说明"",
      ""cultureScore"": 文化评分（1-5），
      ""cultureDescription"": ""文化特色说明""
    }},
    {{
      ""name"": ""区域名称5"",
      ""description"": ""区域整体描述"",
      ""entertainmentScore"": 娱乐评分（1-5），
      ""entertainmentDescription"": ""娱乐设施说明"",
      ""tourismScore"": 旅游评分（1-5），
      ""tourismDescription"": ""旅游景点说明"",
      ""economyScore"": 经济评分（1-5），
      ""economyDescription"": ""生活成本说明"",
      ""cultureScore"": 文化评分（1-5），
      ""cultureDescription"": ""文化特色说明""
    }}
  ]
}}

要求：必须包含5个区域，从娱乐、旅游、经济、文化四个维度评分(1-5数字)，使用中文，返回严格 JSON 格式。";

        _logger.LogInformation("🤖 [2/3] 调用 AI 生成推荐区域...");
        var aiResponse = await CallAIAsync(prompt, 1200);

        if (onProgress != null) await onProgress(65, "正在解析推荐区域...");

        try
        {
            var jsonContent = ExtractJsonFromAIResponse(aiResponse);

            // 尝试修复不完整的 JSON
            jsonContent = TryFixIncompleteJson(jsonContent);

            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            guide.BestAreas = root.TryGetProperty("bestAreas", out var areas)
                ? ParseBestAreas(areas)
                : new List<BestAreaDto>();

            _logger.LogInformation("✅ [2/3] 推荐区域生成完成，共 {Count} 个区域", guide.BestAreas.Count);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON 解析失败，原始响应: {Response}",
                aiResponse.Length > 500 ? aiResponse.Substring(0, 500) + "..." : aiResponse);

            // 如果解析失败，返回空列表而不是抛出异常
            guide.BestAreas = new List<BestAreaDto>();
            _logger.LogWarning("⚠️ 使用空的推荐区域列表继续");
        }
    }

    /// <summary>
    ///     第 3 部分：生成实用信息（工作空间 + 建议 + 基本信息）
    /// </summary>
    private async Task GeneratePracticalInfoAsync(
        GenerateTravelGuideRequest request,
        TravelGuideResponse guide,
        Func<int, string, Task>? onProgress)
    {
        var prompt = $@"请为 {request.CityName} 的数字游民提供实用的工作和生活建议。

请以 JSON 格式返回：

{{
  ""workspaceRecommendations"": [
    ""共享办公空间推荐1（包括名称、地址、价格范围、特色）"",
    ""共享办公空间推荐2"",
    ""共享办公空间推荐3"",
    ""适合工作的咖啡馆推荐1"",
    ""适合工作的咖啡馆推荐2""
  ],
  ""tips"": [
    ""实用建议1（关于生活、工作、社交等方面）"",
    ""实用建议2"",
    ""实用建议3"",
    ""实用建议4"",
    ""实用建议5""
  ],
  ""essentialInfo"": {{
    ""SIM卡"": ""当地 SIM 卡购买和使用建议"",
    ""银行开户"": ""银行账户开设建议"",
    ""交通"": ""当地交通方式和建议"",
    ""医疗"": ""医疗保险和就医建议"",
    ""网络"": ""互联网质量和推荐供应商"",
    ""语言"": ""当地语言和英语使用情况"",
    ""安全"": ""安全注意事项"",
    ""社区"": ""数字游民社区和活动信息""
  }}
}}

要求：信息要具体可操作，使用中文，返回严格 JSON 格式。";

        _logger.LogInformation("🤖 [3/3] 调用 AI 生成实用信息...");
        var aiResponse = await CallAIAsync(prompt, 1000);

        if (onProgress != null) await onProgress(95, "正在解析实用信息...");

        try
        {
            var jsonContent = ExtractJsonFromAIResponse(aiResponse);
            jsonContent = TryFixIncompleteJson(jsonContent);

            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            guide.WorkspaceRecommendations = root.TryGetProperty("workspaceRecommendations", out var workspaces)
                ? ParseStringArray(workspaces)
                : new List<string>();
            guide.Tips = root.TryGetProperty("tips", out var tips) ? ParseStringArray(tips) : new List<string>();
            guide.EssentialInfo = root.TryGetProperty("essentialInfo", out var essentialInfo)
                ? ParseEssentialInfo(essentialInfo)
                : new Dictionary<string, string>();

            _logger.LogInformation("✅ [3/3] 实用信息生成完成");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON 解析失败，原始响应: {Response}",
                aiResponse.Length > 500 ? aiResponse.Substring(0, 500) + "..." : aiResponse);

            // 提供默认值
            guide.WorkspaceRecommendations = new List<string>();
            guide.Tips = new List<string>();
            guide.EssentialInfo = new Dictionary<string, string>();
            _logger.LogWarning("⚠️ 使用空的实用信息继续");
        }
    }

    private List<BestAreaDto> ParseBestAreas(JsonElement element)
    {
        var areas = new List<BestAreaDto>();

        if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                if (item.ValueKind == JsonValueKind.Object)
                    areas.Add(new BestAreaDto
                    {
                        Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        Description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                        EntertainmentScore = item.TryGetProperty("entertainmentScore", out var entScore)
                            ? entScore.GetDouble()
                            : 0,
                        EntertainmentDescription = item.TryGetProperty("entertainmentDescription", out var entDesc)
                            ? entDesc.GetString() ?? ""
                            : "",
                        TourismScore = item.TryGetProperty("tourismScore", out var tourScore)
                            ? tourScore.GetDouble()
                            : 0,
                        TourismDescription = item.TryGetProperty("tourismDescription", out var tourDesc)
                            ? tourDesc.GetString() ?? ""
                            : "",
                        EconomyScore = item.TryGetProperty("economyScore", out var ecoScore) ? ecoScore.GetDouble() : 0,
                        EconomyDescription = item.TryGetProperty("economyDescription", out var ecoDesc)
                            ? ecoDesc.GetString() ?? ""
                            : "",
                        CultureScore = item.TryGetProperty("cultureScore", out var culScore) ? culScore.GetDouble() : 0,
                        CultureDescription = item.TryGetProperty("cultureDescription", out var culDesc)
                            ? culDesc.GetString() ?? ""
                            : ""
                    });

        return areas;
    }

    private VisaInfoDto ParseVisaInfo(JsonElement element)
    {
        return new VisaInfoDto
        {
            Type = element.TryGetProperty("type", out var type) ? type.GetString() ?? "" : "",
            Duration = element.TryGetProperty("duration", out var duration) ? duration.GetInt32() : 0,
            Requirements = element.TryGetProperty("requirements", out var requirements)
                ? requirements.GetString() ?? ""
                : "",
            Cost = element.TryGetProperty("cost", out var cost) ? cost.GetDouble() : 0,
            Process = element.TryGetProperty("process", out var process) ? process.GetString() ?? "" : ""
        };
    }

    private Dictionary<string, string> ParseEssentialInfo(JsonElement element)
    {
        var dict = new Dictionary<string, string>();
        foreach (var property in element.EnumerateObject()) dict[property.Name] = property.Value.GetString() ?? "";
        return dict;
    }

    #region 分段生成旅行计划的辅助方法

    /// <summary>
    ///     步骤1: 生成交通和住宿信息
    /// </summary>
    private async Task<(TransportationPlanDto, AccommodationPlanDto)> GenerateBasicInfoAsync(
        GenerateTravelPlanRequest request)
    {
        // 构建出发地点和日期信息
        var departureDateInfo = request.DepartureDate.HasValue
            ? $"出发日期：{request.DepartureDate.Value:yyyy年MM月dd日}"
            : "";

        var prompt = $@"请为{request.CityName}的旅行规划交通和住宿方案。

旅行信息：
- 目的地：{request.CityName}
- 旅行天数：{request.Duration}天
- 预算：{GetBudgetDescription(request.Budget)}
{(string.IsNullOrEmpty(request.DepartureLocation) ? "" : $"- 出发地：{request.DepartureLocation}")}
{(string.IsNullOrEmpty(departureDateInfo) ? "" : $"- {departureDateInfo}")}

交通规划要求：
1. 如果提供了出发地点，请推荐3-5个航班选择，包括：
   - 航空公司
   - 航班号
   - 大致时间（早班/午班/晚班）
   - 估计价格区间
   - 飞行时长
2. 如果没有出发地点，提供一般性的到达建议

请以 JSON 格式返回，包含两个部分：

{{
  ""transportation"": {{
    ""arrivalMethod"": ""到达方式（飞机/火车/汽车）"",
    ""arrivalDetails"": ""到达详情"",
    ""flightRecommendations"": [
      {{
        ""airline"": ""航空公司"",
        ""flightNumber"": ""航班号（如：CA1234）"",
        ""timeSlot"": ""时间段（早班/午班/晚班）"",
        ""priceRange"": ""价格区间（如：500-800美元）"",
        ""duration"": ""飞行时长（如：2小时30分钟）"",
        ""notes"": ""备注信息""
      }}
    ],
    ""estimatedCost"": 费用数字,
    ""localTransport"": ""当地交通方式（用逗号分隔，如：地铁,公交,出租车）"",
    ""localTransportDetails"": ""详情"",
    ""dailyTransportCost"": 每日费用数字
  }},
  ""accommodation"": {{
    ""type"": ""hotel"",
    ""recommendation"": ""推荐说明"",
    ""area"": ""推荐区域"",
    ""pricePerNight"": 每晚价格数字,
    ""amenities"": [""设施1"", ""设施2""],
    ""bookingTips"": ""预订建议""
  }}
}}

注意：
1. 所有数字字段必须是数字类型，不要用字符串
2. localTransport 必须是字符串，用逗号分隔多个交通方式
3. flightRecommendations 数组在没有出发地时可以为空数组
4. 如果有出发地和日期，请提供符合该时间段的实际航班建议（基于常见航线）";

        _logger.LogInformation($"📝 发送给 AI 的 prompt (前500字符): {prompt.Substring(0, Math.Min(500, prompt.Length))}...");
        _logger.LogInformation(
            $"📍 出发地: {request.DepartureLocation ?? "未提供"}, 出发日期: {(request.DepartureDate.HasValue ? request.DepartureDate.Value.ToString("yyyy-MM-dd") : "未提供")}");

        var response = await CallAIAsync(prompt, 2000); // 增加token以容纳航班信息
        var json = ExtractJsonFromAIResponse(response);

        // 调试日志：打印 AI 返回的 JSON
        _logger.LogInformation($"🔍 AI 返回的交通住宿 JSON: {json.Substring(0, Math.Min(500, json.Length))}...");

        // 验证 JSON 是否完整
        ValidateJsonComplete(json, "步骤1-交通住宿");

        // 直接反序列化
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var transportation = new TransportationPlanDto();
        var accommodation = new AccommodationPlanDto();

        if (root.TryGetProperty("transportation", out var trans))
        {
            transportation.ArrivalMethod =
                trans.TryGetProperty("arrivalMethod", out var am) ? am.GetString() ?? "" : "";
            transportation.ArrivalDetails =
                trans.TryGetProperty("arrivalDetails", out var ad) ? ad.GetString() ?? "" : "";

            // 处理航班推荐
            if (trans.TryGetProperty("flightRecommendations", out var flights) &&
                flights.ValueKind == JsonValueKind.Array)
            {
                _logger.LogInformation($"✈️ 找到航班推荐数组，数量: {flights.GetArrayLength()}");
                var flightsList = new List<string>();
                foreach (var flight in flights.EnumerateArray())
                {
                    var airline = flight.TryGetProperty("airline", out var al) ? al.GetString() ?? "" : "";
                    var flightNum = flight.TryGetProperty("flightNumber", out var fn) ? fn.GetString() ?? "" : "";
                    var timeSlot = flight.TryGetProperty("timeSlot", out var ts) ? ts.GetString() ?? "" : "";
                    var priceRange = flight.TryGetProperty("priceRange", out var pr) ? pr.GetString() ?? "" : "";
                    var duration = flight.TryGetProperty("duration", out var dur) ? dur.GetString() ?? "" : "";
                    var notes = flight.TryGetProperty("notes", out var nt) ? nt.GetString() ?? "" : "";

                    var flightInfo = $"{airline} {flightNum} ({timeSlot}) - {priceRange}, {duration}";
                    if (!string.IsNullOrEmpty(notes)) flightInfo += $" - {notes}";
                    flightsList.Add(flightInfo);
                    _logger.LogInformation($"✈️ 解析航班: {flightInfo}");
                }

                if (flightsList.Any())
                {
                    // 将航班信息添加到 arrivalDetails 中
                    var flightInfo = string.Join("\n", flightsList);
                    transportation.ArrivalDetails += $"\n\n航班推荐：\n{flightInfo}";
                    _logger.LogInformation($"✅ 已添加 {flightsList.Count} 个航班推荐到 arrivalDetails");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ JSON 中未找到 flightRecommendations 数组或格式不正确");
            }

            transportation.EstimatedCost = trans.TryGetProperty("estimatedCost", out var ec) ? ec.GetDouble() : 0;

            // 处理 localTransport - 可能是字符串或数组
            if (trans.TryGetProperty("localTransport", out var lt))
            {
                if (lt.ValueKind == JsonValueKind.Array)
                {
                    // 如果是数组，转换为逗号分隔的字符串
                    var transports = new List<string>();
                    foreach (var item in lt.EnumerateArray())
                    {
                        var val = item.GetString();
                        if (!string.IsNullOrWhiteSpace(val))
                            transports.Add(val);
                    }

                    transportation.LocalTransport = string.Join(", ", transports);
                }
                else
                {
                    transportation.LocalTransport = lt.GetString() ?? "";
                }
            }

            transportation.LocalTransportDetails = trans.TryGetProperty("localTransportDetails", out var ltd)
                ? ltd.GetString() ?? ""
                : "";
            transportation.DailyTransportCost =
                trans.TryGetProperty("dailyTransportCost", out var dtc) ? dtc.GetDouble() : 0;
        }

        if (root.TryGetProperty("accommodation", out var acc))
        {
            accommodation.Type = acc.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
            accommodation.Recommendation = acc.TryGetProperty("recommendation", out var r) ? r.GetString() ?? "" : "";
            accommodation.Area = acc.TryGetProperty("area", out var a) ? a.GetString() ?? "" : "";
            accommodation.PricePerNight = acc.TryGetProperty("pricePerNight", out var ppn) ? ppn.GetDouble() : 0;
            accommodation.Amenities = acc.TryGetProperty("amenities", out var amenities)
                ? ParseStringArray(amenities)
                : new List<string>();
            accommodation.BookingTips = acc.TryGetProperty("bookingTips", out var bt) ? bt.GetString() ?? "" : "";
        }

        return (transportation, accommodation);
    }

    /// <summary>
    ///     步骤2: 生成每日行程（按天循环生成）
    /// </summary>
    private async Task<List<DailyItineraryDto>> GenerateDailyItinerariesAsync(
        GenerateTravelPlanRequest request,
        Func<int, int, Task>? onDayProgress = null)
    {
        var allItineraries = new List<DailyItineraryDto>();

        // 收集已访问的景点，确保不重复
        var visitedLocations = new HashSet<string>();

        // 按天循环生成，每天一个独立请求
        for (var day = 1; day <= request.Duration; day++)
        {
            _logger.LogInformation("📅 生成第 {Day}/{Total} 天的行程...", day, request.Duration);

            // 回调进度
            if (onDayProgress != null)
                await onDayProgress(day, request.Duration);

            // 构建已访问景点列表
            var visitedLocationsText = visitedLocations.Count > 0
                ? $"\n⚠️ 重要：以下景点已在前{day - 1}天访问过，请安排不同的景点：\n- {string.Join("\n- ", visitedLocations)}"
                : "";

            var prompt = $@"请为{request.CityName}第{day}天的旅行制定行程计划（共{request.Duration}天）。

旅行风格：{GetStyleDescription(request.TravelStyle)}
{(day == 1 ? "第一天：初到城市，安排轻松适应性活动" : "")}
{(day == request.Duration ? "最后一天：安排返程前的活动，预留离开时间" : "")}{visitedLocationsText}

返回 JSON 格式（安排3-4个活动，描述简洁）：

{{
  ""day"": {day},
  ""theme"": ""当天主题"",
  ""activities"": [
    {{
      ""time"": ""09:00"",
      ""name"": ""活动名称"",
      ""description"": ""简短描述(20字内)"",
      ""location"": ""地点"",
      ""estimatedCost"": 数字,
      ""duration"": 分钟数字
    }}
  ],
  ""notes"": ""简要提示""
}}

要求：
1. 每天安排3-4个不同的活动
2. 每天的景点/地点必须与之前的天数不重复
3. description不超过20字
4. 所有数字用数字类型";

            var response = await CallAIAsync(prompt, 1000); // 单天只需要 1000 tokens
            var json = ExtractJsonFromAIResponse(response);
            ValidateJsonComplete(json, $"第{day}天行程");

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var itinerary = new DailyItineraryDto
            {
                Day = root.TryGetProperty("day", out var d) ? d.GetInt32() : day,
                Theme = root.TryGetProperty("theme", out var t) ? t.GetString() ?? "" : "",
                Activities = root.TryGetProperty("activities", out var acts)
                    ? ParseActivities(acts)
                    : new List<ActivityDto>(),
                Notes = root.TryGetProperty("notes", out var n) ? n.GetString() ?? "" : ""
            };

            // 收集本天访问的景点到已访问列表
            foreach (var activity in itinerary.Activities)
                if (!string.IsNullOrWhiteSpace(activity.Location))
                    visitedLocations.Add(activity.Location);

            allItineraries.Add(itinerary);
            _logger.LogInformation("✅ 第 {Day} 天行程生成完成，包含 {Count} 个活动，已访问 {Visited} 个景点",
                day, itinerary.Activities.Count, visitedLocations.Count);

            // 避免请求过快，添加小延迟
            if (day < request.Duration) await Task.Delay(500);
        }

        _logger.LogInformation("✅ 所有 {Total} 天行程生成完成", request.Duration);
        return allItineraries;
    }

    /// <summary>
    ///     步骤3: 生成景点和餐厅推荐
    /// </summary>
    private async Task<(List<AttractionDto>, List<RestaurantDto>)> GenerateAttractionsAndRestaurantsAsync(
        GenerateTravelPlanRequest request)
    {
        var prompt = $@"推荐{request.CityName}的景点(5-8个)和餐厅(3-5个)。

JSON 格式（描述简洁）：

{{
  ""attractions"": [
    {{
      ""name"": ""名称"",
      ""description"": ""简短描述(30字内)"",
      ""category"": ""类别"",
      ""rating"": 评分数字,
      ""location"": ""位置"",
      ""entryFee"": 费用数字,
      ""bestTime"": ""时间"",
      ""image"": """"
    }}
  ],
  ""restaurants"": [
    {{
      ""name"": ""名称"",
      ""cuisine"": ""菜系"",
      ""description"": ""简短描述(20字内)"",
      ""rating"": 评分数字,
      ""priceRange"": ""$$ 或 $$$"",
      ""location"": ""位置"",
      ""specialty"": ""招牌菜"",
      ""image"": """"
    }}
  ]
}}";

        var response = await CallAIAsync(prompt, 3000); // 增加到 3000
        var json = ExtractJsonFromAIResponse(response);
        ValidateJsonComplete(json, "步骤3-景点餐厅");

        var doc = JsonDocument.Parse(json);

        var attractions = doc.RootElement.TryGetProperty("attractions", out var attr)
            ? ParseAttractions(attr)
            : new List<AttractionDto>();

        var restaurants = doc.RootElement.TryGetProperty("restaurants", out var rest)
            ? ParseRestaurants(rest)
            : new List<RestaurantDto>();

        return (attractions, restaurants);
    }

    /// <summary>
    ///     步骤4: 生成预算明细和旅行建议
    /// </summary>
    private async Task<(BudgetBreakdownDto, List<string>)> GenerateBudgetAndTipsAsync(
        GenerateTravelPlanRequest request,
        TransportationPlanDto transportation,
        AccommodationPlanDto accommodation,
        List<DailyItineraryDto> dailyItineraries)
    {
        // 计算已知的费用
        var transportCost = transportation.EstimatedCost + transportation.DailyTransportCost * request.Duration;
        var accommodationCost = accommodation.PricePerNight * request.Duration;
        var activitiesCost = dailyItineraries
            .SelectMany(d => d.Activities)
            .Sum(a => a.EstimatedCost);

        var prompt = $@"请为{request.CityName}的{request.Duration}天旅行提供预算明细和实用建议。

已知费用：
- 交通费用：{transportCost}
- 住宿费用：{accommodationCost}
- 活动费用：{activitiesCost}

请以 JSON 格式返回：

{{
  ""budgetBreakdown"": {{
    ""transportation"": {transportCost},
    ""accommodation"": {accommodationCost},
    ""food"": 估算餐饮费用数字,
    ""activities"": {activitiesCost},
    ""miscellaneous"": 其他费用数字,
    ""total"": 总费用数字,
    ""currency"": ""USD""
  }},
  ""tips"": [
    ""建议1"",
    ""建议2"",
    ""建议3""
  ]
}}";

        var response = await CallAIAsync(prompt, 1200); // 增加到 1200
        var json = ExtractJsonFromAIResponse(response);
        ValidateJsonComplete(json, "步骤4-预算建议");

        var doc = JsonDocument.Parse(json);

        var budget = doc.RootElement.TryGetProperty("budgetBreakdown", out var bud)
            ? ParseBudgetBreakdown(bud)
            : new BudgetBreakdownDto();

        var tips = doc.RootElement.TryGetProperty("tips", out var t)
            ? ParseStringArray(t)
            : new List<string>();

        return (budget, tips);
    }

    /// <summary>
    ///     调用 AI 的通用方法（带重试机制）- 直接使用 HttpClient
    /// </summary>
    private async Task<string> CallAIAsync(string userPrompt, int maxTokens, int maxRetries = 3)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                _logger.LogInformation("🔄 AI 请求尝试 {Attempt}/{MaxRetries}, MaxTokens: {MaxTokens}",
                    attempt, maxRetries, maxTokens);

                var stopwatch = Stopwatch.StartNew();

                // 直接使用 HttpClient 调用 Qwen API
                var apiKey = _configuration["Qwen:ApiKey"] ?? throw new InvalidOperationException("Qwen API Key 未配置");
                var baseUrl = _configuration["Qwen:BaseUrl"] ?? "https://dashscope.aliyuncs.com/compatible-mode/v1";
                var model = _configuration["SemanticKernel:DefaultModel"] ?? "qwen-plus";

                // 创建 HttpClient 时配置更长的超时和缓冲区
                using var httpClient = new HttpClient(new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 10,
                    ResponseDrainTimeout = TimeSpan.FromMinutes(5)
                });

                httpClient.Timeout = TimeSpan.FromMinutes(10); // 增加到 10 分钟

                var requestBody = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = "你是一个专业的旅行规划助手。请以有效的 JSON 格式返回结果，不要包含其他文字说明。" },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.7,
                    max_tokens = maxTokens,
                    stream = false // 明确禁用流式输出
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Headers.Add("Accept", "application/json");
                request.Content = content;

                _logger.LogInformation("📤 发送 AI 请求到: {Url}, MaxTokens: {MaxTokens}", $"{baseUrl}/chat/completions",
                    maxTokens);

                // 使用 ResponseHeadersRead 模式，避免缓冲整个响应
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // 流式读取响应内容
                var responseBody = await response.Content.ReadAsStringAsync();
                stopwatch.Stop();

                _logger.LogInformation("✅ AI 响应成功 (尝试 {Attempt}), 耗时: {ElapsedMs}ms, 响应长度: {Length}",
                    attempt, stopwatch.ElapsedMilliseconds, responseBody.Length);

                // 解析响应
                var jsonDoc = JsonDocument.Parse(responseBody);
                var choices = jsonDoc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0) throw new InvalidOperationException("AI 响应中没有 choices");

                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var aiContent = message.GetProperty("content").GetString() ?? string.Empty;

                _logger.LogInformation("📝 AI 返回内容长度: {Length}", aiContent.Length);

                return aiContent;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "⚠️ AI HTTP 请求失败 (尝试 {Attempt}/{MaxRetries}), 错误: {Message}, 正在重试...",
                    attempt, maxRetries, ex.Message);

                if (attempt < maxRetries)
                {
                    var delaySeconds = attempt * 3; // 增加重试间隔：3秒、6秒、9秒
                    _logger.LogInformation("⏳ 等待 {DelaySeconds} 秒后重试...", delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "⚠️ AI 请求超时 (尝试 {Attempt}/{MaxRetries}), 正在重试...",
                    attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    var delaySeconds = attempt * 3; // 增加重试间隔
                    _logger.LogInformation("⏳ 等待 {DelaySeconds} 秒后重试...", delaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AI 请求失败 (尝试 {Attempt}/{MaxRetries})", attempt, maxRetries);
                throw;
            }

        _logger.LogError(lastException, "❌ AI 请求失败: 已重试 {MaxRetries} 次仍然失败", maxRetries);
        throw new InvalidOperationException($"AI 请求失败: 已重试 {maxRetries} 次", lastException);
    }

    /// <summary>
    ///     验证 JSON 是否完整（括号匹配）
    /// </summary>
    private void ValidateJsonComplete(string json, string step)
    {
        var openBraces = json.Count(c => c == '{');
        var closeBraces = json.Count(c => c == '}');
        var openBrackets = json.Count(c => c == '[');
        var closeBrackets = json.Count(c => c == ']');

        if (openBraces != closeBraces || openBrackets != closeBrackets)
        {
            _logger.LogError("❌ {Step} JSON 不完整 - 大括号: {OpenBraces}/{CloseBraces}, 中括号: {OpenBrackets}/{CloseBrackets}",
                step, openBraces, closeBraces, openBrackets, closeBrackets);
            _logger.LogError("JSON 内容 (前500字符): {JsonPreview}", json.Substring(0, Math.Min(500, json.Length)));
            _logger.LogError("JSON 内容 (后500字符): {JsonSuffix}",
                json.Length > 500 ? json.Substring(json.Length - 500) : json);
            throw new JsonException($"{step}: AI 返回的 JSON 不完整，可能是 token 限制导致截断");
        }

        _logger.LogInformation("✅ {Step} JSON 验证通过 - 大括号: {Braces}, 中括号: {Brackets}",
            step, openBraces, openBrackets);
    }

    private string GetBudgetDescription(string budget)
    {
        return budget switch
        {
            "low" => "经济型（每天50-100美元）",
            "medium" => "中等（每天100-200美元）",
            "high" => "豪华（每天200美元以上）",
            _ => "中等预算"
        };
    }

    private string GetStyleDescription(string style)
    {
        return style switch
        {
            "adventure" => "冒险探索",
            "relaxation" => "休闲放松",
            "culture" => "文化探索",
            "nightlife" => "夜生活娱乐",
            _ => "文化探索"
        };
    }

    #endregion
}
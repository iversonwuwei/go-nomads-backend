using AIService.API.Hubs;
using AIService.API.Models;
using AIService.Application.DTOs;
using AIService.Domain.Entities;
using AIService.Infrastructure.Cache;
using AIService.Infrastructure.MessageBus;
using AIService.Infrastructure.Repositories;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace AIService.API.Services;

/// <summary>
/// AI 任务处理后台服务
/// </summary>
public class AIWorkerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIWorkerService> _logger;

    public AIWorkerService(
        IServiceProvider serviceProvider,
        ILogger<AIWorkerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 AI Worker Service 已启动");

        using var scope = _serviceProvider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await messageBus.SubscribeAsync<TravelPlanTaskMessage>(
            queueName: "travel-plan-tasks",
            handler: async (message) => await ProcessTaskAsync(message, stoppingToken),
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("⏳ AI Worker Service 正在等待任务...");
    }

    private async Task ProcessTaskAsync(TravelPlanTaskMessage taskMessage, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IRedisCache>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var kernel = scope.ServiceProvider.GetRequiredService<Kernel>();

        var taskId = taskMessage.TaskId;

        try
        {
            _logger.LogInformation("🎯 开始处理任务: {TaskId}", taskId);

            // 更新状态为处理中
            await UpdateTaskStatusAsync(cache, taskId, "processing", 10, "正在生成旅行计划...");
            await notificationService.SendTaskProgressAsync(taskId, 10, "正在生成旅行计划...");

            // 构建提示词
            var prompt = BuildPrompt(taskMessage.Request);
            _logger.LogInformation("📝 提示词已生成,长度: {Length}", prompt.Length);

            await UpdateTaskStatusAsync(cache, taskId, "processing", 30, "正在调用 AI 模型...");
            await notificationService.SendTaskProgressAsync(taskId, 30, "正在调用 AI 模型...");

            // 调用 AI 生成
#pragma warning disable SKEXP0010 // ResponseFormat is experimental
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("你是一个专业的旅行规划助手,擅长根据用户需求制定详细的旅行计划。你必须以有效的 JSON 格式返回旅行计划,不要包含任何其他文本。");
            chatHistory.AddUserMessage(prompt);

            // 配置 AI 执行参数 - Qwen 支持 response_format
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
                MaxTokens = 4000,
                ResponseFormat = "json_object" // Qwen 兼容 OpenAI 的 JSON 模式
            };

            var result = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                cancellationToken: cancellationToken
            );
#pragma warning restore SKEXP0010

            var responseContent = result.Content ?? string.Empty;
            _logger.LogInformation("🤖 AI 响应已接收,长度: {Length}", responseContent.Length);

            await UpdateTaskStatusAsync(cache, taskId, "processing", 70, "正在解析结果...");
            await notificationService.SendTaskProgressAsync(taskId, 70, "正在解析结果...");

            // 解析 AI 响应为 TravelPlanResponse (使用与同步方法相同的解析逻辑)
            TravelPlanResponse travelPlan;
            try
            {
                // 移除可能的 markdown 代码块标记
                var jsonContent = responseContent.Trim();
                if (jsonContent.StartsWith("```json"))
                {
                    jsonContent = jsonContent.Substring(7);
                }
                if (jsonContent.StartsWith("```"))
                {
                    jsonContent = jsonContent.Substring(3);
                }
                if (jsonContent.EndsWith("```"))
                {
                    jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
                }
                jsonContent = jsonContent.Trim();

                // 使用 JSON 反序列化
                travelPlan = System.Text.Json.JsonSerializer.Deserialize<TravelPlanResponse>(
                    jsonContent,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? throw new InvalidOperationException("JSON 解析结果为 null");

                // 设置基本信息
                travelPlan.Id = Guid.NewGuid().ToString();
                travelPlan.CityId = taskMessage.Request.CityId;
                travelPlan.CityName = taskMessage.Request.CityName;
                travelPlan.CreatedAt = DateTime.UtcNow;

                _logger.LogInformation("✅ 旅行计划解析成功,ID: {PlanId}", travelPlan.Id);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "❌ JSON 解析失败,原始内容: {Content}", responseContent.Substring(0, Math.Min(500, responseContent.Length)));
                throw new InvalidOperationException("AI 响应格式错误，无法生成旅行计划", ex);
            }

            await UpdateTaskStatusAsync(cache, taskId, "processing", 90, "正在保存结果...");
            await notificationService.SendTaskProgressAsync(taskId, 90, "正在保存结果...");

            var planId = travelPlan.Id;
            
            // 将解析后的 JSON 对象保存到 Redis (序列化为 JSON 字符串)
            var planJson = System.Text.Json.JsonSerializer.Serialize(travelPlan);
            await cache.SetStringAsync($"plan:{planId}", planJson, TimeSpan.FromHours(24));
            
            _logger.LogInformation("💾 旅行计划已保存到 Redis: plan:{PlanId}, Size: {Size} bytes", planId, planJson.Length);

            // 更新为完成状态
            await UpdateTaskStatusAsync(cache, taskId, "completed", 100, "生成完成!", planId);
            await notificationService.SendTaskCompletedAsync(taskId, planId);

            _logger.LogInformation("✅ 任务处理完成: {TaskId} - PlanId: {PlanId}", taskId, planId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 任务处理失败: {TaskId}", taskId);

            // 更新为失败状态
            await UpdateTaskStatusAsync(cache, taskId, "failed", 0, null, null, ex.Message);
            await notificationService.SendTaskFailedAsync(taskId, ex.Message);
        }
    }

    private async Task UpdateTaskStatusAsync(
        IRedisCache cache,
        string taskId,
        string status,
        int progress,
        string? progressMessage = null,
        string? planId = null,
        string? error = null)
    {
        var taskStatus = new Models.TaskStatus
        {
            TaskId = taskId,
            Status = status,
            Progress = progress,
            ProgressMessage = progressMessage,
            PlanId = planId,
            Error = error,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CompletedAt = status == "completed" || status == "failed" ? DateTime.UtcNow : null
        };

        // 缓存24小时
        await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));
    }

    private string BuildPrompt(GenerateTravelPlanRequest request)
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

        var interestsText = request.Interests != null && request.Interests.Any()
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
}

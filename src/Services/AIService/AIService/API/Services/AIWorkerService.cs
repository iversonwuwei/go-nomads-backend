using AIService.API.Hubs;
using AIService.API.Models;
using AIService.Application.DTOs;
using AIService.Application.Services;
using AIService.Infrastructure.Cache;
using AIService.Infrastructure.MessageBus;
using Dapr.Client;

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

        // 订阅旅行计划任务
        await messageBus.SubscribeAsync<TravelPlanTaskMessage>(
            queueName: "travel-plan-tasks",
            handler: async (message) => await ProcessTravelPlanTaskAsync(message, stoppingToken),
            cancellationToken: stoppingToken
        );

        // 订阅数字游民指南任务
        await messageBus.SubscribeAsync<DigitalNomadGuideTaskMessage>(
            queueName: "digital-nomad-guide-tasks",
            handler: async (message) => await ProcessGuideTaskAsync(message, stoppingToken),
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("⏳ AI Worker Service 正在等待任务...");
    }

    private async Task ProcessTravelPlanTaskAsync(TravelPlanTaskMessage taskMessage, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IRedisCache>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var chatService = scope.ServiceProvider.GetRequiredService<IAIChatService>();

        var taskId = taskMessage.TaskId;

        try
        {
            _logger.LogInformation("🎯 开始处理任务: {TaskId}", taskId);

            // 更新状态为处理中
            await UpdateTaskStatusAsync(cache, taskId, "processing", 10, "正在生成旅行计划...");
            await notificationService.SendTaskProgressAsync(taskId, 10, "正在生成旅行计划...");

            // 调用新的分段生成服务，并传递进度回调
            var travelPlan = await chatService.GenerateTravelPlanAsync(
                taskMessage.Request, 
                taskMessage.UserId,
                async (progress, message) =>
                {
                    // 将内部进度（15-85%）映射到 10-90% 范围
                    var mappedProgress = 10 + (int)((progress - 15) / 70.0 * 80);
                    mappedProgress = Math.Max(10, Math.Min(90, mappedProgress)); // 确保在 10-90 范围内
                    
                    _logger.LogInformation("📊 任务进度: {Progress}% - {Message}", mappedProgress, message);
                    await UpdateTaskStatusAsync(cache, taskId, "processing", mappedProgress, message);
                    await notificationService.SendTaskProgressAsync(taskId, mappedProgress, message);
                });
            
            _logger.LogInformation("✅ 旅行计划生成成功,ID: {PlanId}", travelPlan.Id);

            await UpdateTaskStatusAsync(cache, taskId, "processing", 90, "正在保存结果...");
            await notificationService.SendTaskProgressAsync(taskId, 90, "正在保存结果...");

            var planId = travelPlan.Id;
            
            // 将解析后的 JSON 对象保存到 Redis (序列化为 JSON 字符串)
            var planJson = System.Text.Json.JsonSerializer.Serialize(travelPlan);
            await cache.SetStringAsync($"plan:{planId}", planJson, TimeSpan.FromHours(24));
            
            _logger.LogInformation("💾 旅行计划已保存到 Redis: plan:{PlanId}, Size: {Size} bytes", planId, planJson.Length);

            // 更新为完成状态
            await UpdateTaskStatusAsync(cache, taskId, "completed", 100, "生成完成!", planId: planId, result: travelPlan);
            await notificationService.SendTaskCompletedAsync(taskId, planId: planId, result: travelPlan);

            _logger.LogInformation("✅ 旅行计划任务处理完成: {TaskId} - PlanId: {PlanId}", taskId, planId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 任务处理失败: {TaskId}", taskId);

            // 更新为失败状态
            await UpdateTaskStatusAsync(cache, taskId, "failed", 0, null, null, ex.Message);
            await notificationService.SendTaskFailedAsync(taskId, ex.Message);
        }
    }

    private async Task ProcessGuideTaskAsync(DigitalNomadGuideTaskMessage taskMessage, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IRedisCache>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var chatService = scope.ServiceProvider.GetRequiredService<IAIChatService>();

        var taskId = taskMessage.TaskId;

        try
        {
            _logger.LogInformation("📖 开始处理数字游民指南任务: {TaskId}", taskId);

            // 更新状态为处理中
            await UpdateTaskStatusAsync(cache, taskId, "processing", 10, "正在生成数字游民指南...");
            await notificationService.SendTaskProgressAsync(taskId, 10, "正在生成数字游民指南...");

            // 调用 AI 服务生成指南，传递进度回调
            var guide = await chatService.GenerateTravelGuideAsync(
                taskMessage.Request, 
                taskMessage.UserId,
                async (progress, message) =>
                {
                    // 将内部进度映射到 10-90% 范围
                    var mappedProgress = 10 + (int)((progress - 15) / 70.0 * 80);
                    mappedProgress = Math.Max(10, Math.Min(90, mappedProgress));
                    
                    _logger.LogInformation("📊 指南任务进度: {Progress}% - {Message}", mappedProgress, message);
                    await UpdateTaskStatusAsync(cache, taskId, "processing", mappedProgress, message);
                    await notificationService.SendTaskProgressAsync(taskId, mappedProgress, message);
                });
            
            _logger.LogInformation("✅ 数字游民指南生成成功");

            await UpdateTaskStatusAsync(cache, taskId, "processing", 90, "正在保存结果...");
            await notificationService.SendTaskProgressAsync(taskId, 90, "正在保存结果...");

            // 通过Dapr调用CityService保存到Supabase
            try
            {
                var daprClient = scope.ServiceProvider.GetRequiredService<DaprClient>();
                
                _logger.LogInformation("📤 通过Dapr调用CityService保存指南: cityId={CityId}", taskMessage.Request.CityId);

                // 使用大驼峰命名匹配 CityService 的 SaveDigitalNomadGuideRequest
                var saveRequest = new
                {
                    CityId = taskMessage.Request.CityId,
                    CityName = taskMessage.Request.CityName,
                    Overview = guide.Overview,
                    VisaInfo = new
                    {
                        Type = guide.VisaInfo.Type,
                        Duration = guide.VisaInfo.Duration,
                        Requirements = guide.VisaInfo.Requirements,
                        Cost = guide.VisaInfo.Cost,
                        Process = guide.VisaInfo.Process
                    },
                    BestAreas = guide.BestAreas.Select(a => new
                    {
                        Name = a.Name,
                        Description = a.Description,
                        EntertainmentScore = a.EntertainmentScore,
                        EntertainmentDescription = a.EntertainmentDescription,
                        TourismScore = a.TourismScore,
                        TourismDescription = a.TourismDescription,
                        EconomyScore = a.EconomyScore,
                        EconomyDescription = a.EconomyDescription,
                        CultureScore = a.CultureScore,
                        CultureDescription = a.CultureDescription
                    }).ToList(),
                    WorkspaceRecommendations = guide.WorkspaceRecommendations,
                    Tips = guide.Tips,
                    EssentialInfo = guide.EssentialInfo
                };

                var response = await daprClient.InvokeMethodAsync<object, object>(
                    HttpMethod.Post,
                    "city-service",
                    $"api/v1/cities/{taskMessage.Request.CityId}/guide",
                    saveRequest);

                _logger.LogInformation("✅ 指南已通过Dapr保存到CityService: cityId={CityId}", taskMessage.Request.CityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Dapr调用CityService保存失败,但不影响任务完成: cityId={CityId}", taskMessage.Request.CityId);
            }

            // 将指南数据保存到 Redis (作为缓存)
            var guideJson = System.Text.Json.JsonSerializer.Serialize(guide);
            var guideId = $"guide_{taskMessage.Request.CityId}_{Guid.NewGuid():N}";
            await cache.SetStringAsync($"guide:{guideId}", guideJson, TimeSpan.FromHours(24));
            
            _logger.LogInformation("💾 数字游民指南已保存到 Redis: guide:{GuideId}, Size: {Size} bytes", guideId, guideJson.Length);

            // 更新为完成状态，同时保存 Result 数据
            await UpdateTaskStatusAsync(cache, taskId, "completed", 100, "生成完成!", guideId: guideId, result: guide);
            await notificationService.SendTaskCompletedAsync(taskId, guideId: guideId, result: guide);

            _logger.LogInformation("✅ 指南任务处理完成: {TaskId} - GuideId: {GuideId}", taskId, guideId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 指南任务处理失败: {TaskId}", taskId);

            // 更新为失败状态
            await UpdateTaskStatusAsync(cache, taskId, "failed", 0, error: ex.Message);
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
        string? guideId = null,
        object? result = null,
        string? error = null)
    {
        var taskStatus = new Models.TaskStatus
        {
            TaskId = taskId,
            Status = status,
            Progress = progress,
            ProgressMessage = progressMessage,
            PlanId = planId,
            GuideId = guideId,
            Result = result,
            Error = error,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CompletedAt = status == "completed" || status == "failed" ? DateTime.UtcNow : null
        };

        // 缓存24小时
        await cache.SetAsync($"task:{taskId}", taskStatus, TimeSpan.FromHours(24));
    }
}

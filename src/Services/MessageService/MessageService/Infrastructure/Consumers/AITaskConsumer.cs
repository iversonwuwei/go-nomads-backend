using MassTransit;
using MessageService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
/// AI 任务创建消息消费者
/// 接收任务创建通知，记录任务信息
/// </summary>
public class AITaskConsumer : IConsumer<AITaskMessage>
{
    private readonly ILogger<AITaskConsumer> _logger;

    public AITaskConsumer(ILogger<AITaskConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AITaskMessage> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("收到 AI 任务创建消息: TaskId={TaskId}, UserId={UserId}, TaskType={TaskType}",
            message.TaskId, message.UserId, message.TaskType);

        try
        {
            // TODO: 保存任务信息到数据库（如需持久化）
            // await _taskRepository.CreateAsync(message);

            _logger.LogInformation("AI 任务信息已记录: TaskId={TaskId}", message.TaskId);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 AI 任务消息失败: TaskId={TaskId}", message.TaskId);
            throw;
        }
    }
}

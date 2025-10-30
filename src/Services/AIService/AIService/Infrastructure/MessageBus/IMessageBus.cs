namespace AIService.Infrastructure.MessageBus;

/// <summary>
/// 消息总线接口
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// 发布消息到队列
    /// </summary>
    Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// 订阅队列消息
    /// </summary>
    Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class;
}

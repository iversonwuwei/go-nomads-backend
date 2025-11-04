using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AIService.Infrastructure.MessageBus;

/// <summary>
/// RabbitMQ 消息总线实现
/// </summary>
public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQMessageBus> _logger;

    public RabbitMQMessageBus(IConfiguration configuration, ILogger<RabbitMQMessageBus> logger)
    {
        _logger = logger;
        
        var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
        var port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");
        
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
            SocketReadTimeout = TimeSpan.FromSeconds(30),
            SocketWriteTimeout = TimeSpan.FromSeconds(30)
        };

        // 重试连接逻辑 - 最多重试5次,每次间隔5秒
        const int maxRetries = 5;
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                _logger.LogInformation("🔌 正在连接到 RabbitMQ: {HostName}:{Port} (尝试 {Retry}/{MaxRetries})", 
                    hostName, port, retryCount + 1, maxRetries);
                
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                _logger.LogInformation("✅ RabbitMQ 连接成功: {HostName}:{Port}", hostName, port);
                return; // 连接成功,退出
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                
                if (retryCount < maxRetries)
                {
                    var delaySeconds = 5;
                    _logger.LogWarning(ex, 
                        "⚠️ RabbitMQ 连接失败 (尝试 {Retry}/{MaxRetries}),{Delay}秒后重试... 错误: {Error}", 
                        retryCount, maxRetries, delaySeconds, ex.Message);
                    Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
                }
            }
        }

        // 所有重试都失败
        _logger.LogError(lastException, 
            "❌ RabbitMQ 连接失败: 已重试 {MaxRetries} 次仍无法连接到 {HostName}:{Port}", 
            maxRetries, hostName, port);
        throw new InvalidOperationException(
            $"无法连接到 RabbitMQ ({hostName}:{port}),已重试 {maxRetries} 次", 
            lastException);
    }

    public Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // 确保队列存在
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation("📤 [RabbitMQ] 发布消息到队列 {QueueName}, 消息大小: {Size} bytes", 
                queueName, body.Length);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [RabbitMQ] 发布消息失败: {QueueName}", queueName);
            throw;
        }
    }

    public Task SubscribeAsync<T>(string queueName, Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // 确保队列存在
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // 设置预取数量(每次只处理一个消息)
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    _logger.LogInformation("📥 [RabbitMQ] 收到消息: {QueueName}, 大小: {Size} bytes", 
                        queueName, body.Length);

                    var message = JsonSerializer.Deserialize<T>(json);
                    if (message != null)
                    {
                        await handler(message);
                        
                        // 手动确认消息
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        
                        _logger.LogInformation("✅ [RabbitMQ] 消息处理成功: {QueueName}", queueName);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [RabbitMQ] 消息反序列化为空: {QueueName}", queueName);
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [RabbitMQ] 消息处理失败: {QueueName}, 消息: {Message}", 
                        queueName, json);
                    
                    // 拒绝消息并重新入队(最多重试3次)
                    var retryCount = ea.BasicProperties.Headers?.ContainsKey("x-retry-count") == true 
                        ? (int)ea.BasicProperties.Headers["x-retry-count"] 
                        : 0;

                    if (retryCount < 3)
                    {
                        _logger.LogInformation("🔄 [RabbitMQ] 重新入队消息,重试次数: {RetryCount}", retryCount + 1);
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                    else
                    {
                        _logger.LogError("❌ [RabbitMQ] 消息处理失败次数过多,丢弃消息");
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: false, // 手动确认
                consumer: consumer
            );

            _logger.LogInformation("👂 [RabbitMQ] 开始监听队列: {QueueName}", queueName);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [RabbitMQ] 订阅队列失败: {QueueName}", queueName);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("🔌 [RabbitMQ] 连接已关闭");
    }
}

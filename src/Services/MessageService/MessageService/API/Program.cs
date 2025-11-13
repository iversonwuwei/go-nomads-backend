using MassTransit;
using MessageService.API.Hubs;
using MessageService.API.Services;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using MessageService.Infrastructure.Consumers;
using Serilog;
using Consul;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 配置正确的服务器 URL
        document.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
        {
            new() { Url = "http://localhost:5005", Description = "Local Development" }
        };
        return Task.CompletedTask;
    });
});

// 注册 Consul 客户端
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    var address = builder.Configuration["Consul:Address"] ?? "http://localhost:7500";
    consulConfig.Address = new Uri(address);
}));

// 注册 SignalRNotifier 接口实现
builder.Services.AddSingleton<ISignalRNotifier, SignalRNotifierImpl>();

// 配置 SignalR + Redis Backplane
builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis") 
        ?? "localhost:6379", options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("MessageService");
    });

// 配置 MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // 注册消费者
    x.AddConsumer<AIProgressConsumer>();
    x.AddConsumer<NotificationConsumer>();
    x.AddConsumer<AITaskConsumer>();
    
    // 注册新的 AI 消息消费者
    x.AddConsumer<AIProgressMessageConsumer>();
    x.AddConsumer<AITaskCompletedMessageConsumer>();
    x.AddConsumer<AITaskFailedMessageConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
        
        cfg.Host(rabbitMqConfig["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitMqConfig["Username"] ?? "guest");
            h.Password(rabbitMqConfig["Password"] ?? "guest");
        });

        // 配置 Exchange 和队列
        cfg.Message<AIProgressMessage>(x => x.SetEntityName("ai.progress.exchange"));
        cfg.Message<NotificationMessage>(x => x.SetEntityName("notifications.exchange"));
        cfg.Message<AITaskMessage>(x => x.SetEntityName("ai.tasks.exchange"));
        
        // 配置新的 AI 消息 Exchange
        cfg.Message<Shared.Messages.AIProgressMessage>(x => x.SetEntityName("ai.realtime.progress.exchange"));
        cfg.Message<Shared.Messages.AITaskCompletedMessage>(x => x.SetEntityName("ai.task.completed.exchange"));
        cfg.Message<Shared.Messages.AITaskFailedMessage>(x => x.SetEntityName("ai.task.failed.exchange"));

        // 配置消费者端点
        cfg.ReceiveEndpoint("ai-progress-queue", e =>
        {
            e.ConfigureConsumer<AIProgressConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        cfg.ReceiveEndpoint("notifications-queue", e =>
        {
            e.ConfigureConsumer<NotificationConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        cfg.ReceiveEndpoint("ai-tasks-queue", e =>
        {
            e.ConfigureConsumer<AITaskConsumer>(context);
            e.PrefetchCount = 8;
        });

        // 配置新的 AI 实时进度消息队列
        cfg.ReceiveEndpoint("ai-realtime-progress-queue", e =>
        {
            e.ConfigureConsumer<AIProgressMessageConsumer>(context);
            e.PrefetchCount = 32; // 进度消息频繁，增加并发
            e.UseMessageRetry(r => r.Interval(2, TimeSpan.FromSeconds(2)));
        });

        // 配置 AI 任务完成消息队列
        cfg.ReceiveEndpoint("ai-task-completed-queue", e =>
        {
            e.ConfigureConsumer<AITaskCompletedMessageConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        // 配置 AI 任务失败消息队列
        cfg.ReceiveEndpoint("ai-task-failed-queue", e =>
        {
            e.ConfigureConsumer<AITaskFailedMessageConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });
    });
});

// 配置 CORS（允许 Flutter 客户端连接）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutter", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 配置中间件
app.MapOpenApi();
app.MapScalarApiReference();

app.UseSerilogRequestLogging();

app.UseCors("AllowFlutter");

app.UseAuthorization();

app.MapControllers();

// 映射 SignalR Hubs
app.MapHub<AIProgressHub>("/hubs/ai-progress");
app.MapHub<NotificationHub>("/hubs/notifications");

// 注册到 Consul
var consulClient = app.Services.GetRequiredService<IConsulClient>();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

var serviceId = $"message-service-{Guid.NewGuid()}";
var serviceName = "message-service";
var serviceAddress = builder.Configuration["ServiceAddress"] ?? "go-nomads-message-service";
var servicePort = 8080;

var registration = new AgentServiceRegistration
{
    ID = serviceId,
    Name = serviceName,
    Address = serviceAddress,
    Port = servicePort,
    Tags = new[] { "message-service", "signalr", "rabbitmq", "api" },
    Meta = new Dictionary<string, string>
    {
        { "version", "1.0.0" },
        { "protocol", "http" },
        { "api_path", "/scalar/v1" },
        { "signalr_hubs", "/hubs/ai-progress,/hubs/notifications" }
    },
    Check = new AgentServiceCheck
    {
        HTTP = $"http://{serviceAddress}:{servicePort}/health",
        Interval = TimeSpan.FromSeconds(10),
        Timeout = TimeSpan.FromSeconds(5),
        DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
    }
};

lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await consulClient.Agent.ServiceRegister(registration);
        Log.Information("服务已注册到 Consul: {ServiceId}", serviceId);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "注册到 Consul 失败");
    }
});

lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        await consulClient.Agent.ServiceDeregister(serviceId);
        Log.Information("服务已从 Consul 注销: {ServiceId}", serviceId);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "从 Consul 注销失败");
    }
});

Log.Information("MessageService 启动成功，监听端口: {Port}", 
    builder.Configuration["ASPNETCORE_URLS"] ?? "http://+:8080");

app.Run();

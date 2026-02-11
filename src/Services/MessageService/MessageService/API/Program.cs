using MassTransit;
using MessageService.API.Hubs;
using MessageService.API.Services;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using MessageService.Domain.Repositories;
using MessageService.Infrastructure.Consumers;
using MessageService.Infrastructure.Repositories;
using MessageService.Infrastructure.TencentIM;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using GoNomads.Shared.Extensions;
using StackExchange.Redis;

const string serviceName = "MessageService";

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// Aspire ServiceDefaults (OpenTelemetry + HealthChecks + ServiceDiscovery)
// ============================================================
builder.AddServiceDefaults();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 配置正确的服务器 URL
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "http://localhost:5005", Description = "Local Development" }
        };
        return Task.CompletedTask;
    });
});

// 注册 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

// 注册 SignalRNotifier 接口实现
builder.Services.AddSingleton<ISignalRNotifier, SignalRNotifierImpl>();

// 注册通知服务
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationApplicationService>();

// 注册 UserService 客户端 (typed HttpClient, 用于动态获取用户信息)
builder.Services.AddServiceClient<IUserServiceClient, UserServiceClient>("user-service");

// 注册聊天服务
builder.Services.AddScoped<IChatRoomRepository, SupabaseChatRoomRepository>();
builder.Services.AddScoped<IChatMessageRepository, SupabaseChatMessageRepository>();
builder.Services.AddScoped<IChatMemberRepository, SupabaseChatMemberRepository>();
builder.Services.AddScoped<IChatService, ChatApplicationService>();

// 注册腾讯云IM服务
builder.Services.Configure<TencentIMConfig>(builder.Configuration.GetSection(TencentIMConfig.SectionName));
builder.Services.AddHttpClient<ITencentIMService, TencentIMService>();

// 配置 SignalR + Redis Backplane
builder.Services.AddSignalR(options =>
    {
        // 增加超时配置，防止连接超时
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // 客户端超时 60 秒
        options.KeepAliveInterval = TimeSpan.FromSeconds(15); // 保持连接间隔 15 秒
        options.HandshakeTimeout = TimeSpan.FromSeconds(30); // 握手超时 30 秒
        options.MaximumReceiveMessageSize = 1024 * 1024; // 最大消息大小 1MB
        options.StreamBufferCapacity = 20; // 流缓冲区容量
    })
    .AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis")
                           ?? "localhost:6379",
        options => { options.Configuration.ChannelPrefix = RedisChannel.Literal("MessageService"); });

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

    // 注册 AI Chat 流式响应消费者
    x.AddConsumer<AIChatStreamChunkConsumer>();

    // 注册城市图片生成完成消息消费者
    x.AddConsumer<CityImageGeneratedMessageConsumer>();

    // 注册聊天室在线状态消息消费者
    x.AddConsumer<ChatRoomOnlineStatusConsumer>();

    // 注册 Coworking 验证人数变化消息消费者
    x.AddConsumer<CoworkingVerificationVotesConsumer>();

    // 注册城市评分更新消息消费者
    x.AddConsumer<CityRatingUpdatedMessageConsumer>();

    // 注册城市评论更新消息消费者
    x.AddConsumer<CityReviewUpdatedMessageConsumer>();

    // 注册城市版主变更消息消费者
    x.AddConsumer<CityModeratorUpdatedMessageConsumer>();

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

        // ⚠️ 不要自定义 Exchange 名称，使用 MassTransit 默认命名：Shared.Messages:AIProgressMessage
        // 这样才能与 AIService 的发布匹配
        // cfg.Message<Shared.Messages.AIProgressMessage>(x => x.SetEntityName("ai.realtime.progress.exchange"));
        // cfg.Message<Shared.Messages.AITaskCompletedMessage>(x => x.SetEntityName("ai.task.completed.exchange"));
        // cfg.Message<Shared.Messages.AITaskFailedMessage>(x => x.SetEntityName("ai.task.failed.exchange"));

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

        // 配置城市图片生成完成消息队列
        cfg.ReceiveEndpoint("city-image-generated-queue", e =>
        {
            e.ConfigureConsumer<CityImageGeneratedMessageConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        // 配置聊天室在线状态消息队列
        cfg.ReceiveEndpoint("chatroom-online-status-queue", e =>
        {
            e.ConfigureConsumer<ChatRoomOnlineStatusConsumer>(context);
            e.PrefetchCount = 32; // 在线状态消息频繁，增加并发
            e.UseMessageRetry(r => r.Interval(2, TimeSpan.FromSeconds(2)));
        });

        // 配置 Coworking 验证人数变化消息队列
        cfg.ReceiveEndpoint("coworking-verification-votes-queue", e =>
        {
            e.ConfigureConsumer<CoworkingVerificationVotesConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        // 配置城市评分更新消息队列
        cfg.ReceiveEndpoint("city-rating-updated-queue", e =>
        {
            e.ConfigureConsumer<CityRatingUpdatedMessageConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        // 配置城市评论更新消息队列
        cfg.ReceiveEndpoint("city-review-updated-queue", e =>
        {
            e.ConfigureConsumer<CityReviewUpdatedMessageConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        // 配置城市版主变更消息队列
        cfg.ReceiveEndpoint("city-moderator-updated-queue", e =>
        {
            e.ConfigureConsumer<CityModeratorUpdatedMessageConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });

        // 配置 AI Chat 流式响应消息队列
        cfg.ReceiveEndpoint("ai-chat-stream-chunk-queue", e =>
        {
            e.ConfigureConsumer<AIChatStreamChunkConsumer>(context);
            e.PrefetchCount = 64; // 流式消息非常频繁，增加并发
            e.UseMessageRetry(r => r.Interval(2, TimeSpan.FromSeconds(1)));
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

// 启用 UserContext 中间件（从 Gateway 传递的请求头获取用户信息）
app.UseUserContext();

app.MapControllers();

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

// 映射 SignalR Hubs
app.MapHub<AIProgressHub>("/hubs/ai-progress");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");

Log.Information("MessageService 启动成功，监听端口: {Port}",
    builder.Configuration["ASPNETCORE_URLS"] ?? "http://+:8080");

app.Run();
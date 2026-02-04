using EventService.API.Hubs;
using EventService.Application.Services;
using EventService.BackgroundServices;
using EventService.Domain.Repositories;
using EventService.Infrastructure.Consumers;
using EventService.Infrastructure.GrpcClients;
using EventService.Infrastructure.Repositories;
using GoNomads.Shared.Extensions;
using GoNomads.Shared.Observability;
using MassTransit;
using Microsoft.OpenApi.Models;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;

const string serviceName = "EventService";

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/eventservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// OpenTelemetry 可观测性配置 (Traces + Metrics + Logs)
// ============================================================
builder.Services.AddGoNomadsObservability(builder.Configuration, serviceName);
builder.Logging.AddGoNomadsLogging(builder.Configuration, serviceName);

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

// 注册仓储 (Infrastructure Layer)
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventParticipantRepository, EventParticipantRepository>();
builder.Services.AddScoped<IEventFollowerRepository, EventFollowerRepository>();
builder.Services.AddScoped<IEventTypeRepository, EventTypeRepository>();
builder.Services.AddScoped<IEventInvitationRepository, EventInvitationRepository>();

// 注册 gRPC 客户端 (通过 Dapr Service Invocation)
builder.Services.AddScoped<ICityGrpcClient, CityGrpcClient>();
builder.Services.AddScoped<IUserGrpcClient, UserGrpcClient>();

// 注册应用服务 (Application Layer)
builder.Services.AddScoped<IEventService, EventApplicationService>();
builder.Services.AddScoped<IEventTypeService, EventTypeService>();

// 注册 Meetup 实时通知服务
builder.Services.AddScoped<IMeetupNotificationService, MeetupNotificationService>();

// 注册后台服务
builder.Services.AddHostedService<EventStatusUpdateService>();

// 配置 MassTransit + RabbitMQ（用于接收用户和城市信息更新事件）
builder.Services.AddMassTransit(x =>
{
    // 注册事件消费者
    x.AddConsumer<UserUpdatedMessageConsumer>();
    x.AddConsumer<CityUpdatedMessageConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(rabbitMqConfig["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitMqConfig["Username"] ?? "guest");
            h.Password(rabbitMqConfig["Password"] ?? "guest");
        });

        // 配置接收端点用于消费事件
        cfg.ReceiveEndpoint("event-service-user-updated", e =>
        {
            e.ConfigureConsumer<UserUpdatedMessageConsumer>(context);
        });

        cfg.ReceiveEndpoint("event-service-city-updated", e =>
        {
            e.ConfigureConsumer<CityUpdatedMessageConsumer>(context);
        });
    });
});

// 配置 DaprClient - 方案A: 使用 HTTP 端点（原生支持 InvokeMethodAsync，访问控制策略自动生效）
// 在 container sidecar 模式下，EventService 和 Dapr 共享网络命名空间，使用 localhost
builder.Services.AddDaprClient(daprClientBuilder =>
{
    // 使用 HTTP 端点（默认端口 3500）
    var daprHttpPort = builder.Configuration.GetValue("Dapr:HttpPort", 3500);
    var daprHttpEndpoint = $"http://localhost:{daprHttpPort}";

    daprClientBuilder.UseHttpEndpoint(daprHttpEndpoint);

    // 记录配置
    var logger = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole()).CreateLogger("DaprSetup");
    logger.LogInformation("🚀 Dapr Client 配置使用 HTTP: {Endpoint}", daprHttpEndpoint);
});

// Add services to the container.
builder.Services.AddControllers().AddDapr();

// 添加 SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// 添加 CORS（支持 SignalR WebSocket 连接）
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5080",
                "http://10.0.2.2:5080",      // Android 模拟器
                "http://59.46.235.173:5080"  // 真机测试
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();  // SignalR 需要这个
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 配置正确的服务器 URL
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "http://localhost:8005", Description = "Local Development" }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapOpenApi();

// Configure Scalar UI
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Event Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithEndpointPrefix("/scalar/{documentName}");
});

app.UseSerilogRequestLogging();

// 使用 CORS - SignalR 需要 AllowCredentials
app.UseCors("SignalRPolicy");

app.UseRouting();

// Enable Prometheus metrics
app.UseHttpMetrics();

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

// Map controllers
app.MapControllers();

// 映射 SignalR Hub
app.MapHub<MeetupHub>("/hubs/meetup");

// Add health check endpoint
app.MapGet("/health",
    () => Results.Ok(new { status = "healthy", service = "EventService", timestamp = DateTime.UtcNow }));

// Map Prometheus metrics endpoint
app.MapMetrics();

Log.Information("Event Service starting on port 8005...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();
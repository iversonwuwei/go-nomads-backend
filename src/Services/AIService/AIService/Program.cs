using AIService.API.Hubs;
using AIService.Application.Services;
using AIService.Domain.Repositories;
using AIService.Infrastructure.Cache;
using AIService.Infrastructure.GrpcClients;
using AIService.Infrastructure.Repositories;
using GoNomads.Shared.Extensions;
using MassTransit;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// OpenTelemetry 可观测性配置 (Traces + Metrics + Logs)
// ============================================================
builder.AddServiceDefaults();

// 注册 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

// 注册仓储 (Infrastructure Layer)
builder.Services.AddScoped<IAIConversationRepository, AIConversationRepository>();
builder.Services.AddScoped<IAIMessageRepository, AIMessageRepository>();
builder.Services.AddScoped<ITravelPlanRepository, TravelPlanRepository>();

// 注册 gRPC 客户端 (typed HttpClient)
builder.Services.AddServiceClient<IUserGrpcClient, UserGrpcClient>("user-service");
builder.Services.AddServiceClient<ICityGrpcClient, CityGrpcClient>("city-service");

// Named HttpClient for controllers using IHttpClientFactory
builder.Services.AddServiceClient("city-service");

// 配置 Semantic Kernel - 使用 Qwen 模型
try
{
    var qwenApiKey = builder.Configuration["Qwen:ApiKey"] ?? "test-key";
    var qwenBaseUrl = builder.Configuration["Qwen:BaseUrl"] ?? "https://dashscope.aliyuncs.com/compatible-mode/v1";
    var defaultModelId = builder.Configuration["SemanticKernel:DefaultModel"] ?? "qwen-plus";

    // 配置 HttpClient 用于 Qwen API 调用，增加超时时间
    builder.Services.AddHttpClient("QwenClient", client =>
    {
        client.Timeout = TimeSpan.FromMinutes(3); // 增加超时到 3 分钟（AI 生成可能需要较长时间）
        client.DefaultRequestHeaders.Add("User-Agent", "GoNomads-AIService/1.0");
    });

#pragma warning disable SKEXP0010
    var kernelBuilder = Kernel.CreateBuilder();

    // 创建配置了超时和连接设置的 HttpClient for Qwen API
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        MaxConnectionsPerServer = 10
    };

    var httpClient = new HttpClient(handler)
    {
        Timeout = TimeSpan.FromMinutes(3) // 3 分钟超时
    };
    httpClient.DefaultRequestHeaders.Add("User-Agent", "GoNomads-AIService/1.0");
    httpClient.DefaultRequestHeaders.ConnectionClose = false; // 保持连接

    kernelBuilder.AddOpenAIChatCompletion(
        defaultModelId,
        apiKey: qwenApiKey,
        endpoint: new Uri(qwenBaseUrl),
        httpClient: httpClient);

    var kernel = kernelBuilder.Build();
    builder.Services.AddSingleton(kernel);
#pragma warning restore SKEXP0010

    Log.Information("✅ Qwen AI 模型配置成功（超时: 3分钟）");
}
catch (Exception ex)
{
    // 忽略 Semantic Kernel 配置错误，服务仍可正常启动
    Log.Warning(ex, "⚠️ Semantic Kernel 配置失败，AI 功能可能不可用");
}

// 注册应用服务 (Application Layer)
builder.Services.AddScoped<IAIChatService, AIChatApplicationService>();

// 注册图片生成服务 (通义万象)
builder.Services.AddHttpClient("WanxClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // 图片生成可能需要较长时间
    client.DefaultRequestHeaders.Add("User-Agent", "GoNomads-AIService/1.0");
});
builder.Services.AddScoped<IImageGenerationService, ImageGenerationService>();
Log.Information("✅ 图片生成服务已注册（通义万象 + Supabase Storage）");

// 配置 MassTransit + RabbitMQ（使用 Aspire 注入的连接字符串）
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("rabbitmq");
        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(new Uri(connectionString));
        }
        else
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
        }

        cfg.ConfigureEndpoints(context);
    });
});

// 注册 Redis 缓存服务 (Aspire 集成 - 自动注册 IConnectionMultiplexer)
builder.AddRedisClient("redis");
builder.Services.AddSingleton<IRedisCache, RedisCache>();

// 注册 SignalR 通知服务
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();

// AIWorkerService 已废弃: 现在使用 Controller 中的 Task.Run() 直接处理 AI 生成任务

Log.Information("✅ MassTransit、缓存服务已注册");

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });

    // SignalR 需要更宽松的 CORS 策略
    options.AddPolicy("SignalRPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:5000", "http://localhost:8009")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // SignalR 需要允许凭据
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
            new() { Url = "http://localhost:8009", Description = "Local Development" }
        };

        // 添加 API 信息
        document.Info.Title = "AI Service API";
        document.Info.Description = "Go Nomads AI 聊天服务 - 基于 Qwen 大模型和 Semantic Kernel";
        document.Info.Version = "v1.0";

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
        .WithTitle("AI Service API")
        .WithTheme(ScalarTheme.Mars)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithEndpointPrefix("/scalar/{documentName}")
        .WithModels(false); // 简化文档显示
});

app.UseSerilogRequestLogging();

app.UseRouting();

// Enable CORS
app.UseCors();

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

// Map controllers
app.MapControllers();

// Map SignalR Hub
app.MapHub<NotificationHub>("/hubs/notifications");

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

// AI 服务专用健康检查
app.MapGet("/health/ai", () =>
{
    var defaultModel = builder.Configuration["SemanticKernel:DefaultModel"] ?? "qwen-plus";
    return Results.Ok(new
    {
        status = "healthy",
        ai_service = "connected",
        model = defaultModel,
        provider = "Qwen",
        max_tokens = 32000,
        timestamp = DateTime.UtcNow
    });
});

// Map Prometheus metrics endpoint (已迁移到 Aspire OpenTelemetry)

// 启动时日志
app.Lifetime.ApplicationStarted.Register(() =>
{
    Log.Information("🤖 AI Service 启动成功!");
    Log.Information("📊 Scalar API 文档: http://localhost:8009/scalar/v1");
    Log.Information("🔍 健康检查: http://localhost:8009/health");
    Log.Information("🧠 AI 健康检查: http://localhost:8009/health/ai");
    Log.Information("📈 监控指标: http://localhost:8009/metrics");
});

app.Run();
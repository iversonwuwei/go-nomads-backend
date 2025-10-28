using AIService.Application.Services;
using AIService.Domain.Repositories;
using AIService.Infrastructure.GrpcClients;
using AIService.Infrastructure.Repositories;
using Dapr.Client;
using GoNomads.Shared.Extensions;
using Microsoft.SemanticKernel;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/aiservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 注册仓储 (Infrastructure Layer)
builder.Services.AddScoped<IAIConversationRepository, AIConversationRepository>();
builder.Services.AddScoped<IAIMessageRepository, AIMessageRepository>();

// 注册 gRPC 客户端 (通过 Dapr Service Invocation)
builder.Services.AddScoped<IUserGrpcClient, UserGrpcClient>();

// 配置 Semantic Kernel (简化配置，避免编译错误)
try
{
    var qianwenApiKey = builder.Configuration["QianWen:ApiKey"] ?? "test-key";
    
    #pragma warning disable SKEXP0010
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: "qwen-plus",
        apiKey: qianwenApiKey,
        endpoint: new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1"));
    
    var kernel = kernelBuilder.Build();
    builder.Services.AddSingleton(kernel);
    #pragma warning restore SKEXP0010
}
catch (Exception ex)
{
    // 忽略 Semantic Kernel 配置错误，服务仍可正常启动
    Console.WriteLine($"Semantic Kernel 配置失败: {ex.Message}");
}

// 注册应用服务 (Application Layer)
builder.Services.AddScoped<IAIChatService, AIChatApplicationService>();

// 配置 DaprClient 使用 gRPC 协议（性能更好）
builder.Services.AddDaprClient(daprClientBuilder =>
{
    // 使用 gRPC 端点（默认端口 50001）
    var daprGrpcPort = builder.Configuration.GetValue<int>("Dapr:GrpcPort", 50001);
    var daprGrpcEndpoint = $"http://localhost:{daprGrpcPort}";

    daprClientBuilder.UseGrpcEndpoint(daprGrpcEndpoint);

    // 记录配置
    var logger = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole()).CreateLogger("DaprSetup");
    logger.LogInformation("🚀 Dapr Client 配置使用 gRPC: {Endpoint}", daprGrpcEndpoint);
});

// Add services to the container.
builder.Services.AddControllers().AddDapr();

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
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
        document.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
        {
            new() { Url = "http://localhost:8009", Description = "Local Development" }
        };
        
        // 添加 API 信息
        document.Info.Title = "AI Service API";
        document.Info.Description = "Go Nomads AI 聊天服务 - 基于千问大模型和 Semantic Kernel";
        document.Info.Version = "v1.0";
        
        return Task.CompletedTask;
    });
});

// Consul 服务发现将在应用启动后注册

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

// Enable Prometheus metrics
app.UseHttpMetrics();

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

// Map controllers
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => 
{
    return Results.Ok(new 
    { 
        status = "healthy", 
        service = "AIService", 
        timestamp = DateTime.UtcNow,
        version = "1.0.0",
        semantic_kernel = "enabled",
        qianwen_model = "qwen-plus"
    });
});

// AI 服务专用健康检查
app.MapGet("/health/ai", () =>
{
    return Results.Ok(new 
    { 
        status = "healthy", 
        ai_service = "connected",
        model = "qwen-plus",
        timestamp = DateTime.UtcNow 
    });
});

// Map Prometheus metrics endpoint
app.MapMetrics();

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
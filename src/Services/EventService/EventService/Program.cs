using Dapr.Client;
using Scalar.AspNetCore;
using Prometheus;
using Shared.Extensions;
using Serilog;
using GoNomads.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/eventservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 注册仓储 (Infrastructure Layer)
builder.Services.AddScoped<EventService.Domain.Repositories.IEventRepository, EventService.Infrastructure.Repositories.EventRepository>();
builder.Services.AddScoped<EventService.Domain.Repositories.IEventParticipantRepository, EventService.Infrastructure.Repositories.EventParticipantRepository>();
builder.Services.AddScoped<EventService.Domain.Repositories.IEventFollowerRepository, EventService.Infrastructure.Repositories.EventFollowerRepository>();

// 注册应用服务 (Application Layer)
builder.Services.AddScoped<EventService.Application.Services.IEventService, EventService.Application.Services.EventApplicationService>();

// 配置 DaprClient 使用 gRPC 协议（性能更好）
// 在 container sidecar 模式下，EventService 和 Dapr 共享网络命名空间，使用 localhost
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

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 配置正确的服务器 URL
        document.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
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

app.UseRouting();

// Enable Prometheus metrics
app.UseHttpMetrics();

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

// Map controllers
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "EventService", timestamp = DateTime.UtcNow }));

// Map Prometheus metrics endpoint
app.MapMetrics();

Log.Information("Event Service starting on port 8005...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();

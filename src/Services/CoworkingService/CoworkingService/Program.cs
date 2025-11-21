using CoworkingService.Application.Services;
using CoworkingService.Domain.Repositories;
using CoworkingService.Infrastructure.Repositories;
using GoNomads.Shared.Extensions;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 配置 DaprClient 使用 gRPC 协议
// 在 container sidecar 模式下，CoworkingService 和 Dapr 共享网络命名空间，使用 localhost
builder.Services.AddDaprClient(daprClientBuilder =>
{
    // 使用 gRPC 端点（默认端口 50001）
    var daprGrpcPort = builder.Configuration.GetValue("Dapr:GrpcPort", 50001);
    var daprGrpcEndpoint = $"http://localhost:{daprGrpcPort}";

    daprClientBuilder.UseGrpcEndpoint(daprGrpcEndpoint);

    // 记录配置
    var logger = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole()).CreateLogger("DaprSetup");
    logger.LogInformation("🚀 Dapr Client 配置使用 gRPC: {Endpoint}", daprGrpcEndpoint);
});

// ============================================================
// DDD 架构依赖注入配置
// ============================================================

// Infrastructure Layer - 仓储实现
builder.Services.AddScoped<ICoworkingRepository, CoworkingRepository>();
builder.Services.AddScoped<ICoworkingBookingRepository, CoworkingBookingRepository>();
builder.Services.AddScoped<ICoworkingVerificationRepository, CoworkingVerificationRepository>();
builder.Services.AddScoped<ICoworkingCommentRepository, CoworkingCommentRepository>();
builder.Services.AddScoped<ICoworkingReviewRepository, CoworkingReviewRepository>();

// Application Layer - 应用服务
builder.Services.AddScoped<ICoworkingService, CoworkingApplicationService>();
builder.Services.AddScoped<ICoworkingReviewService, CoworkingReviewService>();

// External Services - 外部服务客户端
builder.Services.AddScoped<CoworkingService.Services.ICacheServiceClient, CoworkingService.Services.CacheServiceClient>();
builder.Services.AddScoped<CoworkingService.Services.IUserServiceClient, CoworkingService.Services.UserServiceClient>();

// Domain Layer 不需要注册（纯 POCO）

// 添加控制器
builder.Services.AddControllers()
    .AddDapr()
    .AddJsonOptions(options =>
    {
        // 配置 JSON 序列化为 camelCase（默认行为，但显式配置更清晰）
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 添加 OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// 添加 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// 添加健康检查
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Coworking Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseUserContext();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("CoworkingService 正在启动...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CoworkingService 启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
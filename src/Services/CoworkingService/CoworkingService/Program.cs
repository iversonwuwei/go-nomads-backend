using CoworkingService.Application.Services;
using CoworkingService.Domain.Repositories;
using CoworkingService.Infrastructure.Repositories;
using GoNomads.Shared.Extensions;
using MassTransit;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

const string serviceName = "CoworkingService";

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// OpenTelemetry 可观测性配置 (Traces + Metrics + Logs)
// ============================================================
builder.AddServiceDefaults();

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

// External Services - 外部服务客户端 (typed HttpClient)
builder.Services.AddServiceClient<CoworkingService.Services.ICacheServiceClient, CoworkingService.Services.CacheServiceClient>("cache-service");
builder.Services.AddServiceClient<CoworkingService.Services.IUserServiceClient, CoworkingService.Services.UserServiceClient>("user-service");
builder.Services.AddServiceClient<CoworkingService.Services.ICityServiceClient, CoworkingService.Services.CityServiceClient>("city-service");

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

// Domain Layer 不需要注册（纯 POCO）

// 配置 MassTransit + RabbitMQ（用于发布消息到 MessageService 和接收事件）
builder.Services.AddMassTransit(x =>
{
    // 注册事件消费者
    x.AddConsumer<CoworkingService.Infrastructure.Consumers.UserUpdatedMessageConsumer>();
    x.AddConsumer<CoworkingService.Infrastructure.Consumers.CityUpdatedMessageConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(rabbitMqConfig["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitMqConfig["Username"] ?? "guest");
            h.Password(rabbitMqConfig["Password"] ?? "guest");
        });

        // 配置接收端点用于消费事件
        cfg.ReceiveEndpoint("coworking-service-user-updated", e =>
        {
            e.ConfigureConsumer<CoworkingService.Infrastructure.Consumers.UserUpdatedMessageConsumer>(context);
        });

        cfg.ReceiveEndpoint("coworking-service-city-updated", e =>
        {
            e.ConfigureConsumer<CoworkingService.Infrastructure.Consumers.CityUpdatedMessageConsumer>(context);
        });
    });
});

// 添加控制器
builder.Services.AddControllers()
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
// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

Log.Information("CoworkingService 正在启动...");

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
using GoNomads.Shared.Extensions;
using InnovationService.Infrastructure.Consumers;
using InnovationService.Repositories;
using InnovationService.Services;
using MassTransit;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;

const string serviceName = "InnovationService";

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/innovationservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// OpenTelemetry 可观测性配置 (Traces + Metrics + Logs)
// ============================================================
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "http://localhost:8011", Description = "Local Development" }
        };
        return Task.CompletedTask;
    });
});

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

// 注册服务客户端 (typed HttpClient)
builder.Services.AddServiceClient<IUserServiceClient, UserServiceClient>("user-service");

// 注册 Repository
builder.Services.AddScoped<IInnovationRepository, InnovationRepository>();

// 配置 MassTransit + RabbitMQ（用于接收用户信息更新事件）
builder.Services.AddMassTransit(x =>
{
    // 注册事件消费者
    x.AddConsumer<UserUpdatedMessageConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(rabbitMqConfig["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitMqConfig["Username"] ?? "guest");
            h.Password(rabbitMqConfig["Password"] ?? "guest");
        });

        // 配置接收端点用于消费事件
        cfg.ReceiveEndpoint("innovation-service-user-updated", e =>
        {
            e.ConfigureConsumer<UserUpdatedMessageConsumer>(context);
        });
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置 HTTP 请求管道
app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

app.UseRouting();

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Innovation Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

app.MapControllers();

app.Run();
using AccommodationService.Application.Services;
using AccommodationService.Domain.Repositories;
using AccommodationService.Infrastructure.Repositories;
using GoNomads.Shared.Extensions;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

const string serviceName = "AccommodationService";

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

// 跨服务调用客户端 (typed HttpClient)
builder.Services.AddServiceClient<AccommodationService.Services.IUserServiceClient, AccommodationService.Services.UserServiceClient>("user-service");

// ============================================================
// DDD 架构依赖注入配置
// ============================================================

// Infrastructure Layer - 仓储实现
builder.Services.AddScoped<IHotelRepository, HotelRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<IHotelReviewRepository, HotelReviewRepository>();

// Application Layer - 应用服务
builder.Services.AddScoped<IHotelService, HotelApplicationService>();

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
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

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");

// 用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Accommodation Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// 映射控制器路由
app.MapControllers();

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

app.Run();
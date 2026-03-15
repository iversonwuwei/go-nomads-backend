using AccommodationService.Application.Services;
using AccommodationService.Domain.Repositories;
using AccommodationService.Infrastructure.Repositories;
using AccommodationService.Services;
using GoNomads.Shared.Extensions;
using Scalar.AspNetCore;
using Serilog;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

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
builder.Services.AddServiceClient<ICityServiceClient, CityServiceClient>("city-service");

builder.Services.Configure<BookingDemandOptions>(builder.Configuration.GetSection("BookingDemand"));
builder.Services.AddHttpClient<IBookingDemandClient, BookingDemandClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BookingDemandOptions>>().Value;
    var baseUrl = options.UseSandbox
        ? "https://demandapi-sandbox.booking.com/3.1/"
        : "https://demandapi.booking.com/3.1/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(options.TimeoutSeconds, 5));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    if (!string.IsNullOrWhiteSpace(options.Token))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
    }

    if (!string.IsNullOrWhiteSpace(options.AffiliateId))
    {
        client.DefaultRequestHeaders.Add("X-Affiliate-Id", options.AffiliateId);
    }
});

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
    .WithTitle("Accommodation Service API - Resilient Hotel Fallback")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// 映射控制器路由
app.MapControllers();

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

app.Run();
using System.Text;
using GoNomads.Shared.Extensions;
using GoNomads.Shared.Security;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using UserService.Application.Services;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Configuration;
using UserService.Infrastructure.Repositories;
using UserService.Infrastructure.Services;

const string serviceName = "user-service";

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// OpenTelemetry 可观测性配置 (Traces + Metrics + Logs)
// ============================================================
builder.AddServiceDefaults();

// 注册 IDistributedCache（Redis 或内存后备）
var redisConn = builder.Configuration.GetConnectionString("redis");
if (!string.IsNullOrEmpty(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = "user-service:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// 添加 Supabase 客户端（使用 Shared 扩展方法）
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

// 添加 JWT Token 服务
builder.Services.AddSingleton<JwtTokenService>();

// 配置 PayPal
builder.Services.Configure<PayPalSettings>(builder.Configuration.GetSection(PayPalSettings.SectionName));
builder.Services.AddHttpClient<IPayPalService, PayPalService>();

// 配置抖音
builder.Services.Configure<DouyinSettings>(builder.Configuration.GetSection(DouyinSettings.SectionName));
builder.Services.AddHttpClient<IDouyinService, DouyinService>();

// 配置阿里云短信
builder.Services.Configure<AliyunSmsSettings>(builder.Configuration.GetSection(AliyunSmsSettings.SectionName));
builder.Services.AddSingleton<IAliyunSmsService, AliyunSmsService>();

// 配置微信 OAuth 服务
builder.Services.AddHttpClient<IWeChatOAuthService, WeChatOAuthService>();

// 配置 Google OAuth 服务
builder.Services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>();

// 配置 Twitter OAuth 服务
builder.Services.AddHttpClient<ITwitterOAuthService, TwitterOAuthService>();

// Register Domain Repositories (Infrastructure Layer)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserStatsRepository, UserStatsRepository>();
builder.Services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
builder.Services.AddScoped<IMembershipPlanRepository, MembershipPlanRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<ITravelHistoryRepository, TravelHistoryRepository>();
builder.Services.AddScoped<IVisitedPlaceRepository, VisitedPlaceRepository>();

// Register Application Services
builder.Services.AddScoped<IUserService, UserApplicationService>();
builder.Services.AddScoped<IAuthService, AuthApplicationService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IInterestService, InterestService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITravelHistoryService, TravelHistoryService>();
builder.Services.AddScoped<IVisitedPlaceService, VisitedPlaceService>();

// Register Service Clients (for inter-service communication)
builder.Services.AddServiceClient<ICityServiceClient, CityServiceClient>("city-service");

// Named HttpClient for controllers using IHttpClientFactory
builder.Services.AddServiceClient("city-service");
builder.Services.AddServiceClient("product-service");
builder.Services.AddServiceClient("event-service");

// 配置 MassTransit + RabbitMQ（用于发布用户更新事件）
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(rabbitMqConfig["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitMqConfig["Username"] ?? "guest");
            h.Password(rabbitMqConfig["Password"] ?? "guest");
        });
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 添加 JWT 认证
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();

// Configure Scalar UI
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("User Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseRouting();

// 启用认证和授权中间件
app.UseAuthentication();
app.UseAuthorization();

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

// Map controllers
app.MapControllers();

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

app.Run();
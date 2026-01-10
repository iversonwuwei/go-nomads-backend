using System.Text;
using CityService.Application.Abstractions.Services;
using CityService.Application.Services;
using CityService.Domain.Repositories;
using CityService.Infrastructure.Auth;
using CityService.Infrastructure.Consumers;
using CityService.Infrastructure.Integrations.Geocoding;
using CityService.Infrastructure.Integrations.Weather;
using CityService.Infrastructure.Repositories;
using CityService.Infrastructure.Services;
using CityService.Services;
using GoNomads.Shared.Extensions;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Shared.Messages;
using IUserCityContentService = CityService.Application.Services.IUserCityContentService;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 - 容器内监听 8080，外部通过 docker-compose 映射到 8002
// builder.WebHost.UseUrls("http://localhost:8002"); // 注释掉，使用环境变量 ASPNETCORE_URLS

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cityservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddDapr()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 配置正确的服务器 URL
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "http://localhost:8002", Description = "Local Development" }
        };
        return Task.CompletedTask;
    });
});

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

// 配置 DaprClient - 使用 gRPC 端点
var daprGrpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "50001";
builder.Services.AddDaprClient(daprClientBuilder =>
{
    daprClientBuilder.UseGrpcEndpoint($"http://localhost:{daprGrpcPort}");
});

// 注册服务客户端
builder.Services.AddScoped<IUserServiceClient, UserServiceClient>();
builder.Services.AddScoped<ICacheServiceClient, CacheServiceClient>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            // 映射 JWT 中的 role claim 到 .NET 的 Role claim type
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Register Services - Domain Repositories
builder.Services.AddScoped<ICityRepository, SupabaseCityRepository>();
builder.Services.AddScoped<ICountryRepository, SupabaseCountryRepository>();
builder.Services.AddScoped<IProvinceRepository, SupabaseProvinceRepository>();
builder.Services.AddScoped<IUserCityPhotoRepository, SupabaseUserCityPhotoRepository>();
builder.Services.AddScoped<IUserCityExpenseRepository, SupabaseUserCityExpenseRepository>();
builder.Services.AddScoped<IUserCityReviewRepository, SupabaseUserCityReviewRepository>();
builder.Services.AddScoped<IUserCityProsConsRepository, SupabaseUserCityProsConsRepository>();
builder.Services.AddScoped<IUserFavoriteCityRepository, SupabaseUserFavoriteCityRepository>();
builder.Services.AddScoped<IGeoNamesCityRepository, SupabaseGeoNamesCityRepository>();
builder.Services.AddScoped<IDigitalNomadGuideRepository, SupabaseDigitalNomadGuideRepository>();
builder.Services.AddScoped<INearbyCityRepository, SupabaseNearbyCityRepository>();
builder.Services.AddScoped<ICityModeratorRepository, CityModeratorRepository>();
builder.Services.AddScoped<IModeratorApplicationRepository, ModeratorApplicationRepository>();
builder.Services.AddScoped<IModeratorTransferRepository, ModeratorTransferRepository>();
builder.Services.AddScoped<ICityRatingCategoryRepository, CityRatingCategoryRepository>();
builder.Services.AddScoped<ICityRatingRepository, CityRatingRepository>();
builder.Services.AddScoped<IWeatherCacheRepository, WeatherCacheRepository>();

// Application Services
builder.Services.AddScoped<ICityService, CityApplicationService>();
builder.Services.AddScoped<IUserCityContentService, UserCityContentApplicationService>();
builder.Services.AddScoped<IUserFavoriteCityService, UserFavoriteCityService>();
builder.Services.AddScoped<IGeoNamesImportService, GeoNamesImportService>();
builder.Services.AddScoped<IDigitalNomadGuideService, DigitalNomadGuideService>();
builder.Services.AddScoped<INearbyCityService, NearbyCityService>();
builder.Services.AddScoped<IModeratorApplicationService, ModeratorApplicationService>();
builder.Services.AddScoped<IModeratorTransferService, ModeratorTransferService>();
builder.Services.AddScoped<GeographyDataSeeder>();
builder.Services.AddScoped<RatingCategorySeeder>();

// 添加内存缓存 - 用于天气数据缓存
builder.Services.AddMemoryCache();

// 注册后台天气刷新服务
builder.Services.AddHostedService<WeatherCacheRefreshService>();

// 注册天气服务
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpClient<IAmapGeocodingService, CityService.Infrastructure.Integrations.Geocoding.AmapGeocodingService>();

// 注册城市匹配服务
builder.Services.AddScoped<IGeocodingService, CityService.Application.Services.AmapGeocodingService>();
builder.Services.AddScoped<ICityMatchingService, CityMatchingService>();

// 注册 AIService 客户端 (使用 Dapr Service Invocation)
builder.Services.AddScoped<CityService.Infrastructure.Clients.IAIServiceClient, CityService.Infrastructure.Clients.AIServiceClient>();

// 配置 MassTransit - 用于接收 AIService 的图片生成完成消息
builder.Services.AddMassTransit(x =>
{
    // 注册消费者
    x.AddConsumer<CityImageGeneratedMessageConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ");

        cfg.Host(rabbitMqConfig["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitMqConfig["Username"] ?? "guest");
            h.Password(rabbitMqConfig["Password"] ?? "guest");
        });

        // 配置城市图片生成完成消息队列
        cfg.ReceiveEndpoint("city-image-generated-cityservice-queue", e =>
        {
            e.ConfigureConsumer<CityImageGeneratedMessageConsumer>(context);
            e.PrefetchCount = 16;
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("City Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithEndpointPrefix("/scalar/{documentName}");
});

// 后台服务已通过 HostedService 运行

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health",
    () => Results.Ok(new { status = "healthy", service = "CityService", timestamp = DateTime.UtcNow }));

// 天气缓存刷新服务已通过 BackgroundService 自动运行
Log.Information("Weather cache refresh service enabled (refresh every 30 minutes)");

Log.Information("City Service starting on port 8002...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();
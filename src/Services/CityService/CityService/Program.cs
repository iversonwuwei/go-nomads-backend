using System.Text;
using CityService.Application.Abstractions.Services;
using CityService.Application.Services;
using CityService.Domain.Repositories;
using CityService.Infrastructure.Integrations.Geocoding;
using CityService.Infrastructure.Integrations.Weather;
using CityService.Infrastructure.Repositories;
using CityService.Services;
using GoNomads.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
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
builder.Services.AddControllers().AddDapr();
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

// 配置 DaprClient
builder.Services.AddDaprClient();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
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
builder.Services.AddScoped<ICityModeratorRepository, CityModeratorRepository>();
builder.Services.AddScoped<ICityRatingCategoryRepository, CityRatingCategoryRepository>();
builder.Services.AddScoped<ICityRatingRepository, CityRatingRepository>();

// Application Services
builder.Services.AddScoped<ICityService, CityApplicationService>();
builder.Services.AddScoped<IUserCityContentService, UserCityContentApplicationService>();
builder.Services.AddScoped<IUserFavoriteCityService, UserFavoriteCityService>();
builder.Services.AddScoped<IGeoNamesImportService, GeoNamesImportService>();
builder.Services.AddScoped<IDigitalNomadGuideService, DigitalNomadGuideService>();
builder.Services.AddScoped<GeographyDataSeeder>();

// 添加内存缓存
builder.Services.AddMemoryCache();

// 注册天气服务
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddHttpClient<IAmapGeocodingService, AmapGeocodingService>();

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

Log.Information("City Service starting on port 8002...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();
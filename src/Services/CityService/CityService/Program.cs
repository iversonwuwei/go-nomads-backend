using CityService.Application.Abstractions.Services;
using CityService.Application.Services;
using CityService.Domain.Repositories;
using CityService.Infrastructure.Repositories;
using CityService.Infrastructure.Integrations.Weather;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Shared.Extensions;
using Dapr.Client;
using Scalar.AspNetCore;

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
        document.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
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

// Register Services
builder.Services.AddScoped<ICityRepository, SupabaseCityRepository>();
builder.Services.AddScoped<ICountryRepository, SupabaseCountryRepository>();
builder.Services.AddScoped<IProvinceRepository, SupabaseProvinceRepository>();
builder.Services.AddScoped<ICityService, CityApplicationService>();
builder.Services.AddScoped<GeographyDataSeeder>();

// 添加内存缓存
builder.Services.AddMemoryCache();

// 注册天气服务
builder.Services.AddHttpClient<IWeatherService, WeatherService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "CityService", timestamp = DateTime.UtcNow }));

Log.Information("City Service starting on port 8002...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();

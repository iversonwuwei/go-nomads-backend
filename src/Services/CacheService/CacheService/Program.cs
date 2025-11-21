using CacheService.Application.Abstractions.Services;
using CacheService.Application.Services;
using CacheService.Domain.Repositories;
using CacheService.Infrastructure.Integrations;
using CacheService.Infrastructure.Repositories;
using GoNomads.Shared.Extensions;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cacheservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers().AddDapr();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "http://localhost:8010", Description = "Local Development" }
        };
        return Task.CompletedTask;
    });
});

// 配置 Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
    ?? throw new InvalidOperationException("Redis connection string not configured");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectTimeout = 5000;
    configuration.SyncTimeout = 5000;
    return ConnectionMultiplexer.Connect(configuration);
});

// 配置 DaprClient
builder.Services.AddDaprClient();

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

// 注册 Domain Repositories
builder.Services.AddScoped<IScoreCacheRepository, RedisScoreCacheRepository>();
builder.Services.AddScoped<ICostCacheRepository, RedisCostCacheRepository>();

// 注册 Infrastructure 集成客户端
builder.Services.AddScoped<ICityServiceClient, CityServiceClient>();
builder.Services.AddScoped<ICoworkingServiceClient, CoworkingServiceClient>();

// 注册 Application Services
builder.Services.AddScoped<IScoreCacheService, ScoreCacheApplicationService>();
builder.Services.AddScoped<ICostCacheService, CostCacheApplicationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Cache Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithEndpointPrefix("/scalar/{documentName}");
});

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

// 使用用户上下文中间件
app.UseUserContext();

app.MapControllers();

// Health check endpoint
app.MapGet("/health",
    () => Results.Ok(new { status = "healthy", service = "CacheService", timestamp = DateTime.UtcNow }));

Log.Information("Cache Service starting on port 8010...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();

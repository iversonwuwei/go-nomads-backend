using GoNomads.Shared.Extensions;
using Scalar.AspNetCore;
using SearchService.Application.Interfaces;
using SearchService.Application.Services;
using SearchService.Infrastructure.Configuration;
using SearchService.Infrastructure.Services;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/searchservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================================
// 配置绑定
// ============================================================
builder.Services.Configure<ElasticsearchSettings>(
    builder.Configuration.GetSection("Elasticsearch"));
builder.Services.Configure<IndexSettings>(
    builder.Configuration.GetSection("IndexSettings"));

// ============================================================
// 添加 HttpClient 工厂
// ============================================================
var cityServiceUrl = builder.Configuration["ServiceUrls:CityService"] ?? "http://localhost:8002";
var coworkingServiceUrl = builder.Configuration["ServiceUrls:CoworkingService"] ?? "http://localhost:8004";

builder.Services.AddHttpClient("CityService", client =>
{
    client.BaseAddress = new Uri(cityServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient("CoworkingService", client =>
{
    client.BaseAddress = new Uri(coworkingServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ============================================================
// 依赖注入 - Infrastructure 层
// ============================================================
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();
builder.Services.AddScoped<ICityServiceClient, CityServiceClient>();
builder.Services.AddScoped<ICoworkingServiceClient, CoworkingServiceClient>();
builder.Services.AddScoped<IIndexSyncService, IndexSyncService>();

// ============================================================
// 依赖注入 - Application 层
// ============================================================
builder.Services.AddScoped<ISearchService, SearchApplicationService>();

// ============================================================
// 添加控制器
// ============================================================
builder.Services.AddControllers()
    .AddDapr()
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

// 添加健康检查
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Search Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("SearchService 正在启动...");

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

// 启动时初始化索引
using (var scope = app.Services.CreateScope())
{
    var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
    var indexSettings = builder.Configuration.GetSection("IndexSettings").Get<IndexSettings>() ?? new IndexSettings();

    // 检查 Elasticsearch 连接
    var isHealthy = await elasticsearchService.IsHealthyAsync();
    if (isHealthy)
    {
        Log.Information("Elasticsearch 连接成功，正在初始化索引...");

        // 创建索引（如果不存在）
        await elasticsearchService.CreateIndexIfNotExistsAsync<SearchService.Domain.Models.CitySearchDocument>(
            indexSettings.CityIndex);
        await elasticsearchService.CreateIndexIfNotExistsAsync<SearchService.Domain.Models.CoworkingSearchDocument>(
            indexSettings.CoworkingIndex);

        Log.Information("索引初始化完成");
    }
    else
    {
        Log.Warning("Elasticsearch 连接失败，索引初始化跳过。请确保 Elasticsearch 正在运行。");
    }
}

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SearchService 启动失败");
}
finally
{
    Log.CloseAndFlush();
}

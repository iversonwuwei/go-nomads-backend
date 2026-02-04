using GoNomads.Shared.Extensions;
using GoNomads.Shared.Observability;
using Scalar.AspNetCore;
using SearchService.Application.Interfaces;
using SearchService.Application.Services;
using SearchService.Infrastructure.Configuration;
using SearchService.Infrastructure.HostedServices;
using SearchService.Infrastructure.Services;
using Serilog;
using System.Text.Json.Serialization;

const string serviceName = "SearchService";

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
// OpenTelemetry 可观测性配置 (Traces + Metrics + Logs)
// ============================================================
builder.Services.AddGoNomadsObservability(builder.Configuration, serviceName);
builder.Logging.AddGoNomadsLogging(builder.Configuration, serviceName);

// ============================================================
// 配置绑定
// ============================================================
builder.Services.Configure<ElasticsearchSettings>(
    builder.Configuration.GetSection("Elasticsearch"));
builder.Services.Configure<IndexSettings>(
    builder.Configuration.GetSection("IndexSettings"));
builder.Services.Configure<IndexMaintenanceSettings>(
    builder.Configuration.GetSection("IndexMaintenance"));

// ============================================================
// 添加 Dapr 客户端 - 方案A: 使用 HTTP 端点（原生支持 InvokeMethodAsync）
// ============================================================
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
builder.Services.AddDaprClient(daprClientBuilder =>
{
    daprClientBuilder.UseHttpEndpoint($"http://localhost:{daprHttpPort}");
});

// ============================================================
// 依赖注入 - Infrastructure 层
// ============================================================
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();
builder.Services.AddScoped<ICityServiceClient, CityServiceClient>();
builder.Services.AddScoped<ICoworkingServiceClient, CoworkingServiceClient>();
builder.Services.AddScoped<IIndexSyncService, IndexSyncService>();
builder.Services.AddHostedService<IndexVerificationHostedService>();

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

using Prometheus;
using Scalar.AspNetCore;
using GoNomads.Shared.Extensions;
using Serilog;

const string serviceName = "ProductService";

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
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

// Named HttpClient for controllers using IHttpClientFactory
builder.Services.AddServiceClient("user-service");

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();

// Configure Scalar UI
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Product Service API")
        .WithTheme(ScalarTheme.Mars)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseRouting();

// Enable Prometheus metrics
app.UseHttpMetrics();

// Map controllers
app.MapControllers();

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

// Map Prometheus metrics endpoint
app.MapMetrics();

app.Run();
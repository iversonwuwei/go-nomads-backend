using Prometheus;
using Scalar.AspNetCore;
using GoNomads.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 配置 DaprClient - 方案A: 使用 HTTP 端点（原生支持 InvokeMethodAsync）
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
builder.Services.AddDaprClient(daprClientBuilder =>
{
    daprClientBuilder.UseHttpEndpoint($"http://localhost:{daprHttpPort}");
});
builder.Services.AddControllers().AddDapr();

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

// Add health check endpoint
app.MapGet("/health",
    () => Results.Ok(new { status = "healthy", service = "ProductService", timestamp = DateTime.UtcNow }));

// Map Prometheus metrics endpoint
app.MapMetrics();

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();
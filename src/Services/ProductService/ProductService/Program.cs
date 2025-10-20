using Dapr.Client;
using Scalar.AspNetCore;
using Prometheus;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 配置 DaprClient (通过环境变量 DAPR_GRPC_PORT 使用 gRPC)
builder.Services.AddDaprClient();
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
        .WithTheme(Scalar.AspNetCore.ScalarTheme.Mars)
        .WithDefaultHttpClient(Scalar.AspNetCore.ScalarTarget.CSharp, Scalar.AspNetCore.ScalarClient.HttpClient);
});

app.UseRouting();

// Enable Prometheus metrics
app.UseHttpMetrics();

// Map controllers
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ProductService", timestamp = DateTime.UtcNow }));

// Map Prometheus metrics endpoint
app.MapMetrics();

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();

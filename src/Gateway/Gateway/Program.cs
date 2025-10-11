using Consul;
using Dapr.Client;
using Gateway.Services;
using Yarp.ReverseProxy.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Consul client
builder.Services.AddSingleton<IConsulClient>(sp =>
{
    var consulAddress = builder.Configuration["Consul:Address"] ?? "http://go-nomads-consul:8500";
    return new ConsulClient(config => config.Address = new Uri(consulAddress));
});

// Add services to the container.
builder.Services.AddDaprClient();

// Add YARP with Consul-based service discovery
builder.Services.AddSingleton<IProxyConfigProvider, ConsulProxyConfigProvider>();
builder.Services.AddReverseProxy();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers().AddDapr();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();

// Configure Scalar UI
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Go-Nomads Gateway API")
        .WithTheme(ScalarTheme.Saturn)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseRouting();

// Map the reverse proxy routes
app.MapReverseProxy();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

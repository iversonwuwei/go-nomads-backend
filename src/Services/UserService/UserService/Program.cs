using UserService.Services;
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddDaprClient();
builder.Services.AddControllers().AddDapr();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();

// Map gRPC service
app.MapGrpcService<UserGrpcService>();

// Map controllers
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "UserService", timestamp = DateTime.UtcNow }));

app.Run();

using CoworkingService.Repositories;
using Shared.Extensions;
using Dapr.Client;
using Serilog;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 配置 DaprClient
builder.Services.AddDaprClient();

// 注册 Supabase 仓储
builder.Services.AddScoped<SupabaseCoworkingRepository>();
builder.Services.AddScoped<SupabaseCoworkingBookingRepository>();

// 添加控制器
builder.Services.AddControllers().AddDapr();

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
        .WithTitle("Coworking Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("CoworkingService 正在启动...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CoworkingService 启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

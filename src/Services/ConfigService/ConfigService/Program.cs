using System.Text;
using ConfigService.Application.Services;
using ConfigService.Domain.Repositories;
using ConfigService.Infrastructure.Repositories;
using GoNomads.Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Microsoft.Extensions.Hosting.Extensions.AddServiceDefaults(builder);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/configservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "http://localhost:5213", Description = "Local Development" }
        };
        return Task.CompletedTask;
    });
});

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 添加当前用户服务（统一的用户身份和权限检查）
builder.Services.AddCurrentUserService();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
    {
        b.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Register Repositories
builder.Services.AddScoped<IStaticTextRepository, SupabaseStaticTextRepository>();
builder.Services.AddScoped<IOptionGroupRepository, SupabaseOptionGroupRepository>();
builder.Services.AddScoped<IOptionItemRepository, SupabaseOptionItemRepository>();
builder.Services.AddScoped<IConfigSnapshotRepository, SupabaseConfigSnapshotRepository>();
builder.Services.AddScoped<ISystemSettingRepository, SupabaseSystemSettingRepository>();

// Register Application Services
builder.Services.AddScoped<IStaticTextService, StaticTextApplicationService>();
builder.Services.AddScoped<IOptionGroupService, OptionGroupApplicationService>();
builder.Services.AddScoped<IConfigPublishService, ConfigPublishApplicationService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingApplicationService>();
builder.Services.AddScoped<IAppConfigBootstrapService, AppConfigBootstrapApplicationService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var bootstrapService = scope.ServiceProvider.GetRequiredService<IAppConfigBootstrapService>();
        await bootstrapService.BootstrapAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "app/config bootstrap 执行失败，服务将继续启动并保留现有配置状态");
    }
}

// Configure the HTTP request pipeline
app.MapOpenApi();
app.MapScalarApiReference("/scalar", options =>
{
    options
        .WithTitle("Config Service API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health",
    () => Results.Ok(new { status = "healthy", service = "ConfigService", timestamp = DateTime.UtcNow }));

Log.Information("Config Service starting on port 5213...");

app.Run();

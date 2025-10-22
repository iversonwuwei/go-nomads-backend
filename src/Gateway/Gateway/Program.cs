using Consul;
using Dapr.Client;
using Gateway.Services;
using Gateway.Middleware;
using Yarp.ReverseProxy.Configuration;
using Scalar.AspNetCore;
using Prometheus;
using Shared.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add Consul client
builder.Services.AddSingleton<IConsulClient>(sp =>
{
    var consulAddress = builder.Configuration["Consul:Address"] ?? "http://go-nomads-consul:8500";
    return new ConsulClient(config => config.Address = new Uri(consulAddress));
});

// Add services to the container.
builder.Services.AddDaprClient();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT Secret is not configured. Please set Jwt:Secret in appsettings.json");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    
    // Supabase 使用 HS256 算法签名,需要用 JWT Secret 验证
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    {
        KeyId = "TD8wsInnx6ikLidH" // 设置 KeyId 以匹配 token header 中的 kid
    };
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = builder.Configuration.GetValue<bool>("Jwt:ValidateIssuer", true),
        ValidIssuer = jwtIssuer,
        ValidateAudience = builder.Configuration.GetValue<bool>("Jwt:ValidateAudience", true),
        ValidAudience = jwtAudience,
        ValidateLifetime = builder.Configuration.GetValue<bool>("Jwt:ValidateLifetime", true),
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("❌ JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst("sub")?.Value;
            var email = context.Principal?.FindFirst("email")?.Value;
            var role = context.Principal?.FindFirst("role")?.Value;
            logger.LogInformation("✅ JWT Token validated - UserId: {UserId}, Email: {Email}, Role: {Role}", userId, email, role);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(RateLimitConfig.ConfigureRateLimiter);

// Add YARP with Consul-based service discovery
builder.Services.AddSingleton<IProxyConfigProvider, ConsulProxyConfigProvider>();
builder.Services.AddSingleton<JwtAuthenticationTransform>();
builder.Services.AddReverseProxy()
    .LoadFromMemory(Array.Empty<Yarp.ReverseProxy.Configuration.RouteConfig>(), Array.Empty<Yarp.ReverseProxy.Configuration.ClusterConfig>())
    .AddTransforms<JwtAuthenticationTransform>();

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

// Enable Prometheus metrics
app.UseHttpMetrics();

// Add Rate Limiting
app.UseRateLimiter();

// Add Dynamic Rate Limit Middleware
app.UseDynamicRateLimit();

// Add Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 使用 JWT 认证拦截中间件 - 在转发前验证 token
// 如果 token 无效或缺失,直接返回 401,不转发请求
// 如果 token 有效,提取用户信息并添加到请求头,然后转发
app.UseJwtAuthenticationInterceptor();

// Map controllers BEFORE reverse proxy (so /api/test/* routes are handled first)
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Map the reverse proxy routes (this should be LAST as it's a catch-all)
app.MapReverseProxy();

// Map Prometheus metrics endpoint
app.MapMetrics();

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();

using System.Text;
using Gateway.Middleware;
using Gateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

const string serviceName = "gateway";

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Aspire ServiceDefaults (OpenTelemetry + HealthChecks + ServiceDiscovery)
// ============================================================
builder.AddServiceDefaults();

// ============================================================
// YARP 路由配置 (从 yarp.json 加载, Aspire 端点自动覆盖)
// ============================================================
builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);

// 如果运行在 Aspire 编排下，使用 Aspire 注入的服务端点覆盖默认地址
// Aspire 会通过环境变量注入 services:{name}:http:0 = http://localhost:XXXXX
ResolveAspireServiceEndpoints(builder.Configuration);

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtSecret))
    throw new InvalidOperationException("JWT Secret is not configured. Please set Jwt:Secret in appsettings.json");

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
            ValidateIssuer = builder.Configuration.GetValue("Jwt:ValidateIssuer", true),
            ValidIssuer = jwtIssuer,
            ValidateAudience = builder.Configuration.GetValue("Jwt:ValidateAudience", true),
            ValidAudience = jwtAudience,
            ValidateLifetime = builder.Configuration.GetValue("Jwt:ValidateLifetime", true),
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
                logger.LogInformation("✅ JWT Token validated - UserId: {UserId}, Email: {Email}, Role: {Role}", userId,
                    email, role);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(RateLimitConfig.ConfigureRateLimiter);

// ============================================================
// YARP 反向代理 (标准配置加载 + JWT 认证转换)
// ============================================================
builder.Services.AddSingleton<JwtAuthenticationTransform>();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver()
    .AddTransforms<JwtAuthenticationTransform>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// Enable WebSockets for SignalR proxy
app.UseWebSockets();

// HTTP Method Override - 必须在路由之前
// 用于解决某些网络环境不支持 PUT/DELETE 方法的问题
// 客户端发送 POST + X-HTTP-Method-Override 头，此中间件会重写请求方法
app.UseCustomHttpMethodOverride();

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

// Aspire 默认端点 (健康检查 /health + /alive)
app.MapDefaultEndpoints();

// Map the reverse proxy routes (this should be LAST as it's a catch-all)
app.MapReverseProxy();

app.Run();

// =============================================================================
// Aspire 服务端点解析
// 当运行在 Aspire 编排下时，自动将 YARP 集群目标地址替换为 Aspire 注入的端点
// 这样 yarp.json 中的默认地址（Docker/K8s 环境）会被 Aspire 动态端口覆盖
// =============================================================================
static void ResolveAspireServiceEndpoints(ConfigurationManager configuration)
{
    string[] serviceNames =
    [
        "user-service",
        "city-service",
        "coworking-service",
        "event-service",
        "ai-service",
        "cache-service",
        "message-service",
        "innovation-service",
        "search-service",
        "accommodation-service",
        "product-service"
    ];

    var overrides = new Dictionary<string, string?>();

    foreach (var name in serviceNames)
    {
        // Aspire 通过环境变量注入: services:{name}:http:0 = http://localhost:XXXXX
        var aspireUrl = configuration[$"services:{name}:http:0"]
                     ?? configuration[$"services:{name}:https:0"];

        if (!string.IsNullOrEmpty(aspireUrl))
        {
            overrides[$"ReverseProxy:Clusters:{name}-cluster:Destinations:{name}:Address"] = aspireUrl;
        }
    }

    if (overrides.Count > 0)
    {
        configuration.AddInMemoryCollection(overrides);
    }
}
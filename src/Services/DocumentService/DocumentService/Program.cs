using System.Net;
using System.Text;
using System.Text.Json;
using Dapr.Client;
using DocumentService.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Prometheus;
using Scalar.AspNetCore;
using GoNomads.Shared.Extensions;
using GoNomads.Shared.Observability;
using Serilog;

const string serviceName = "DocumentService";

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
builder.Services.AddGoNomadsObservability(builder.Configuration, serviceName);
builder.Logging.AddGoNomadsLogging(builder.Configuration, serviceName);

// Bind service configuration
builder.Services.Configure<ServiceConfiguration>(
    builder.Configuration.GetSection("Services"));

// Add services to the container - 方案A: 使用 HTTP 端点（原生支持 InvokeMethodAsync）
var daprHttpPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
builder.Services.AddDaprClient(daprClientBuilder =>
{
    daprClientBuilder.UseHttpEndpoint($"http://localhost:{daprHttpPort}");
});
builder.Services.AddControllers().AddDapr();

// Configure OpenAPI with enhanced documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Go-Nomads API Documentation Hub";
        document.Info.Version = "v1";
        document.Info.Description = "统一的 API 文档中心 - 聚合所有微服务的 API (基于 Group 设计)";
        document.Info.Contact = new OpenApiContact
        {
            Name = "Go-Nomads Team",
            Email = "dev@go-nomads.com"
        };
        return Task.CompletedTask;
    });
});

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add HttpClient for fetching remote OpenAPI specs
builder.Services.AddHttpClient("ServiceClient", client => { client.Timeout = TimeSpan.FromSeconds(30); })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // 允许自签名证书（开发环境）
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

var app = builder.Build();

// Get service configuration
var serviceConfig = app.Services.GetRequiredService<IOptions<ServiceConfiguration>>().Value;

// Configure the HTTP request pipeline
app.UseCors();
app.UseRouting();

// Enable Prometheus metrics
app.UseHttpMetrics();

// Map OpenAPI endpoint
app.MapOpenApi();

// Configure Scalar UI with aggregated documentation
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Go-Nomads API Documentation Hub")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithModels(true)
        .WithDownloadButton(true)
        .WithSearchHotKey("k");
});


// ==================== Product Service Group ====================
var productsGroup = app.MapGroup("/api/products")
    .WithTags("Products")
    .WithOpenApi();

// GET /api/products - 获取产品列表
productsGroup.MapGet("/",
        async (DaprClient daprClient, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
        {
            try
            {
                // 使用 Dapr Service Invocation 调用 ProductService
                var response = await daprClient.InvokeMethodAsync<JsonElement>(
                    HttpMethod.Get,
                    "product-service",
                    $"api/products?page={page}&pageSize={pageSize}");
                return Results.Json(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get products: {ex.Message}");
            }
        })
    .WithName("GetProducts")
    .WithSummary("获取产品列表")
    .WithDescription("分页获取所有产品信息")
    .WithOpenApi();

// GET /api/products/{id} - 获取单个产品
productsGroup.MapGet("/{id}", async (DaprClient daprClient, string id) =>
    {
        try
        {
            var response = await daprClient.InvokeMethodAsync<JsonElement>(
                HttpMethod.Get,
                "product-service",
                $"api/products/{id}");
            return Results.Json(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get product: {ex.Message}");
        }
    })
    .WithName("GetProductById")
    .WithSummary("获取产品详情")
    .WithDescription("根据 ID 获取单个产品的详细信息")
    .WithOpenApi();

// GET /api/products/user/{userId} - 获取用户的产品
productsGroup.MapGet("/user/{userId}",
        async (DaprClient daprClient, string userId, [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            try
            {
                var response = await daprClient.InvokeMethodAsync<JsonElement>(
                    HttpMethod.Get,
                    "product-service",
                    $"api/products/user/{userId}?page={page}&pageSize={pageSize}");
                return Results.Json(response);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get user products: {ex.Message}");
            }
        })
    .WithName("GetProductsByUserId")
    .WithSummary("获取用户的产品")
    .WithDescription("根据用户 ID 获取该用户的所有产品")
    .WithOpenApi();

// POST /api/products - 创建产品
productsGroup.MapPost("/", async (DaprClient daprClient, HttpRequest request) =>
    {
        try
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(body);
            
            var response = await daprClient.InvokeMethodAsync<JsonElement, JsonElement>(
                HttpMethod.Post,
                "product-service",
                "api/products",
                requestData);
            return Results.Json(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to create product: {ex.Message}");
        }
    })
    .WithName("CreateProduct")
    .WithSummary("创建新产品")
    .WithDescription("创建一个新的产品记录")
    .WithOpenApi();

// PUT /api/products/{id} - 更新产品
productsGroup.MapPut("/{id}", async (DaprClient daprClient, string id, HttpRequest request) =>
    {
        try
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            var requestData = JsonSerializer.Deserialize<JsonElement>(body);
            
            var response = await daprClient.InvokeMethodAsync<JsonElement, JsonElement>(
                HttpMethod.Put,
                "product-service",
                $"api/products/{id}",
                requestData);
            return Results.Json(response);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to update product: {ex.Message}");
        }
    })
    .WithName("UpdateProduct")
    .WithSummary("更新产品")
    .WithDescription("根据 ID 更新产品信息")
    .WithOpenApi();

// DELETE /api/products/{id} - 删除产品
productsGroup.MapDelete("/{id}", async (DaprClient daprClient, string id) =>
    {
        try
        {
            await daprClient.InvokeMethodAsync(
                HttpMethod.Delete,
                "product-service",
                $"api/products/{id}");
            return Results.Ok(new { success = true, message = "Product deleted successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to delete product: {ex.Message}");
        }
    })
    .WithName("DeleteProduct")
    .WithSummary("删除产品")
    .WithDescription("根据 ID 删除产品")
    .WithOpenApi();

// ==================== User Service Group ====================
var usersGroup = app.MapGroup("/api/users")
    .WithTags("Users")
    .WithOpenApi();

// GET /api/users - 获取用户列表
usersGroup.MapGet("/", async (
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("ServiceClient");
            var userServiceUrl = serviceConfig.UserService?.Url ?? "http://localhost:8080";
            var url = $"{userServiceUrl}/api/users?page={page}&pageSize={pageSize}";

            logger.LogInformation("Calling UserService at: {Url}", url);

            var response = await client.GetStringAsync(url);
            return Results.Content(response, "application/json");
        }
        catch (HttpRequestException ex)
        {
            app.Logger.LogError(ex, "HTTP request failed when calling UserService");
            return Results.Problem(
                $"Failed to connect to UserService: {ex.Message}. Please check if UserService is running.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Unexpected error when calling UserService");
            return Results.Problem($"Failed to get users: {ex.Message}");
        }
    })
    .WithName("GetUsers")
    .WithSummary("获取用户列表")
    .WithDescription("分页获取所有用户信息 - 返回 ApiResponse<PaginatedResponse<User>> 格式")
    .WithOpenApi();

// GET /api/users/{id} - 获取单个用户
usersGroup.MapGet("/{id}", async (
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        string id) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("ServiceClient");
            var userServiceUrl = serviceConfig.UserService?.Url ?? "http://localhost:8080";
            var url = $"{userServiceUrl}/api/users/{id}";

            logger.LogInformation("Calling UserService at: {Url}", url);

            var response = await client.GetStringAsync(url);
            return Results.Content(response, "application/json");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { success = false, message = "User not found" });
        }
        catch (HttpRequestException ex)
        {
            app.Logger.LogError(ex, "HTTP request failed when calling UserService");
            return Results.Problem(
                $"Failed to connect to UserService: {ex.Message}. Please check if UserService is running.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Unexpected error when getting user");
            return Results.Problem($"Failed to get user: {ex.Message}");
        }
    })
    .WithName("GetUserById")
    .WithSummary("获取用户详情")
    .WithDescription("根据 ID 获取单个用户的详细信息 - 返回 ApiResponse<User> 格式")
    .WithOpenApi();

// POST /api/users - 创建用户
usersGroup.MapPost("/", async (
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        HttpRequest request) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("ServiceClient");
            var userServiceUrl = serviceConfig.UserService?.Url ?? "http://localhost:8080";
            var url = $"{userServiceUrl}/api/users";

            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            logger.LogInformation("Creating user at: {Url}", url);

            var response = await client.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
            return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            return Results.BadRequest(new { success = false, message = "Invalid user data" });
        }
        catch (HttpRequestException ex)
        {
            app.Logger.LogError(ex, "HTTP request failed when creating user");
            return Results.Problem(
                $"Failed to connect to UserService: {ex.Message}. Please check if UserService is running.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Unexpected error when creating user");
            return Results.Problem($"Failed to create user: {ex.Message}");
        }
    })
    .WithName("CreateUser")
    .WithSummary("创建新用户")
    .WithDescription("创建一个新的用户记录 - 需要提供 name, email, phone(可选) - 返回 ApiResponse<User> 格式")
    .WithOpenApi();

// PUT /api/users/{id} - 更新用户
usersGroup.MapPut("/{id}", async (
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        string id,
        HttpRequest request) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("ServiceClient");
            var userServiceUrl = serviceConfig.UserService?.Url ?? "http://localhost:8080";
            var url = $"{userServiceUrl}/api/users/{id}";

            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            logger.LogInformation("Updating user at: {Url}", url);

            var response = await client.PutAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
            return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { success = false, message = "User not found" });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            return Results.BadRequest(new { success = false, message = "Invalid user data" });
        }
        catch (HttpRequestException ex)
        {
            app.Logger.LogError(ex, "HTTP request failed when updating user");
            return Results.Problem(
                $"Failed to connect to UserService: {ex.Message}. Please check if UserService is running.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Unexpected error when updating user");
            return Results.Problem($"Failed to update user: {ex.Message}");
        }
    })
    .WithName("UpdateUser")
    .WithSummary("更新用户")
    .WithDescription("根据 ID 更新用户信息 - 需要提供 name, email, phone(可选) - 返回 ApiResponse<User> 格式")
    .WithOpenApi();

// DELETE /api/users/{id} - 删除用户
usersGroup.MapDelete("/{id}", async (
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        string id) =>
    {
        try
        {
            var client = httpClientFactory.CreateClient("ServiceClient");
            var userServiceUrl = serviceConfig.UserService?.Url ?? "http://localhost:8080";
            var url = $"{userServiceUrl}/api/users/{id}";

            logger.LogInformation("Deleting user at: {Url}", url);

            var response = await client.DeleteAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            return Results.Content(result, "application/json", statusCode: (int)response.StatusCode);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { success = false, message = "User not found" });
        }
        catch (HttpRequestException ex)
        {
            app.Logger.LogError(ex, "HTTP request failed when deleting user");
            return Results.Problem(
                $"Failed to connect to UserService: {ex.Message}. Please check if UserService is running.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Unexpected error when deleting user");
            return Results.Problem($"Failed to delete user: {ex.Message}");
        }
    })
    .WithName("DeleteUser")
    .WithSummary("删除用户")
    .WithDescription("根据 ID 删除用户 - 返回 ApiResponse<object> 格式")
    .WithOpenApi();

// ==================== System Group ====================
var systemGroup = app.MapGroup("/api/system")
    .WithTags("System")
    .WithOpenApi();

// GET /api/system/health - 健康检查
systemGroup.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "document-service",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    }))
    .WithName("HealthCheck")
    .WithSummary("健康检查")
    .WithDescription("检查 DocumentService 服务健康状态")
    .WithOpenApi();

// GET /api/system/services - 获取服务列表
systemGroup.MapGet("/services", () =>
    {
        var services = new object[]
        {
            new
            {
                name = "Gateway",
                appId = "gateway",
                url = "http://localhost:5000",
                docsUrl = "http://localhost:5000/scalar/v1",
                status = "running",
                description = (string?)null,
                apis = (string[]?)null
            },
            new
            {
                name = "Product Service",
                appId = "product-service",
                url = "http://localhost:5001",
                docsUrl = "http://localhost:5001/scalar/v1",
                status = "running",
                description = (string?)null,
                apis = new[]
                {
                    "GET /api/products", "POST /api/products", "GET /api/products/{id}", "PUT /api/products/{id}",
                    "DELETE /api/products/{id}"
                }
            },
            new
            {
                name = "User Service",
                appId = "user-service",
                url = "http://localhost:5002",
                docsUrl = "http://localhost:5002/scalar/v1",
                status = "running",
                description = "用户管理服务 - 使用 Supabase PostgreSQL 数据库",
                apis = new[]
                {
                    "GET /api/users - 获取用户列表（分页）",
                    "POST /api/users - 创建用户（name, email, phone）",
                    "GET /api/users/{id} - 获取用户详情",
                    "PUT /api/users/{id} - 更新用户信息",
                    "DELETE /api/users/{id} - 删除用户"
                }
            },
            new
            {
                name = "Document Service (API Hub)",
                appId = "document-service",
                url = "http://localhost:5003",
                docsUrl = "http://localhost:5003/scalar/v1",
                status = "running",
                description = "统一 API 文档中心 - 聚合所有微服务 API",
                apis = (string[]?)null
            }
        };
        return Results.Ok(services);
    })
    .WithName("GetServices")
    .WithSummary("获取服务列表")
    .WithDescription("返回所有已注册的微服务及其文档地址")
    .WithOpenApi();

// GET /api/system/specs - 获取聚合的 OpenAPI 规范
systemGroup.MapGet("/specs", async (IHttpClientFactory httpClientFactory) =>
    {
        var client = httpClientFactory.CreateClient();
        var services = new Dictionary<string, string>
        {
            { "gateway", "http://go-nomads-gateway:8080/openapi/v1.json" },
            { "product-service", "http://go-nomads-product-service:8080/openapi/v1.json" },
            { "user-service", "http://go-nomads-user-service:8080/openapi/v1.json" }
        };

        var specs = new Dictionary<string, object>();

        foreach (var (serviceName, url) in services)
            try
            {
                var response = await client.GetStringAsync(url);
                specs[serviceName] = JsonSerializer.Deserialize<object>(response) ?? new { };
            }
            catch (Exception ex)
            {
                specs[serviceName] = new { error = ex.Message, status = "unavailable" };
            }

        return Results.Ok(specs);
    })
    .WithName("GetAggregatedSpecs")
    .WithSummary("获取所有服务的 OpenAPI 规范")
    .WithDescription("聚合所有微服务的 OpenAPI 文档 (用于高级集成)")
    .WithOpenApi();

// Add health check endpoint for Consul
app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "document-service",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    }))
    .WithTags("System")
    .WithName("HealthCheckRoot")
    .WithSummary("Consul 健康检查端点")
    .WithDescription("用于 Consul 服务发现的健康检查")
    .WithOpenApi();

// Map Prometheus metrics endpoint
app.MapMetrics();

app.MapControllers();

// 自动注册到 Consul
await app.RegisterWithConsulAsync();

app.Run();
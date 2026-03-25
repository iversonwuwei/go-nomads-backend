# Service Scaffold Skill

创建新的 Go-Nomads 后端微服务所需的全套文件。

## 使用方式

```
/service-scaffold {ServiceName}
```

示例：`/service-scaffold BookmarkService`

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `ServiceName` | 服务名称（PascalCase + Service 后缀） | 必填 |
| `--ddd` | 使用 DDD 四层分离项目结构 | 否（默认单项目） |
| `--port` | 服务监听端口号 | 自动分配 |
| `--with-signalr` | 包含 SignalR Hub | 否 |
| `--with-masstransit` | 包含 MassTransit Consumer | 否 |

## 生成文件清单

### 单项目模式（默认）

```
src/Services/{ServiceName}/{ServiceName}/
├── Program.cs                      # 服务入口
├── {ServiceName}.csproj            # 项目文件
├── Dockerfile                      # Docker 多阶段构建
├── appsettings.json                # 配置文件
├── appsettings.Development.json    # 开发配置
├── API/
│   └── Controllers/
│       └── {Resource}Controller.cs # 主 Controller
├── Application/
│   ├── Abstractions/
│   │   └── I{Resource}Service.cs   # 服务接口
│   └── Services/
│       └── {Resource}AppService.cs # 服务实现
├── Domain/
│   ├── Entities/
│   │   └── {Resource}.cs           # 领域实体
│   └── Repositories/
│       └── I{Resource}Repository.cs # 仓库接口
├── Infrastructure/
│   └── Repositories/
│       └── {Resource}Repository.cs  # 仓库实现
└── DTOs/
    └── {Resource}Dto.cs             # DTO
```

### DDD 四层模式（`--ddd`）

```
src/Services/{ServiceName}/
├── {ServiceName}.API/
│   ├── Program.cs
│   ├── {ServiceName}.API.csproj
│   ├── Dockerfile
│   ├── Controllers/
│   └── Hubs/                        (if --with-signalr)
├── {ServiceName}.Application/
│   ├── {ServiceName}.Application.csproj
│   ├── DTOs/
│   └── Services/
├── {ServiceName}.Domain/
│   ├── {ServiceName}.Domain.csproj
│   ├── Entities/
│   └── Repositories/
└── {ServiceName}.Infrastructure/
    ├── {ServiceName}.Infrastructure.csproj
    ├── Repositories/
    └── Consumers/                   (if --with-masstransit)
```

## 额外操作

生成文件后，还需手动完成：

1. **Aspire 注册** — 在 `GoNomads.AppHost/Program.cs` 添加：
   ```csharp
   var xxxService = builder.AddProject<Projects.{ServiceName}>("{service-name}")
       .WithReference(redis)
       .WithReference(rabbitmq);
   ```

2. **Gateway 路由** — 在 `Gateway/yarp.json` 添加：
   ```json
   {
     "RouteId": "{resource}-route",
     "ClusterId": "{resource}-cluster",
     "Match": { "Path": "/api/v1/{resource}/{**catch-all}" }
   }
   ```

3. **Gateway 引用** — 在 `AppHost/Program.cs` 的 gateway 添加 `.WithReference(xxxService)`

4. **Solution 添加** — `dotnet sln add src/Services/{ServiceName}/{ServiceName}/{ServiceName}.csproj`

## 模板参考

### Program.cs 模板

```csharp
using GoNomads.Shared.Extensions;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/{service-name}-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Aspire ServiceDefaults
builder.AddServiceDefaults();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Supabase
builder.Services.AddSupabase(builder.Configuration);
builder.Services.AddCurrentUserService();

// DI 注册
// builder.Services.AddScoped<I{Resource}Repository, {Resource}Repository>();
// builder.Services.AddScoped<I{Resource}Service, {Resource}AppService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("{ServiceName} API").WithTheme(ScalarTheme.BluePlanet);
});

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseUserContext();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

Log.Information("{ServiceName} starting on port {Port}...", {port});
app.Run();
```

### Controller 模板

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace {ServiceName}.API.Controllers;

[ApiController]
[Route("api/v1/{resource}")]
public class {Resource}Controller : ControllerBase
{
    private readonly I{Resource}Service _service;
    private readonly ILogger<{Resource}Controller> _logger;

    public {Resource}Controller(I{Resource}Service service, ILogger<{Resource}Controller> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }
}
```

### Dockerfile 模板

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE {port}

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/{ServiceName}/{ServiceName}/{ServiceName}.csproj", "src/Services/{ServiceName}/{ServiceName}/"]
COPY ["src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults.csproj", "src/GoNomads.ServiceDefaults/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/{ServiceName}/{ServiceName}/{ServiceName}.csproj"
COPY . .
WORKDIR "/src/src/Services/{ServiceName}/{ServiceName}"
RUN dotnet build "{ServiceName}.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "{ServiceName}.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "{ServiceName}.dll"]
```

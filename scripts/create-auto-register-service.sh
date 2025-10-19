#!/bin/bash

# 🚀 快速创建自动注册的新服务脚本
# 使用方法: ./create-auto-register-service.sh order-service 5005

set -e

# 颜色定义
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 参数检查
if [ $# -lt 2 ]; then
    echo "使用方法: $0 <service-name> <host-port>"
    echo "示例: $0 order-service 5005"
    exit 1
fi

SERVICE_NAME=$1
HOST_PORT=$2
PASCAL_CASE_NAME=$(echo "$SERVICE_NAME" | sed -r 's/(^|-)([a-z])/\U\2/g' | sed 's/-//g')
CONTAINER_NAME="go-nomads-${SERVICE_NAME}"

echo -e "${BLUE}📦 创建新服务: ${SERVICE_NAME}${NC}"
echo -e "${BLUE}   PascalCase: ${PASCAL_CASE_NAME}${NC}"
echo -e "${BLUE}   容器名称: ${CONTAINER_NAME}${NC}"
echo -e "${BLUE}   主机端口: ${HOST_PORT}${NC}"
echo ""

# 1. 创建服务目录结构
echo -e "${GREEN}✅ 步骤 1/6: 创建目录结构${NC}"
SERVICE_DIR="src/Services/${PASCAL_CASE_NAME}/${PASCAL_CASE_NAME}"
mkdir -p "${SERVICE_DIR}/Controllers"

# 2. 创建 .csproj 文件
echo -e "${GREEN}✅ 步骤 2/6: 创建项目文件${NC}"
cat > "${SERVICE_DIR}/${PASCAL_CASE_NAME}.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
    <PackageReference Include="Scalar.AspNetCore" Version="1.2.44" />
    <PackageReference Include="prometheus-net.AspNetCore" Version="8.2.1" />
    <PackageReference Include="Dapr.AspNetCore" Version="1.14.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../Shared/Shared/Shared.csproj" />
  </ItemGroup>

</Project>
EOF

# 3. 创建 Program.cs（带自动注册）
echo -e "${GREEN}✅ 步骤 3/6: 创建 Program.cs（已集成自动注册）${NC}"
cat > "${SERVICE_DIR}/Program.cs" << EOF
using Dapr.Client;
using Scalar.AspNetCore;
using Prometheus;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDaprClient();
builder.Services.AddControllers().AddDapr();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapOpenApi();

// Configure Scalar UI
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("${PASCAL_CASE_NAME} API")
        .WithTheme(ScalarTheme.BluePlanet)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseRouting();

// Enable Prometheus metrics
app.UseHttpMetrics();

// Map controllers
app.MapControllers();

// Add health check endpoint (Required for Consul)
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    service = "${PASCAL_CASE_NAME}", 
    timestamp = DateTime.UtcNow 
}));

// Map Prometheus metrics endpoint (Required for monitoring)
app.MapMetrics();

// ⭐ 自动注册到 Consul（无需手动配置）
await app.RegisterWithConsulAsync();

app.Run();
EOF

# 4. 创建 appsettings.json
echo -e "${GREEN}✅ 步骤 4/6: 创建配置文件${NC}"
cat > "${SERVICE_DIR}/appsettings.json" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
EOF

# 5. 创建 appsettings.Development.json（包含 Consul 配置）
cat > "${SERVICE_DIR}/appsettings.Development.json" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Consul": {
    "Address": "http://go-nomads-consul:8500",
    "ServiceName": "${SERVICE_NAME}",
    "ServiceAddress": "${CONTAINER_NAME}",
    "ServicePort": 8080,
    "HealthCheckPath": "/health",
    "HealthCheckInterval": "10s",
    "ServiceVersion": "1.0.0"
  }
}
EOF

# 6. 创建 Dockerfile
echo -e "${GREEN}✅ 步骤 5/6: 创建 Dockerfile${NC}"
cat > "${SERVICE_DIR}/Dockerfile" << EOF
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/${PASCAL_CASE_NAME}/${PASCAL_CASE_NAME}/${PASCAL_CASE_NAME}.csproj", "src/Services/${PASCAL_CASE_NAME}/${PASCAL_CASE_NAME}/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]

# Restore dependencies
RUN dotnet restore "src/Services/${PASCAL_CASE_NAME}/${PASCAL_CASE_NAME}/${PASCAL_CASE_NAME}.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/Services/${PASCAL_CASE_NAME}/${PASCAL_CASE_NAME}"
RUN dotnet build "${PASCAL_CASE_NAME}.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "${PASCAL_CASE_NAME}.csproj" -c Release -o /app/out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/out .
ENTRYPOINT ["dotnet", "${PASCAL_CASE_NAME}.dll"]
EOF

# 7. 创建示例 Controller
echo -e "${GREEN}✅ 步骤 6/6: 创建示例 Controller${NC}"
cat > "${SERVICE_DIR}/Controllers/${PASCAL_CASE_NAME}Controller.cs" << EOF
using Microsoft.AspNetCore.Mvc;

namespace ${PASCAL_CASE_NAME}.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ${PASCAL_CASE_NAME}Controller : ControllerBase
{
    private readonly ILogger<${PASCAL_CASE_NAME}Controller> _logger;

    public ${PASCAL_CASE_NAME}Controller(ILogger<${PASCAL_CASE_NAME}Controller> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Getting ${SERVICE_NAME} data");
        return Ok(new 
        { 
            service = "${SERVICE_NAME}", 
            message = "Hello from ${PASCAL_CASE_NAME}!",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        _logger.LogInformation("Getting ${SERVICE_NAME} by id: {Id}", id);
        return Ok(new 
        { 
            id, 
            service = "${SERVICE_NAME}",
            timestamp = DateTime.UtcNow
        });
    }
}
EOF

echo ""
echo -e "${BLUE}🎉 服务创建完成！${NC}"
echo ""
echo -e "${YELLOW}📋 后续步骤：${NC}"
echo ""
echo -e "1️⃣  ${GREEN}构建 Docker 镜像:${NC}"
echo "   cd $(pwd)"
echo "   docker build -t ${CONTAINER_NAME}:latest -f ${SERVICE_DIR}/Dockerfile ."
echo ""
echo -e "2️⃣  ${GREEN}启动服务（将自动注册到 Consul）:${NC}"
echo "   docker run -d \\"
echo "     --name ${CONTAINER_NAME} \\"
echo "     --network go-nomads-network \\"
echo "     -e ASPNETCORE_ENVIRONMENT=Development \\"
echo "     -p ${HOST_PORT}:8080 \\"
echo "     ${CONTAINER_NAME}:latest"
echo ""
echo -e "3️⃣  ${GREEN}验证服务注册:${NC}"
echo "   # 检查健康状态"
echo "   curl http://localhost:${HOST_PORT}/health"
echo ""
echo "   # 检查 Consul 注册"
echo "   curl http://localhost:8500/v1/catalog/service/${SERVICE_NAME}"
echo ""
echo "   # 测试 API"
echo "   curl http://localhost:${HOST_PORT}/api/${PASCAL_CASE_NAME}"
echo ""
echo -e "4️⃣  ${GREEN}查看监控（自动出现在 Dashboard 中）:${NC}"
echo "   Grafana: http://localhost:3000/d/go-nomads-services"
echo "   Prometheus: http://localhost:9090/targets"
echo ""
echo -e "${YELLOW}💡 提示: 服务将在 20-30 秒后自动出现在 Prometheus 和 Grafana 中${NC}"

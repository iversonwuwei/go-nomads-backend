# Go-Nomads Scalar 文档系统 📚

![Scalar](https://img.shields.io/badge/Scalar-1.2.42-purple)
![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Status](https://img.shields.io/badge/Status-Running-green)

## 快速开始 🚀

### 访问文档

**统一文档门户** (推荐):
```
http://localhost:5003/scalar/v1
```

**各服务独立文档**:
- Gateway: `http://localhost:5000/scalar/v1`
- Product Service: `http://localhost:5001/scalar/v1`
- User Service: `http://localhost:5002/scalar/v1`
- Document Service: `http://localhost:5003/scalar/v1`

## 系统架构 🏗️

```
DocumentService (文档中心)
    ├── 聚合所有服务的 OpenAPI 规范
    ├── 提供统一的文档访问入口
    └── 管理服务列表和健康状态

各微服务
    ├── Gateway (Saturn 主题)
    ├── ProductService (Mars 主题)
    ├── UserService (BluePlanet 主题)
    └── 各自独立的 Scalar UI
```

## 主要特性 ✨

### 1. 统一文档门户
- 📚 一站式访问所有微服务文档
- 🎨 优雅的 Scalar UI 界面
- 🔍 强大的搜索功能

### 2. 多服务支持
- 🌐 4 个微服务独立文档
- 🎨 不同主题区分服务
- 🔄 实时同步 OpenAPI 规范

### 3. 交互式测试
- 🧪 在文档中直接测试 API
- 📝 多语言代码示例
- 📊 实时查看请求/响应

## API 端点 🔧

### DocumentService API

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/services` | GET | 获取所有服务列表 |
| `/api/specs` | GET | 获取聚合的 OpenAPI 规范 |
| `/health` | GET | 健康检查 |
| `/scalar/v1` | GET | Scalar UI 界面 |

### 示例

```bash
# 获取服务列表
curl http://localhost:5003/api/services

# 获取聚合的 OpenAPI 规范
curl http://localhost:5003/api/specs

# 健康检查
curl http://localhost:5003/health
```

## 部署 📦

### 使用部署脚本

```powershell
.\deployment\scripts\deploy-document-service.ps1
```

### 手动部署

```powershell
# 1. 构建镜像
podman build -f src/Services/DocumentService/Dockerfile `
  -t go-nomads-document-service:latest .

# 2. 运行容器
podman run -d `
  --name go-nomads-document-service `
  --network go-nomads-network `
  -p 5003:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  go-nomads-document-service:latest

# 3. 注册到 Consul
$service = Get-Content deployment/consul/services/document-service.json
Invoke-RestMethod -Uri "http://localhost:8500/v1/agent/service/register" `
  -Method Put -Body $service -ContentType "application/json"
```

## 配置 ⚙️

### Scalar 主题

每个服务配置了不同的主题:

```csharp
// Gateway - Saturn (土星)
app.MapScalarApiReference(options => 
    options.WithTheme(ScalarTheme.Saturn));

// ProductService - Mars (火星)
app.MapScalarApiReference(options => 
    options.WithTheme(ScalarTheme.Mars));

// UserService - BluePlanet (蓝色星球)
app.MapScalarApiReference(options => 
    options.WithTheme(ScalarTheme.BluePlanet));

// DocumentService - Purple (紫色)
app.MapScalarApiReference(options => 
    options.WithTheme(ScalarTheme.Purple));
```

### 可用主题

- `Default`
- `Alternate`
- `Moon`
- `Purple`
- `Solarized`
- `BluePlanet`
- `Saturn`
- `Kepler`
- `Mars`
- `DeepSpace`

### 自定义 OpenAPI 文档

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "My API";
        document.Info.Description = "API 描述";
        document.Info.Version = "v1.0";
        return Task.CompletedTask;
    });
});
```

## 使用技巧 💡

### 快捷键

- `Ctrl/Cmd + K` - 打开搜索
- `Tab` - 在元素间导航
- `Enter` - 展开/折叠

### 添加 API 描述

```csharp
app.MapGet("/api/products", async () => { ... })
   .WithName("GetProducts")
   .WithTags("Products")
   .WithOpenApi(operation =>
   {
       operation.Summary = "获取产品列表";
       operation.Description = "详细的操作描述";
       return operation;
   });
```

### 代码示例

Scalar 自动生成多种语言的示例:
- C# (HttpClient)
- JavaScript (Fetch, Axios)
- Python (Requests)
- cURL
- Go
- PHP
- 等等...

## 故障排查 🔍

### Scalar UI 无法加载

```powershell
# 检查服务状态
podman ps | Select-String "document-service"

# 查看日志
podman logs go-nomads-document-service

# 重启服务
podman restart go-nomads-document-service
```

### 无法获取其他服务的规范

```powershell
# 测试网络连接
podman exec go-nomads-document-service `
  curl http://go-nomads-gateway:8080/openapi/v1.json

# 检查服务是否在同一网络
podman network inspect go-nomads-network
```

## 文档 📖

详细文档请查看:
- **完整文档**: `deployment/SCALAR_DOCUMENTATION.md`
- **部署报告**: `deployment/SCALAR_DEPLOYMENT_REPORT.md`
- **快速访问**: `SCALAR_QUICK_ACCESS.md`

## 验证状态 ✅

| 服务 | 状态 | 端口 | Scalar UI |
|------|------|------|-----------|
| DocumentService | ✅ 运行中 | 5003 | ✅ 可访问 |
| Gateway | ✅ 运行中 | 5000 | ✅ 可访问 |
| ProductService | ✅ 运行中 | 5001 | ✅ 可访问 |
| UserService | ✅ 运行中 | 5002 | ✅ 可访问 |

## 技术栈 🛠️

- **Scalar.AspNetCore**: 1.2.42
- **.NET**: 9.0
- **Dapr**: 1.16.0
- **OpenAPI**: 3.0+
- **容器**: Podman
- **服务发现**: Consul

## 贡献 🤝

欢迎贡献改进:
1. 添加更多 API 文档描述
2. 改进 OpenAPI 规范
3. 优化 Scalar 配置
4. 添加更多示例

## 许可证 📄

MIT License

---

**快速访问主文档**: http://localhost:5003/scalar/v1 🎉

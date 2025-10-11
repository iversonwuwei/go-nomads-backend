# Scalar 文档管理系统

## 概述

DocumentService 是 Go-Nomads 项目的统一 API 文档中心,使用 Scalar 提供优雅的 API 文档界面。

## 架构

```
┌─────────────────────────────────────────────┐
│         DocumentService (统一文档中心)         │
│         http://localhost:5003                │
└─────────────────┬───────────────────────────┘
                  │
      ┌───────────┼───────────┐
      │           │           │
┌─────▼─────┐ ┌──▼──────┐ ┌──▼──────────┐
│  Gateway  │ │ Product │ │    User     │
│   :5000   │ │ Service │ │  Service    │
│           │ │  :5001  │ │   :5002     │
└───────────┘ └─────────┘ └─────────────┘
     │             │              │
     └─────────────┴──────────────┘
               Scalar UI
```

## 功能特性

### 1. 统一文档门户
- 📚 聚合所有微服务的 OpenAPI 文档
- 🎨 漂亮的 Scalar UI 界面
- 🔍 快速搜索和导航

### 2. 各服务独立文档
每个服务都配置了 Scalar UI:

| 服务 | 文档地址 | 主题 |
|------|---------|------|
| Gateway | http://localhost:5000/scalar/v1 | Saturn |
| Product Service | http://localhost:5001/scalar/v1 | Mars |
| User Service | http://localhost:5002/scalar/v1 | BluePlanet |
| Document Service | http://localhost:5003/scalar/v1 | Purple |

### 3. API 端点

#### `/api/specs` - 获取所有服务的 OpenAPI 规范
```bash
curl http://localhost:5003/api/specs
```

返回示例:
```json
{
  "gateway": { "openapi": "3.0.1", ... },
  "product-service": { "openapi": "3.0.1", ... },
  "user-service": { "openapi": "3.0.1", ... }
}
```

#### `/api/services` - 获取服务列表
```bash
curl http://localhost:5003/api/services
```

返回示例:
```json
[
  {
    "name": "Gateway",
    "url": "http://localhost:5000",
    "docsUrl": "http://localhost:5000/scalar/v1"
  },
  ...
]
```

## 部署步骤

### 1. 构建并部署 DocumentService

```powershell
# 使用部署脚本
.\deployment\scripts\deploy-document-service.ps1
```

或手动部署:

```powershell
# 构建镜像
podman build -f src/Services/DocumentService/Dockerfile -t go-nomads-document-service:latest .

# 运行容器
podman run -d `
  --name go-nomads-document-service `
  --network go-nomads-network `
  -p 5003:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  go-nomads-document-service:latest
```

### 2. 注册到 Consul

```powershell
# 进入 Consul 容器
podman exec -it go-nomads-consul /bin/sh

# 注册服务
consul services register /consul/config/document-service.json
```

或使用 API:
```powershell
$service = Get-Content deployment/consul/services/document-service.json
Invoke-RestMethod -Uri "http://localhost:8500/v1/agent/service/register" -Method Put -Body $service -ContentType "application/json"
```

### 3. 重新部署其他服务

由于我们为 Gateway、ProductService 和 UserService 添加了 Scalar 支持,需要重新构建部署:

```powershell
# 重新构建 Gateway
podman build -f src/Gateway/Gateway/Dockerfile -t go-nomads-gateway:latest .
podman stop go-nomads-gateway; podman rm go-nomads-gateway
podman run -d --name go-nomads-gateway --network go-nomads-network -p 5000:8080 -e ASPNETCORE_ENVIRONMENT=Development go-nomads-gateway:latest

# 重新构建 ProductService
podman build -f src/Services/ProductService/ProductService/Dockerfile -t go-nomads-product-service:latest .
podman stop go-nomads-product-service; podman rm go-nomads-product-service
podman run -d --name go-nomads-product-service --network go-nomads-network -p 5001:8080 -e ASPNETCORE_ENVIRONMENT=Development go-nomads-product-service:latest

# 重新构建 UserService
podman build -f src/Services/UserService/UserService/Dockerfile -t go-nomads-user-service:latest .
podman stop go-nomads-user-service; podman rm go-nomads-user-service
podman run -d --name go-nomads-user-service --network go-nomads-network -p 5002:8080 -e ASPNETCORE_ENVIRONMENT=Development go-nomads-user-service:latest
```

## 使用指南

### 访问文档中心
打开浏览器访问: http://localhost:5003/scalar/v1

### 查看各服务文档
- **Gateway**: http://localhost:5000/scalar/v1
- **Product Service**: http://localhost:5001/scalar/v1
- **User Service**: http://localhost:5002/scalar/v1

### Scalar UI 特性

#### 1. 快捷键
- `Ctrl/Cmd + K` - 快速搜索
- 支持键盘导航

#### 2. 代码示例
Scalar 自动生成多种语言的示例代码:
- C# (HttpClient)
- JavaScript (Fetch)
- Python (Requests)
- cURL
- 等等...

#### 3. 实时测试
- 直接在文档界面测试 API
- 支持认证配置
- 查看请求/响应

#### 4. 主题定制
我们为每个服务配置了不同的主题:
- `Saturn` - Gateway (土星主题)
- `Mars` - Product Service (火星主题)
- `BluePlanet` - User Service (蓝色星球)
- `Purple` - Document Service (紫色主题)

可选主题:
- `Default`, `Alternate`, `Moon`, `Purple`, `Solarized`, `BluePlanet`, `Saturn`, `Kepler`, `Mars`, `DeepSpace`, `None`

## 配置说明

### DocumentService 配置 (appsettings.json)

```json
{
  "Services": {
    "Gateway": {
      "Name": "Gateway",
      "Url": "http://go-nomads-gateway:8080",
      "OpenApiUrl": "http://go-nomads-gateway:8080/openapi/v1.json"
    },
    "ProductService": {
      "Name": "Product Service",
      "Url": "http://go-nomads-product-service:8080",
      "OpenApiUrl": "http://go-nomads-product-service:8080/openapi/v1.json"
    },
    "UserService": {
      "Name": "User Service",
      "Url": "http://go-nomads-user-service:8080",
      "OpenApiUrl": "http://go-nomads-user-service:8080/openapi/v1.json"
    }
  }
}
```

### Scalar 配置选项

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("API Title")                  // 文档标题
        .WithTheme(ScalarTheme.Purple)            // UI 主题
        .WithDefaultHttpClient(                   // 默认客户端
            ScalarTarget.CSharp,                 // 目标语言
            ScalarClient.HttpClient)             // 客户端类型
        .WithModels(true)                        // 显示模型
        .WithDownloadButton(true)                // 下载按钮
        .WithSearchHotKey("k");                  // 搜索快捷键
});
```

## 故障排查

### 问题: 无法访问文档页面

**检查项:**
1. 确认服务已启动
```powershell
podman ps | Select-String "document-service"
```

2. 检查服务健康状态
```powershell
curl http://localhost:5003/health
```

### 问题: 无法获取其他服务的 OpenAPI 规范

**检查项:**
1. 确认目标服务已启动
2. 检查网络连接
```powershell
podman exec go-nomads-document-service curl http://go-nomads-gateway:8080/openapi/v1.json
```

3. 查看 DocumentService 日志
```powershell
podman logs go-nomads-document-service
```

### 问题: Scalar UI 显示不正常

**检查项:**
1. 清除浏览器缓存
2. 检查 Console 错误信息
3. 确认 Scalar.AspNetCore 包版本 (1.2.42)

## 高级功能

### 1. 自定义 OpenAPI 文档

在 Program.cs 中配置:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "My API";
        document.Info.Description = "Detailed API description";
        document.Info.Version = "v1.0";
        document.Info.Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@example.com",
            Url = new Uri("https://example.com/support")
        };
        return Task.CompletedTask;
    });
});
```

### 2. 添加 API 标签和描述

```csharp
app.MapGet("/api/products", async () => { ... })
   .WithName("GetProducts")
   .WithTags("Products")
   .WithOpenApi(operation =>
   {
       operation.Summary = "获取产品列表";
       operation.Description = "返回所有可用的产品信息";
       return operation;
   });
```

### 3. 动态服务发现

可以扩展 DocumentService 从 Consul 动态发现服务:

```csharp
app.MapGet("/api/dynamic-specs", async (IConsulClient consulClient) =>
{
    // 从 Consul 获取所有服务
    var services = await consulClient.Catalog.Services();
    
    // 动态获取每个服务的 OpenAPI 规范
    // ...
});
```

## 最佳实践

1. **使用有意义的操作 ID**
```csharp
.WithName("GetProductById")  // 清晰的操作名称
```

2. **添加详细的描述**
```csharp
.WithOpenApi(op => {
    op.Summary = "简短摘要";
    op.Description = "详细的操作描述";
    return op;
})
```

3. **使用标签组织 API**
```csharp
.WithTags("Products", "v1")
```

4. **定期更新文档**
- 代码变更后及时更新描述
- 保持 OpenAPI 规范与实际 API 同步

5. **版本管理**
- 使用版本号标识 API 版本
- 为不同版本提供独立的文档

## 总结

✅ 已完成:
- 创建 DocumentService 统一文档中心
- 为所有服务添加 Scalar UI 支持
- 配置不同主题区分各服务
- 提供 API 聚合和服务列表端点
- 创建部署脚本和 Consul 注册配置

📚 DocumentService 提供:
- 优雅的 API 文档界面
- 统一的文档入口
- 实时 API 测试
- 多语言代码示例
- 快速搜索和导航

现在您可以通过访问 http://localhost:5003/scalar/v1 查看完整的 API 文档!

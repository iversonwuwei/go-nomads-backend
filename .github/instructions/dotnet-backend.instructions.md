---
applyTo: "src/**/*.cs"
---

# .NET 后端开发规范

## Aspire 编排
- 新服务必须在 `GoNomads.AppHost/Program.cs` 中注册
- 使用 `builder.AddProject<Projects.XxxService>()` 注册
- 服务间引用用 `.WithReference(otherService)`
- Gateway 必须 `.WithReference()` 所有需要代理的服务
- 新路由必须在 `Gateway/yarp.json` 中添加对应条目

## Controller 模式
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class XxxController : ControllerBase
{
    // 注入 Service，不要在 Controller 写业务逻辑
}
```

## DTO 规范
- 请求: `CreateXxxRequest`, `UpdateXxxRequest`
- 响应: `XxxDto`, `XxxListDto`
- 使用 AutoMapper Profile 映射
- 使用 FluentValidation 验证请求

## 异常处理
- 服务层抛出业务异常，Controller 层由全局异常中间件统一处理
- 不要在 Controller 中写 try-catch（全局中间件已处理）

## 数据库
- ORM: SqlSugarCore
- 外部 Supabase PostgreSQL，不由 Aspire 编排
- 数据库迁移脚本放在 `migrations/` 目录

## 中间件顺序（Gateway）
1. Authentication
2. RateLimiting
3. DynamicRateLimit
4. HttpMethodOverride
5. JwtAuthenticationInterceptor
6. YARP ReverseProxy

## 日志
- 使用 Serilog，不要使用 `Console.WriteLine`
- 结构化日志：`Log.Information("Processing {Action} for {UserId}", action, userId)`

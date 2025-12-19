# 私有方法重构指南 - ICurrentUserService

## 概述

本项目创建了统一的 `ICurrentUserService` 接口来替代各 Controller 中重复定义的私有方法。

## ✅ 重构完成状态

| 服务 | Controller | 状态 |
|------|------------|------|
| CityService | CitiesController | ✅ 已重构 |
| CityService | CityRatingsController | ✅ 已重构 |
| CityService | UserFavoriteCitiesController | ✅ 已重构 |
| CoworkingService | CoworkingController | ✅ 已重构 |
| CoworkingService | CoworkingReviewController | ✅ 已重构 |
| UserService | MembershipController | ✅ 已重构 |
| MessageService | ChatsController | ✅ 已重构 |
| AIService | - | 无需重构 |
| EventService | - | 无需重构 |

## 已创建的文件

```
src/Shared/Shared/Services/
├── ICurrentUserService.cs      # 接口定义
├── CurrentUserService.cs       # 实现类
src/Shared/Shared/Extensions/
└── UserServiceExtensions.cs    # DI 扩展方法
```

## 使用方法

### 1. 在服务的 Program.cs 中注册

```csharp
using GoNomads.Shared.Extensions;

// 在 builder.Services 配置中添加
builder.Services.AddCurrentUserService();
```

### 2. 在 Controller 中注入使用

**重构前（旧代码）：**

```csharp
[ApiController]
public class CityRatingsController : ControllerBase
{
    // ❌ 每个 Controller 都重复定义这些私有方法
    private Guid? GetCurrentUserId()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
            {
                if (Guid.TryParse(userContext.UserId, out var userId))
                    return userId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "获取当前用户ID失败");
        }
        return null;
    }

    private bool IsAdmin()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            return userContext?.Role == "admin";  // ⚠️ 有的地方用 "Admin"，大小写不一致
        }
        catch { return false; }
    }

    // 使用
    [HttpPost]
    public async Task<IActionResult> CreateRating(...)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        
        if (!IsAdmin()) return Forbid();
        // ...
    }
}
```

**重构后（新代码）：**

```csharp
using GoNomads.Shared.Services;

[ApiController]
public class CityRatingsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;  // ✅ 注入服务

    public CityRatingsController(
        ICurrentUserService currentUser,
        // ... 其他依赖
    )
    {
        _currentUser = currentUser;
    }

    // ✅ 不再需要私有方法！

    [HttpPost]
    public async Task<IActionResult> CreateRating(...)
    {
        // ✅ 直接使用服务方法
        var userId = _currentUser.TryGetUserId();
        if (userId == null) return Unauthorized();
        
        if (!_currentUser.IsAdmin()) return Forbid();
        // ...
    }
}
```

## API 参考

### 用户身份获取

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `GetUserId()` | `Guid` | 获取用户ID，未认证时抛出异常 |
| `TryGetUserId()` | `Guid?` | 尝试获取用户ID，未认证返回 null |
| `GetUserIdString()` | `string?` | 获取用户ID字符串形式 |
| `GetUserEmail()` | `string?` | 获取用户邮箱 |
| `GetUserRole()` | `string?` | 获取用户角色 |
| `GetUserContext()` | `UserContext?` | 获取完整用户上下文 |

### 认证状态

| 属性 | 返回类型 | 说明 |
|------|---------|------|
| `IsAuthenticated` | `bool` | 是否已认证 |

### 权限检查

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `IsAdmin()` | `bool` | 是否为管理员 |
| `IsModerator()` | `bool` | 是否为版主（含管理员） |
| `HasRole(role)` | `bool` | 是否拥有指定角色 |
| `HasAnyRole(roles)` | `bool` | 是否拥有任一角色 |
| `HasAdminOrModeratorPrivileges()` | `bool` | 是否有管理权限 |

### 资源所有权

| 方法 | 返回类型 | 说明 |
|------|---------|------|
| `IsOwner(ownerId)` | `bool` | 是否为资源所有者 |
| `CanAccess(ownerId)` | `bool` | 是否可访问（所有者或管理员） |
| `CanAccessOrModerate(ownerId)` | `bool` | 是否可访问或管理 |

## 需要重构的文件清单

以下 Controller 文件包含重复的私有方法，应逐步重构：

### 高优先级（GetUserId 相关）

- [ ] `CityService/API/Controllers/CitiesController.cs`
- [ ] `CityService/API/Controllers/CityRatingsController.cs`
- [ ] `CityService/API/Controllers/UserCityContentController.cs`
- [ ] `CityService/API/Controllers/MyContentController.cs`
- [ ] `CityService/API/Controllers/UserFavoriteCitiesController.cs`
- [ ] `AIService/API/Controllers/ChatController.cs`
- [ ] `CoworkingService/API/Controllers/CoworkingController.cs`
- [ ] `CoworkingService/API/Controllers/CoworkingReviewController.cs`
- [ ] `UserService/API/Controllers/MembershipController.cs`
- [ ] `MessageService/API/Controllers/ChatsController.cs`

### 中优先级（权限检查相关）

- [ ] `CityService/API/Controllers/CityRatingsController.cs` - IsAdmin(), IsModerator()
- [ ] `UserService/API/Controllers/MembershipController.cs` - IsAdmin()
- [ ] `CoworkingService/API/Controllers/CoworkingController.cs` - HasAdminPrivileges(), HasModeratorPrivileges()

## 重构步骤

1. **在每个服务的 Program.cs 中注册服务**
   ```csharp
   builder.Services.AddCurrentUserService();
   ```

2. **在 Controller 构造函数中注入 ICurrentUserService**

3. **删除私有方法，改用服务方法**

4. **运行测试确保功能正常**

## 注意事项

- 角色名称已统一为**小写**（admin, moderator, user）
- `IsModerator()` 会返回 true 如果是 admin 或 moderator
- 建议保留领域特定的 `MapToDto()` 方法，它们不应该被统一

# UserService DDD + 三层架构重构

## ✅ 已完成工作

### 1. Domain Layer (领域层)
- ✅ `Domain/Entities/User.cs` - User 聚合根
  - 私有 setter 封装状态
  - 工厂方法: `Create()`, `CreateWithPassword()`
  - 领域方法: `Update()`, `ChangePassword()`, `SetPassword()`, `ValidatePassword()`, `ChangeRole()`
  
- ✅ `Domain/Entities/Role.cs` - Role 实体
  - 私有 setter
  - 工厂方法: `Create()`
  - 领域方法: `Update()`

- ✅ `Domain/Repositories/IUserRepository.cs` - 用户仓储接口
- ✅ `Domain/Repositories/IRoleRepository.cs` - 角色仓储接口

### 2. Infrastructure Layer (基础设施层)
- ✅ `Infrastructure/Repositories/UserRepository.cs` - Supabase 实现
- ✅ `Infrastructure/Repositories/RoleRepository.cs` - Supabase 实现

### 3. Application Layer (应用层)
- ✅ DTOs 已移动到 `Application/DTOs/`

### 4. 编译状态
- ✅ 编译成功，无警告

## ⏳ 待完成工作

### 1. Application Layer (应用层)
- ⏳ 创建 `Application/Services/IUserService.cs`
- ⏳ 创建 `Application/Services/UserApplicationService.cs`
- ⏳ 创建 `Application/Services/IAuthService.cs`
- ⏳ 创建 `Application/Services/AuthApplicationService.cs`
- ⏳ 更新 DTOs namespace

### 2. API Layer (表现层)
- ⏳ 移动 `Controllers/UsersController.cs` 到 `API/Controllers/`
- ⏳ 更新 Controller 使其变薄
- ⏳ 使用 UserContext 获取用户信息

### 3. 依赖注入
- ⏳ 更新 `Program.cs` 注册新的仓储和服务

### 4. 清理
- ⏳ 删除旧的 `Services/` 和 `Repositories/` 目录

## 📝 下一步操作

继续执行以下命令完成重构：

```bash
# 1. 创建 Application Services
# 2. 更新 API Controllers
# 3. 更新 Program.cs
# 4. 编译并测试
# 5. 部署服务
```

## 🎯 参考
- EventService/ARCHITECTURE_DDD.md
- EventService 目录结构

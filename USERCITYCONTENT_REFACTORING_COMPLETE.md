# UserCityContent 架构重构完成

## 📋 重构概述

按照 CityService 的 Clean Architecture 设计标准,成功重构了 UserCityContent 模块。

## ✅ 完成内容

### 1️⃣ Domain 层 (领域层)

#### 实体 (`Domain/Entities/`)
- ✅ `UserCityPhoto.cs` - 用户城市照片实体
- ✅ `UserCityExpense.cs` - 用户城市费用实体
- ✅ `UserCityReview.cs` - 用户城市评论实体

**特点**:
- 使用 Postgrest 特性标记表映射
- 包含完整的验证特性 (`[Required]`, `[MaxLength]`, `[Range]`)
- 与数据库无关,仅定义领域模型

#### 仓储接口 (`Domain/Repositories/`)
- ✅ `IUserCityPhotoRepository.cs`
- ✅ `IUserCityExpenseRepository.cs`
- ✅ `IUserCityReviewRepository.cs`

**特点**:
- 定义数据访问契约
- 不包含实现细节
- 支持 CRUD 和查询操作

---

### 2️⃣ Infrastructure 层 (基础设施层)

#### 仓储实现 (`Infrastructure/Repositories/`)
- ✅ `SupabaseUserCityPhotoRepository.cs`
- ✅ `SupabaseUserCityExpenseRepository.cs`
- ✅ `SupabaseUserCityReviewRepository.cs`

**特点**:
- 继承 `SupabaseRepositoryBase<T>`
- 实现 Domain 层定义的接口
- 使用 Supabase Postgrest 客户端进行数据库操作
- 包含错误处理和日志记录

**关键方法**:
```csharp
// 照片仓储
Task<UserCityPhoto> CreateAsync(UserCityPhoto photo)
Task<IEnumerable<UserCityPhoto>> GetByCityIdAsync(string cityId)
Task<IEnumerable<UserCityPhoto>> GetByCityIdAndUserIdAsync(string cityId, Guid userId)
Task<bool> DeleteAsync(Guid id, Guid userId)

// 费用仓储
Task<UserCityExpense> CreateAsync(UserCityExpense expense)
Task<IEnumerable<UserCityExpense>> GetByCityIdAsync(string cityId)
Task<bool> DeleteAsync(Guid id, Guid userId)

// 评论仓储
Task<UserCityReview> UpsertAsync(UserCityReview review)
Task<IEnumerable<UserCityReview>> GetByCityIdAsync(string cityId)
Task<UserCityReview?> GetByCityIdAndUserIdAsync(string cityId, Guid userId)
Task<decimal?> GetAverageRatingAsync(string cityId)
```

---

### 3️⃣ Application 层 (应用层)

#### DTOs (`Application/DTOs/UserCityContentDTOs.cs`)
- ✅ `UserCityPhotoDto` - 照片数据传输对象
- ✅ `AddCityPhotoRequest` - 添加照片请求
- ✅ `UserCityExpenseDto` - 费用数据传输对象
- ✅ `AddCityExpenseRequest` - 添加费用请求
- ✅ `UserCityReviewDto` - 评论数据传输对象
- ✅ `UpsertCityReviewRequest` - 创建/更新评论请求
- ✅ `CityUserContentStatsDto` - 统计数据对象
- ✅ `ExpenseCategory` - 费用分类常量

**特点**:
- 包含完整的验证特性
- 与实体分离,用于 API 交互
- 符合 RESTful 设计

#### 应用服务 (`Application/Services/`)
- ✅ `IUserCityContentService.cs` - 服务接口
- ✅ `UserCityContentApplicationService.cs` - 服务实现

**特点**:
- 编排业务逻辑
- Entity ↔ DTO 映射
- 调用 Domain 层仓储
- 统一日志记录

**核心方法**:
```csharp
// 照片相关
Task<UserCityPhotoDto> AddPhotoAsync(Guid userId, AddCityPhotoRequest request)
Task<IEnumerable<UserCityPhotoDto>> GetCityPhotosAsync(string cityId, Guid? userId = null)
Task<bool> DeletePhotoAsync(Guid userId, Guid photoId)

// 费用相关
Task<UserCityExpenseDto> AddExpenseAsync(Guid userId, AddCityExpenseRequest request)
Task<IEnumerable<UserCityExpenseDto>> GetCityExpensesAsync(string cityId, Guid? userId = null)
Task<bool> DeleteExpenseAsync(Guid userId, Guid expenseId)

// 评论相关
Task<UserCityReviewDto> UpsertReviewAsync(Guid userId, UpsertCityReviewRequest request)
Task<IEnumerable<UserCityReviewDto>> GetCityReviewsAsync(string cityId)
Task<bool> DeleteReviewAsync(Guid userId, string cityId)

// 统计
Task<CityUserContentStatsDto> GetCityStatsAsync(string cityId)
```

---

### 4️⃣ API 层 (控制器层)

#### Controllers (`API/Controllers/`)
- ✅ `UserCityContentController.cs` - 城市内容 API
- ✅ `MyContentController.cs` - 我的内容 API

**特点**:
- 仅处理 HTTP 请求/响应
- 使用 `UserContextMiddleware` 获取用户信息
- 统一返回 `ApiResponse<T>` 格式
- 完整的异常处理和日志
- 支持 `[AllowAnonymous]` 公开访问

**API 端点**:

#### UserCityContentController (`/api/v1/cities/{cityId}/user-content`)
```http
# 照片
POST   /photos              - 添加照片
GET    /photos?onlyMine     - 获取照片 [AllowAnonymous]
DELETE /photos/{photoId}    - 删除照片

# 费用
POST   /expenses            - 添加费用
GET    /expenses?onlyMine   - 获取费用 [AllowAnonymous]
DELETE /expenses/{expenseId} - 删除费用

# 评论
POST   /reviews             - 创建/更新评论
GET    /reviews             - 获取评论 [AllowAnonymous]
DELETE /reviews             - 删除评论

# 统计
GET    /stats               - 获取统计 [AllowAnonymous]
```

#### MyContentController (`/api/v1/user/city-content`)
```http
GET /photos              - 获取我的所有照片
GET /expenses            - 获取我的所有费用
GET /reviews/{cityId}    - 获取我对某城市的评论
```

---

### 5️⃣ 依赖注入配置

#### Program.cs 更新
```csharp
// Domain Repositories
builder.Services.AddScoped<IUserCityPhotoRepository, SupabaseUserCityPhotoRepository>();
builder.Services.AddScoped<IUserCityExpenseRepository, SupabaseUserCityExpenseRepository>();
builder.Services.AddScoped<IUserCityReviewRepository, SupabaseUserCityReviewRepository>();

// Application Services
builder.Services.AddScoped<IUserCityContentService, UserCityContentApplicationService>();
```

---

## 🔄 架构对比

### ❌ 重构前
```
Services/
  UserCityContentService.cs  ← 直接操作数据库 (NpgsqlConnection)
DTOs/
  UserCityContentDTOs.cs     ← 位置错误
API/
  UserCityContentController.cs ← 位置错误
```

### ✅ 重构后
```
Domain/
  Entities/
    UserCityPhoto.cs         ← 领域实体
    UserCityExpense.cs
    UserCityReview.cs
  Repositories/
    IUserCityPhotoRepository.cs    ← 仓储接口
    IUserCityExpenseRepository.cs
    IUserCityReviewRepository.cs

Infrastructure/
  Repositories/
    SupabaseUserCityPhotoRepository.cs    ← 仓储实现
    SupabaseUserCityExpenseRepository.cs
    SupabaseUserCityReviewRepository.cs

Application/
  DTOs/
    UserCityContentDTOs.cs   ← DTO 定义
  Services/
    IUserCityContentService.cs          ← 服务接口
    UserCityContentApplicationService.cs ← 服务实现 (调用 Repository)

API/
  Controllers/
    UserCityContentController.cs  ← HTTP 请求处理
    MyContentController.cs
```

---

## 📊 设计原则遵循

### ✅ 单一职责原则 (SRP)
- Controller: 仅处理 HTTP
- Service: 业务逻辑编排
- Repository: 数据访问
- Entity: 领域模型

### ✅ 依赖倒置原则 (DIP)
- Service 依赖 Repository 接口 (不是实现)
- Controller 依赖 Service 接口 (不是实现)

### ✅ 开闭原则 (OCP)
- 通过接口扩展,而非修改
- 新增仓储实现无需修改 Service

### ✅ 接口隔离原则 (ISP)
- 三个独立的仓储接口 (Photo, Expense, Review)
- 单一服务接口聚合所有操作

---

## 🎯 关键改进

### 1. **数据访问层分离**
- ❌ 原来: Service 直接操作 `NpgsqlConnection`
- ✅ 现在: Service → Repository Interface → Supabase Repository

### 2. **类型安全**
- ❌ 原来: SQL 字符串拼接,运行时错误
- ✅ 现在: 强类型实体和 LINQ 查询

### 3. **可测试性**
- ❌ 原来: 无法 Mock 数据库连接
- ✅ 现在: 可以 Mock IRepository 接口

### 4. **符合 CityService 标准**
- ✅ 与 `CitiesController` 相同的结构
- ✅ 与 `CityApplicationService` 相同的模式
- ✅ 与 `SupabaseCityRepository` 相同的实现

---

## 🚀 下一步

### 可选优化
1. **删除旧文件**:
   - `Services/UserCityContentService.cs` (已废弃)
   - `DTOs/UserCityContentDTOs.cs` (已迁移到 Application/DTOs/)
   - `API/UserCityContentController.cs` (已迁移到 API/Controllers/)

2. **重新构建 Docker 镜像**:
   ```bash
   cd e:\Workspaces\WaldenProjects\go-nomads
   docker-compose build cityservice
   docker-compose up -d cityservice
   ```

3. **测试 API**:
   - 访问 http://localhost:8002/scalar/v1 查看 API 文档
   - 测试照片上传、费用记录、评论功能

---

## 📝 总结

✅ **完全符合 Clean Architecture 设计**
✅ **与 CityService 其他模块保持一致**
✅ **代码可维护性大幅提升**
✅ **编译通过,无错误**

重构完成时间: 2025-01-31

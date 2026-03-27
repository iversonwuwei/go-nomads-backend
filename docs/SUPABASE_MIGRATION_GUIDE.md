# Supabase 集成迁移指南

## 概述

本文档说明如何将所有 *Service 项目从 Entity Framework Core 模式迁移到 Supabase 直接集成模式。

## 支付字段通用化与旧字段退役计划

### 背景

- UserService 支付链路已不再只服务 PayPal，同一套订单与交易表需要兼容 WeChat Pay 等第三方支付方式。
- 新代码已切换到 `external_*` 通用字段，避免继续把支付语义绑定到 `paypal_*` 列名。

### 当前状态

- 新建库脚本已使用通用字段，见 `migrations/create_payment_tables.sql`。
- 存量库需执行 `migrations/add_generic_external_payment_columns.sql`，为 `orders` 与 `payment_transactions` 添加 `external_*` 字段并从旧 `paypal_*` 字段回填数据。
- 现阶段旧 `paypal_*` 字段保留，仅用于历史兼容与安全回滚。

### 退役阶段

1. 阶段一：新增并回填通用字段，应用代码全部切到 `external_*`。
2. 阶段二：验证生产流量与历史订单读写均不再依赖 `paypal_*` 字段。
3. 阶段三：补充最终迁移脚本，移除旧索引、旧列和旧注释。

### 退役前检查

- 确认所有服务、后台任务和报表查询都不再读取 `paypal_*` 字段。
- 确认所有支付方式的 webhook、主动确认、退款查询都以通用字段为准。
- 确认至少完成一轮生产数据抽样核对，检查新旧字段值一致。
- 确认有可回滚脚本或完整备份，再执行旧字段删除。

## 已完成的服务

### ✅ UserService
- **状态**: 已使用 Supabase 模式实现
- **模型**: `User` 继承自 `BaseModel`
- **仓储**: `SupabaseUserRepository` 继承自 `SupabaseRepositoryBase<User>`
- **参考位置**: `src/Shared/Shared/Models/User.cs`, `src/Services/UserService/UserService/Repositories/SupabaseUserRepository.cs`

### ✅ CoworkingService
- **状态**: 已转换为 Supabase 模式
- **模型**: 
  - `CoworkingSpace` 继承自 `BaseModel`
  - `CoworkingBooking` 继承自 `BaseModel`
- **仓储**: 
  - `SupabaseCoworkingRepository`
  - `SupabaseCoworkingBookingRepository`
- **位置**: `src/Services/CoworkingService/CoworkingService/`

## 待迁移服务

### 🔄 AccommodationService (酒店住宿服务)

**需要转换的模型**:
1. `Hotel` - 酒店信息
2. `RoomType` - 房型信息
3. `HotelBooking` - 酒店预订

**转换要点**:
- 移除 `using System.ComponentModel.DataAnnotations.Schema`
- 添加 `using Postgrest.Attributes` 和 `using Postgrest.Models`
- 类继承从 `public class Hotel` 改为 `public class Hotel : BaseModel`
- `[Table("hotels")]` 保持不变
- `[Key]` 改为 `[PrimaryKey("id", false)]`
- `[Column("column_name", TypeName = "...")]` 改为 `[Column("column_name")]`
- 移除所有导航属性 (如 `public virtual ICollection<RoomType>? RoomTypes { get; set; }`)
- PostGIS `Point` 类型改为 `string?` (存储 "POINT(longitude latitude)")
- JSONB 字段使用 `string?` 类型

**需要创建的仓储**:
- `SupabaseHotelRepository : SupabaseRepositoryBase<Hotel>`
- `SupabaseRoomTypeRepository : SupabaseRepositoryBase<RoomType>`
- `SupabaseHotelBookingRepository : SupabaseRepositoryBase<HotelBooking>`

### 🔄 EventService (活动服务)

**需要转换的模型**:
1. `Event` - 活动信息
2. `EventParticipant` - 活动参与者

**转换要点**: 同上

**需要创建的仓储**:
- `SupabaseEventRepository : SupabaseRepositoryBase<Event>`
- `SupabaseEventParticipantRepository : SupabaseRepositoryBase<EventParticipant>`

### 🔄 InnovationService (创新项目服务)

**需要转换的模型**:
1. `Innovation` - 创新项目
2. `InnovationLike` - 项目点赞
3. `InnovationComment` - 项目评论

**转换要点**: 同上

**需要创建的仓储**:
- `SupabaseInnovationRepository : SupabaseRepositoryBase<Innovation>`
- `SupabaseInnovationLikeRepository : SupabaseRepositoryBase<InnovationLike>`
- `SupabaseInnovationCommentRepository : SupabaseRepositoryBase<InnovationComment>`

### 🔄 TravelPlanningService (旅行规划服务)

**需要转换的模型**:
1. `TravelPlan` - 旅行计划
2. `TravelPlanCollaborator` - 计划协作者

**转换要点**: 同上

**需要创建的仓储**:
- `SupabaseTravelPlanRepository : SupabaseRepositoryBase<TravelPlan>`
- `SupabaseTravelPlanCollaboratorRepository : SupabaseRepositoryBase<TravelPlanCollaborator>`

### 🔄 EcommerceService (电商服务)

**需要转换的模型**:
1. `Product` - 产品
2. `CartItem` - 购物车项
3. `Order` - 订单
4. `OrderItem` - 订单项

**转换要点**: 同上

**需要创建的仓储**:
- `SupabaseProductRepository : SupabaseRepositoryBase<Product>`
- `SupabaseCartItemRepository : SupabaseRepositoryBase<CartItem>`
- `SupabaseOrderRepository : SupabaseRepositoryBase<Order>`
- `SupabaseOrderItemRepository : SupabaseRepositoryBase<OrderItem>`

### 🔄 DocumentService (文档服务)

**需要转换的模型**:
1. `Document` - 文档信息

**需要创建的仓储**:
- `SupabaseDocumentRepository : SupabaseRepositoryBase<Document>`

### 🔄 CityService (城市服务)

**需要转换的模型**:
1. `City` - 城市信息

**需要创建的仓储**:
- `SupabaseCityRepository : SupabaseRepositoryBase<City>`

### 🔄 Shared/Models/SharedEntities

**需要转换的共享模型**:
1. `Review` - 评论
2. `Favorite` - 收藏
3. `ChatMessage` - 聊天消息
4. `Notification` - 通知

## Supabase 模型转换模板

### 转换前 (EF Core 风格):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

[Table("hotels")]
public class Hotel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("location")]
    public Point? Location { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    // 导航属性
    public virtual ICollection<RoomType>? RoomTypes { get; set; }
}
```

### 转换后 (Supabase 风格):
```csharp
using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

[Table("hotels")]
public class Hotel : BaseModel
{
    [PrimaryKey("id", false)] // false = 数据库生成UUID
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// PostGIS POINT - 存储为字符串 "POINT(longitude latitude)"
    /// </summary>
    [Column("location")]
    public string? Location { get; set; }

    /// <summary>
    /// JSONB 字段 - 存储为 JSON 字符串
    /// </summary>
    [Column("metadata")]
    public string? Metadata { get; set; }

    // 不需要导航属性
}
```

## Supabase 仓储模板

```csharp
using YourService.Models;
using Shared.Repositories;
using Supabase;

namespace YourService.Repositories;

public class SupabaseYourEntityRepository : SupabaseRepositoryBase<YourEntity>
{
    public SupabaseYourEntityRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    /// <summary>
    /// 自定义查询方法示例
    /// </summary>
    public async Task<List<YourEntity>> GetByCustomFieldAsync(string fieldValue)
    {
        var response = await _supabaseClient
            .From<YourEntity>()
            .Where(x => x.CustomField == fieldValue)
            .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// 分页查询示例
    /// </summary>
    public async Task<List<YourEntity>> GetPagedAsync(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        
        var response = await _supabaseClient
            .From<YourEntity>()
            .Range(offset, offset + pageSize - 1)
            .Get();

        return response.Models;
    }
}
```

## Program.cs 配置模板

```csharp
using YourService.Repositories;
using Shared.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// 添加 Supabase 客户端
builder.Services.AddSupabase(builder.Configuration);

// 注册 Supabase 仓储
builder.Services.AddScoped<SupabaseYourEntityRepository>();
builder.Services.AddScoped<SupabaseAnotherEntityRepository>();

// 添加控制器
builder.Services.AddControllers();

// 添加 OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Your Service API",
        Version = "v1",
        Description = "服务描述"
    });
});

// 添加 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加健康检查
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseSerilogRequestLogging();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("YourService 正在启动...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "YourService 启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

## appsettings.json 配置

每个服务的 `appsettings.json` 需要添加 Supabase 配置:

```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your-anon-key",
    "Schema": "public",
    "AutoConnectRealtime": false,
    "AutoRefreshToken": true
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## 项目依赖包

每个服务的 `.csproj` 文件需要添加:

```xml
<ItemGroup>
  <PackageReference Include="Supabase" Version="0.13.4" />
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\Shared\Shared\Shared.csproj" />
</ItemGroup>
```

## Supabase 查询操作参考

### 基本 CRUD 操作
```csharp
// 查询所有
var all = await _supabaseClient.From<Entity>().Get();

// 按ID查询
var one = await _supabaseClient.From<Entity>().Where(x => x.Id == id).Single();

// 插入
var inserted = await _supabaseClient.From<Entity>().Insert(entity);

// 更新
var updated = await _supabaseClient.From<Entity>().Update(entity);

// 删除
await _supabaseClient.From<Entity>().Where(x => x.Id == id).Delete();
```

### 高级查询操作
```csharp
// 分页
var paged = await _supabaseClient
    .From<Entity>()
    .Range(0, 19) // 获取前20条
    .Get();

// 排序
var sorted = await _supabaseClient
    .From<Entity>()
    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
    .Get();

// 过滤
var filtered = await _supabaseClient
    .From<Entity>()
    .Where(x => x.IsActive && x.Rating >= 4)
    .Get();

// 模糊搜索
var searched = await _supabaseClient
    .From<Entity>()
    .Ilike(nameof(Entity.Name), "%search%")
    .Get();

// 或查询
var orResult = await _supabaseClient
    .From<Entity>()
    .Or(x => x.Status == "active")
    .Or(x => x.Status == "pending")
    .Get();

// 限制数量
var limited = await _supabaseClient
    .From<Entity>()
    .Limit(10)
    .Get();
```

## 注意事项

1. **不使用 DbContext/DbSet**: 完全避免 Entity Framework Core 的 DbContext 和 DbSet 模式
2. **模型继承**: 所有模型必须继承 `Postgrest.Models.BaseModel`
3. **主键标注**: 使用 `[PrimaryKey("id", false)]`,第二个参数 `false` 表示数据库生成 UUID
4. **无导航属性**: Supabase 不支持 EF Core 风格的导航属性,需要手动关联查询
5. **PostGIS 类型**: `Point` 类型在 Supabase 中使用 `string?` 存储,格式为 "POINT(longitude latitude)"
6. **JSONB 字段**: 使用 `string?` 存储 JSON 格式字符串
7. **数组字段**: PostgreSQL 数组类型可以直接映射到 C# 数组 (如 `string[]`)
8. **时间类型**: `TimeSpan?` 对应 PostgreSQL 的 `time` 类型
9. **验证注解**: 保留 `System.ComponentModel.DataAnnotations` 用于 DTO 验证

## 迁移检查清单

对于每个服务,确保完成以下步骤:

- [ ] 模型转换为 `BaseModel` 继承
- [ ] 移除所有导航属性
- [ ] 创建 Supabase 仓储类
- [ ] 更新 `Program.cs` 使用 `AddSupabase()`
- [ ] 注册仓储到 DI 容器
- [ ] 配置 `appsettings.json` 添加 Supabase 设置
- [ ] 创建 DTOs
- [ ] 创建 Services 业务逻辑层
- [ ] 创建 Controllers
- [ ] 添加必要的 NuGet 包引用
- [ ] 添加 Shared 项目引用
- [ ] 测试基本 CRUD 操作

## 下一步行动

1. **AccommodationService**: 转换酒店相关模型
2. **EventService**: 转换活动相关模型
3. **InnovationService**: 转换创新项目相关模型
4. **TravelPlanningService**: 转换旅行计划相关模型
5. **EcommerceService**: 转换电商相关模型
6. **DocumentService**: 转换文档相关模型
7. **CityService**: 转换城市相关模型
8. **Shared/SharedEntities**: 转换共享实体模型

每个服务转换后,需要创建相应的 DTOs、Services 和 Controllers 完成完整实现。

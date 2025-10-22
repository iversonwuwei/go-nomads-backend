# Supabase 模型批量转换脚本
# 将所有服务的模型从 EF Core 风格转换为 Supabase 风格

## 已完成转换

### ✅ CoworkingService
**文件**: `src/Services/CoworkingService/CoworkingService/Models/CoworkingSpace.cs`

**转换内容**:
- `CoworkingSpace` 继承自 `BaseModel`
- `CoworkingBooking` 继承自 `BaseModel`
- 移除导航属性
- 使用 Postgrest.Attributes

**仓储已创建**: `SupabaseCoworkingRepository.cs`

---

## 待转换服务列表

由于所有服务的模型转换模式完全一致,建议按以下步骤批量处理:

### 1. AccommodationService (酒店服务)

**模型文件**: `src/Services/AccommodationService/AccommodationService/Models/Hotel.cs`

**需要转换的实体**:
- `Hotel`
- `RoomType`  
- `HotelBooking`

**转换步骤**:
```powershell
# 1. 读取现有模型文件
# 2. 替换 using 语句
#    移除: using System.ComponentModel.DataAnnotations.Schema;
#    移除: using NetTopologySuite.Geometries;
#    添加: using Postgrest.Attributes;
#    添加: using Postgrest.Models;
# 3. 修改类声明添加 : BaseModel
# 4. 替换 [Key] 为 [PrimaryKey("id", false)]
# 5. 移除 [Column] 的 TypeName 参数
# 6. Point 类型改为 string?
# 7. 移除所有导航属性
```

**需要创建的文件**:
- `Repositories/SupabaseHotelRepository.cs`
- `Repositories/SupabaseRoomTypeRepository.cs`
- `Repositories/SupabaseHotelBookingRepository.cs`
- `DTOs/HotelDtos.cs`
- `Services/HotelService.cs`
- `Controllers/HotelsController.cs`

**Program.cs 更新**:
```csharp
builder.Services.AddSupabase(builder.Configuration);
builder.Services.AddScoped<SupabaseHotelRepository>();
builder.Services.AddScoped<SupabaseRoomTypeRepository>();
builder.Services.AddScoped<SupabaseHotelBookingRepository>();
```

---

### 2. EventService (活动服务)

**模型文件**: `src/Services/EventService/EventService/Models/Event.cs`

**需要转换的实体**:
- `Event`
- `EventParticipant`

**需要创建的文件**:
- `Repositories/SupabaseEventRepository.cs`
- `Repositories/SupabaseEventParticipantRepository.cs`
- `DTOs/EventDtos.cs`
- `Services/EventService.cs`
- `Controllers/EventsController.cs`

---

### 3. InnovationService (创新项目服务)

**模型文件**: `src/Services/InnovationService/InnovationService/Models/Innovation.cs`

**需要转换的实体**:
- `Innovation`
- `InnovationLike`
- `InnovationComment`

**需要创建的文件**:
- `Repositories/SupabaseInnovationRepository.cs`
- `Repositories/SupabaseInnovationLikeRepository.cs`
- `Repositories/SupabaseInnovationCommentRepository.cs`
- `DTOs/InnovationDtos.cs`
- `Services/InnovationService.cs`
- `Controllers/InnovationsController.cs`

---

### 4. TravelPlanningService (旅行规划服务)

**模型文件**: `src/Services/TravelPlanningService/TravelPlanningService/Models/TravelPlan.cs`

**需要转换的实体**:
- `TravelPlan`
- `TravelPlanCollaborator`

**需要创建的文件**:
- `Repositories/SupabaseTravelPlanRepository.cs`
- `Repositories/SupabaseTravelPlanCollaboratorRepository.cs`
- `DTOs/TravelPlanDtos.cs`
- `Services/TravelPlanService.cs`
- `Controllers/TravelPlansController.cs`

---

### 5. EcommerceService (电商服务)

**模型文件**: `src/Services/EcommerceService/EcommerceService/Models/Product.cs`

**需要转换的实体**:
- `Product`
- `CartItem`
- `Order`
- `OrderItem`

**需要创建的文件**:
- `Repositories/SupabaseProductRepository.cs`
- `Repositories/SupabaseCartItemRepository.cs`
- `Repositories/SupabaseOrderRepository.cs`
- `Repositories/SupabaseOrderItemRepository.cs`
- `DTOs/EcommerceDtos.cs`
- `Services/ProductService.cs`
- `Services/CartService.cs`
- `Services/OrderService.cs`
- `Controllers/ProductsController.cs`
- `Controllers/CartsController.cs`
- `Controllers/OrdersController.cs`

---

### 6. DocumentService (文档服务)

**模型文件**: `src/Services/DocumentService/DocumentService/Models/Document.cs`

**需要转换的实体**:
- `Document`

**需要创建的文件**:
- `Repositories/SupabaseDocumentRepository.cs`
- `DTOs/DocumentDtos.cs`
- `Services/DocumentService.cs`
- `Controllers/DocumentsController.cs`

---

### 7. CityService (城市服务)

**状态**: ⚠️ 已在初始实现中创建,但可能需要更新为 Supabase 模式

**模型文件**: 需要检查是否存在

**需要转换的实体**:
- `City`

---

### 8. Shared/Models/SharedEntities (共享实体)

**模型文件**: `src/Shared/Shared/Models/SharedEntities.cs` (如果存在)

**需要转换的实体**:
- `Review`
- `Favorite`
- `ChatMessage`
- `Notification`

这些是跨服务共享的实体,应放在 Shared 项目中。

---

## 批量转换工具建议

### PowerShell 批量替换脚本

```powershell
# 批量转换模型文件的 PowerShell 脚本
$services = @(
    "AccommodationService",
    "EventService",
    "InnovationService",
    "TravelPlanningService",
    "EcommerceService",
    "DocumentService"
)

foreach ($service in $services) {
    $modelPath = "src/Services/$service/$service/Models/*.cs"
    $files = Get-ChildItem -Path $modelPath
    
    foreach ($file in $files) {
        Write-Host "Converting $($file.Name)..."
        
        $content = Get-Content $file.FullName -Raw
        
        # 替换 using 语句
        $content = $content -replace "using System\.ComponentModel\.DataAnnotations\.Schema;", ""
        $content = $content -replace "using NetTopologySuite\.Geometries;", ""
        
        # 如果还没有,添加 Postgrest using
        if ($content -notmatch "using Postgrest\.Attributes;") {
            $content = $content -replace "(using System\.ComponentModel\.DataAnnotations;)", "`$1`nusing Postgrest.Attributes;`nusing Postgrest.Models;"
        }
        
        # 替换类声明
        $content = $content -replace "^(\s*public class \w+)", "`$1 : BaseModel" -replace " : BaseModel : BaseModel", " : BaseModel"
        
        # 替换 [Key] 为 [PrimaryKey]
        $content = $content -replace "\[Key\]", "[PrimaryKey(`"id`", false)]"
        
        # 移除 TypeName
        $content = $content -replace '\[Column\("(\w+)", TypeName = ".*?"\)\]', '[Column("$1")]'
        
        # 替换 Point 为 string
        $content = $content -replace "public Point\?", "public string?"
        
        # 移除导航属性 (简单匹配,可能需要手动检查)
        $content = $content -replace "public virtual .*?;", ""
        $content = $content -replace "\[ForeignKey.*?\]", ""
        
        # 保存文件
        Set-Content -Path $file.FullName -Value $content
        
        Write-Host "Converted $($file.Name) successfully!" -ForegroundColor Green
    }
}
```

---

## 手动转换检查清单

对每个服务的每个模型文件:

- [ ] 移除 `using System.ComponentModel.DataAnnotations.Schema;`
- [ ] 移除 `using NetTopologySuite.Geometries;` (如果有)
- [ ] 添加 `using Postgrest.Attributes;`
- [ ] 添加 `using Postgrest.Models;`
- [ ] 类声明添加 `: BaseModel`
- [ ] `[Key]` 改为 `[PrimaryKey("id", false)]`
- [ ] 移除 `[Column]` 的 `TypeName` 参数
- [ ] `Point?` 类型改为 `string?`
- [ ] 移除所有 `public virtual ICollection<...>` 导航属性
- [ ] 移除所有 `[ForeignKey(...)]` 特性
- [ ] 保留 `System.ComponentModel.DataAnnotations` 验证特性 (如 [Required], [MaxLength])

---

## 验证步骤

转换完成后,对每个服务:

1. **编译检查**: `dotnet build src/Services/{ServiceName}/{ServiceName}/{ServiceName}.csproj`
2. **依赖检查**: 确保引用了 `Supabase` NuGet 包和 `Shared` 项目
3. **运行检查**: 启动服务确保无运行时错误
4. **API 测试**: 使用 Swagger UI 测试基本 CRUD 操作

---

## 下一步行动计划

由于模型转换是重复性工作,建议:

1. **优先级 1**: 完成 AccommodationService 完整转换 (模型+仓储+DTOs+Services+Controllers) 作为第二个完整示例
2. **优先级 2**: 批量转换其他服务的模型
3. **优先级 3**: 为每个服务创建仓储层
4. **优先级 4**: 为每个服务创建 DTOs
5. **优先级 5**: 为每个服务创建 Services 和 Controllers
6. **优先级 6**: 更新所有 Program.cs
7. **优先级 7**: 添加集成测试

建议一次完成一个服务的完整实现,而不是分层批量处理,这样可以更快地获得可用的微服务。

# Go Nomads 下一步开发任务清单

## 📋 任务概览

本文档列出了 Go Nomads 项目的下一步开发任务,按优先级分类。

## ✅ 已完成

- [x] 数据库架构设计 (`database/schema.sql`)
- [x] 所有服务的实体模型 (Models)
- [x] 共享实体模型 (Review, Favorite, ChatMessage, Notification)
- [x] Docker Compose 配置
- [x] 项目文档(架构、部署、实现总结)

## 🔥 高优先级任务

### 1. DbContext 实现

为每个服务创建 `DbContext` 并配置实体关系。

#### 任务清单

- [ ] **CityService**: `CityDbContext` (已存在,可能需要更新)
  - [ ] 配置 PostGIS (NetTopologySuite)
  - [ ] 添加索引配置
  - [ ] 配置种子数据

- [ ] **CoworkingService**: `CoworkingDbContext`
  - [ ] 配置 CoworkingSpace 实体
  - [ ] 配置 CoworkingBooking 实体
  - [ ] 配置外键关系
  - [ ] 添加 PostGIS 支持

- [ ] **AccommodationService**: `AccommodationDbContext`
  - [ ] 配置 Hotel 实体
  - [ ] 配置 RoomType 实体
  - [ ] 配置 HotelBooking 实体
  - [ ] 配置一对多关系 (Hotel -> RoomTypes)

- [ ] **EventService**: `EventDbContext`
  - [ ] 配置 Event 实体
  - [ ] 配置 EventParticipant 实体
  - [ ] 配置外键关系

- [ ] **InnovationService**: `InnovationDbContext`
  - [ ] 配置 Innovation 实体
  - [ ] 配置 InnovationLike 实体
  - [ ] 配置 InnovationComment 实体
  - [ ] 配置自引用关系 (Comment -> ParentComment)

- [ ] **TravelPlanningService**: `TravelPlanningDbContext`
  - [ ] 配置 TravelPlan 实体
  - [ ] 配置 TravelPlanCollaborator 实体
  - [ ] 配置 JSONB 字段 (itinerary)

- [ ] **EcommerceService**: `EcommerceDbContext`
  - [ ] 配置 Product 实体
  - [ ] 配置 CartItem 实体
  - [ ] 配置 Order 实体
  - [ ] 配置 OrderItem 实体
  - [ ] 配置多对多关系

**示例代码** (CoworkingDbContext):

```csharp
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using CoworkingService.Models;

namespace CoworkingService.Data;

public class CoworkingDbContext : DbContext
{
    public CoworkingDbContext(DbContextOptions<CoworkingDbContext> options) 
        : base(options) { }

    public DbSet<CoworkingSpace> CoworkingSpaces => Set<CoworkingSpace>();
    public DbSet<CoworkingBooking> CoworkingBookings => Set<CoworkingBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 PostGIS
        modelBuilder.HasPostgresExtension("postgis");

        // CoworkingSpace 配置
        modelBuilder.Entity<CoworkingSpace>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("USD");
            entity.Property(e => e.Rating).HasPrecision(3, 2).HasDefaultValue(0.0m);
            
            // PostGIS Point
            entity.Property(e => e.Location).HasColumnType("geography(Point, 4326)");
            
            // 索引
            entity.HasIndex(e => e.CityId);
            entity.HasIndex(e => e.Rating);
            entity.HasIndex(e => e.IsActive);
        });

        // CoworkingBooking 配置
        modelBuilder.Entity<CoworkingBooking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalPrice).HasPrecision(10, 2);
            entity.Property(e => e.BookingType).HasMaxLength(20).HasDefaultValue("daily");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending");

            // 外键
            entity.HasOne(e => e.CoworkingSpace)
                  .WithMany(c => c.Bookings)
                  .HasForeignKey(e => e.CoworkingId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 索引
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BookingDate);
        });
    }
}
```

---

### 2. DTOs 实现

为每个服务创建数据传输对象。

#### 任务清单

每个服务需要以下 DTOs:

- [ ] **CoworkingService DTOs**
  - [ ] `CoworkingSpaceDto` (响应)
  - [ ] `CreateCoworkingSpaceDto` (创建)
  - [ ] `UpdateCoworkingSpaceDto` (更新)
  - [ ] `CoworkingSpaceSearchDto` (搜索)
  - [ ] `CoworkingBookingDto` (预订响应)
  - [ ] `CreateCoworkingBookingDto` (创建预订)

- [ ] **AccommodationService DTOs**
  - [ ] `HotelDto`, `CreateHotelDto`, `UpdateHotelDto`
  - [ ] `RoomTypeDto`, `CreateRoomTypeDto`, `UpdateRoomTypeDto`
  - [ ] `HotelBookingDto`, `CreateHotelBookingDto`
  - [ ] `HotelSearchDto`

- [ ] **EventService DTOs**
  - [ ] `EventDto`, `CreateEventDto`, `UpdateEventDto`
  - [ ] `EventParticipantDto`
  - [ ] `EventSearchDto`
  - [ ] `RegisterForEventDto`

- [ ] **InnovationService DTOs**
  - [ ] `InnovationDto`, `CreateInnovationDto`, `UpdateInnovationDto`
  - [ ] `InnovationCommentDto`, `CreateCommentDto`
  - [ ] `InnovationSearchDto`

- [ ] **TravelPlanningService DTOs**
  - [ ] `TravelPlanDto`, `CreateTravelPlanDto`, `UpdateTravelPlanDto`
  - [ ] `TravelPlanCollaboratorDto`, `AddCollaboratorDto`
  - [ ] `ItineraryDto`

- [ ] **EcommerceService DTOs**
  - [ ] `ProductDto`, `CreateProductDto`, `UpdateProductDto`
  - [ ] `CartItemDto`, `AddToCartDto`
  - [ ] `OrderDto`, `CreateOrderDto`
  - [ ] `ProductSearchDto`

**示例代码** (CoworkingService DTOs):

```csharp
namespace CoworkingService.DTOs;

public record CoworkingSpaceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? CityId { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public string[]? Images { get; init; }
    public decimal? PricePerDay { get; init; }
    public decimal? PricePerMonth { get; init; }
    public decimal? PricePerHour { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal Rating { get; init; }
    public int ReviewCount { get; init; }
    public decimal? WifiSpeed { get; init; }
    public bool HasMeetingRoom { get; init; }
    public bool HasCoffee { get; init; }
    public bool HasParking { get; init; }
    public bool Has247Access { get; init; }
    public string[]? Amenities { get; init; }
    public int? Capacity { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateCoworkingSpaceDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    public Guid? CityId { get; init; }

    [Required]
    public string Address { get; init; } = string.Empty;

    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public decimal? PricePerDay { get; init; }
    public decimal? PricePerMonth { get; init; }
    public decimal? PricePerHour { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal? WifiSpeed { get; init; }
    public bool HasMeetingRoom { get; init; }
    public bool HasCoffee { get; init; }
    public bool HasParking { get; init; }
    public bool Has247Access { get; init; }
    public string[]? Amenities { get; init; }
    public int? Capacity { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
}

public record CoworkingSpaceSearchDto
{
    public Guid? CityId { get; init; }
    public string? Keyword { get; init; }
    public decimal? MaxPricePerDay { get; init; }
    public decimal? MinRating { get; init; }
    public bool? HasMeetingRoom { get; init; }
    public bool? Has247Access { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public double? RadiusKm { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public record CreateCoworkingBookingDto
{
    [Required]
    public DateTime BookingDate { get; init; }

    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }

    [Required]
    public string BookingType { get; init; } = "daily"; // hourly, daily, monthly

    public string? SpecialRequests { get; init; }
}
```

---

### 3. Repositories 实现

为每个服务实现数据访问层。

#### 任务清单

- [ ] **CoworkingService**
  - [ ] `ICoworkingRepository` 接口
  - [ ] `CoworkingRepository` 实现
  - [ ] 方法: GetAll, GetById, Search, Create, Update, Delete, GetNearby

- [ ] **AccommodationService**
  - [ ] `IHotelRepository`, `HotelRepository`
  - [ ] `IRoomTypeRepository`, `RoomTypeRepository`
  - [ ] `IHotelBookingRepository`, `HotelBookingRepository`

- [ ] **EventService**
  - [ ] `IEventRepository`, `EventRepository`
  - [ ] `IEventParticipantRepository`, `EventParticipantRepository`

- [ ] **InnovationService**
  - [ ] `IInnovationRepository`, `InnovationRepository`
  - [ ] `IInnovationCommentRepository`, `InnovationCommentRepository`

- [ ] **TravelPlanningService**
  - [ ] `ITravelPlanRepository`, `TravelPlanRepository`

- [ ] **EcommerceService**
  - [ ] `IProductRepository`, `ProductRepository`
  - [ ] `IOrderRepository`, `OrderRepository`

**示例代码** (CoworkingRepository):

```csharp
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using CoworkingService.Data;
using CoworkingService.Models;

namespace CoworkingService.Repositories;

public interface ICoworkingRepository
{
    Task<IEnumerable<CoworkingSpace>> GetAllAsync(int page, int pageSize);
    Task<CoworkingSpace?> GetByIdAsync(Guid id);
    Task<IEnumerable<CoworkingSpace>> SearchAsync(
        Guid? cityId, 
        string? keyword, 
        decimal? maxPrice, 
        decimal? minRating,
        int page, 
        int pageSize
    );
    Task<IEnumerable<CoworkingSpace>> GetNearbyAsync(
        double latitude, 
        double longitude, 
        double radiusKm, 
        int page, 
        int pageSize
    );
    Task<CoworkingSpace> CreateAsync(CoworkingSpace coworkingSpace);
    Task<CoworkingSpace> UpdateAsync(CoworkingSpace coworkingSpace);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync();
}

public class CoworkingRepository : ICoworkingRepository
{
    private readonly CoworkingDbContext _context;
    private readonly ILogger<CoworkingRepository> _logger;

    public CoworkingRepository(
        CoworkingDbContext context,
        ILogger<CoworkingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CoworkingSpace>> GetAllAsync(int page, int pageSize)
    {
        return await _context.CoworkingSpaces
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CoworkingSpace?> GetByIdAsync(Guid id)
    {
        return await _context.CoworkingSpaces
            .Include(c => c.Bookings)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
    }

    public async Task<IEnumerable<CoworkingSpace>> SearchAsync(
        Guid? cityId,
        string? keyword,
        decimal? maxPrice,
        decimal? minRating,
        int page,
        int pageSize)
    {
        var query = _context.CoworkingSpaces.Where(c => c.IsActive);

        if (cityId.HasValue)
            query = query.Where(c => c.CityId == cityId.Value);

        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(c => c.Name.Contains(keyword) || c.Description!.Contains(keyword));

        if (maxPrice.HasValue)
            query = query.Where(c => c.PricePerDay <= maxPrice.Value);

        if (minRating.HasValue)
            query = query.Where(c => c.Rating >= minRating.Value);

        return await query
            .OrderByDescending(c => c.Rating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<CoworkingSpace>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKm,
        int page,
        int pageSize)
    {
        var point = new Point(longitude, latitude) { SRID = 4326 };
        var radiusMeters = radiusKm * 1000;

        return await _context.CoworkingSpaces
            .Where(c => c.IsActive && c.Location != null &&
                        c.Location.Distance(point) <= radiusMeters)
            .OrderBy(c => c.Location!.Distance(point))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<CoworkingSpace> CreateAsync(CoworkingSpace coworkingSpace)
    {
        // 设置 PostGIS Point
        if (coworkingSpace.Latitude.HasValue && coworkingSpace.Longitude.HasValue)
        {
            coworkingSpace.Location = new Point(
                (double)coworkingSpace.Longitude.Value,
                (double)coworkingSpace.Latitude.Value
            ) { SRID = 4326 };
        }

        _context.CoworkingSpaces.Add(coworkingSpace);
        await _context.SaveChangesAsync();
        return coworkingSpace;
    }

    public async Task<CoworkingSpace> UpdateAsync(CoworkingSpace coworkingSpace)
    {
        coworkingSpace.UpdatedAt = DateTime.UtcNow;

        // 更新 PostGIS Point
        if (coworkingSpace.Latitude.HasValue && coworkingSpace.Longitude.HasValue)
        {
            coworkingSpace.Location = new Point(
                (double)coworkingSpace.Longitude.Value,
                (double)coworkingSpace.Latitude.Value
            ) { SRID = 4326 };
        }

        _context.CoworkingSpaces.Update(coworkingSpace);
        await _context.SaveChangesAsync();
        return coworkingSpace;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var coworkingSpace = await _context.CoworkingSpaces.FindAsync(id);
        if (coworkingSpace == null) return false;

        // 软删除
        coworkingSpace.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.CoworkingSpaces.CountAsync(c => c.IsActive);
    }
}
```

---

### 4. Services 实现

实现业务逻辑层。

#### 任务清单

每个服务需要:

- [ ] Service 接口 (e.g., `ICoworkingService`)
- [ ] Service 实现 (e.g., `CoworkingService`)
- [ ] AutoMapper 配置 (DTO 映射)
- [ ] 业务验证
- [ ] 缓存逻辑 (Redis)
- [ ] 异常处理

**示例代码** (CoworkingService):

```csharp
using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CoworkingService.DTOs;
using CoworkingService.Models;
using CoworkingService.Repositories;

namespace CoworkingService.Services;

public interface ICoworkingService
{
    Task<PaginatedResponse<CoworkingSpaceDto>> GetAllAsync(int page, int pageSize);
    Task<CoworkingSpaceDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<CoworkingSpaceDto>> SearchAsync(CoworkingSpaceSearchDto searchDto);
    Task<PaginatedResponse<CoworkingSpaceDto>> GetNearbyAsync(
        double latitude, double longitude, double radiusKm, int page, int pageSize);
    Task<CoworkingSpaceDto> CreateAsync(CreateCoworkingSpaceDto dto, Guid userId);
    Task<CoworkingSpaceDto> UpdateAsync(Guid id, UpdateCoworkingSpaceDto dto, Guid userId);
    Task<bool> DeleteAsync(Guid id);
}

public class CoworkingService : ICoworkingService
{
    private readonly ICoworkingRepository _repository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CoworkingService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public CoworkingService(
        ICoworkingRepository repository,
        IMapper mapper,
        IDistributedCache cache,
        ILogger<CoworkingService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PaginatedResponse<CoworkingSpaceDto>> GetAllAsync(int page, int pageSize)
    {
        var cacheKey = $"coworking:all:{page}:{pageSize}";
        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (cachedData != null)
        {
            return JsonSerializer.Deserialize<PaginatedResponse<CoworkingSpaceDto>>(cachedData)!;
        }

        var spaces = await _repository.GetAllAsync(page, pageSize);
        var totalCount = await _repository.GetTotalCountAsync();

        var result = new PaginatedResponse<CoworkingSpaceDto>
        {
            Items = _mapper.Map<List<CoworkingSpaceDto>>(spaces),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheExpiration }
        );

        return result;
    }

    public async Task<CoworkingSpaceDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"coworking:{id}";
        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (cachedData != null)
        {
            return JsonSerializer.Deserialize<CoworkingSpaceDto>(cachedData);
        }

        var space = await _repository.GetByIdAsync(id);
        if (space == null) return null;

        var result = _mapper.Map<CoworkingSpaceDto>(space);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheExpiration }
        );

        return result;
    }

    public async Task<CoworkingSpaceDto> CreateAsync(CreateCoworkingSpaceDto dto, Guid userId)
    {
        var space = _mapper.Map<CoworkingSpace>(dto);
        space.CreatedBy = userId;
        space.UpdatedBy = userId;

        var created = await _repository.CreateAsync(space);

        // 清除缓存
        await _cache.RemoveAsync("coworking:all:*");

        return _mapper.Map<CoworkingSpaceDto>(created);
    }

    // ... 其他方法实现
}
```

---

### 5. Controllers 实现

实现 RESTful API 控制器。

#### 任务清单

- [ ] 为每个服务实现 Controller
- [ ] JWT 身份验证
- [ ] 请求验证
- [ ] Swagger 注释
- [ ] 异常处理中间件
- [ ] CORS 配置

**示例代码** (CoworkingController):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoworkingService.DTOs;
using CoworkingService.Services;

namespace CoworkingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoworkingController : ControllerBase
{
    private readonly ICoworkingService _service;
    private readonly ILogger<CoworkingController> _logger;

    public CoworkingController(
        ICoworkingService service,
        ILogger<CoworkingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有共享办公空间(分页)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CoworkingSpaceDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<CoworkingSpaceDto>>
        {
            Success = true,
            Data = result
        });
    }

    /// <summary>
    /// 获取共享办公空间详情
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CoworkingSpaceDto>>> GetById(Guid id)
    {
        var space = await _service.GetByIdAsync(id);
        if (space == null)
        {
            return NotFound(new ApiResponse<CoworkingSpaceDto>
            {
                Success = false,
                Message = "Coworking space not found"
            });
        }

        return Ok(new ApiResponse<CoworkingSpaceDto>
        {
            Success = true,
            Data = space
        });
    }

    /// <summary>
    /// 搜索共享办公空间
    /// </summary>
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CoworkingSpaceDto>>>> Search(
        [FromBody] CoworkingSpaceSearchDto searchDto)
    {
        var result = await _service.SearchAsync(searchDto);
        return Ok(new ApiResponse<PaginatedResponse<CoworkingSpaceDto>>
        {
            Success = true,
            Data = result
        });
    }

    /// <summary>
    /// 创建共享办公空间
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<CoworkingSpaceDto>>> Create(
        [FromBody] CreateCoworkingSpaceDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        var created = await _service.CreateAsync(dto, userId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            new ApiResponse<CoworkingSpaceDto>
            {
                Success = true,
                Data = created
            }
        );
    }

    // ... 其他 CRUD 操作
}
```

---

## 🌟 中优先级任务

### 6. Program.cs 配置

为每个服务配置依赖注入、中间件、Swagger 等。

- [ ] 配置 DbContext 和连接字符串
- [ ] 配置 JWT 认证
- [ ] 配置 Redis 缓存
- [ ] 配置 AutoMapper
- [ ] 配置 Serilog
- [ ] 配置 Swagger
- [ ] 配置 CORS

### 7. AutoMapper 配置

创建 DTO 和实体之间的映射配置。

- [ ] 每个服务创建 `MappingProfile.cs`
- [ ] 配置实体 -> DTO 映射
- [ ] 配置 CreateDto -> 实体映射
- [ ] 配置 UpdateDto -> 实体映射

### 8. Dockerfile 和部署配置

- [ ] 为每个服务创建 Dockerfile
- [ ] 更新 docker-compose.yml
- [ ] 创建 .dockerignore 文件
- [ ] 配置健康检查
- [ ] 配置环境变量

### 9. EF Core Migrations

- [ ] 为每个服务创建初始迁移
- [ ] 或者选择直接使用 schema.sql

```powershell
# 选项 1: 创建 EF Core Migrations
cd src/Services/CoworkingService/CoworkingService
dotnet ef migrations add InitialCreate
dotnet ef database update

# 选项 2: 直接使用 schema.sql (推荐)
# 在 Supabase Dashboard 执行 database/schema.sql
```

---

## 🔧 低优先级任务

### 10. 通用功能

- [ ] 实现 Review 服务
- [ ] 实现 Favorite 服务
- [ ] 实现 Notification 服务
- [ ] 实现 ChatMessage 服务

### 11. API Gateway

- [ ] 配置 Ocelot 路由
- [ ] 配置负载均衡
- [ ] 配置限流
- [ ] 配置熔断器

### 12. 服务通信与基础设施集成

- [ ] 配置服务发现
- [ ] 配置消息总线
- [ ] 配置缓存层
- [ ] 配置 Secrets

### 13. 测试

- [ ] 单元测试
- [ ] 集成测试
- [ ] API 测试
- [ ] 负载测试

---

## 📝 开发建议

### 优先实现顺序

1. **CityService** (参考已有实现)
2. **CoworkingService** (参考上面示例代码)
3. **AccommodationService**
4. **EventService**
5. **InnovationService**
6. **TravelPlanningService**
7. **EcommerceService**

### 开发步骤(每个服务)

1. 创建 DbContext
2. 创建 DTOs
3. 创建 Repository
4. 创建 Service
5. 创建 Controller
6. 配置 Program.cs
7. 测试 API

### 代码复用

可以从 CityService 复制以下文件作为模板:

- `Program.cs`
- `appsettings.json`
- `Dockerfile`
- MappingProfile (如果存在)

然后根据具体服务的需求进行调整。

---

## 🎯 完成标准

每个服务应该具备:

- ✅ 完整的 CRUD 操作
- ✅ 搜索和筛选功能
- ✅ 分页支持
- ✅ JWT 认证
- ✅ Redis 缓存
- ✅ Swagger 文档
- ✅ 异常处理
- ✅ 日志记录
- ✅ Docker 部署

---

**预计完成时间**: 根据开发速度,每个服务约需 2-3 天完成所有层的实现。

**最后更新**: 2025-10-22

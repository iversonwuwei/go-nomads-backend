# EventService 三层架构 + DDD 设计文档

## 📐 架构概览

EventService 采用 **三层架构 + DDD（领域驱动设计）** 的方式重构，实现了清晰的层次分离和领域逻辑封装。

```
┌─────────────────────────────────────────────────────┐
│                  API Layer (表现层)                   │
│              EventsController.cs                    │
│         (HTTP API, 请求响应处理)                      │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│             Application Layer (应用层)               │
│                                                      │
│  ┌────────────────────────────────────────────┐    │
│  │   IEventService / EventApplicationService │    │
│  │     (应用服务接口和实现)                    │    │
│  └────────────────────────────────────────────┘    │
│                                                      │
│  ┌────────────────────────────────────────────┐    │
│  │              DTOs (数据传输对象)             │    │
│  │  CreateEventRequest, EventResponse, etc.   │    │
│  └────────────────────────────────────────────┘    │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│              Domain Layer (领域层)                    │
│                                                      │
│  ┌────────────────────────────────────────────┐    │
│  │         Entities (领域实体)                  │    │
│  │  Event, EventParticipant, EventFollower    │    │
│  │    (工厂方法, 领域逻辑封装)                  │    │
│  └────────────────────────────────────────────┘    │
│                                                      │
│  ┌────────────────────────────────────────────┐    │
│  │      Repository Interfaces (仓储接口)       │    │
│  │  IEventRepository, IEventParticipantRepo   │    │
│  └────────────────────────────────────────────┘    │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│          Infrastructure Layer (基础设施层)            │
│                                                      │
│  ┌────────────────────────────────────────────┐    │
│  │      Repository Implementations              │    │
│  │  EventRepository, EventParticipantRepo      │    │
│  │         (Supabase 数据访问实现)              │    │
│  └────────────────────────────────────────────┘    │
│                                                      │
│  ┌────────────────────────────────────────────┐    │
│  │         External Services                    │    │
│  │ Supabase Client, Internal Service Clients   │    │
│  └────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

---

## 🏗️ 分层详解

### 1. **Domain Layer (领域层)** ✨核心

**位置**: `Domain/`

**职责**: 
- 封装业务领域的核心逻辑和规则
- 定义领域实体和值对象
- 定义仓储接口（不实现）
- **独立于任何基础设施和框架**

#### 1.1 Entities (领域实体)

##### **Event.cs** - Event 聚合根
```csharp
public class Event : BaseModel
{
    // 私有 setter - 封装内部状态
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    
    // 公共无参构造函数 (ORM 需要)
    public Event() { }
    
    // 工厂方法 - 创建实体的唯一入口
    public static Event Create(...)
    {
        // 业务规则验证
        // 初始化默认值
        return new Event { ... };
    }
    
    // 领域方法 - 封装业务逻辑
    public void Update(Guid userId, ...)
    {
        // 权限验证
        if (OrganizerId != userId)
            throw new UnauthorizedAccessException(...);
        
        // 更新逻辑
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AddParticipant()
    {
        // 业务规则：检查人数限制
        if (MaxParticipants.HasValue && CurrentParticipants >= MaxParticipants.Value)
            throw new InvalidOperationException("Event 已满员");
        
        CurrentParticipants++;
    }
    
    public bool CanJoin()
    {
        // 领域查询逻辑
        return Status == "upcoming" && 
               (!MaxParticipants.HasValue || CurrentParticipants < MaxParticipants.Value);
    }
}
```

**DDD 特点**:
- ✅ 私有 setter 保护内部状态
- ✅ 工厂方法控制对象创建
- ✅ 领域方法封装业务规则
- ✅ 自包含验证逻辑

##### **EventParticipant.cs** - 参与者实体
```csharp
public class EventParticipant : BaseModel
{
    public static EventParticipant Create(Guid eventId, Guid userId, ...)
    {
        return new EventParticipant { ... };
    }
    
    public void UpdateStatus(string status) { ... }
}
```

##### **EventFollower.cs** - 关注者实体
```csharp
public class EventFollower : BaseModel
{
    public static EventFollower Create(Guid eventId, Guid userId, ...)
    {
        return new EventFollower { ... };
    }
    
    public void UpdateNotificationSetting(bool enabled) { ... }
}
```

#### 1.2 Repository Interfaces (仓储接口)

##### **IEventRepository.cs**
```csharp
public interface IEventRepository
{
    Task<Event> CreateAsync(Event @event);
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event> UpdateAsync(Event @event);
    Task DeleteAsync(Guid id);
    Task<(List<Event> Events, int Total)> GetListAsync(...);
    Task<List<Event>> GetByOrganizerIdAsync(Guid organizerId);
    Task<bool> ExistsAsync(Guid id);
}
```

**设计原则**:
- ✅ 面向接口编程（依赖倒置）
- ✅ 只操作聚合根
- ✅ 返回领域实体，不返回 DTO

---

### 2. **Application Layer (应用层)**

**位置**: `Application/`

**职责**:
- 协调领域对象完成业务用例
- 处理应用级的事务和安全
- DTO 转换（领域对象 ↔ DTO）
- 不包含业务逻辑（委托给领域层）

#### 2.1 Services (应用服务)

##### **IEventService.cs** - 应用服务接口
```csharp
public interface IEventService
{
    Task<EventResponse> CreateEventAsync(CreateEventRequest request);
    Task<EventResponse> GetEventAsync(Guid id, Guid? userId = null);
    Task<EventResponse> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid userId);
    Task<ParticipantResponse> JoinEventAsync(Guid eventId, JoinEventRequest request);
    // ... 更多用例
}
```

##### **EventApplicationService.cs** - 应用服务实现
```csharp
public class EventApplicationService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipantRepository _participantRepository;
    private readonly IEventFollowerRepository _followerRepository;
    
    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request)
    {
        // 1. 使用领域工厂方法创建实体
        var @event = Event.Create(
            title: request.Title,
            organizerId: request.OrganizerId,
            ...
        );
        
        // 2. 通过仓储持久化
        var createdEvent = await _eventRepository.CreateAsync(@event);
        
        // 3. 转换为 DTO 返回
        return MapToResponse(createdEvent);
    }
    
    public async Task<ParticipantResponse> JoinEventAsync(Guid eventId, JoinEventRequest request)
    {
        // 1. 获取聚合根
        var @event = await _eventRepository.GetByIdAsync(eventId);
        
        // 2. 业务规则验证
        if (!@event.CanJoin())
            throw new InvalidOperationException(...);
        
        // 3. 创建参与者实体
        var participant = EventParticipant.Create(eventId, request.UserId, ...);
        await _participantRepository.CreateAsync(participant);
        
        // 4. 调用领域方法更新状态
        @event.AddParticipant();
        await _eventRepository.UpdateAsync(@event);
        
        return MapToParticipantResponse(participant);
    }
}
```

**关键点**:
- ✅ 协调多个仓储和领域对象
- ✅ 不包含业务逻辑（委托给领域实体）
- ✅ 负责事务边界
- ✅ DTO 映射

#### 2.2 DTOs (数据传输对象)

```csharp
// Request DTOs
public class CreateEventRequest { ... }
public class UpdateEventRequest { ... }
public class JoinEventRequest { ... }

// Response DTOs
public class EventResponse { ... }
public class ParticipantResponse { ... }
public class FollowerResponse { ... }
```

**用途**:
- 前端与后端数据交换
- 避免暴露领域实体
- 数据验证（Data Annotations）

---

### 3. **Infrastructure Layer (基础设施层)**

**位置**: `Infrastructure/`

**职责**:
- 实现领域层定义的仓储接口
- 数据库访问（Supabase）
- 外部服务集成（内部 HTTP / 服务客户端）
- ORM 映射配置

#### 3.1 Repository Implementations

##### **EventRepository.cs**
```csharp
public class EventRepository : IEventRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<EventRepository> _logger;
    
    public async Task<Event> CreateAsync(Event @event)
    {
        var result = await _supabaseClient
            .From<Event>()
            .Insert(@event);
            
        return result.Models.FirstOrDefault();
    }
    
    public async Task<(List<Event> Events, int Total)> GetListAsync(...)
    {
        var query = _supabaseClient.From<Event>();
        
        // 应用筛选条件
        if (cityId.HasValue)
            query = query.Where(e => e.CityId == cityId.Value);
        
        var result = await query.Get();
        return (result.Models.ToList(), result.Models.Count);
    }
}
```

**特点**:
- ✅ 实现领域定义的接口
- ✅ 处理数据库交互细节
- ✅ 日志记录
- ✅ 异常处理

---

### 4. **API Layer (表现层)**

**位置**: `API/Controllers/`

**职责**:
- HTTP 请求/响应处理
- 路由定义
- 参数验证
- 异常转换为 HTTP 状态码

##### **EventsController.cs**
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            var response = await _eventService.CreateEventAsync(request);
            return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinEvent(Guid id, [FromBody] JoinEventRequest request)
    {
        try
        {
            var response = await _eventService.JoinEventAsync(id, request);
            return Ok(new { success = true, participant = response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

**特点**:
- ✅ 薄控制器（Thin Controller）
- ✅ 只负责 HTTP 交互
- ✅ 委托业务逻辑给应用服务
- ✅ 标准 RESTful API

---

## 🔧 依赖注入配置

**Program.cs**:
```csharp
// Infrastructure Layer - 仓储实现
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventParticipantRepository, EventParticipantRepository>();
builder.Services.AddScoped<IEventFollowerRepository, EventFollowerRepository>();

// Application Layer - 应用服务
builder.Services.AddScoped<IEventService, EventApplicationService>();

// Domain Layer 不需要注册（纯 POCO）
```

**依赖方向**:
```
API → Application → Domain ← Infrastructure
```

**关键原则**:
- ✅ **依赖倒置**: Infrastructure 依赖 Domain 的接口
- ✅ **单一职责**: 每层只关注自己的职责
- ✅ **开闭原则**: 扩展仓储实现无需修改领域层

---

## 📂 目录结构

```
EventService/
├── API/
│   └── Controllers/
│       └── EventsController.cs        # API 控制器
│
├── Application/
│   ├── Services/
│   │   ├── IEventService.cs           # 应用服务接口
│   │   └── EventApplicationService.cs # 应用服务实现
│   └── DTOs/
│       └── EventDTOs.cs               # 数据传输对象
│
├── Domain/
│   ├── Entities/
│   │   ├── Event.cs                   # Event 聚合根
│   │   ├── EventParticipant.cs        # 参与者实体
│   │   └── EventFollower.cs           # 关注者实体
│   └── Repositories/
│       ├── IEventRepository.cs        # Event 仓储接口
│       ├── IEventParticipantRepository.cs
│       └── IEventFollowerRepository.cs
│
├── Infrastructure/
│   └── Repositories/
│       ├── EventRepository.cs         # Supabase 实现
│       ├── EventParticipantRepository.cs
│       └── EventFollowerRepository.cs
│
├── Database/
│   └── create-event-followers-table.sql  # 数据库迁移
│
└── Program.cs                         # 启动配置
```

---

## 🎯 DDD 核心概念应用

### 1. **聚合根 (Aggregate Root)**
- **Event** 是聚合根
- 控制 EventParticipant 和 EventFollower 的生命周期
- 外部只能通过 Event 访问内部实体

### 2. **实体 (Entity)**
- Event, EventParticipant, EventFollower
- 有唯一标识（ID）
- 生命周期独立

### 3. **值对象 (Value Object)**
- Currency, Status, LocationType
- 可以扩展为独立的值对象类

### 4. **仓储 (Repository)**
- 只为聚合根提供仓储
- 抽象数据访问细节
- 提供集合式接口

### 5. **领域服务 (Domain Service)**
- 复杂的跨聚合业务逻辑
- 当前在应用服务中实现（可优化）

### 6. **工厂 (Factory)**
- `Event.Create()` - 工厂方法
- 封装复杂的对象创建逻辑

---

## ✅ 架构优势

### 1. **可测试性**
```csharp
// 单元测试领域逻辑
[Fact]
public void Event_AddParticipant_Should_Increase_Count()
{
    var @event = Event.Create(...);
    @event.AddParticipant();
    Assert.Equal(1, @event.CurrentParticipants);
}

// 应用服务测试（Mock 仓储）
var mockRepo = new Mock<IEventRepository>();
var service = new EventApplicationService(mockRepo.Object, ...);
```

### 2. **可维护性**
- 每层职责清晰
- 领域逻辑集中在 Domain 层
- 更换数据库只需修改 Infrastructure 层

### 3. **可扩展性**
- 添加新功能：扩展领域实体和应用服务
- 更换框架：只需修改 API 层
- 多数据源：实现新的仓储

### 4. **业务规则集中**
- 所有业务规则在 Domain 层
- 避免逻辑散落在多处
- 易于理解和维护

---

## 🚀 部署与运行

```bash
# 编译
cd src/Services/EventService/EventService
dotnet build

# 部署
cd deployment
./deploy-services-local.sh

# 访问
Scalar API: http://localhost:8005/scalar/v1
健康检查: http://localhost:8005/health
```

---

## 📚 参考资料

- **DDD**: Eric Evans - "Domain-Driven Design"
- **三层架构**: Microsoft - "Architecting Modern Web Applications"
- **Clean Architecture**: Robert C. Martin - "Clean Architecture"

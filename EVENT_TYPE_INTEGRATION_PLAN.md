# Event Type 完整集成架构方案

## 问题分析

### 当前状态
1. **后端 Event 表**：`category` 字段存储 **字符串**（如 "coffee-chat", "networking"）
2. **Flutter Meetup**：使用 `MeetupType` 值对象（字符串枚举）
3. **EventType 表**：已创建，包含 20 种预设类型，但与 Event 表没有关联

### 期望架构
1. **后端 Event 表**：`event_type_id` 字段存储 **UUID**（外键关联 event_types 表）
2. **后端 EventResponse DTO**：包含完整的 `EventTypeInfo` 对象
3. **Flutter Meetup**：使用 `EventType` 领域实体（包含 id, name, enName, description 等）

---

## 实现方案

### 阶段 1：数据库迁移（后端）

#### 1.1 修改 events 表结构
```sql
-- 1. 添加新字段 event_type_id (UUID)
ALTER TABLE events ADD COLUMN event_type_id UUID;

-- 2. 添加外键约束
ALTER TABLE events 
ADD CONSTRAINT fk_events_event_type 
FOREIGN KEY (event_type_id) REFERENCES event_types(id);

-- 3. 创建索引
CREATE INDEX idx_events_event_type_id ON events(event_type_id);

-- 4. 数据迁移：将现有 category 字符串映射到 event_type_id
-- （需要根据实际数据编写迁移脚本）
UPDATE events 
SET event_type_id = (
  SELECT id FROM event_types 
  WHERE en_name = events.category 
  LIMIT 1
)
WHERE category IS NOT NULL;

-- 5. （可选）保留 category 作为兼容字段，或直接删除
-- ALTER TABLE events DROP COLUMN category;
```

### 阶段 2：后端代码修改

#### 2.1 修改 Event 实体
**文件**：`EventService.Domain.Entities.Event.cs`

```csharp
[Column("event_type_id")] 
public Guid? EventTypeId { get; set; }

// 保留 Category 作为兼容字段（可选）
[MaxLength(50)] 
[Column("category")] 
public string? Category { get; set; }
```

#### 2.2 修改 EventResponse DTO
**文件**：`EventService.Application.DTOs.EventDTOs.cs`

```csharp
public class EventResponse
{
    // ... 现有字段 ...
    
    /// <summary>
    /// 活动类型（从 event_types 表关联查询）
    /// </summary>
    [JsonInclude]
    public EventTypeInfo? EventType { get; set; }
    
    // 保留 Category 作为兼容字段
    [Obsolete("Use EventType instead")]
    public string? Category { get; set; }
}

/// <summary>
/// 活动类型信息
/// </summary>
public class EventTypeInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EnName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}
```

#### 2.3 修改 EventRepository 查询逻辑
**文件**：`EventService.Infrastructure.Repositories.EventRepository.cs`

```csharp
public async Task<Event?> GetByIdAsync(Guid id)
{
    var response = await _supabaseClient
        .From<Event>()
        .Select("*, event_types(*)")  // 关联查询 event_types
        .Filter("id", Operator.Equals, id.ToString())
        .Single();
    
    return response;
}
```

#### 2.4 修改 EventApplicationService
**文件**：`EventService.Application.Services.EventApplicationService.cs`

添加 EventType 到 EventResponse 的映射逻辑：

```csharp
private async Task<EventResponse> MapToResponseAsync(Event @event)
{
    var response = new EventResponse
    {
        // ... 现有映射 ...
    };
    
    // 查询 EventType
    if (@event.EventTypeId.HasValue)
    {
        var eventType = await _eventTypeRepository.GetByIdAsync(@event.EventTypeId.Value);
        if (eventType != null)
        {
            response.EventType = new EventTypeInfo
            {
                Id = eventType.Id,
                Name = eventType.Name,
                EnName = eventType.EnName,
                Description = eventType.Description,
                Icon = eventType.Icon,
                SortOrder = eventType.SortOrder
            };
        }
    }
    
    return response;
}
```

#### 2.5 修改 CreateEventRequest
**文件**：`EventService.Application.DTOs.EventDTOs.cs`

```csharp
public class CreateEventRequest
{
    // 替换 Category
    [Required(ErrorMessage = "活动类型不能为空")]
    public Guid EventTypeId { get; set; }
    
    // ... 其他字段不变 ...
}
```

---

### 阶段 3：Flutter 代码修改

#### 3.1 修改 Meetup 实体
**文件**：`lib/features/meetup/domain/entities/meetup.dart`

```dart
class Meetup {
  final String id;
  final String title;
  final EventType eventType;  // 改为 EventType 实体（不是 MeetupType）
  final String description;
  // ... 其他字段不变 ...
  
  Meetup({
    required this.id,
    required this.title,
    required this.eventType,
    // ...
  });
}
```

#### 3.2 创建/更新 EventType 实体（如果不存在）
**文件**：`lib/features/meetup/domain/entities/event_type.dart`

```dart
class EventType {
  final String id;
  final String name;        // 中文名
  final String enName;      // 英文名
  final String? description;
  final String? icon;
  final int sortOrder;
  
  EventType({
    required this.id,
    required this.name,
    required this.enName,
    this.description,
    this.icon,
    required this.sortOrder,
  });
  
  /// 根据语言环境获取显示名称
  String getDisplayName(String locale) {
    return locale.startsWith('zh') ? name : enName;
  }
}
```

#### 3.3 修改 MeetupDto
**文件**：`lib/features/meetup/infrastructure/models/meetup_dto.dart`

```dart
class MeetupDto {
  final String id;
  final String title;
  final EventTypeDto? eventType;  // 改为嵌套对象
  // ...
  
  factory MeetupDto.fromJson(Map<String, dynamic> json) {
    return MeetupDto(
      id: json['id'] as String,
      title: json['title'] as String,
      eventType: json['eventType'] != null 
          ? EventTypeDto.fromJson(json['eventType'] as Map<String, dynamic>)
          : null,
      // ...
    );
  }
  
  Meetup toDomain() {
    return Meetup(
      id: id,
      title: title,
      eventType: eventType?.toDomain() ?? _getDefaultEventType(),
      // ...
    );
  }
  
  // 兜底方案
  EventType _getDefaultEventType() {
    return EventType(
      id: '',
      name: '其他',
      enName: 'other',
      sortOrder: 999,
    );
  }
}
```

#### 3.4 修改 CreateMeetupPage
**文件**：`lib/features/meetup/presentation/pages/create_meetup_page.dart`

提交表单时，发送 `eventTypeId` 而不是 `category` 字符串：

```dart
// 提交按钮
final request = {
  'title': _titleController.text,
  'eventTypeId': controller.selectedType.value?.id,  // 发送 UUID
  // ... 其他字段 ...
};
```

---

## 迁移策略

### 向后兼容方案（推荐）

1. **数据库**：
   - 保留 `category` 字段（标记为 deprecated）
   - 新增 `event_type_id` 字段
   - 两个字段同时存在，逐步迁移

2. **后端 API**：
   - `CreateEventRequest` 同时接受 `category` (string) 和 `eventTypeId` (Guid)
   - 优先使用 `eventTypeId`，如果为空则使用 `category` 查找对应的 EventType
   - `EventResponse` 同时返回 `category` 和 `EventType` 对象

3. **Flutter**：
   - 新版本使用 `EventType` 对象
   - 如果后端返回的 `EventType` 为 null，回退到 `category` 字符串解析

### 完全迁移方案（长期目标）

1. 完成所有代码迁移后，删除 `category` 字段
2. `event_type_id` 设为 NOT NULL
3. 移除所有兼容代码

---

## 实施步骤

### Step 1：后端数据库迁移
1. 执行 SQL 脚本添加 `event_type_id` 字段
2. 编写数据迁移脚本（category → event_type_id）
3. 验证数据完整性

### Step 2：后端代码修改
1. 修改 Event 实体
2. 修改 DTOs（EventResponse, CreateEventRequest, UpdateEventRequest）
3. 修改 Repository 查询逻辑（JOIN event_types）
4. 修改 Application Service 映射逻辑
5. 测试 API 接口

### Step 3：Gateway 路由确认
1. 确认 `/api/v1/events` 路由配置
2. 确认返回的 EventType 数据格式

### Step 4：Flutter 代码修改
1. 修改 Meetup 实体
2. 修改 MeetupDto 解析逻辑
3. 修改 CreateMeetupPage 提交逻辑
4. 更新 UI 显示（使用 eventType.getDisplayName()）
5. 测试完整流程

### Step 5：清理工作
1. 删除旧的 `MeetupType` 枚举（如果不再使用）
2. 移除兼容代码
3. 更新文档

---

## 当前状态

- ✅ EventType 表已创建（20 种类型）
- ✅ EventType API 已实现（GET /api/v1/event-types）
- ✅ Flutter EventType 实体已创建
- ✅ Flutter EventTypeController 已实现
- ✅ CreateMeetupPage 已集成 EventTypeController
- ⚠️ **缺失**：后端 Event 表与 EventType 表的关联
- ⚠️ **缺失**：后端 EventResponse 返回 EventType 对象
- ⚠️ **缺失**：Flutter Meetup 实体使用 EventType（当前仍是 MeetupType 枚举）

---

## 下一步行动

**建议按以下顺序执行：**

1. **确认需求**：是否要进行完整的架构调整？
2. **数据库迁移**：创建 SQL 迁移脚本
3. **后端修改**：修改 Event 实体和 DTOs
4. **API 测试**：验证后端返回正确的 EventType 对象
5. **Flutter 修改**：调整 Meetup 实体和 DTO 解析
6. **端到端测试**：创建活动 → 提交 → 列表显示

---

## 风险评估

### 高风险
- 数据库迁移可能影响现有数据
- 需要兼容旧版本客户端

### 中风险
- 后端 JOIN 查询可能影响性能
- DTO 映射逻辑复杂度增加

### 低风险
- Flutter 代码修改相对独立
- 可以通过 Feature Toggle 控制新旧逻辑

---

## 备选方案

如果不想进行数据库迁移，可以采用**轻量级方案**：

1. **后端**：保持 `category` 字段不变，在返回 EventResponse 时，根据 `category` 字符串查询 EventType 对象
2. **Flutter**：接收 EventType 对象，但创建时仍发送 `category` 字符串

这样可以快速实现 UI 显示需求，但不是最优架构。

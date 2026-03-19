# 用户信息存储优化重构计划

## 目标
将项目中冗余存储的用户信息（userName, userAvatar）改为只存储 userId，在需要时通过 UserService 动态获取用户信息，确保用户信息更新后全局同步。

---

## 进度总览

| 模块 | 状态 | 完成日期 |
|-----|------|---------|
| UserService 批量查询 | ✅ 已完成 | 已存在 |
| CoworkingService 评论 | ✅ 已完成 | 2024 |
| AccommodationService 评论 | ✅ 已完成 | 2024 |
| MessageService 聊天 | ✅ 已完成 | 2024 |
| InnovationService | ✅ 已完成 | 2024 |
| Flutter 端验证 | 🔴 待处理 | - |
| 数据库迁移 | 🔴 待处理 | - |

---

## 一、后端重构 (go-nomads)

### 1.1 聊天模块 (MessageService) - ✅ 已完成

#### 已完成的修改

1. **新增 UserServiceClient** - `Application/Services/UserServiceClient.cs`

- 通过内部服务调用访问 UserService
   - 支持单个和批量用户信息查询

2. **重构 ChatApplicationService** - `Application/Services/ChatApplicationService.cs`
   - 注入 `IUserServiceClient`
   - `GetMessagesAsync()` 现在批量获取用户信息再映射
   - `SearchMessagesAsync()` 同样动态获取用户信息
   - `GetMembersAsync()` 使用动态用户信息
   - `GetOnlineMembersAsync()` 使用动态用户信息
   - `MapToDto(ChatRoomMessage, Dictionary<string, UserInfoDto>)` 方法支持用户信息字典

3. **注册服务** - `API/Program.cs`
   - 注册 `IUserServiceClient` 为 Scoped 服务

#### 数据库表修改 (待执行)

**chat_room_messages 表：**
```sql
-- 删除冗余字段 (可选 - 保留用于向后兼容)
ALTER TABLE chat_room_messages DROP COLUMN IF EXISTS user_name;
ALTER TABLE chat_room_messages DROP COLUMN IF EXISTS user_avatar;
```

**chat_room_members 表：**
```sql
-- 删除冗余字段 (可选 - 保留用于向后兼容)
ALTER TABLE chat_room_members DROP COLUMN IF EXISTS user_name;
ALTER TABLE chat_room_members DROP COLUMN IF EXISTS user_avatar;
```

---

### 1.2 评论模块 (Review) - ✅ 已完成

#### CoworkingService 重构 ✅

1. **已修改 CoworkingReviewService** - 动态获取用户信息
2. **已修改 CoworkingReview 实体** - UserName/UserAvatar 标记为可选

#### AccommodationService 重构 ✅

1. **新增 UserServiceClient** - `Services/UserServiceClient.cs`
2. **重构 HotelReviewController** - 动态获取用户信息
3. **已注册服务** - `Program.cs`

#### 数据库表修改 (待执行)

#### 数据库表修改

**innovation_team_members 表：**
```sql
ALTER TABLE innovation_team_members DROP COLUMN IF EXISTS name;
ALTER TABLE innovation_team_members DROP COLUMN IF EXISTS avatar_url;
```

#### 需要修改的文件

| 文件 | 修改内容 |
|-----|---------|
| `src/InnovationService/Models/TeamMember.cs` | 删除 Name, AvatarUrl 属性 |
| `src/InnovationService/Services/InnovationService.cs` | 查询时动态填充 |
| `src/InnovationService/DTOs/TeamMemberDto.cs` | 保留字段用于返回 |

---

### 1.4 UserService 新增批量查询接口

**新增方法：**
```csharp
// IUserService.cs
Task<List<UserBasicInfo>> GetUsersByIdsAsync(IEnumerable<string> userIds);

// UserBasicInfo.cs (新建或复用)
public class UserBasicInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? AvatarUrl { get; set; }
}
```

---

## 二、Flutter端重构 (df_admin_mobile)

### 2.1 聊天模块

#### 需要修改的文件

| 文件 | 修改内容 |
|-----|---------|
| `lib/features/chat/domain/entities/message.dart` | 无需修改，保留显示字段 |
| `lib/features/chat/infrastructure/models/message_dto.dart` | 无需修改，接收服务端数据 |
| `lib/services/database/chat_database_service.dart` | 本地缓存可保留用户信息 |

**说明：** Flutter 端主要是接收数据展示，不需要大改。关键是确保 DTO 能正确解析服务端返回的数据。

---

### 2.2 评论模块

#### 需要修改的文件

| 文件 | 修改内容 |
|-----|---------|
| `lib/features/coworking/domain/entities/coworking_review.dart` | 无需修改 |
| `lib/features/coworking/infrastructure/models/coworking_review_dto.dart` | 无需修改 |
| `lib/features/hotel/domain/entities/hotel_review.dart` | 无需修改 |

---

### 2.3 创新项目模块

#### 需要修改的文件

| 文件 | 修改内容 |
|-----|---------|
| `lib/features/innovation/domain/entities/*.dart` | 无需修改 |
| `lib/features/innovation/infrastructure/models/*.dart` | 无需修改 |

---

## 三、数据库迁移脚本

### 3.1 创建迁移文件

```sql
-- migrations/user_info_cleanup.sql

-- Step 1: 聊天消息表
ALTER TABLE chat_room_messages 
  DROP COLUMN IF EXISTS user_name,
  DROP COLUMN IF EXISTS user_avatar;

-- Step 2: 聊天成员表
ALTER TABLE chat_room_members 
  DROP COLUMN IF EXISTS user_name,
  DROP COLUMN IF EXISTS user_avatar;

-- Step 3: Coworking 评论表
ALTER TABLE coworking_reviews 
  DROP COLUMN IF EXISTS user_name,
  DROP COLUMN IF EXISTS user_avatar;

-- Step 4: 酒店评论表
ALTER TABLE hotel_reviews 
  DROP COLUMN IF EXISTS user_name;

-- Step 5: 创新项目团队成员表
ALTER TABLE innovation_team_members 
  DROP COLUMN IF EXISTS name,
  DROP COLUMN IF EXISTS avatar_url;
```

---

## 四、重构执行顺序

### Phase 1: 准备工作
- [x] 1.1 在 UserService 中添加批量查询用户接口 ✅ (已存在 GetUsersByIdsAsync)
- [x] 1.2 创建 UserBasicInfo 类（如不存在）✅ (已存在 UserBasicDto)
- [x] 1.3 测试批量查询接口 ✅

### Phase 2: 评论模块重构（作为模板）
- [x] 2.1 修改 CoworkingReviewService 使用动态查询 ✅
- [x] 2.2 修改 HotelReviewController 使用动态查询 ✅
- [x] 2.3 为 AccommodationService 添加 UserServiceClient ✅
- [x] 2.4 修改 CoworkingApplicationService.GetCommentsAsync 使用动态查询 ✅
- [ ] 2.5 测试评论功能正常
- [ ] 2.6 执行数据库迁移删除冗余字段（待测试通过后执行）

### Phase 3: 聊天模块重构
- [x] 3.1 为 MessageService 添加 UserServiceClient ✅
- [x] 3.2 修改 ChatApplicationService 查询逻辑 (GetMessagesAsync, SearchMessagesAsync, GetMembersAsync) ✅
- [x] 3.3 修改 MapToDto 方法支持动态用户信息 ✅
- [x] 3.4 在 Program.cs 注册 UserServiceClient ✅
- [ ] 3.5 测试聊天功能正常
- [ ] 3.6 执行数据库迁移删除冗余字段

### Phase 4: 创新项目模块重构
- [x] 4.1 UserServiceClient 已存在 ✅
- [x] 4.2 修改 GetCommentsAsync 动态获取用户信息 ✅
- [x] 4.3 添加 EnrichCommentUserInfoAsync 方法 ✅
- [ ] 4.4 测试功能正常
- [ ] 4.5 执行数据库迁移

### Phase 5: Flutter 端验证
- [ ] 5.1 验证聊天功能
- [ ] 5.2 验证评论功能
- [ ] 5.3 验证创新项目功能
- [ ] 5.4 验证用户修改信息后各模块同步更新

---

## 五、注意事项

1. **向后兼容**：在删除数据库字段前，确保所有服务已更新为动态查询
2. **性能优化**：使用批量查询避免 N+1 问题
3. **缓存策略**：可考虑在 UserService 层添加短期缓存
4. **WebSocket 消息**：实时消息仍需包含用户信息，但从 UserService 获取
5. **本地存储**：Flutter 端本地数据库可以继续存储用户信息作为缓存

---

## 六、测试检查清单

- [ ] 用户修改头像后，聊天消息显示新头像
- [ ] 用户修改昵称后，评论显示新昵称
- [ ] 批量加载时性能正常（无明显延迟）
- [ ] WebSocket 实时消息显示正确用户信息
- [ ] 离线状态下本地缓存正常显示

# EventService API 接口文档

## 📋 概述

EventService 提供 Meetup/Event 管理功能，支持创建、浏览、参加和关注 Meetup。

**基础 URL**: `http://localhost:5205`  
**Scalar API 文档**: `http://localhost:5205/scalar/v1`

---

## 🎯 核心功能接口

### 1. 创建 Meetup

APP 端用户提交创建 Meetup 请求

**接口**: `POST /api/v1/Events`

**请求体**:

```json
{
  "title": "周末咖啡聚会",
  "description": "欢迎所有咖啡爱好者参加",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "cityId": "660e8400-e29b-41d4-a716-446655440000",
  "location": "星巴克臻选店",
  "address": "上海市黄浦区南京东路123号",
  "imageUrl": "https://example.com/coffee.jpg",
  "category": "social",
  "startTime": "2025-10-25T14:00:00Z",
  "endTime": "2025-10-25T16:00:00Z",
  "maxParticipants": 20,
  "locationType": "physical",
  "latitude": 31.2304,
  "longitude": 121.4737,
  "tags": ["coffee", "social", "weekend"]
}
```

**响应**: `201 Created`

```json
{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "title": "周末咖啡聚会",
  "currentParticipants": 0,
  "status": "upcoming",
  "createdAt": "2025-10-23T10:00:00Z"
}
```

---

### 2. 获取 Meetup 详情

获取单个 Meetup 的完整信息

**接口**: `GET /api/v1/Events/{id}?userId={userId}`

**Query 参数**:

- `userId` (可选): 用于检查当前用户是否已关注/参加

**响应**: `200 OK`

```json
{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "title": "周末咖啡聚会",
  "description": "欢迎所有咖啡爱好者参加",
  "organizerId": "550e8400-e29b-41d4-a716-446655440000",
  "location": "星巴克臻选店",
  "currentParticipants": 5,
  "maxParticipants": 20,
  "isFollowing": true,
  "isParticipant": false,
  "followerCount": 12,
  "status": "upcoming"
}
```

---

### 3. 获取 Meetup 列表

浏览和筛选 Meetup

**接口**: `GET /api/v1/Events?cityId={cityId}&category={category}&status={status}&page={page}&pageSize={pageSize}`

**Query 参数**:

- `cityId` (可选): 按城市筛选
- `category` (可选): 按类别筛选 (social, tech, sports等)
- `status` (可选): 按状态筛选 (upcoming, ongoing, completed, cancelled)，默认 "upcoming"
- `page` (可选): 页码，默认 1
- `pageSize` (可选): 每页数量，默认 20

**响应**: `200 OK`

```json
{
  "data": [...],
  "page": 1,
  "pageSize": 20,
  "total": 15
}
```

---

### 4. 更新 Meetup

仅创建者可更新

**接口**: `PUT /api/v1/Events/{id}?userId={userId}`

**请求体** (所有字段可选):

```json
{
  "title": "更新后的标题",
  "description": "更新后的描述",
  "maxParticipants": 30,
  "status": "ongoing"
}
```

**响应**: `200 OK`

---

## 👥 参与功能接口

### 5. 参加 Meetup

用户加入 Meetup

**接口**: `POST /api/v1/Events/{id}/join`

**请求体**:

```json
{
}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "message": "成功加入 Meetup",
  "participant": {
    "id": "880e8400-e29b-41d4-a716-446655440000",
    "eventId": "770e8400-e29b-41d4-a716-446655440000",
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "registered",
    "registeredAt": "2025-10-23T10:30:00Z"
  }
}
```

**错误响应**:

- `400`: 已经参加 / Meetup 已满员
- `404`: Meetup 不存在

---

### 6. 取消参加 Meetup

用户退出 Meetup

**接口**: `DELETE /api/v1/Events/{id}/join?userId={userId}`

**响应**: `200 OK`

```json
{
  "success": true,
  "message": "已取消参加"
}
```

---

### 7. 获取参与者列表

查看 Meetup 的所有参与者

**接口**: `GET /api/v1/Events/{id}/participants`

**响应**: `200 OK`

```json
[
  {
    "id": "880e8400-e29b-41d4-a716-446655440000",
    "eventId": "770e8400-e29b-41d4-a716-446655440000",
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "registered",
    "registeredAt": "2025-10-23T10:30:00Z"
  }
]
```

---

## ⭐ 关注功能接口

### 8. 关注 Meetup

用户关注感兴趣的 Meetup

**接口**: `POST /api/v1/Events/{id}/follow`

**请求体**:

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "notificationEnabled": true
}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "message": "成功关注 Meetup",
  "follower": {
    "id": "990e8400-e29b-41d4-a716-446655440000",
    "eventId": "770e8400-e29b-41d4-a716-446655440000",
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "followedAt": "2025-10-23T10:45:00Z",
    "notificationEnabled": true
  }
}
```

**错误响应**:

- `400`: 已经关注
- `404`: Meetup 不存在

---

### 9. 取消关注 Meetup

用户取消关注

**接口**: `DELETE /api/v1/Events/{id}/follow?userId={userId}`

**响应**: `200 OK`

```json
{
  "success": true,
  "message": "已取消关注"
}
```

---

### 10. 获取关注者列表

查看 Meetup 的所有关注者

**接口**: `GET /api/v1/Events/{id}/followers`

**响应**: `200 OK`

```json
[
  {
    "id": "990e8400-e29b-41d4-a716-446655440000",
    "eventId": "770e8400-e29b-41d4-a716-446655440000",
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "followedAt": "2025-10-23T10:45:00Z",
    "notificationEnabled": true
  }
]
```

---

## 👤 用户相关接口

### 11. 获取用户创建的 Meetup

查看用户作为组织者创建的所有 Meetup

**接口**: `GET /api/v1/Events/user/{userId}/created`

**响应**: `200 OK` - 返回 EventResponse 数组

---

### 12. 获取用户参加的 Meetup

查看用户已参加的所有 Meetup

**接口**: `GET /api/v1/Events/user/{userId}/joined`

**响应**: `200 OK` - 返回 EventResponse 数组

---

### 13. 获取用户关注的 Meetup

查看用户关注的所有 Meetup

**接口**: `GET /api/v1/Events/user/{userId}/following`

**响应**: `200 OK` - 返回 EventResponse 数组

---

## 📊 数据模型

### Event 字段说明

- `id`: UUID，主键
- `title`: 标题 (必填，最多200字符)
- `description`: 描述
- `organizerId`: 创建者/组织者ID (必填)
- `cityId`: 城市ID
- `location`: 地点名称
- `address`: 详细地址
- `imageUrl`: 封面图片URL
- `images`: 图片数组
- `category`: 类别 (networking, workshop, social, sports, cultural, tech, business, other)
- `startTime`: 开始时间 (必填)
- `endTime`: 结束时间
- `maxParticipants`: 最大参与人数
- `currentParticipants`: 当前参与人数
- `status`: 状态 (upcoming, ongoing, completed, cancelled)
- `locationType`: 类型 (physical, online, hybrid)
- `meetingLink`: 线上会议链接
- `latitude`: 纬度
- `longitude`: 经度
- `tags`: 标签数组
- `isFeatured`: 是否精选

---

## 🔧 测试命令

```bash
# 1. 创建 Meetup
curl -X POST http://localhost:5205/api/v1/Events \
  -H "Content-Type: application/json" \
  -d '{
    "title": "测试聚会",
    "organizerId": "550e8400-e29b-41d4-a716-446655440000",
    "startTime": "2025-10-25T14:00:00Z",
    "locationType": "physical"
  }'

# 2. 获取 Meetup 列表
curl http://localhost:5205/api/v1/Events

# 3. 参加 Meetup
curl -X POST http://localhost:5205/api/v1/Events/{eventId}/join \
  -H "Content-Type: application/json" \
  -d '{"userId": "550e8400-e29b-41d4-a716-446655440000"}'

# 4. 关注 Meetup
curl -X POST http://localhost:5205/api/v1/Events/{eventId}/follow \
  -H "Content-Type: application/json" \
  -d '{"userId": "550e8400-e29b-41d4-a716-446655440000"}'
```

---

## 📝 数据库迁移

执行以下 SQL 在 Supabase 创建 `event_followers` 表：

**文件位置**: `src/Services/EventService/EventService/Database/create-event-followers-table.sql`

```sql
CREATE TABLE IF NOT EXISTS event_followers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    followed_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    notification_enabled BOOLEAN DEFAULT TRUE,
    UNIQUE(event_id, user_id)
);

CREATE INDEX idx_event_followers_event_id ON event_followers(event_id);
CREATE INDEX idx_event_followers_user_id ON event_followers(user_id);
```

---

## ✅ 功能清单

- ✅ **创建 Meetup** - APP 端用户可提交创建请求
- ✅ **浏览 Meetup** - 支持城市、类别、状态筛选
- ✅ **参加 Meetup** - 其他用户可加入，自动更新参与人数
- ✅ **取消参加** - 用户可退出 Meetup
- ✅ **关注 Meetup** - 用户可关注感兴趣的活动
- ✅ **取消关注** - 用户可取消关注
- ✅ **查看参与者** - 获取 Meetup 参与者列表
- ✅ **查看关注者** - 获取 Meetup 关注者列表
- ✅ **用户活动** - 查看用户创建/参加/关注的 Meetup
- ✅ **权限控制** - 仅创建者可修改 Meetup
- ✅ **人数限制** - 自动检查最大参与人数
- ✅ **防重复** - 防止重复参加/关注

---

## 🚀 部署状态

- **服务地址**: http://localhost:5205
- **Scalar 文档**: http://localhost:5205/scalar/v1
- **健康检查**: http://localhost:5205/health
- **内部服务通信**: 通过 HTTP API 与共享服务客户端完成

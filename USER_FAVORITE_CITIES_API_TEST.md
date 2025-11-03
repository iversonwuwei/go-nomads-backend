# 用户收藏城市 API 测试指南

## 功能概述
用户收藏城市功能已完成,包括:
- ✅ 后端 API 接口 (CityService)
- ✅ 前端 UI 和逻辑 (Flutter)
- ✅ Supabase 数据库表和 RLS 策略

## API 端点

### 基础 URL
- 本地开发: `http://localhost:8002/api/v1/user-favorite-cities`
- Docker: `http://localhost:8002/api/v1/user-favorite-cities`

### 认证
所有接口都需要 JWT Bearer Token (从 Supabase 登录获取)

```
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## 1. 检查城市是否已收藏

**GET** `/api/v1/user-favorite-cities/check/{cityId}`

### 请求示例
```bash
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/check/tokyo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 响应示例
```json
{
  "isFavorited": true
}
```

---

## 2. 添加收藏城市

**POST** `/api/v1/user-favorite-cities`

### 请求体
```json
{
  "cityId": "tokyo"
}
```

### 请求示例
```bash
curl -X POST "http://localhost:8002/api/v1/user-favorite-cities" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"cityId": "tokyo"}'
```

### 响应示例 (201 Created)
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "user-uuid",
  "cityId": "tokyo",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-15T10:30:00Z"
}
```

### 错误响应 (409 Conflict - 已存在)
```json
{
  "error": "City already in favorites"
}
```

---

## 3. 取消收藏城市

**DELETE** `/api/v1/user-favorite-cities/{cityId}`

### 请求示例
```bash
curl -X DELETE "http://localhost:8002/api/v1/user-favorite-cities/tokyo" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 响应
- **204 No Content** - 删除成功
- **404 Not Found** - 收藏不存在

---

## 4. 获取收藏城市 ID 列表

**GET** `/api/v1/user-favorite-cities/ids`

### 请求示例
```bash
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/ids" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 响应示例
```json
[
  "tokyo",
  "bangkok",
  "lisbon",
  "bali"
]
```

---

## 5. 获取收藏城市列表 (分页)

**GET** `/api/v1/user-favorite-cities?page={page}&pageSize={pageSize}`

### 查询参数
- `page`: 页码 (默认: 1, 范围: 1-100)
- `pageSize`: 每页数量 (默认: 20, 范围: 1-100)

### 请求示例
```bash
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 响应示例
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "userId": "user-uuid",
      "cityId": "tokyo",
      "createdAt": "2025-01-15T10:30:00Z",
      "updatedAt": "2025-01-15T10:30:00Z"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "userId": "user-uuid",
      "cityId": "bangkok",
      "createdAt": "2025-01-14T08:20:00Z",
      "updatedAt": "2025-01-14T08:20:00Z"
    }
  ],
  "total": 25,
  "page": 1,
  "pageSize": 10
}
```

---

## 测试流程

### 步骤 1: 获取 JWT Token

1. 在 Flutter App 中登录
2. 从开发者工具或代码中获取 JWT token
3. 或使用 Supabase Auth API 直接获取:

```bash
curl -X POST "YOUR_SUPABASE_URL/auth/v1/token?grant_type=password" \
  -H "apikey: YOUR_SUPABASE_ANON_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "your-password"
  }'
```

### 步骤 2: 测试各个端点

```bash
# 设置 Token 变量
export TOKEN="your-jwt-token-here"

# 1. 检查 tokyo 是否已收藏
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/check/tokyo" \
  -H "Authorization: Bearer $TOKEN"

# 2. 添加 tokyo 到收藏
curl -X POST "http://localhost:8002/api/v1/user-favorite-cities" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"cityId": "tokyo"}'

# 3. 再次检查 tokyo (应该返回 true)
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/check/tokyo" \
  -H "Authorization: Bearer $TOKEN"

# 4. 获取所有收藏城市 ID
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/ids" \
  -H "Authorization: Bearer $TOKEN"

# 5. 获取收藏城市列表 (分页)
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities?page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"

# 6. 取消收藏 tokyo
curl -X DELETE "http://localhost:8002/api/v1/user-favorite-cities/tokyo" \
  -H "Authorization: Bearer $TOKEN"

# 7. 再次检查 tokyo (应该返回 false)
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/check/tokyo" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 数据库验证

### 查看 Supabase 表数据

在 Supabase SQL 编辑器中运行:

```sql
-- 查看所有收藏记录
SELECT * FROM user_favorite_cities ORDER BY created_at DESC;

-- 查看特定用户的收藏
SELECT * FROM user_favorite_cities WHERE user_id = 'your-user-uuid';

-- 查看某个城市被收藏的次数
SELECT COUNT(*) FROM user_favorite_cities WHERE city_id = 'tokyo';

-- 查看 RLS 策略
SELECT * FROM pg_policies WHERE tablename = 'user_favorite_cities';
```

---

## 前端集成测试

### Flutter App 测试步骤

1. **启动 App 并登录**
   ```bash
   cd /Users/walden/Workspaces/WaldenProjects/open-platform-app
   flutter run
   ```

2. **导航到城市详情页**
   - 选择任意城市
   - 观察右上角的收藏图标

3. **测试收藏功能**
   - 点击收藏图标
   - 应该看到:
     - 图标变为红色实心 ❤️
     - 显示 Toast: "收藏成功"
     - 按钮显示加载动画期间禁用

4. **测试取消收藏**
   - 再次点击收藏图标
   - 应该看到:
     - 图标变为灰色空心 🤍
     - 显示 Toast: "已取消收藏"

5. **测试状态持久化**
   - 退出城市详情页
   - 重新进入同一城市
   - 收藏状态应该正确显示

---

## 错误处理测试

### 1. 无效 Token
```bash
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/ids" \
  -H "Authorization: Bearer invalid-token"
```
**预期**: 401 Unauthorized

### 2. 缺少 Token
```bash
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities/ids"
```
**预期**: 401 Unauthorized

### 3. 空 CityId
```bash
curl -X POST "http://localhost:8002/api/v1/user-favorite-cities" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"cityId": ""}'
```
**预期**: 400 Bad Request

### 4. 重复添加收藏
```bash
# 添加第一次
curl -X POST "http://localhost:8002/api/v1/user-favorite-cities" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"cityId": "tokyo"}'

# 再次添加同一个城市
curl -X POST "http://localhost:8002/api/v1/user-favorite-cities" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"cityId": "tokyo"}'
```
**预期**: 第二次返回 409 Conflict

### 5. 删除不存在的收藏
```bash
curl -X DELETE "http://localhost:8002/api/v1/user-favorite-cities/nonexistent-city" \
  -H "Authorization: Bearer $TOKEN"
```
**预期**: 404 Not Found

---

## 性能测试

### 批量添加收藏
```bash
export TOKEN="your-token"

for city in tokyo bangkok lisbon bali chiang-mai taipei seoul singapore; do
  curl -X POST "http://localhost:8002/api/v1/user-favorite-cities" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"cityId\": \"$city\"}"
  echo ""
done
```

### 测试分页
```bash
# 测试不同页码
curl -X GET "http://localhost:8002/api/v1/user-favorite-cities?page=1&pageSize=5" \
  -H "Authorization: Bearer $TOKEN"

curl -X GET "http://localhost:8002/api/v1/user-favorite-cities?page=2&pageSize=5" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 常见问题

### Q: 如何获取用户的 JWT Token?

**A**: 有几种方式:

1. **从 Flutter App 获取**:
   ```dart
   final session = Supabase.instance.client.auth.currentSession;
   final token = session?.accessToken;
   print('JWT Token: $token');
   ```

2. **从浏览器开发者工具**:
   - 打开开发者工具 (F12)
   - Network 标签
   - 查看任意 API 请求的 Authorization header

3. **使用 Supabase Auth API**:
   ```bash
   curl -X POST "YOUR_SUPABASE_URL/auth/v1/token?grant_type=password" \
     -H "apikey: YOUR_SUPABASE_ANON_KEY" \
     -H "Content-Type: application/json" \
     -d '{"email": "test@example.com", "password": "password"}'
   ```

### Q: API 返回 401 Unauthorized 怎么办?

**A**: 检查:
1. Token 是否正确
2. Token 是否过期 (Supabase token 默认 1 小时过期)
3. Authorization header 格式: `Bearer YOUR_TOKEN`

### Q: 如何验证 RLS 策略是否生效?

**A**: 
1. 使用不同用户的 token 测试
2. 尝试访问其他用户的收藏 (应该失败)
3. 在 Supabase SQL 编辑器中测试:
   ```sql
   -- 应该只能看到当前用户的数据
   SELECT * FROM user_favorite_cities;
   ```

---

## 下一步

- [ ] 在 Flutter App 中测试收藏功能
- [ ] 验证所有 API 端点正常工作
- [ ] 测试错误处理
- [ ] 性能测试 (大量收藏)
- [ ] 集成到城市列表页显示收藏图标
- [ ] 创建"我的收藏"页面展示所有收藏城市

---

## 相关文件

### 后端 (.NET)
- `go-noma/src/Services/CityService/CityService/DTOs/UserFavoriteCityDto.cs`
- `go-noma/src/Services/CityService/CityService/Domain/Entities/UserFavoriteCity.cs`
- `go-noma/src/Services/CityService/CityService/Domain/Repositories/IUserFavoriteCityRepository.cs`
- `go-noma/src/Services/CityService/CityService/Infrastructure/Repositories/SupabaseUserFavoriteCityRepository.cs`
- `go-noma/src/Services/CityService/CityService/Application/Services/UserFavoriteCityService.cs`
- `go-noma/src/Services/CityService/CityService/API/Controllers/UserFavoriteCitiesController.cs`
- `go-noma/src/Services/CityService/CityService/Program.cs`

### 前端 (Flutter)
- `open-platform-app/lib/models/user_favorite_city_model.dart`
- `open-platform-app/lib/services/user_favorite_city_api_service.dart`
- `open-platform-app/lib/controllers/city_detail_controller.dart`
- `open-platform-app/lib/pages/city_detail_page.dart` (收藏按钮 UI)

### 数据库
- `open-platform-app/supabase_migrations/user_favorite_cities_table.sql`

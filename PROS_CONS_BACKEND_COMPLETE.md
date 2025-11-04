# Pros & Cons 功能后端实现完成

## 功能概述
为城市详情页的"乐趣"tab 添加完整的 Pros & Cons 后端服务,用户可以分享城市的优点和挑战。

## 实现文件清单

### 1. 数据库层
- **`database/migrations/create_city_pros_cons_table.sql`** ✅
  - 表结构: `city_pros_cons`
  - 字段: id, user_id, city_id, text, is_pro, upvotes, downvotes
  - RLS 策略: 公开可见,用户仅可修改自己的内容
  - 索引: user_id, city_id, city_id+is_pro, upvotes
  - 触发器: 自动更新 updated_at

### 2. Domain 层
- **`Domain/Entities/CityProsCons.cs`** ✅
  - 继承 BaseModel (Supabase)
  - 映射到 `city_pros_cons` 表
  - CityId 使用 `string` 类型 (如 "BJ", "SH")

- **`Domain/Repositories/IUserCityProsConsRepository.cs`** ✅
  - AddAsync: 添加新记录
  - GetByCityIdAsync: 获取城市的所有 Pros & Cons (可筛选 isPro)
  - GetByIdAsync: 根据 ID 获取
  - UpdateAsync: 更新记录
  - DeleteAsync: 删除记录 (需验证用户权限)

### 3. Infrastructure 层
- **`Infrastructure/Repositories/SupabaseUserCityProsConsRepository.cs`** ✅
  - 实现 IUserCityProsConsRepository
  - 使用 Supabase Client 进行数据操作
  - 自动设置 CreatedAt 和 UpdatedAt

### 4. Application 层
- **`Application/DTOs/UserCityContentDTOs.cs`** ✅
  - `CityProsConsDto`: 完整数据模型
  - `AddCityProsConsRequest`: 创建请求 (cityId, text, isPro)
  - `UpdateCityProsConsRequest`: 更新请求 (text, isPro)

- **`Application/Services/IUserCityContentService.cs`** ✅
  - 新增 4 个 Pros & Cons 方法接口

- **`Application/Services/UserCityContentApplicationService.cs`** ✅
  - 注入 IUserCityProsConsRepository
  - 实现 4 个 Pros & Cons 方法
  - MapProsConsToDto 映射方法

### 5. API 层
- **`API/Controllers/UserCityContentController.cs`** ✅
  - `POST /api/v1/cities/{cityId}/user-content/pros-cons` - 添加 Pros & Cons
  - `GET /api/v1/cities/{cityId}/user-content/pros-cons?isPro=true` - 获取列表
  - `PUT /api/v1/cities/{cityId}/user-content/pros-cons/{id}` - 更新
  - `DELETE /api/v1/cities/{cityId}/user-content/pros-cons/{id}` - 删除

### 6. 依赖注入配置
- **`Program.cs`** ✅
  - 注册 `IUserCityProsConsRepository` → `SupabaseUserCityProsConsRepository`

## API 端点详情

### 1. 添加 Pros & Cons
```http
POST /api/v1/cities/{cityId}/user-content/pros-cons
Authorization: Bearer <token>
Content-Type: application/json

{
  "cityId": "BJ",
  "text": "互联网氛围浓厚，科技公司多",
  "isPro": true
}
```

**响应**:
```json
{
  "success": true,
  "message": "优点添加成功",
  "data": {
    "id": "uuid",
    "userId": "uuid",
    "cityId": "BJ",
    "text": "互联网氛围浓厚，科技公司多",
    "isPro": true,
    "upvotes": 0,
    "downvotes": 0,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

### 2. 获取城市 Pros & Cons
```http
GET /api/v1/cities/BJ/user-content/pros-cons?isPro=true
```

**参数**:
- `isPro` (可选): `true` 只返回优点, `false` 只返回挑战, 不传返回全部

**响应**:
```json
{
  "success": true,
  "message": "获取成功",
  "data": [
    {
      "id": "uuid",
      "userId": "uuid",
      "cityId": "BJ",
      "text": "互联网氛围浓厚，科技公司多",
      "isPro": true,
      "upvotes": 0,
      "downvotes": 0,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### 3. 更新 Pros & Cons
```http
PUT /api/v1/cities/BJ/user-content/pros-cons/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "text": "互联网氛围浓厚，科技公司多，创业机会多",
  "isPro": true
}
```

### 4. 删除 Pros & Cons
```http
DELETE /api/v1/cities/BJ/user-content/pros-cons/{id}
Authorization: Bearer <token>
```

## 部署步骤

### 1. 执行数据库迁移
```bash
# 使用 psql
psql -h db.xxxx.supabase.co -U postgres -d postgres -f database/migrations/create_city_pros_cons_table.sql

# 或使用 PowerShell 脚本
.\execute-migration.ps1
```

### 2. 重新部署 CityService
```bash
cd src/Services/CityService/CityService
dotnet build
dotnet publish -c Release -o ./publish
```

### 3. 配置 Gateway 路由 (如需要)
检查 Gateway 是否正确转发到 CityService:
```
/api/v1/cities/{cityId}/user-content/pros-cons -> CityService
```

## 测试脚本

### 1. 测试添加优点
```bash
curl -X POST "http://localhost:5001/api/v1/cities/BJ/user-content/pros-cons" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "cityId": "BJ",
    "text": "互联网氛围浓厚，科技公司多",
    "isPro": true
  }'
```

### 2. 测试添加挑战
```bash
curl -X POST "http://localhost:5001/api/v1/cities/BJ/user-content/pros-cons" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "cityId": "BJ",
    "text": "空气质量较差，冬季有雾霾",
    "isPro": false
  }'
```

### 3. 测试获取所有 Pros & Cons
```bash
curl "http://localhost:5001/api/v1/cities/BJ/user-content/pros-cons"
```

### 4. 测试筛选优点
```bash
curl "http://localhost:5001/api/v1/cities/BJ/user-content/pros-cons?isPro=true"
```

### 5. 测试筛选挑战
```bash
curl "http://localhost:5001/api/v1/cities/BJ/user-content/pros-cons?isPro=false"
```

## Flutter 前端集成

前端已实现:
- `ProsAndConsAddPage`: 添加页面
- `ProsAndConsAddController`: 状态管理
- `CityApiService`: API 调用 (需要添加 Pros & Cons 方法)

### 需要在 CityApiService 添加:
```dart
// 添加 Pros & Cons
Future<ProsCons> addProsCons(String cityId, String text, bool isPro) async {
  final response = await dio.post(
    '/cities/$cityId/user-content/pros-cons',
    data: {
      'cityId': cityId,
      'text': text,
      'isPro': isPro,
    },
  );
  return ProsCons.fromJson(response.data['data']);
}

// 获取 Pros & Cons
Future<List<ProsCons>> getCityProsCons(String cityId, {bool? isPro}) async {
  final response = await dio.get(
    '/cities/$cityId/user-content/pros-cons',
    queryParameters: isPro != null ? {'isPro': isPro} : null,
  );
  return (response.data['data'] as List)
      .map((json) => ProsCons.fromJson(json))
      .toList();
}
```

## 权限说明

### RLS 策略
1. **查看**: 所有人可以查看所有 Pros & Cons
2. **创建**: 登录用户可以创建
3. **更新**: 用户只能更新自己创建的内容
4. **删除**: 用户只能删除自己创建的内容

### 认证方式
使用 Supabase Auth + UserContext Middleware:
- 从 HttpContext 获取 UserContext
- 验证 IsAuthenticated 和 UserId
- 所有写操作需要认证

## 数据验证

### 后端验证
- Text: 1-500 字符
- isPro: 必须为 true/false
- CityId: 必须提供

### 前端验证
- Text: 非空验证
- 提交前禁用按钮防止重复提交

## 性能优化

### 数据库索引
```sql
CREATE INDEX idx_city_pros_cons_user_id ON city_pros_cons(user_id);
CREATE INDEX idx_city_pros_cons_city_id ON city_pros_cons(city_id);
CREATE INDEX idx_city_pros_cons_city_is_pro ON city_pros_cons(city_id, is_pro);
CREATE INDEX idx_city_pros_cons_upvotes ON city_pros_cons(upvotes DESC);
```

### API 优化
- 使用 `isPro` 参数筛选,减少不必要的数据传输
- 按 `created_at DESC` 排序,最新的在前
- 未来可添加分页支持

## 后续扩展

### 投票功能
可添加点赞/踩功能:
```csharp
Task<CityProsConsDto> UpvoteAsync(Guid userId, Guid prosConsId);
Task<CityProsConsDto> DownvoteAsync(Guid userId, Guid prosConsId);
```

### 举报功能
可添加举报不当内容:
```csharp
Task<bool> ReportAsync(Guid userId, Guid prosConsId, string reason);
```

### 统计功能
可添加统计数据:
```csharp
Task<ProsConsStatsDto> GetStatsAsync(string cityId);
```

## 编译状态
✅ **编译成功**: CityService 项目无错误

## 完成时间
2024-01-XX XX:XX:XX

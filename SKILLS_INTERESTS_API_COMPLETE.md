# 技能和兴趣爱好 API 集成完成

## 📝 概述

为 UserService 添加了完整的技能和兴趣爱好管理功能,包括:
- ✅ 获取所有技能/兴趣(按类别分组)
- ✅ 获取用户的技能/兴趣
- ✅ 添加/删除/更新用户技能/兴趣
- ✅ 批量操作支持
- ✅ 当前用户认证端点

## 📂 创建的文件

### 1. DTO 层
- `Application/DTOs/SkillDto.cs` - 技能相关 DTO
- `Application/DTOs/InterestDto.cs` - 兴趣相关 DTO

### 2. 服务接口层
- `Application/Services/ISkillService.cs` - 技能服务接口
- `Application/Services/IInterestService.cs` - 兴趣服务接口

### 3. 服务实现层
- `Infrastructure/Services/SkillService.cs` - 技能服务实现(含 Supabase 实体)
- `Infrastructure/Services/InterestService.cs` - 兴趣服务实现(含 Supabase 实体)

### 4. API 控制器层
- `API/Controllers/SkillsController.cs` - 技能 API 端点
- `API/Controllers/InterestsController.cs` - 兴趣 API 端点

### 5. 配置
- `Program.cs` - 已注册服务到 DI 容器

## 🔌 API 端点

### 技能 API (`/api/v1/skills`)

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/skills` | 获取所有技能 |
| GET | `/api/v1/skills/by-category` | 获取按类别分组的技能 |
| GET | `/api/v1/skills/category/{category}` | 获取特定类别的技能 |
| GET | `/api/v1/skills/{id}` | 获取单个技能详情 |
| GET | `/api/v1/skills/users/{userId}` | 获取用户的所有技能 |
| GET | `/api/v1/skills/me` | 获取当前用户的技能 (需认证) |
| POST | `/api/v1/skills/users/{userId}` | 添加用户技能 |
| POST | `/api/v1/skills/me` | 添加当前用户技能 (需认证) |
| POST | `/api/v1/skills/users/{userId}/batch` | 批量添加用户技能 |
| PUT | `/api/v1/skills/users/{userId}/{skillId}` | 更新用户技能 |
| DELETE | `/api/v1/skills/users/{userId}/{skillId}` | 删除用户技能 |

### 兴趣 API (`/api/v1/interests`)

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/v1/interests` | 获取所有兴趣 |
| GET | `/api/v1/interests/by-category` | 获取按类别分组的兴趣 |
| GET | `/api/v1/interests/category/{category}` | 获取特定类别的兴趣 |
| GET | `/api/v1/interests/{id}` | 获取单个兴趣详情 |
| GET | `/api/v1/interests/users/{userId}` | 获取用户的所有兴趣 |
| GET | `/api/v1/interests/me` | 获取当前用户的兴趣 (需认证) |
| POST | `/api/v1/interests/users/{userId}` | 添加用户兴趣 |
| POST | `/api/v1/interests/me` | 添加当前用户兴趣 (需认证) |
| POST | `/api/v1/interests/users/{userId}/batch` | 批量添加用户兴趣 |
| PUT | `/api/v1/interests/users/{userId}/{interestId}` | 更新用户兴趣 |
| DELETE | `/api/v1/interests/users/{userId}/{interestId}` | 删除用户兴趣 |

## 📊 请求/响应示例

### 1. 获取所有技能

**请求:**
```bash
GET /api/v1/skills
```

**响应:**
```json
{
  "success": true,
  "message": "Skills retrieved successfully",
  "data": [
    {
      "id": "skill_javascript",
      "name": "JavaScript",
      "category": "Programming",
      "description": "前端和后端开发语言",
      "icon": "💻",
      "createdAt": "2025-11-02T00:00:00Z"
    }
  ]
}
```

### 2. 获取按类别分组的技能

**请求:**
```bash
GET /api/v1/skills/by-category
```

**响应:**
```json
{
  "success": true,
  "message": "Skills by category retrieved successfully",
  "data": [
    {
      "category": "Programming",
      "skills": [
        {
          "id": "skill_javascript",
          "name": "JavaScript",
          "category": "Programming",
          "icon": "💻"
        },
        {
          "id": "skill_python",
          "name": "Python",
          "category": "Programming",
          "icon": "🐍"
        }
      ]
    },
    {
      "category": "Design",
      "skills": [...]
    }
  ]
}
```

### 3. 添加用户技能

**请求:**
```bash
POST /api/v1/skills/users/{userId}
Content-Type: application/json

{
  "skillId": "skill_javascript",
  "proficiencyLevel": "advanced",
  "yearsOfExperience": 5
}
```

**响应:**
```json
{
  "success": true,
  "message": "User skill added successfully",
  "data": {
    "id": "uuid",
    "userId": "user-id",
    "skillId": "skill_javascript",
    "skillName": "JavaScript",
    "category": "Programming",
    "icon": "💻",
    "proficiencyLevel": "advanced",
    "yearsOfExperience": 5,
    "createdAt": "2025-11-02T10:00:00Z"
  }
}
```

### 4. 批量添加用户技能

**请求:**
```bash
POST /api/v1/skills/users/{userId}/batch
Content-Type: application/json

[
  {
    "skillId": "skill_python",
    "proficiencyLevel": "intermediate",
    "yearsOfExperience": 3
  },
  {
    "skillId": "skill_react",
    "proficiencyLevel": "expert",
    "yearsOfExperience": 7
  }
]
```

### 5. 获取用户技能

**请求:**
```bash
GET /api/v1/skills/users/{userId}
```

**响应:**
```json
{
  "success": true,
  "message": "User skills retrieved successfully",
  "data": [
    {
      "id": "uuid",
      "userId": "user-id",
      "skillId": "skill_javascript",
      "skillName": "JavaScript",
      "category": "Programming",
      "icon": "💻",
      "proficiencyLevel": "advanced",
      "yearsOfExperience": 5
    }
  ]
}
```

### 6. 添加用户兴趣

**请求:**
```bash
POST /api/v1/interests/users/{userId}
Content-Type: application/json

{
  "interestId": "interest_hiking",
  "intensityLevel": "passionate"
}
```

**响应:**
```json
{
  "success": true,
  "message": "User interest added successfully",
  "data": {
    "id": "uuid",
    "userId": "user-id",
    "interestId": "interest_hiking",
    "interestName": "Hiking",
    "category": "Outdoor",
    "icon": "🥾",
    "intensityLevel": "passionate",
    "createdAt": "2025-11-02T10:00:00Z"
  }
}
```

## 🔒 认证端点

使用 `UserContext` 中间件,从 JWT Token 中提取用户信息:

```bash
# 获取当前用户技能
GET /api/v1/skills/me
Authorization: Bearer {jwt-token}

# 添加当前用户技能
POST /api/v1/skills/me
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "skillId": "skill_flutter",
  "proficiencyLevel": "advanced",
  "yearsOfExperience": 4
}
```

## 📝 熟练度和强度级别

### 技能熟练度 (proficiencyLevel)
- `beginner` - 初学者
- `intermediate` - 中级
- `advanced` - 高级
- `expert` - 专家

### 兴趣强度 (intensityLevel)
- `casual` - 随意
- `moderate` - 适度
- `passionate` - 热情/专注

## 🧪 测试

1. **运行测试脚本:**
```bash
chmod +x test-skills-interests.sh
./test-skills-interests.sh
```

2. **手动测试:**
```bash
# 获取所有技能
curl http://localhost:5001/api/v1/skills | jq '.'

# 获取分类技能
curl http://localhost:5001/api/v1/skills/by-category | jq '.'

# 添加用户技能
curl -X POST http://localhost:5001/api/v1/skills/users/{userId} \
  -H "Content-Type: application/json" \
  -d '{"skillId":"skill_javascript","proficiencyLevel":"advanced","yearsOfExperience":5}'
```

## 🔧 依赖注入配置

已在 `Program.cs` 中注册服务:

```csharp
// Register Application Services
builder.Services.AddScoped<ISkillService, UserService.Infrastructure.Services.SkillService>();
builder.Services.AddScoped<IInterestService, UserService.Infrastructure.Services.InterestService>();
```

## 📡 Supabase 查询优化

服务层使用了优化的 JOIN 查询来获取用户的技能/兴趣及其详细信息,避免多次数据库调用:

```sql
SELECT 
    us.id,
    us.user_id,
    us.skill_id,
    s.name as skill_name,
    s.category,
    s.icon,
    us.proficiency_level,
    us.years_of_experience,
    us.created_at
FROM user_skills us
JOIN skills s ON us.skill_id = s.id
WHERE us.user_id = '{userId}'
ORDER BY s.category, s.name
```

## ⚠️ 注意事项

1. **RPC 函数**: 需要在 Supabase 中创建 `execute_sql` RPC 函数,或修改服务层使用 Supabase 的标准查询方式

2. **UUID 类型**: `user_skills` 和 `user_interests` 表的 `user_id` 字段已修正为 `UUID` 类型

3. **错误处理**: 所有端点都包含完整的错误处理和日志记录

4. **批量操作**: 批量添加时会跳过已存在的项,不会抛出异常

## 🚀 下一步

1. **前端集成**:
   - 在 Flutter 中创建 `SkillService` 调用这些 API
   - 创建技能/兴趣选择 UI 组件
   - 在用户注册流程中集成

2. **Gateway 路由**:
   - 在 BFF/Gateway 中添加路由配置
   - 配置缓存策略

3. **高级功能**:
   - 技能推荐算法
   - 基于技能/兴趣的用户匹配
   - 技能统计和趋势分析

---

**创建日期**: 2025-11-02  
**服务**: UserService  
**数据库**: Supabase PostgreSQL

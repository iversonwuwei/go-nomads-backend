# 数字游民指南架构迁移完成

## 📋 概述

将数字游民指南(Digital Nomad Guide)从**Flutter本地SQLite缓存**迁移到**后端API + Supabase持久化存储**架构。

**迁移日期**: 2025-11-11

---

## 🎯 架构变更

### 旧架构 (已废弃)
```
Flutter App
    ↓
SQLite本地缓存 ← 从AIService生成时保存
    ↓
读取显示
```

### 新架构 (当前)
```
Flutter App
    ↓
后端CityService API (GET/POST)
    ↓
Supabase数据库 ← AIService生成时通过Dapr调用保存
```

---

## ✅ 已完成的修改

### 1. Flutter端 (df_admin_mobile)

#### 移除SQLite依赖
- ✅ `ai_state_controller.dart`
  - 删除 `DigitalNomadGuideDao`、`DatabaseService` imports
  - 删除 `_guideDao` 字段和 `_initializeDao()` 方法
  - 删除 `_isGuideFromCache` 状态变量
  - 删除 `deleteCachedGuide()` 和 `clearAllCachedGuides()` 方法
  - 删除生成方法中的SQLite save操作

#### 添加后端API调用
- ✅ `ai_use_cases.dart`
  - 新增 `GetDigitalNomadGuideUseCase` 类

- ✅ `iai_repository.dart`
  - 新增 `getDigitalNomadGuideFromBackend(String cityId)` 接口方法

- ✅ `ai_repository.dart`
  - 实现 `getDigitalNomadGuideFromBackend` 方法
  - 调用 `GET /cities/{cityId}/guide` API

- ✅ `dependency_injection.dart`
  - 注册 `GetDigitalNomadGuideUseCase`
  - 更新 `AiStateController` 构造函数参数

#### UI更新
- ✅ `city_detail_page.dart`
  - 移除所有 `isGuideFromCache` 引用
  - 简化状态提示UI为"从后端加载"
  - 删除 `_formatCacheTime()` 方法

---

### 2. 后端CityService

#### Domain层
- ✅ **`Domain/Entities/DigitalNomadGuide.cs`** - 新建
  - 继承 `BaseModel`
  - 包含完整字段映射(Postgrest Attributes)
  - 嵌套类: `VisaInfo`, `BestArea`

- ✅ **`Domain/Repositories/IDigitalNomadGuideRepository.cs`** - 新建
  - `GetByCityIdAsync(string cityId)`
  - `SaveAsync(DigitalNomadGuide guide)`
  - `DeleteAsync(string id)`
  - `ExistsByCityIdAsync(string cityId)`

#### Infrastructure层
- ✅ **`Infrastructure/Repositories/SupabaseDigitalNomadGuideRepository.cs`** - 新建
  - 实现IDigitalNomadGuideRepository
  - Supabase CRUD操作
  - Upsert逻辑(存在则更新,不存在则插入)

#### Application层
- ✅ **`Application/Services/IDigitalNomadGuideService.cs`** - 新建
  - 服务接口定义

- ✅ **`Application/Services/DigitalNomadGuideService.cs`** - 新建
  - 业务逻辑层实现

- ✅ **`Application/DTOs/DigitalNomadGuideDto.cs`** - 新建
  - `DigitalNomadGuideDto`
  - `VisaInfoDto`
  - `BestAreaDto`
  - `SaveDigitalNomadGuideRequest`

#### API层
- ✅ **`API/Controllers/CitiesController.cs`** - 修改
  - 注入 `IDigitalNomadGuideService`
  - 新增 **GET `/api/v1/cities/{cityId}/guide`**
    - 返回指南或404
  - 新增 **POST `/api/v1/cities/{cityId}/guide`**
    - 保存/更新指南
  - 新增 `MapToDto(DigitalNomadGuide guide)` 辅助方法

#### 依赖注入
- ✅ **`Program.cs`** - 修改
  - 注册 `IDigitalNomadGuideRepository` → `SupabaseDigitalNomadGuideRepository`
  - 注册 `IDigitalNomadGuideService` → `DigitalNomadGuideService`

---

### 3. 后端AIService

#### Worker服务集成
- ✅ **`API/Services/AIWorkerService.cs`** - 修改
  - 添加 `using Dapr.Client;`
  - 在 `ProcessGuideTaskAsync` 方法中:
    - 生成完成后,通过Dapr HTTP调用CityService
    - `daprClient.InvokeMethodAsync` → `cityservice` → `POST /api/v1/cities/{cityId}/guide`
    - 传递Guide数据给CityService保存到Supabase
    - 捕获异常但不影响任务完成

---

### 4. 数据库 (Supabase)

#### 表结构
- ✅ **`database/create_digital_nomad_guides_table.sql`** - 新建

```sql
CREATE TABLE digital_nomad_guides (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL,
    city_name TEXT NOT NULL,
    overview TEXT NOT NULL,
    visa_info JSONB NOT NULL DEFAULT '{}'::jsonb,
    best_areas JSONB NOT NULL DEFAULT '[]'::jsonb,
    workspace_recommendations JSONB NOT NULL DEFAULT '[]'::jsonb,
    tips JSONB NOT NULL DEFAULT '[]'::jsonb,
    essential_info JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 索引
CREATE INDEX idx_digital_nomad_guides_city_id ON digital_nomad_guides(city_id);
CREATE UNIQUE INDEX idx_digital_nomad_guides_city_id_unique ON digital_nomad_guides(city_id);
```

---

## 🚀 部署步骤

### 1. 创建数据库表
在Supabase SQL Editor中执行:
```bash
database/create_digital_nomad_guides_table.sql
```

### 2. 编译后端服务
```bash
cd src/Services/CityService/CityService
dotnet build

cd ../../../AIService/AIService
dotnet build
```

### 3. 部署后端服务
```bash
cd deployment
.\deploy-services-local.ps1
```

### 4. Flutter清理缓存
```bash
cd df_admin_mobile
flutter clean
flutter pub get
```

### 5. 测试流程
1. 启动Flutter应用
2. 进入城市详情页 → Guide Tab
3. 点击生成按钮 → 观察进度
4. 生成完成 → 验证Supabase数据
5. 重新进入Guide Tab → 验证从后端加载

---

## 🔍 API端点

### CityService

#### 获取指南
```http
GET /api/v1/cities/{cityId}/guide
```

**响应示例**:
```json
{
  "success": true,
  "message": "Guide retrieved successfully",
  "data": {
    "id": "guide-123",
    "cityId": "city-456",
    "cityName": "Bangkok",
    "overview": "Great for digital nomads...",
    "visaInfo": { ... },
    "bestAreas": [ ... ],
    "workspaceRecommendations": [ ... ],
    "tips": [ ... ],
    "essentialInfo": { ... },
    "createdAt": "2025-11-11T10:00:00Z",
    "updatedAt": "2025-11-11T10:00:00Z"
  }
}
```

#### 保存指南
```http
POST /api/v1/cities/{cityId}/guide
Content-Type: application/json

{
  "cityId": "city-456",
  "cityName": "Bangkok",
  "overview": "...",
  "visaInfo": { ... },
  "bestAreas": [ ... ],
  "workspaceRecommendations": [ ... ],
  "tips": [ ... ],
  "essentialInfo": { ... }
}
```

---

## 🧪 测试检查清单

- [ ] Supabase表创建成功
- [ ] CityService编译通过
- [ ] AIService编译通过
- [ ] Flutter编译通过
- [ ] GET `/cities/{cityId}/guide` 返回404(初始无数据)
- [ ] 生成Guide成功
- [ ] AIService通过Dapr调用CityService成功
- [ ] Supabase中有新数据
- [ ] GET `/cities/{cityId}/guide` 返回数据
- [ ] Flutter显示Guide内容
- [ ] 切换城市后加载不同Guide
- [ ] 重新生成Guide更新Supabase数据

---

## 📊 数据流程

### 生成流程
```
Flutter → AI后台生成按钮
    ↓
AIService.GenerateDigitalNomadGuideStream
    ↓
生成Guide (DeepSeek AI)
    ↓
通过Dapr调用 → CityService.SaveDigitalNomadGuide
    ↓
Supabase.digital_nomad_guides 插入/更新
    ↓
返回成功 → Flutter显示完成
```

### 加载流程
```
Flutter → 打开Guide Tab
    ↓
controller.loadCityGuide(cityId)
    ↓
GetDigitalNomadGuideUseCase.execute
    ↓
repository.getDigitalNomadGuideFromBackend
    ↓
HTTP GET → CityService /api/v1/cities/{cityId}/guide
    ↓
Supabase查询 digital_nomad_guides
    ↓
返回Guide DTO → Flutter显示
```

---

## ⚠️ 注意事项

1. **数据库Policy**: 当前SQL脚本创建了RLS(Row Level Security),需要确保service_role有写入权限
2. **Dapr依赖**: AIService需要Dapr sidecar才能调用CityService
3. **向后兼容**: 旧的SQLite数据不会自动迁移,用户需要重新生成Guide
4. **缓存策略**: Redis仍然缓存24小时,但主要数据源是Supabase
5. **错误处理**: AIService中Dapr调用失败不会阻塞任务完成,只记录警告日志

---

## 📝 相关文件

### Flutter (df_admin_mobile)
- `lib/features/ai/presentation/controllers/ai_state_controller.dart`
- `lib/features/ai/application/use_cases/ai_use_cases.dart`
- `lib/features/ai/domain/repositories/iai_repository.dart`
- `lib/features/ai/infrastructure/repositories/ai_repository.dart`
- `lib/core/di/dependency_injection.dart`
- `lib/pages/city_detail_page.dart`

### Backend (go-nomads)
- `src/Services/CityService/CityService/Domain/Entities/DigitalNomadGuide.cs`
- `src/Services/CityService/CityService/Domain/Repositories/IDigitalNomadGuideRepository.cs`
- `src/Services/CityService/CityService/Infrastructure/Repositories/SupabaseDigitalNomadGuideRepository.cs`
- `src/Services/CityService/CityService/Application/Services/IDigitalNomadGuideService.cs`
- `src/Services/CityService/CityService/Application/Services/DigitalNomadGuideService.cs`
- `src/Services/CityService/CityService/Application/DTOs/DigitalNomadGuideDto.cs`
- `src/Services/CityService/CityService/API/Controllers/CitiesController.cs`
- `src/Services/CityService/CityService/Program.cs`
- `src/Services/AIService/AIService/API/Services/AIWorkerService.cs`
- `database/create_digital_nomad_guides_table.sql`

---

## 🎉 完成状态

所有代码修改已完成并编译通过!

**下一步**: 在Supabase执行SQL脚本创建表,然后测试完整流程。

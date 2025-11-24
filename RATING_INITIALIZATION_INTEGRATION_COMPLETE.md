# Flutter 评分系统后端初始化集成完成

## 功能概述

已在 Flutter 端集成后端服务初始化评分项的功能。当评分列表为空时，系统会自动调用后端 API 初始化 10 个默认评分项。

## 实现内容

### 1. 后端服务（go-noma）

#### 创建的文件：
- `src/Services/CityService/CityService/Application/Services/RatingCategorySeeder.cs`
  - 评分项初始化服务
  - 包含 10 个默认评分项的定义和创建逻辑

#### 修改的文件：

**Program.cs**
- 注册 `RatingCategorySeeder` 服务

**CityRatingsController.cs**
- 添加 `InitializeDefaultCategories` API endpoint
- 路由: `POST /api/v1/cities/{cityId}/ratings/categories/initialize`
- 注入 `RatingCategorySeeder` 依赖

### 2. Flutter 端（open-platform-app）

#### 修改的文件：

**ICityRatingRepository.dart**
- 添加 `initializeDefaultCategories()` 接口方法

**CityRatingRepository.dart**
- 实现 `initializeDefaultCategories()` 方法
- 调用后端 API: `POST /cities/{cityId}/ratings/categories/initialize`

**CityRatingUseCases.dart**
- 添加 `initializeDefaultCategories()` 用例方法

**CityRatingController.dart**
- 在 `loadCityRatings()` 方法中添加自动初始化逻辑
- 当 `categories.isEmpty` 时自动调用初始化
- 初始化成功后重新加载数据

## 默认评分项列表

系统会自动创建以下 10 个默认评分项：

1. 生活成本 (Cost of Living) - attach_money
2. 天气 (Weather) - wb_sunny
3. 交通 (Transportation) - directions_bus
4. 美食 (Food) - restaurant
5. 安全 (Safety) - security
6. 网络 (Internet) - wifi
7. 娱乐 (Entertainment) - local_activity
8. 医疗 (Healthcare) - local_hospital
9. 友好度 (Friendliness) - people
10. 英语水平 (English Level) - language

## 工作流程

```
1. 用户打开城市详情页的 Scores 标签
   ↓
2. CityRatingsCard 加载数据
   ↓
3. CityRatingController.loadCityRatings(cityId)
   ↓
4. 调用 API: GET /cities/{cityId}/ratings
   ↓
5. 如果 categories.isEmpty:
   a. 调用 API: POST /cities/{cityId}/ratings/categories/initialize
   b. 后端创建 10 个默认评分项
   c. 重新调用 GET /cities/{cityId}/ratings
   ↓
6. 显示评分列表（10 个评分项）
```

## 测试步骤

### 前置条件
1. 确保 Supabase 数据库中 `city_rating_categories` 表为空
2. 重启 City Service（应用新代码）
3. 重启 Flutter 应用

### 测试步骤

1. **打开 Flutter 应用**
   - 登录应用

2. **进入城市详情页**
   - 选择任意城市
   - 切换到 "Scores" 标签页

3. **观察日志**
   ```
   🔍 [CityRatingController] 开始加载评分数据: cityId=xxx
   📡 [CityRatingController] 调用 API 获取评分信息...
   📊 [CityRatingController] API 返回数据:
     - categories: 0 项
     - statistics: 0 项
   ⚠️ [CityRatingController] 没有评分项，开始初始化默认评分项...
   🎬 [CityRatingRepository] 开始初始化默认评分项...
   ✅ [CityRatingRepository] 初始化完成
   ✅ [CityRatingController] 默认评分项初始化成功，重新加载数据...
   📊 [CityRatingController] 重新加载后的数据:
     - categories: 10 项
     - statistics: 10 项
   ✅ [CityRatingController] 评分数据加载完成
   ```

4. **验证结果**
   - 页面显示 10 个评分项
   - 每个评分项显示正确的图标和名称
   - 可以点击星星进行评分
   - 刷新后评分项仍然存在（已持久化到数据库）

### 手动测试 API

如果需要手动触发初始化：

```bash
# 初始化评分项
curl -X POST http://localhost:8002/api/v1/cities/00000000-0000-0000-0000-000000000000/ratings/categories/initialize

# 查看评分项列表
curl http://localhost:8002/api/v1/cities/00000000-0000-0000-0000-000000000000/ratings/categories
```

## 错误处理

- 如果初始化失败，系统会记录错误日志但不会阻塞用户操作
- 用户仍可以手动创建自定义评分项
- 下次加载时会再次尝试初始化（如果列表仍为空）

## 幂等性保证

- `RatingCategorySeeder` 在初始化前会检查是否已存在评分项
- 如果已存在，直接返回成功，不会重复创建
- 确保多次调用初始化 API 不会产生重复数据

## 数据库表结构

确保 Supabase 中存在以下表：

```sql
CREATE TABLE city_rating_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    name_en TEXT,
    description TEXT,
    icon TEXT,
    is_default BOOLEAN DEFAULT false,
    created_by UUID,
    created_at TIMESTAMP DEFAULT now(),
    updated_at TIMESTAMP,
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0
);
```

## 注意事项

1. **权限要求**
   - 初始化 API 不需要认证（系统级操作）
   - 创建自定义评分项需要登录

2. **性能考虑**
   - 初始化只在首次加载时执行一次
   - 后续加载使用缓存数据

3. **未来优化**
   - 可考虑在应用首次启动时后台初始化
   - 可添加管理员手动初始化的入口

## 相关文件

### 后端
- `go-noma/src/Services/CityService/CityService/Application/Services/RatingCategorySeeder.cs`
- `go-noma/src/Services/CityService/CityService/API/Controllers/CityRatingsController.cs`
- `go-noma/src/Services/CityService/CityService/Program.cs`

### Flutter
- `lib/features/city/domain/repositories/icity_rating_repository.dart`
- `lib/features/city/infrastructure/repositories/city_rating_repository.dart`
- `lib/features/city/domain/usecases/city_rating_usecases.dart`
- `lib/features/city/presentation/controllers/city_rating_controller.dart`

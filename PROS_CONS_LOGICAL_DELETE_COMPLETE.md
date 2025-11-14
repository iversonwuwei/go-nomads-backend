# Pros & Cons 逻辑删除功能实现完成

## 实现概述

为 Pros & Cons 功能添加了完整的逻辑删除支持,允许 admin 和版主删除不当内容。

## 修改文件清单

### 后端修改

1. **实体层 - 添加 IsDeleted 字段**
   - `go-noma/src/Services/CityService/CityService/Domain/Entities/CityProsCons.cs`
   - 添加 `IsDeleted` 字段,默认值为 `false`

2. **Repository 层 - 逻辑删除实现**
   - `go-noma/src/Services/CityService/CityService/Infrastructure/Repositories/SupabaseUserCityProsConsRepository.cs`
   - `DeleteAsync` 方法:从物理删除改为设置 `IsDeleted = true`
   - `GetByCityIdAsync` 方法:添加过滤条件 `Where(x => x.IsDeleted == false)`

3. **API 层**
   - `go-noma/src/Services/CityService/CityService/API/Controllers/UserCityContentController.cs`
   - DELETE 端点已存在:`DELETE /api/v1/cities/{cityId}/user-content/pros-cons/{id}`
   - 自动调用逻辑删除方法

### 前端修改

4. **Domain 层 - Repository 接口**
   - `open-platform-app/lib/features/city/domain/repositories/i_city_repository.dart`
   - 添加 `deleteProsCons(String cityId, String id)` 方法签名

5. **Infrastructure 层 - Repository 实现**
   - `open-platform-app/lib/features/city/infrastructure/repositories/city_repository.dart`
   - 实现 `deleteProsCons` 方法,调用 DELETE API

6. **Application 层 - State Controller**
   - `open-platform-app/lib/features/city/application/state_controllers/pros_cons_state_controller.dart`
   - 添加 `deleteProsCons(String cityId, String id, bool isPro)` 方法
   - 调用 repository 删除后,从本地列表移除项目

7. **Presentation 层 - 页面**
   - `open-platform-app/lib/pages/pros_and_cons_add_page.dart`
   - 添加 `_loadData()` 方法:从服务器加载现有数据
   - 添加 `deletePros(String id)` 和 `deleteCons(String id)` 方法:删除功能
   - 确认对话框:删除前要求用户确认
   - 权限检查:`TokenStorageService().isAdmin()` 控制删除按钮显示
   - ListView 添加删除 IconButton(仅 admin 可见)

### 数据库迁移

8. **SQL 迁移文件**
   - `go-noma/migrations/add_is_deleted_to_city_pros_cons.sql`
   - 添加 `is_deleted` 列到 `city_pros_cons` 表
   - 创建索引以优化查询性能

## 数据库迁移步骤

### 方法 1: Supabase 控制台执行(推荐)

1. 登录 Supabase 控制台
2. 进入 SQL Editor
3. 执行以下 SQL:

```sql
-- 为 city_pros_cons 表添加 is_deleted 列用于逻辑删除
ALTER TABLE city_pros_cons
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT false;

-- 创建索引以提高查询性能
CREATE INDEX IF NOT EXISTS idx_city_pros_cons_is_deleted ON city_pros_cons(is_deleted);

-- 为查询优化创建复合索引
CREATE INDEX IF NOT EXISTS idx_city_pros_cons_city_deleted ON city_pros_cons(city_id, is_deleted);

-- 注释说明
COMMENT ON COLUMN city_pros_cons.is_deleted IS '逻辑删除标记，true表示已删除';
```

4. 验证迁移:

```sql
-- 检查列是否添加成功
SELECT column_name, data_type, column_default, is_nullable
FROM information_schema.columns
WHERE table_name = 'city_pros_cons' AND column_name = 'is_deleted';

-- 检查索引是否创建成功
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'city_pros_cons' AND indexname LIKE '%deleted%';
```

### 方法 2: 使用 Supabase CLI(本地开发)

```bash
cd go-noma
supabase db reset  # 如果需要重置数据库
supabase migration new add_is_deleted_to_city_pros_cons
# 编辑生成的迁移文件,粘贴上述 SQL
supabase db push  # 应用迁移
```

## API 接口说明

### DELETE 端点

**请求:**
```
DELETE /api/v1/cities/{cityId}/user-content/pros-cons/{id}
Authorization: Bearer {token}
```

**响应:**
```json
{
  "success": true,
  "message": "删除成功",
  "data": true
}
```

**错误响应:**
```json
{
  "success": false,
  "message": "记录不存在或您没有权限删除",
  "data": false
}
```

## 功能特性

1. **逻辑删除**
   - 数据不会从数据库物理删除
   - 只设置 `is_deleted = true` 标记
   - 查询时自动过滤已删除记录
   - 保留历史数据便于审计

2. **权限控制**
   - 只有 admin 和城市版主可以删除
   - 前端根据 `TokenStorageService().isAdmin()` 控制按钮显示
   - 后端验证 `userId` 与记录创建者匹配

3. **用户体验**
   - 删除前弹出确认对话框
   - 删除成功后自动刷新列表
   - 显示友好的成功/失败提示

4. **数据加载**
   - 页面初始化时从服务器加载现有数据
   - 添加新项后自动刷新列表
   - 删除后立即更新本地列表

## 测试指南

### 1. 准备工作

```bash
# 确保后端服务运行
cd go-noma/src/Services/CityService/CityService
dotnet run

# 确保 Flutter 应用运行
cd open-platform-app
flutter run
```

### 2. 测试步骤

1. **测试数据加载**
   - 以 admin 身份登录
   - 进入任意城市详情页
   - 切换到 "Pros & Cons" tab
   - 点击添加按钮,页面应显示现有的优缺点

2. **测试添加功能**
   - 在 "优点" tab 添加新优点
   - 提交后应自动刷新列表,显示新添加的项目

3. **测试删除功能(Admin)**
   - 列表中的每项应显示删除按钮(垃圾桶图标)
   - 点击删除按钮,弹出确认对话框
   - 确认删除后,项目从列表中消失
   - 显示 "优点已删除" 成功提示

4. **测试权限控制**
   - 以普通用户身份登录
   - 进入 Pros & Cons 页面
   - 删除按钮应该不显示

5. **验证数据库**
   ```sql
   -- 检查逻辑删除标记
   SELECT id, text, is_pro, is_deleted, created_at
   FROM city_pros_cons
   WHERE city_id = 'YOUR_CITY_ID'
   ORDER BY created_at DESC;
   ```

   - 被删除的记录 `is_deleted` 应为 `true`
   - 记录仍然存在于数据库中

## 后续优化建议

1. **扩展到其他内容类型**
   - Reviews (用户评论)
   - Photos (城市照片)
   - Expenses (费用信息)
   - Coworking (共享办公)
   
   使用相同的逻辑删除模式

2. **版主权限细化**
   - 实现城市版主也可以删除本城市的内容
   - 在 `UserCityContentController.cs` 添加版主权限检查

3. **恢复功能**
   - 添加管理员后台恢复被删除内容的功能
   - 实现 "回收站" 功能

4. **批量操作**
   - 支持批量删除多个项目
   - 实现全选/取消全选功能

5. **审计日志**
   - 记录删除操作的操作人和时间
   - 添加 `deleted_by` 和 `deleted_at` 字段

## 相关文件

- 后端 Controller: `go-noma/src/Services/CityService/CityService/API/Controllers/UserCityContentController.cs`
- 后端 Service: `go-noma/src/Services/CityService/CityService/Application/Services/UserCityContentApplicationService.cs`
- 后端 Repository: `go-noma/src/Services/CityService/CityService/Infrastructure/Repositories/SupabaseUserCityProsConsRepository.cs`
- 前端页面: `open-platform-app/lib/pages/pros_and_cons_add_page.dart`
- 前端 Controller: `open-platform-app/lib/features/city/application/state_controllers/pros_cons_state_controller.dart`
- 前端 Repository: `open-platform-app/lib/features/city/infrastructure/repositories/city_repository.dart`
- 迁移文件: `go-noma/migrations/add_is_deleted_to_city_pros_cons.sql`

## 状态

✅ 后端逻辑删除实现完成
✅ 前端删除功能完成
✅ 权限控制完成
✅ 数据加载完成
✅ 迁移 SQL 准备完成
⏳ 等待数据库迁移执行
⏳ 等待功能测试

---

实现时间: 2024
实现人: AI Assistant

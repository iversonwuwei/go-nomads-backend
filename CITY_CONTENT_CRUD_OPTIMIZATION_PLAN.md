# City Content CRUD 优化项目

## 项目目标

优化城市详情页的所有添加页面,实现完整的 CRUD 功能:
1. **加载现有数据** - 页面打开时从服务器加载已有内容
2. **添加新数据** - 保留原有添加功能
3. **删除数据** - admin/版主可以删除不当内容(逻辑删除)
4. **权限控制** - 只有 admin 和版主能看到删除按钮

## 技术要求

- **逻辑删除**:设置 `is_deleted = true`,不物理删除记录
- **权限验证**:前端使用 `TokenStorageService().isAdmin()` 控制 UI,后端验证用户身份
- **用户体验**:删除前弹出确认对话框,操作后显示友好提示

## 实现进度

### ✅ 1. Pros & Cons (优缺点) - 已完成

**文件修改:**
- ✅ 后端实体添加 `IsDeleted` 字段
- ✅ 后端 Repository 实现逻辑删除
- ✅ 后端 API 端点已存在
- ✅ 前端 Repository 接口和实现
- ✅ 前端 Controller 添加删除方法
- ✅ 前端页面添加加载、删除功能和权限控制
- ✅ 数据库迁移 SQL 已准备

**API 端点:**
- `GET /api/v1/cities/{cityId}/user-content/pros-cons?isPro=true` - 获取列表
- `POST /api/v1/cities/{cityId}/user-content/pros-cons` - 添加
- `DELETE /api/v1/cities/{cityId}/user-content/pros-cons/{id}` - 删除

**待办:**
- ⏳ 执行数据库迁移
- ⏳ 端到端测试

**参考文档:** `PROS_CONS_LOGICAL_DELETE_COMPLETE.md`

---

### ⏳ 2. Reviews (用户评论) - 待实现

**数据表:** `user_city_reviews`

**需要实现:**
1. 后端实体添加 `is_deleted` 字段
2. 后端 Repository 实现逻辑删除
3. 检查/添加 DELETE API 端点
4. 前端 Repository 添加 `deleteReview` 方法
5. 前端 Controller 添加删除逻辑
6. 前端页面添加数据加载和删除功能
7. 数据库迁移

**API 端点(预期):**
- `GET /api/v1/cities/{cityId}/user-content/reviews`
- `POST /api/v1/cities/{cityId}/user-content/reviews`
- `DELETE /api/v1/cities/{cityId}/user-content/reviews/{id}`

---

### ⏳ 3. Expenses (费用信息) - 待实现

**数据表:** `user_city_expenses`

**需要实现:**
1. 后端实体添加 `is_deleted` 字段
2. 后端 Repository 实现逻辑删除
3. 检查/添加 DELETE API 端点
4. 前端 Repository 添加 `deleteExpense` 方法
5. 前端 Controller 添加删除逻辑
6. 前端页面添加数据加载和删除功能
7. 数据库迁移

**API 端点(预期):**
- `GET /api/v1/cities/{cityId}/user-content/expenses`
- `POST /api/v1/cities/{cityId}/user-content/expenses`
- `DELETE /api/v1/cities/{cityId}/user-content/expenses/{id}`

---

### ⏳ 4. Photos (城市照片) - 待实现

**数据表:** `user_city_photos`

**需要实现:**
1. 后端实体添加 `is_deleted` 字段
2. 后端 Repository 实现逻辑删除
3. 检查/添加 DELETE API 端点
4. 前端 Repository 添加 `deletePhoto` 方法
5. 前端 Controller 添加删除逻辑
6. 前端页面添加数据加载和删除功能
7. 数据库迁移

**API 端点(预期):**
- `GET /api/v1/cities/{cityId}/user-content/photos`
- `POST /api/v1/cities/{cityId}/user-content/photos`
- `DELETE /api/v1/cities/{cityId}/user-content/photos/{id}`

---

### ⏳ 5. Coworking (共享办公) - 待实现

**数据表:** (需要确认表名)

**需要实现:**
1. 检查数据表结构
2. 后端实体添加 `is_deleted` 字段
3. 后端 Repository 实现逻辑删除
4. 检查/添加 DELETE API 端点
5. 前端 Repository 添加 `deleteCoworking` 方法
6. 前端 Controller 添加删除逻辑
7. 前端页面添加数据加载和删除功能
8. 数据库迁移

---

## 统一数据库迁移

所有表都需要添加以下字段和索引:

```sql
-- 添加 is_deleted 列
ALTER TABLE {table_name}
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT false;

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_{table_name}_is_deleted ON {table_name}(is_deleted);
CREATE INDEX IF NOT EXISTS idx_{table_name}_city_deleted ON {table_name}(city_id, is_deleted);

-- 添加注释
COMMENT ON COLUMN {table_name}.is_deleted IS '逻辑删除标记，true表示已删除';
```

## 实现模式(Pattern)

### 1. 后端实体

```csharp
[Column("is_deleted")]
public bool IsDeleted { get; set; } = false;
```

### 2. 后端 Repository

```csharp
// 查询时过滤已删除
.Where(x => x.IsDeleted == false)

// 删除时设置标记
public async Task<bool> DeleteAsync(Guid id, Guid userId)
{
    var entity = await GetByIdAsync(id);
    if (entity == null || entity.UserId != userId)
        return false;
    
    entity.IsDeleted = true;
    entity.UpdatedAt = DateTime.UtcNow;
    
    await SupabaseClient
        .From<TEntity>()
        .Where(x => x.Id == id)
        .Update(entity);
    
    return true;
}
```

### 3. 后端 Controller

```csharp
[HttpDelete("{resourceType}/{id}")]
public async Task<ActionResult<ApiResponse<bool>>> Delete(string cityId, Guid id)
{
    var userId = GetUserId();
    var success = await _service.DeleteAsync(userId, id);
    
    if (!success)
        return NotFound(new ApiResponse<bool> { Success = false, Message = "记录不存在或无权删除" });
    
    return Ok(new ApiResponse<bool> { Success = true, Message = "删除成功", Data = true });
}
```

### 4. 前端 Repository Interface

```dart
/// 删除资源（逻辑删除）
Future<Result<void>> deleteResource(String cityId, String id);
```

### 5. 前端 Repository Implementation

```dart
@override
Future<Result<void>> deleteResource(String cityId, String id) async {
  try {
    final endpoint = '$_baseUrl/$cityId/user-content/{resource}/$id';
    await _httpService.delete(endpoint);
    return const Success(null);
  } on HttpException catch (e) {
    return Failure(_convertHttpException(e));
  } catch (e) {
    return Failure(UnknownException('删除失败: ${e.toString()}'));
  }
}
```

### 6. 前端 Controller

```dart
/// 删除资源（逻辑删除）
Future<bool> deleteResource(String cityId, String id) async {
  isLoading.value = true;
  error.value = null;

  try {
    final result = await _repository.deleteResource(cityId, id);
    
    return result.fold(
      onSuccess: (_) {
        resourceList.removeWhere((item) => item.id == id);
        return true;
      },
      onFailure: (err) {
        error.value = err.message;
        return false;
      },
    );
  } finally {
    isLoading.value = false;
  }
}
```

### 7. 前端页面

```dart
// 初始化时加载数据
@override
void initState() {
  super.initState();
  _loadData();
}

Future<void> _loadData() async {
  final controller = Get.find<ResourceController>();
  await controller.loadResources(widget.cityId);
  // 更新本地列表
}

// 删除方法
Future<void> deleteResource(String id) async {
  final confirmed = await Get.dialog<bool>(
    AlertDialog(
      title: const Text('确认删除'),
      content: const Text('确定要删除吗？'),
      actions: [
        TextButton(onPressed: () => Get.back(result: false), child: const Text('取消')),
        TextButton(
          onPressed: () => Get.back(result: true),
          style: TextButton.styleFrom(foregroundColor: Colors.red),
          child: const Text('删除'),
        ),
      ],
    ),
  );

  if (confirmed != true) return;

  final controller = Get.find<ResourceController>();
  final success = await controller.deleteResource(widget.cityId, id);

  if (success) {
    Get.snackbar('成功', '删除成功', backgroundColor: Colors.green[100]);
    await _loadData();
  } else {
    Get.snackbar('失败', '删除失败，请重试');
  }
}

// ListView 添加删除按钮
FutureBuilder<bool>(
  future: TokenStorageService().isAdmin(),
  builder: (context, snapshot) {
    final canDelete = snapshot.data == true;
    if (canDelete) {
      return IconButton(
        icon: const Icon(Icons.delete_outline),
        onPressed: () => deleteResource(item['id']),
        color: Colors.red,
      );
    }
    return const SizedBox.shrink();
  },
)
```

## 测试检查清单

对每个功能模块测试:

- [ ] 页面打开时正确加载现有数据
- [ ] 添加新数据后列表自动刷新
- [ ] Admin 用户能看到删除按钮
- [ ] 普通用户不能看到删除按钮
- [ ] 删除前显示确认对话框
- [ ] 删除成功后列表更新
- [ ] 删除成功后显示成功提示
- [ ] 数据库中 `is_deleted` 标记正确设置
- [ ] 查询时不返回已删除的记录

## 执行顺序

1. ✅ **Pros & Cons** - 作为原型完成(待测试)
2. ⏳ **Reviews** - 复用相同模式
3. ⏳ **Expenses** - 复用相同模式
4. ⏳ **Photos** - 复用相同模式
5. ⏳ **Coworking** - 复用相同模式

## 数据库迁移执行

执行所有迁移前,建议先备份数据库:

```sql
-- Supabase 控制台 SQL Editor
-- 1. 备份关键表(可选)
CREATE TABLE city_pros_cons_backup AS SELECT * FROM city_pros_cons;

-- 2. 执行迁移(分别为每个表)
-- 详见 migrations/ 目录下的 SQL 文件

-- 3. 验证迁移
SELECT table_name, column_name
FROM information_schema.columns
WHERE column_name = 'is_deleted';
```

## 总结

- **当前进度**: 1/5 功能完成
- **预计时间**: 每个功能约 1-2 小时
- **总工作量**: 约 8-10 小时
- **已完成工作**: Pros & Cons 逻辑删除(100%)

---

最后更新: 2024
项目负责人: AI Assistant

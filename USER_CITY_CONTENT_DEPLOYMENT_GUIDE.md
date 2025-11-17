# 🎉 用户城市内容系统集成 - 部署指南

## ✅ 已完成的工作

### 后端 (CityService)

1. **文件已创建并移动到正确位置:**
   - ✅ `UserCityContentDTOs.cs` → `/src/Services/CityService/CityService/DTOs/`
   - ✅ `UserCityContentService.cs` → `/src/Services/CityService/CityService/Services/`
   - ✅ `UserCityContentController.cs` → `/src/Services/CityService/CityService/API/`

2. **代码集成完成:**
   - ✅ 命名空间已更新为 `CityService.*`
   - ✅ `Program.cs` 已注册 `IUserCityContentService`
   - ✅ `CityService.csproj` 已添加 `Npgsql` 包引用
   - ✅ 项目构建成功 ✓
   - ✅ Docker 镜像已重新构建 ✓
   - ✅ 容器已重启并运行 ✓

3. **API 端点 (http://localhost:8002):**
   ```
   GET    /api/cities/{cityId}/user-content/photos
   POST   /api/cities/{cityId}/user-content/photos
   DELETE /api/cities/{cityId}/user-content/photos/{photoId}
   
   GET    /api/cities/{cityId}/user-content/expenses
   POST   /api/cities/{cityId}/user-content/expenses
   DELETE /api/cities/{cityId}/user-content/expenses/{expenseId}
   
   GET    /api/cities/{cityId}/user-content/reviews
   POST   /api/cities/{cityId}/user-content/reviews
   GET    /api/cities/{cityId}/user-content/reviews/mine
   DELETE /api/cities/{cityId}/user-content/reviews
   
   GET    /api/cities/{cityId}/user-content/stats
   
   GET    /api/user/city-content/photos
   GET    /api/user/city-content/expenses
   ```

### 前端 (Flutter)

1. **数据模型:**
   - ✅ `user_city_content_models.dart` (UserCityPhoto, UserCityExpense, UserCityReview, CityUserContentStats)

2. **API 服务:**
   - ✅ `user_city_content_api_service.dart` (完整的 CRUD 操作)

---

## ⚠️ 需要手动完成的步骤

### 步骤 0: 配置 AMap API Key (新)

批量照片上传会调用高德地图地理编码接口来补全经纬度和地址信息。在本地或测试环境中，请先在 `src/Services/CityService/CityService/appsettings.Development.json` 中设置 `Amap.ApiKey`，或通过环境变量/KeyVault 覆盖 `Amap:ApiKey`。

```json
   "Amap": {
      "ApiKey": "9194496314986698ad76d755f6349325",
      "GeocodeEndpoint": "https://restapi.amap.com/v3/geocode/geo"
   }
```

> ⚠️ 生产环境请通过安全的 Secret 管理方案提供密钥，不要将真实 Key 提交到版本库中。

### 步骤 1: 执行数据库迁移 (必须!)

**方式 1: Supabase SQL Editor (推荐)**

1. 打开浏览器访问: https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao
2. 登录 Supabase
3. 左侧菜单选择 **SQL Editor**
4. 点击 **"+ New Query"**
5. 复制粘贴以下文件的全部内容:
   ```
   /Users/walden/Workspaces/WaldenProjects/go-noma/database/migrations/create_user_city_content_tables.sql
   ```
6. 点击 **"Run"** 按钮执行
7. 看到成功提示即可

**方式 2: 使用 DBeaver/pgAdmin**

连接信息:
- Host: `db.lcfbajrocmjlqndkrsao.supabase.co`
- Port: `6543`
- Database: `postgres`
- Username: `postgres.lcfbajrocmjlqndkrsao`
- Password: `bwTyaM1eJ1TRIZI3`
- SSL Mode: Require

打开 SQL 文件执行即可。

---

### 步骤 2: 验证迁移成功

在 Supabase SQL Editor 中执行:

```sql
-- 检查表是否创建成功
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
  AND table_name LIKE 'user_city_%'
ORDER BY table_name;
```

应该看到:
- user_city_expenses
- user_city_photos
- user_city_reviews

---

### 步骤 3: 测试 API

执行测试脚本:
```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma
chmod +x test-user-city-content-api.sh
./test-user-city-content-api.sh
```

或者手动测试单个端点:
```bash
# 获取统计 (应返回全0的空统计)
curl "http://localhost:8002/api/cities/bangkok-thailand/user-content/stats"

# 获取评论列表 (应返回空数组)
curl "http://localhost:8002/api/cities/bangkok-thailand/user-content/reviews"
```

**预期结果:**
- 迁移前: `{"error": "获取统计失败", "details": "..."}`
- 迁移后: `{"cityId": "bangkok-thailand", "photoCount": 0, ...}`

---

## 📱 下一步: Flutter UI 开发

迁移成功后,可以开发 Flutter 页面:

### 页面需求

1. **添加照片页面** (`add_photo_page.dart`)
   - 图片选择 (image_picker)
   - 说明输入
   - 地点输入
   - 时间选择

2. **添加费用页面** (`add_expense_page.dart`)
   - 分类选择 (Dropdown)
   - 金额输入
   - 货币选择
   - 日期选择
   - 描述输入

3. **添加评论页面** (`add_review_page.dart`)
   - 星级评分 (Rating widget)
   - 标题输入
   - 内容输入 (多行)
   - 访问日期选择

4. **集成到 city_detail_page.dart**
   - 在 Photos/Expenses/Reviews Tab 添加 FAB
   - 显示用户内容列表
   - 支持编辑/删除

### 示例代码

```dart
// 在 city_detail_page.dart 的 Photos Tab
FloatingActionButton(
  onPressed: () async {
    final result = await Get.to(() => AddPhotoPage(cityId: city.id));
    if (result == true) {
      _loadPhotos(); // 刷新列表
    }
  },
  child: Icon(Icons.add_photo_alternate),
)

// 添加照片
final service = UserCityContentApiService();
try {
  await service.addCityPhoto(
    cityId: cityId,
    imageUrl: imageUrl,
    caption: caption,
  );
  Get.back(result: true);
  Get.snackbar('Success', 'Photo added!');
} catch (e) {
  Get.snackbar('Error', e.toString());
}
```

---

## 🔧 故障排除

### API 返回 401 Unauthorized
- ✓ 正常! 说明认证机制工作正常
- 需要先登录获取 JWT token
- 确保 HttpService 已设置 authToken

### API 返回 "Failed to connect to database"
- ❌ 数据库迁移未执行
- 请按照 **步骤 1** 执行迁移

### Docker 容器无法启动
```bash
# 查看日志
docker logs go-nomads-city-service --tail 50

# 重启容器
docker restart go-nomads-city-service
```

---

## 📊 架构总结

```
Flutter App (用户端)
    ↓ HTTP/JWT
CityService (Go Nomads 后端)
    ↓ Npgsql
Supabase PostgreSQL (数据存储)
    ├─ user_city_photos (照片)
    ├─ user_city_expenses (费用)
    ├─ user_city_reviews (评论)
    └─ RLS 策略 (安全控制)
```

**独立表设计的优势:**
- ✅ 结构清晰,易于维护
- ✅ 性能优化 (独立索引)
- ✅ 灵活扩展 (添加字段不影响其他表)
- ✅ RLS 策略精细控制

---

## ✅ 检查清单

- [x] 后端代码集成到 CityService
- [x] 命名空间正确更新
- [x] Npgsql 包已添加
- [x] Program.cs 服务已注册
- [x] Docker 镜像已重构建
- [x] 容器已重启
- [x] Flutter 数据模型已创建
- [x] Flutter API 服务已创建
- [ ] **数据库迁移已执行** ⬅️ **当前待完成**
- [ ] API 端点已测试
- [ ] Flutter UI 页面开发
- [ ] 完整流程测试

---

**当前状态:** 后端和前端代码已完成,等待数据库迁移后即可测试 API 并开发 UI。

**下一个行动:** 在 Supabase SQL Editor 中执行 `create_user_city_content_tables.sql`

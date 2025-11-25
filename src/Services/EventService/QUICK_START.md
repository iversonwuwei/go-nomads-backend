# 聚会类型 API 快速开始

## 🚀 5分钟快速部署

### 步骤 1: 创建数据库表

**方式 A - 在 Supabase 控制台执行（推荐）**
1. 打开 Supabase 项目
2. 进入 SQL Editor
3. 复制 `src/Services/EventService/EventService/Database/quick-create-event-types.sql` 的内容
4. 粘贴并执行
5. 看到 "✅ 聚会类型表创建成功！已插入 20 个预设类型。"

**方式 B - 使用 PowerShell 脚本**
```powershell
# 设置数据库连接
$env:SUPABASE_DB_URL = "postgresql://postgres.xxx:password@host:port/postgres"

# 执行脚本
./execute-event-types-migration.ps1
```

### 步骤 2: 启动 EventService

```powershell
cd src/Services/EventService/EventService
dotnet run
```

等待看到：
```
✅ EventService 启动成功
🌐 运行在: http://localhost:8005
📖 API 文档: http://localhost:8005/scalar/v1
```

### 步骤 3: 测试 API

```powershell
# 回到项目根目录
cd ../../../../../

# 运行测试脚本
./test-event-types.ps1
```

预期输出：
```
✅ 成功获取聚会类型列表
总数: 20

前 5 个聚会类型:
  ID: xxx
  中文名: 社交网络
  英文名: Networking
  描述: 商务社交和职业发展
  ...
```

## 📱 移动端集成

### 更新 Flutter 代码

修改 `df_admin_mobile/lib/pages/create_meetup_page.dart` 中的 `_loadMeetupTypes()` 方法：

```dart
Future<void> _loadMeetupTypes() async {
  setState(() {
    _isLoadingTypes = true;
  });
  
  try {
    // 调用后端 API
    final response = await dioClient.get('/api/events/types');
    final data = response.data['data'] as List;
    
    // 根据当前语言选择显示名称
    final localeCode = Localizations.localeOf(context).languageCode;
    _meetupTypes = data.map((item) {
      return localeCode == 'zh' ? item['name'] : item['enName'];
    }).toList().cast<String>();
    
  } catch (e) {
    print('加载聚会类型失败: $e');
    // 失败时使用默认值
    _meetupTypes = ['Networking', 'Social Gathering', 'Workshop'];
  } finally {
    setState(() {
      _isLoadingTypes = false;
    });
  }
}
```

## ✅ 验证

### 测试 API 响应

```powershell
# 测试获取类型列表
curl http://localhost:8005/api/v1/event-types

# 预期响应
{
  "success": true,
  "message": "获取聚会类型列表成功",
  "data": [
    {
      "id": "xxx",
      "name": "社交网络",
      "enName": "Networking",
      "description": "商务社交和职业发展",
      "sortOrder": 1,
      "isActive": true,
      "isSystem": true
    },
    ...
  ]
}
```

### 检查数据库

```sql
-- 查询所有类型
SELECT * FROM event_types ORDER BY sort_order;

-- 统计数量
SELECT COUNT(*) FROM event_types WHERE is_active = TRUE;
-- 应该返回: 20
```

## 🎯 完成！

现在你可以：
- ✅ 在移动端获取聚会类型列表
- ✅ 根据用户语言显示对应名称
- ✅ 支持管理员添加自定义类型
- ✅ 所有类型数据持久化到数据库

## 📞 遇到问题？

### 常见问题

**Q: 数据库连接失败？**
A: 检查 `appsettings.json` 中的 Supabase 配置

**Q: API 返回 404？**
A: 确认 EventService 已启动且运行在 8005 端口

**Q: 移动端获取不到数据？**
A: 检查 Gateway 路由配置，确保 `/api/events/types` 路由到 EventService

**Q: 数据重复插入？**
A: 使用 `ON CONFLICT DO NOTHING` 已处理，可安全重复执行

### 查看日志

```powershell
# EventService 日志
cat src/Services/EventService/EventService/logs/eventservice-*.txt
```

## 📚 相关文档

- 完整文档: `src/Services/EventService/EVENT_TYPES_IMPLEMENTATION.md`
- API 文档: `http://localhost:8005/scalar/v1`
- 数据库脚本: `src/Services/EventService/EventService/Database/`

# 城市名称英文化方案

## 概述
将数据库中的中文城市名称转换为英文，通过前端国际化实现中英文显示。

## 优势
1. ✅ **天气API更准确**：英文城市名称被OpenWeatherMap更好地识别
2. ✅ **国际化友好**：方便多语言扩展
3. ✅ **统一数据格式**：所有城市使用英文名称作为标准
4. ✅ **前端灵活显示**：根据用户语言设置显示对应的城市名称

## 实施步骤

### 1. 执行数据库迁移

```bash
# 连接到Supabase数据库
psql "postgresql://postgres.lcfbajrocmjlqndkrsao:bwTyaM1eJ1TRIZI3@aws-0-us-west-1.pooler.supabase.com:6543/postgres"

# 或者使用本地文件执行
psql "postgresql://postgres.lcfbajrocmjlqndkrsao:bwTyaM1eJ1TRIZI3@aws-0-us-west-1.pooler.supabase.com:6543/postgres" < /Users/walden/Workspaces/WaldenProjects/go-noma/database/migrations/convert_city_names_to_english.sql
```

### 2. 验证数据转换

```sql
-- 查看转换后的城市名称
SELECT name, country, COUNT(*) as count
FROM cities 
WHERE country = 'China'
GROUP BY name, country
ORDER BY name
LIMIT 20;

-- 预期结果：所有城市名称都是英文
```

### 3. 前端国际化配置

已创建的文件：
- `/Users/walden/Workspaces/WaldenProjects/open-platform-app/lib/l10n/city_names_zh.json` - 中文映射
- `/Users/walden/Workspaces/WaldenProjects/open-platform-app/lib/l10n/city_names_en.json` - 英文映射

### 4. 前端使用示例

```dart
// 在Flutter中使用城市名称翻译
import 'dart:convert';
import 'package:flutter/services.dart';

class CityNameHelper {
  static Map<String, String>? _cityNames;
  
  static Future<void> loadCityNames(String locale) async {
    final fileName = locale == 'zh' ? 'city_names_zh.json' : 'city_names_en.json';
    final jsonString = await rootBundle.loadString('lib/l10n/$fileName');
    final jsonData = json.decode(jsonString);
    _cityNames = Map<String, String>.from(jsonData['cityNames']);
  }
  
  static String getLocalizedCityName(String englishName) {
    return _cityNames?[englishName] ?? englishName;
  }
}

// 使用方法
void main() async {
  await CityNameHelper.loadCityNames('zh');
  
  // 从API获取的英文名称
  String apiCityName = 'Beijing';
  
  // 显示本地化名称
  String displayName = CityNameHelper.getLocalizedCityName(apiCityName);
  print(displayName); // 输出: 北京
}
```

### 5. Widget使用示例

```dart
// 创建一个城市名称显示Widget
class LocalizedCityName extends StatelessWidget {
  final String cityName;
  
  const LocalizedCityName({required this.cityName});
  
  @override
  Widget build(BuildContext context) {
    return FutureBuilder(
      future: CityNameHelper.loadCityNames(
        Localizations.localeOf(context).languageCode
      ),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.done) {
          return Text(CityNameHelper.getLocalizedCityName(cityName));
        }
        return Text(cityName); // 加载中显示英文名称
      },
    );
  }
}

// 使用
LocalizedCityName(cityName: 'Beijing')  // 会显示"北京"
```

## 影响范围

### 后端（无需修改）
- ✅ 数据库：cities表的name字段从中文改为英文
- ✅ API返回：cityName将返回英文名称
- ✅ 天气API：使用英文名称调用，更准确

### 前端（需要适配）
需要修改的页面：
1. **城市列表页面** - 使用`CityNameHelper.getLocalizedCityName()`
2. **城市详情页面** - 使用`LocalizedCityName` Widget
3. **搜索结果页面** - 显示翻译后的城市名称
4. **收藏城市列表** - 显示翻译后的城市名称
5. **旅行计划页面** - 显示翻译后的城市名称

## 回滚方案

如果需要回滚，执行以下SQL：

```sql
-- 回滚示例（北京）
UPDATE cities SET name = '北京市' WHERE name = 'Beijing';
UPDATE cities SET name = '上海市' WHERE name = 'Shanghai';
-- ... 其他城市
```

## 注意事项

1. ⚠️ **执行前备份数据库**
2. ⚠️ **先在测试环境验证**
3. ⚠️ **前端需要同步更新**才能正确显示中文
4. ⚠️ **缓存清理**：执行后可能需要清理Redis缓存

## 执行清单

- [ ] 备份Supabase数据库
- [ ] 执行SQL迁移脚本
- [ ] 验证数据转换结果
- [ ] 前端集成国际化文件
- [ ] 更新所有显示城市名称的页面
- [ ] 测试中英文切换
- [ ] 验证天气数据获取是否正常
- [ ] 清理缓存
- [ ] 部署到生产环境

## 测试验证

```bash
# 1. 测试API返回
curl "http://localhost:5000/api/v1/cities?pageSize=3" | jq '.data.items[0].name'
# 预期：返回英文名称如 "Beijing"

# 2. 测试天气数据
curl "http://localhost:5000/api/v1/cities/search?country=China&pageSize=1" | jq '.data[0] | {name, weather}'
# 预期：name为英文，weather数据正常

# 3. 测试前端显示
# 启动Flutter应用，检查城市名称是否显示为中文
```

## 完成时间
预计执行时间：30分钟（包括验证）

## 联系人
如有问题，请联系开发团队。

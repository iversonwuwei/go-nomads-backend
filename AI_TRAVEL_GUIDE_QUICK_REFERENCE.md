# AI 生成旅游指南 - 快速参考

## 🎯 功能概述
使用 Qwen AI 为数字游民生成城市旅游指南,包括签证信息、居住区域、工作空间推荐、实用建议等。

## 📡 API 接口

### 生成旅游指南
```http
POST /api/v1/ai/travel-guide
Content-Type: application/json

{
  "cityId": "chiang-mai-thailand",
  "cityName": "清迈"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "旅游指南生成成功",
  "data": {
    "cityId": "chiang-mai-thailand",
    "cityName": "清迈",
    "overview": "城市概述...",
    "visaInfo": {
      "type": "旅游签证",
      "duration": 60,
      "requirements": "护照、照片...",
      "cost": 40,
      "process": "申请流程..."
    },
    "bestAreas": ["宁曼路", "古城区", "素贴山"],
    "workspaceRecommendations": ["Camp Coworking", "Punspace", "咖啡馆推荐"],
    "tips": ["建议1", "建议2", "建议3", "建议4", "建议5"],
    "essentialInfo": {
      "SIM卡": "购买和使用建议",
      "银行开户": "开户建议",
      "交通": "交通方式",
      "医疗": "医疗建议",
      "网络": "网络情况",
      "语言": "语言使用",
      "安全": "安全提示",
      "社区": "社区信息"
    }
  }
}
```

## 🔧 前端使用

### 1. 调用服务生成指南
```dart
import 'package:df_admin_mobile/services/ai_api_service.dart';
import 'package:df_admin_mobile/models/city_detail_model.dart';

final aiService = AiApiService();

// 生成指南
final guideData = await aiService.generateDigitalNomadGuide(
  cityId: 'chiang-mai-thailand',
  cityName: '清迈',
);

// 解析数据
final guide = DigitalNomadGuide.fromJson(guideData);
```

### 2. 在 Controller 中使用
```dart
class CityDetailController extends GetxController {
  final guide = Rx<DigitalNomadGuide?>(null);
  final isLoadingGuide = false.obs;

  Future<void> generateGuideWithAI() async {
    isLoadingGuide.value = true;
    try {
      final aiService = AiApiService();
      final guideData = await aiService.generateDigitalNomadGuide(
        cityId: currentCityId.value,
        cityName: currentCityName.value,
      );
      guide.value = DigitalNomadGuide.fromJson(guideData);
      AppToast.success('AI 指南生成成功!');
    } catch (e) {
      AppToast.error('生成指南失败: $e');
    } finally {
      isLoadingGuide.value = false;
    }
  }
}
```

### 3. UI 显示
```dart
Obx(() {
  if (controller.isLoadingGuide.value) {
    return const Center(
      child: Text('🤖 AI 正在生成旅游指南...'),
    );
  }

  if (controller.guide.value == null) {
    return Center(
      child: ElevatedButton.icon(
        onPressed: () => controller.generateGuideWithAI(),
        icon: const Icon(Icons.auto_awesome),
        label: const Text('AI 生成旅游指南'),
      ),
    );
  }

  final guide = controller.guide.value!;
  return Column(
    children: [
      // 重新生成按钮
      TextButton.icon(
        onPressed: () => controller.generateGuideWithAI(),
        icon: const Icon(Icons.refresh),
        label: const Text('AI 重新生成'),
      ),
      // 显示指南内容
      Text(guide.overview),
      // ... 其他内容
    ],
  );
})
```

## 📊 数据结构

### DigitalNomadGuide
```dart
class DigitalNomadGuide {
  final String cityId;           // 城市ID
  final String cityName;         // 城市名称
  final String overview;         // 概述 (200-300字)
  final VisaInfo visaInfo;       // 签证信息
  final List<String> bestAreas;  // 推荐居住区域 (3个)
  final List<String> workspaceRecommendations;  // 工作空间推荐 (2-3个)
  final List<String> tips;       // 实用建议 (5个)
  final Map<String, String> essentialInfo;  // 必要信息字典
}
```

### VisaInfo
```dart
class VisaInfo {
  final String type;        // 签证类型
  final int duration;       // 有效天数
  final String requirements; // 申请要求
  final double cost;        // 费用(美元)
  final String process;     // 申请流程
}
```

## ⚙️ 配置说明

### 后端配置 (appsettings.json)
```json
{
  "Qwen": {
    "ApiKey": "your-api-key",
    "BaseUrl": "https://dashscope.aliyuncs.com/compatible-mode/v1"
  },
  "SemanticKernel": {
    "DefaultModel": "qwen-plus"
  }
}
```

### 前端配置
- **超时时间**: 3分钟 (充足的 AI 生成时间)
- **API 路径**: `/ai/travel-guide`
- **HTTP 方法**: POST

## 🚀 测试命令

### 使用 curl 测试
```bash
curl -X POST http://localhost:5003/api/v1/ai/travel-guide \
  -H "Content-Type: application/json" \
  -d '{
    "cityId": "chiang-mai-thailand",
    "cityName": "清迈"
  }'
```

### 使用 PowerShell 测试
```powershell
$body = @{
    cityId = "chiang-mai-thailand"
    cityName = "清迈"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5003/api/v1/ai/travel-guide" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"
```

## ⏱️ 性能指标

- **Token 限制**: 2000 tokens
- **预期响应时间**: 5-30 秒
- **超时设置**: 前端 3分钟, 后端 5分钟
- **重试次数**: 最多 3 次

## 🔍 常见问题

### Q: 生成失败怎么办?
A: 检查以下内容:
1. Qwen API Key 是否配置正确
2. 网络连接是否正常
3. 查看后端日志中的详细错误信息

### Q: 超时怎么办?
A: 
1. 检查网络连接
2. 增加超时时间
3. 查看 AI 服务是否正常

### Q: 如何优化生成质量?
A: 
1. 调整 Prompt 内容
2. 修改 temperature 参数 (当前 0.7)
3. 增加示例数据

## 📝 日志示例

### 成功日志
```
📖 开始生成数字游民旅游指南 - 城市: 清迈, 用户ID: xxx
🤖 调用 Qwen AI 生成旅游指南...
✅ AI 响应接收完成，长度: 2345
✅ 数字游民旅游指南生成成功 - 城市: 清迈
```

### 错误日志
```
❌ 生成数字游民旅游指南失败，城市: 清迈
System.Net.Http.HttpRequestException: Connection refused
```

## 🔗 相关文档

- [完整实现文档](./AI_TRAVEL_GUIDE_IMPLEMENTATION.md)
- [API 文档](./API_DOCUMENTATION.md)
- [前端架构](./FRONTEND_ARCHITECTURE.md)

---

**更新日期**: 2024
**维护者**: AI Travel Guide Team

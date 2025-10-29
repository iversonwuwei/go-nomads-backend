# Travel Plan 流式生成 - 快速参考 🚀

## 🎯 核心改进

**问题**: AI 生成耗时长 (30s-2min),用户体验差
**方案**: Server-Sent Events 流式响应 + 实时进度显示

---

## 📡 API 端点

### 流式端点 (新增)
```
POST http://localhost:8009/api/ai/travel-plan/stream
Content-Type: application/json
Accept: text/event-stream
```

### 同步端点 (保留)
```
POST http://localhost:8009/api/ai/travel-plan
Content-Type: application/json
Accept: application/json
```

---

## 📊 SSE 事件类型

| 事件类型 | 进度 | 说明 |
|---------|------|------|
| `start` | 0% | 开始生成旅行计划 |
| `analyzing` | 10% | 正在分析需求 |
| `generating` | 30% | AI 正在生成行程 |
| `success` | 100% | 生成成功 (包含完整数据) |
| `error` | 0% | 生成失败 |

---

## 🧪 快速测试

### 1. 测试流式 API
```powershell
cd e:\Workspaces\WaldenProjects\go-nomads
.\test-travel-plan-stream.ps1
```

### 2. 预期输出
```
[10:30:00.123] 🚀 START: 开始生成旅行计划... (进度: 0%)
[10:30:00.456] 🔍 ANALYZING: 正在分析您的需求... (进度: 10%)
[10:30:01.789] ⚙️  GENERATING: AI 正在生成行程安排... (进度: 30%)
[10:30:45.012] ✅ SUCCESS: 旅行计划生成成功! (进度: 100%)
```

---

## 📱 Flutter 使用示例

### 调用流式 API
```dart
final controller = Get.find<CityDetailController>();

await controller.generateTravelPlanStream(
  duration: 3,
  budget: 'medium',
  travelStyle: 'culture',
  interests: ['历史文化', '美食'],
  
  // 实时进度回调
  onProgress: (String message, int progress) {
    print('进度: $progress% - $message');
    setState(() {
      _progressMessage = message;
      _progressValue = progress;
    });
  },
  
  // 完成回调
  onData: (TravelPlan plan) {
    print('生成成功: ${plan.id}');
    setState(() {
      _plan = plan;
      _isLoading = false;
    });
  },
  
  // 错误回调
  onError: (String error) {
    print('生成失败: $error');
    AppToast.error(error);
  },
);
```

---

## 📁 修改文件

### 后端
- ✅ `ChatController.cs` - 添加 `GenerateTravelPlanStream()`

### 前端
- ✅ `ai_api_service.dart` - 添加 `generateTravelPlanStream()`
- ✅ `city_detail_controller.dart` - 添加 `generateTravelPlanStream()`
- ✅ `travel_plan_page.dart` - 使用流式生成 + 显示实时进度

### 测试
- ✅ `test-travel-plan-stream.ps1` - PowerShell 测试脚本

---

## 🔧 关键代码片段

### 后端发送进度
```csharp
private async Task SendProgressEvent(string eventType, object data)
{
    var json = JsonSerializer.Serialize(new
    {
        type = eventType,
        timestamp = DateTime.UtcNow,
        payload = data
    });

    var message = $"data: {json}\n\n";
    var bytes = Encoding.UTF8.GetBytes(message);
    await Response.Body.WriteAsync(bytes);
    await Response.Body.FlushAsync();
}
```

### 前端解析 SSE
```dart
await for (final Uint8List data in response.data!.stream) {
  final chunk = utf8.decode(data);
  buffer += chunk;

  while (buffer.contains('\n\n')) {
    final index = buffer.indexOf('\n\n');
    final message = buffer.substring(0, index);
    buffer = buffer.substring(index + 2);

    if (message.startsWith('data: ')) {
      final jsonStr = message.substring(6).trim();
      final event = json.decode(jsonStr);
      // 处理事件...
    }
  }
}
```

---

## ✅ 测试检查清单

- [ ] 后端流式 API 响应正常
- [ ] 前端正确接收 SSE 事件
- [ ] UI 实时显示进度
- [ ] 最终数据完整
- [ ] 错误正确处理
- [ ] 超时配置合理 (5 分钟)

---

## 📚 详细文档

完整实现细节请查看: `TRAVEL_PLAN_STREAM_OPTIMIZATION.md`

---

**更新时间**: 2024-01-15
**状态**: ✅ 已完成

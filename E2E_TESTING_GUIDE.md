# 端到端测试指南

## 🎯 完整测试流程

### 第一步: 启动后端服务

```powershell
# 1. 进入项目目录
cd E:\Workspaces\WaldenProjects\go-nomads

# 2. 启动基础设施服务
docker-compose up -d redis rabbitmq

# 3. 等待5秒,确保服务就绪
Start-Sleep -Seconds 5

# 4. 启动 AI Service
docker-compose up -d ai-service

# 5. 检查服务状态
docker ps | Select-String "rabbitmq|redis|ai-service"
```

### 第二步: 验证服务

```powershell
# 1. 检查 AI Service 日志
docker logs --tail 20 go-nomads-ai-service

# 应该看到:
# ✅ AI Worker Service 已启动
# ⏳ AI Worker Service 正在等待任务...
# ✅ 消息队列、缓存和后台服务已注册

# 2. 访问 RabbitMQ 管理界面
Start-Process "http://localhost:15672"
# 登录: guest / guest
# 查看: Queues -> 应该有 travel-plan-tasks 队列

# 3. 测试 Redis
docker exec -it go-nomads-redis redis-cli ping
# 应该返回: PONG
```

### 第三步: 测试后端 API

```powershell
# 运行测试脚本
cd E:\Workspaces\WaldenProjects\go-nomads
.\test-async-travel-plan.ps1

# 期望输出:
# 🚀 测试异步旅行计划生成 API
# ================================
# 
# 📤 步骤 1: 创建异步任务
# ✅ 任务创建成功!
# 任务ID: abc123...
# 状态: queued
# 
# 📊 步骤 2: 轮询任务状态
# ⏳ 查询任务状态 (第 1 次)...
#    状态: processing
#    进度: 10%
#    消息: 正在生成旅行计划...
# 
# ... (持续轮询)
# 
# 🎉 任务完成!
# 旅行计划 ID: uuid-xxx-xxx
```

### 第四步: 运行 Flutter 应用

```bash
# 1. 进入 Flutter 项目
cd E:\Workspaces\WaldenProjects\df_admin_mobile

# 2. 获取依赖
flutter pub get

# 3. 运行应用 (Chrome 浏览器)
flutter run -d chrome

# 或运行在 Windows 桌面
flutter run -d windows
```

### 第五步: UI 测试

1. **打开应用**
   - 应用启动后会显示城市列表

2. **进入城市详情**
   - 点击任意城市卡片
   - 进入城市详情页

3. **生成旅行计划**
   - 点击 "Generate Travel Plan" 或 "AI Travel Plan" 按钮
   - **立即显示进度对话框**
   
4. **观察进度更新**
   - 圆形进度条: 0% → 100%
   - 进度消息:
     - "任务已创建,等待处理..."
     - "正在生成旅行计划..."
     - "正在调用 AI 模型..."
     - "正在解析结果..."
     - "正在保存到数据库..."
     - "生成完成!"

5. **完成**
   - 进度达到 100%
   - 对话框自动关闭
   - 显示成功消息: "Travel plan generated! ID: xxx"

### 第六步: 验证数据

```powershell
# 1. 查看 Redis 中的任务状态
docker exec -it go-nomads-redis redis-cli KEYS "task:*"
docker exec -it go-nomads-redis redis-cli GET "task:abc123..."

# 2. 查看 RabbitMQ 队列消费情况
# 访问 http://localhost:15672
# Queues -> travel-plan-tasks
# 应该看到消息已被消费 (Total: 0, Ready: 0)

# 3. 查看 AI Service 日志
docker logs --tail 100 go-nomads-ai-service | Select-String "任务"

# 应该看到:
# ✅ 任务已创建: abc123...
# 🎯 开始处理任务: abc123...
# 📝 提示词已生成
# 🤖 AI 响应已接收
# 💾 旅行计划已保存
# ✅ 任务处理完成
```

## 🐛 故障排查

### 问题 1: 任务创建失败

**症状**: Flutter 报错 "Failed to create task"

**检查**:
```powershell
# 1. AI Service 是否运行
docker ps | Select-String "ai-service"

# 2. 查看日志
docker logs --tail 50 go-nomads-ai-service

# 3. 检查网络连接
curl http://localhost:8009/health
```

**解决**:
```powershell
# 重启 AI Service
docker-compose restart ai-service
```

### 问题 2: 任务一直处于 queued 状态

**症状**: 进度一直是 0%, 消息 "任务已创建,等待处理..."

**检查**:
```powershell
# 1. Worker Service 是否运行
docker logs go-nomads-ai-service | Select-String "Worker"

# 应该看到: ✅ AI Worker Service 已启动

# 2. RabbitMQ 连接是否正常
docker logs go-nomads-ai-service | Select-String "RabbitMQ"
```

**解决**:
```powershell
# 重启 RabbitMQ
docker-compose restart rabbitmq
Start-Sleep -Seconds 5
docker-compose restart ai-service
```

### 问题 3: 任务超时

**症状**: 2分钟后显示 "TimeoutException"

**检查**:
```powershell
# 查看 AI Service 是否有错误
docker logs --tail 100 go-nomads-ai-service | Select-String "错误|失败|Exception"
```

**解决**:
```dart
// 在 async_task_service.dart 中增加超时时间
maxAttempts: 60,  // 改为 3 分钟
```

### 问题 4: 进度不更新

**症状**: 进度条停在某个百分比不动

**检查**:
```powershell
# 1. 查看任务状态
docker exec -it go-nomads-redis redis-cli KEYS "task:*"

# 2. 检查 Worker 是否在处理
docker logs -f go-nomads-ai-service
```

**解决**:
- 检查网络连接
- 查看 AI API (DeepSeek) 是否正常
- 重启 AI Service

## 📊 性能指标

正常情况下的时间分布:

| 阶段 | 时间 | 累计 |
|------|------|------|
| 创建任务 | ~500ms | 0.5s |
| 入队等待 | ~1s | 1.5s |
| AI 生成 | ~30-60s | 31.5-61.5s |
| 解析保存 | ~2s | 33.5-63.5s |
| 轮询延迟 | ~3s | 36.5-66.5s |
| **总计** | **~37-67秒** | **0.6-1.1分钟** |

## ✅ 成功标志

- ✅ 后端服务全部启动
- ✅ RabbitMQ 队列正常工作
- ✅ Redis 缓存正常
- ✅ Worker Service 消费任务
- ✅ Flutter 显示进度对话框
- ✅ 进度实时更新
- ✅ 任务成功完成
- ✅ 返回 planId

## 🎉 下一步

1. **获取完整计划**: 实现 `getTravelPlanById()` API
2. **显示结果页**: 解析并展示旅行计划详情
3. **添加 SignalR**: 替代轮询,实现真正实时推送
4. **错误优化**: 更友好的错误提示
5. **离线支持**: 本地缓存任务状态

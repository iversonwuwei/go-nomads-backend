# DeepSeek 大模型快速参考

## 🎯 快速开始

### 1. 配置 API Key

```bash
# 编辑配置文件
vi src/Services/AIService/AIService/appsettings.json

# 或使用环境变量（推荐）
export DeepSeek__ApiKey="sk-your-deepseek-api-key"
export DeepSeek__BaseUrl="https://api.deepseek.com"
```

### 2. 启动服务

```bash
cd src/Services/AIService/AIService
dotnet run
```

### 3. 验证集成

```bash
# 健康检查
curl http://localhost:8009/health

# 运行测试脚本
./test-deepseek-integration.sh
```

---

## 📝 模型选择

### DeepSeek Chat (通用对话)
- **模型名称**: `deepseek-chat`
- **最大 Token**: 32,000
- **适用场景**: 日常对话、问答、创作、分析
- **温度建议**: 0.7 (创意任务)

### DeepSeek Coder (代码专用)
- **模型名称**: `deepseek-coder`
- **最大 Token**: 16,000
- **适用场景**: 代码生成、调试、技术问答
- **温度建议**: 0.3 (精确任务)

---

## 🔌 API 调用示例

### 创建通用对话

```bash
curl -X POST http://localhost:8009/api/chat/conversations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "title": "日常对话",
    "systemPrompt": "你是一个友好的助手",
    "modelName": "deepseek-chat"
  }'
```

### 创建代码助手

```bash
curl -X POST http://localhost:8009/api/chat/conversations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "title": "代码助手",
    "systemPrompt": "你是一个专业的编程助手",
    "modelName": "deepseek-coder"
  }'
```

### 发送消息

```bash
curl -X POST http://localhost:8009/api/chat/conversations/{conversationId}/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "content": "你的问题",
    "temperature": 0.7,
    "maxTokens": 2000
  }'
```

---

## 🛠️ 配置说明

### 核心配置 (appsettings.json)

```json
{
  "DeepSeek": {
    "ApiKey": "YOUR_DEEPSEEK_API_KEY_HERE",
    "BaseUrl": "https://api.deepseek.com"
  },
  "SemanticKernel": {
    "DefaultModel": "deepseek-chat",
    "MaxTokens": 4000,
    "Temperature": 0.7,
    "TopP": 0.9
  }
}
```

### 环境变量

```bash
# 必需
DeepSeek__ApiKey=sk-your-key

# 可选（默认值已设置）
DeepSeek__BaseUrl=https://api.deepseek.com
SemanticKernel__DefaultModel=deepseek-chat
SemanticKernel__Temperature=0.7
```

---

## 🔍 健康检查

### 基础健康检查
```bash
curl http://localhost:8009/health

# 预期返回
{
  "status": "healthy",
  "service": "AIService",
  "ai_model": "deepseek-chat",
  "provider": "DeepSeek"
}
```

### AI 专用健康检查
```bash
curl http://localhost:8009/health/ai

# 预期返回
{
  "status": "healthy",
  "ai_service": "connected",
  "model": "deepseek-chat",
  "provider": "DeepSeek",
  "max_tokens": 32000
}
```

---

## 📊 参数调优建议

### 创意任务（写作、头脑风暴）
```json
{
  "modelName": "deepseek-chat",
  "temperature": 0.8,
  "maxTokens": 2000,
  "topP": 0.95
}
```

### 精确任务（代码、数据分析）
```json
{
  "modelName": "deepseek-coder",
  "temperature": 0.3,
  "maxTokens": 1000,
  "topP": 0.9
}
```

### 平衡模式（日常对话）
```json
{
  "modelName": "deepseek-chat",
  "temperature": 0.7,
  "maxTokens": 1500,
  "topP": 0.9
}
```

---

## 🐛 常见问题

### Q1: 服务启动失败

**检查**:
```bash
# 查看日志
tail -f src/Services/AIService/AIService/logs/aiservice-*.txt

# 验证配置
cat appsettings.json | grep DeepSeek
```

### Q2: API Key 无效

**解决**:
1. 访问 https://platform.deepseek.com/ 验证 API Key
2. 确保配置文件中没有多余空格
3. 检查环境变量是否正确设置

### Q3: 模型不存在

**确认**:
- 使用 `deepseek-chat` 或 `deepseek-coder`
- 不要使用旧的 `qwen-plus` 或 `qwen-turbo`

---

## 📚 相关链接

- **API 文档**: http://localhost:8009/scalar/v1
- **健康检查**: http://localhost:8009/health
- **监控指标**: http://localhost:8009/metrics
- **DeepSeek 官方文档**: https://platform.deepseek.com/docs
- **迁移指南**: [DEEPSEEK_MIGRATION_GUIDE.md](./DEEPSEEK_MIGRATION_GUIDE.md)
- **完整总结**: [DEEPSEEK_MIGRATION_COMPLETE.md](./DEEPSEEK_MIGRATION_COMPLETE.md)

---

## 💡 最佳实践

1. **选择合适的模型**
   - 日常对话 → `deepseek-chat`
   - 代码相关 → `deepseek-coder`

2. **优化 Token 使用**
   - 设置合理的 `maxTokens` (避免浪费)
   - 利用 32K 上下文处理长文本

3. **调整温度参数**
   - 创意任务: 0.7-0.9
   - 精确任务: 0.1-0.3

4. **监控和日志**
   - 定期检查 `/health/ai`
   - 查看日志文件排查问题

---

**快速参考版本**: v1.0  
**最后更新**: 2025年1月29日

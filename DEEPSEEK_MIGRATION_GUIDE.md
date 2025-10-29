# DeepSeek 大模型迁移指南

## 📋 迁移概述

已成功将 AIService 从**千问大模型**切换到 **DeepSeek 大模型**。

---

## 🔄 主要变更

### 1. 配置文件更新

#### `appsettings.json` 和 `appsettings.Development.json`

**之前（千问）：**
```json
{
  "ConnectionStrings": {
    "QianWenApiKey": "YOUR_QIANWEN_API_KEY_HERE"
  },
  "Consul": {
    "Tags": ["ai", "chat", "qianwen", "semantic-kernel"]
  },
  "SemanticKernel": {
    "DefaultModel": "qwen-plus",
    "Models": {
      "qwen-plus": {
        "DisplayName": "千问Plus",
        "MaxTokens": 6000
      }
    }
  }
}
```

**现在（DeepSeek）：**
```json
{
  "DeepSeek": {
    "ApiKey": "YOUR_DEEPSEEK_API_KEY_HERE",
    "BaseUrl": "https://api.deepseek.com"
  },
  "Consul": {
    "Tags": ["ai", "chat", "deepseek", "semantic-kernel"]
  },
  "SemanticKernel": {
    "DefaultModel": "deepseek-chat",
    "Models": {
      "deepseek-chat": {
        "DisplayName": "DeepSeek Chat",
        "MaxTokens": 32000,
        "Description": "DeepSeek 对话模型，支持长文本和复杂推理"
      },
      "deepseek-coder": {
        "DisplayName": "DeepSeek Coder",
        "MaxTokens": 16000,
        "Description": "DeepSeek 代码模型，专注于代码生成和技术问答"
      }
    }
  }
}
```

### 2. Program.cs 更新

**之前（千问）：**
```csharp
var qianwenApiKey = builder.Configuration["QianWen:ApiKey"] ?? "test-key";

kernelBuilder.AddOpenAIChatCompletion(
    modelId: "qwen-plus",
    apiKey: qianwenApiKey,
    endpoint: new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1"));
```

**现在（DeepSeek）：**
```csharp
var deepseekApiKey = builder.Configuration["DeepSeek:ApiKey"] ?? "test-key";
var deepseekBaseUrl = builder.Configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";

kernelBuilder.AddOpenAIChatCompletion(
    modelId: "deepseek-chat",
    apiKey: deepseekApiKey,
    endpoint: new Uri(deepseekBaseUrl));
```

### 3. 健康检查端点更新

**之前：**
```json
{
  "semantic_kernel": "enabled",
  "qianwen_model": "qwen-plus"
}
```

**现在：**
```json
{
  "semantic_kernel": "enabled",
  "ai_model": "deepseek-chat",
  "provider": "DeepSeek"
}
```

---

## 🚀 部署步骤

### 1. 获取 DeepSeek API Key

访问 [DeepSeek 开放平台](https://platform.deepseek.com/) 注册并获取 API Key。

### 2. 更新环境变量

**方式 1：直接修改配置文件**
```json
{
  "DeepSeek": {
    "ApiKey": "sk-your-actual-deepseek-api-key",
    "BaseUrl": "https://api.deepseek.com"
  }
}
```

**方式 2：使用环境变量（推荐）**
```bash
export DeepSeek__ApiKey="sk-your-actual-deepseek-api-key"
export DeepSeek__BaseUrl="https://api.deepseek.com"
```

**方式 3：使用 Docker Secrets（生产环境）**
```yaml
# docker-compose.yml
services:
  ai-service:
    environment:
      - DeepSeek__ApiKey=${DEEPSEEK_API_KEY}
      - DeepSeek__BaseUrl=https://api.deepseek.com
```

### 3. 重新构建和部署

```bash
# 进入 AIService 目录
cd src/Services/AIService/AIService

# 构建项目
dotnet build

# 运行服务
dotnet run

# 或使用 Docker
docker-compose up -d ai-service
```

### 4. 验证部署

```bash
# 健康检查
curl http://localhost:8009/health

# AI 服务健康检查
curl http://localhost:8009/health/ai

# 预期返回
{
  "status": "healthy",
  "ai_service": "connected",
  "model": "deepseek-chat",
  "provider": "DeepSeek",
  "max_tokens": 32000,
  "timestamp": "2025-01-29T..."
}
```

---

## 🎯 模型对比

| 特性 | 千问 (QianWen) | DeepSeek |
|------|----------------|----------|
| **默认模型** | qwen-plus | deepseek-chat |
| **最大 Token** | 6,000 | 32,000 |
| **专用代码模型** | ❌ | ✅ (deepseek-coder) |
| **长文本支持** | ⚠️ 中等 | ✅ 强 |
| **API 端点** | dashscope.aliyuncs.com | api.deepseek.com |
| **价格** | 中等 | 较低 |
| **中文支持** | ✅ 优秀 | ✅ 优秀 |
| **代码生成** | ⚠️ 一般 | ✅ 优秀 |

---

## 📊 API 调用示例

### 创建对话（使用 DeepSeek）

```bash
curl -X POST http://localhost:8009/api/chat/conversations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "title": "测试 DeepSeek 对话",
    "systemPrompt": "你是一个友好的 AI 助手",
    "modelName": "deepseek-chat"
  }'
```

### 发送消息

```bash
curl -X POST http://localhost:8009/api/chat/conversations/{conversationId}/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "content": "你好，DeepSeek！",
    "temperature": 0.7,
    "maxTokens": 2000
  }'
```

### 使用 DeepSeek Coder 模型

```bash
curl -X POST http://localhost:8009/api/chat/conversations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "title": "代码助手",
    "systemPrompt": "你是一个专业的编程助手",
    "modelName": "deepseek-coder"
  }'
```

---

## 🔧 故障排查

### 问题 1: API Key 无效

**错误信息：**
```
Semantic Kernel 配置失败: Invalid API Key
```

**解决方案：**
1. 确认 DeepSeek API Key 正确
2. 检查 `appsettings.json` 或环境变量配置
3. 访问 DeepSeek 控制台验证 API Key 状态

### 问题 2: 模型不存在

**错误信息：**
```
Model 'qwen-plus' not found
```

**解决方案：**
1. 确保使用 DeepSeek 模型名称：`deepseek-chat` 或 `deepseek-coder`
2. 检查数据库中旧对话的 `model_name` 字段
3. 更新旧对话使用新模型：
```sql
UPDATE ai_conversations 
SET model_name = 'deepseek-chat' 
WHERE model_name IN ('qwen-plus', 'qwen-turbo');
```

### 问题 3: Token 限制

**错误信息：**
```
Maximum token limit exceeded
```

**解决方案：**
- DeepSeek Chat: 最大 32,000 tokens
- DeepSeek Coder: 最大 16,000 tokens
- 调整 `maxTokens` 参数或缩短上下文

---

## 📝 数据库迁移

如果需要更新现有对话的模型信息：

```sql
-- 更新默认模型
UPDATE ai_conversations 
SET model_name = 'deepseek-chat' 
WHERE model_name = 'qwen-plus';

UPDATE ai_conversations 
SET model_name = 'deepseek-coder' 
WHERE model_name = 'qwen-turbo' AND system_prompt LIKE '%代码%';

-- 查看更新结果
SELECT model_name, COUNT(*) as count
FROM ai_conversations
GROUP BY model_name;
```

---

## ✅ 迁移检查清单

- [x] 更新 `appsettings.json` 配置
- [x] 更新 `appsettings.Development.json` 配置
- [x] 修改 `Program.cs` 中的 Semantic Kernel 配置
- [x] 更新健康检查端点返回信息
- [x] 更新 API 文档描述
- [x] 更新代码注释（千问 → DeepSeek）
- [ ] 配置 DeepSeek API Key（需要手动操作）
- [ ] 测试 AI 对话功能
- [ ] 测试代码生成功能（使用 deepseek-coder）
- [ ] 更新数据库中的旧对话模型（可选）
- [ ] 监控 API 调用和成本

---

## 🎉 优势总结

### 1. **更长的上下文支持**
- 从 6,000 tokens 增加到 32,000 tokens
- 支持更复杂的对话和文档分析

### 2. **专用代码模型**
- `deepseek-coder` 专注于代码生成和技术问答
- 更适合 Go Nomads 的技术社区场景

### 3. **成本优化**
- DeepSeek 价格相对较低
- 更高的性价比

### 4. **灵活性**
- 支持多模型切换
- 易于扩展其他模型

---

## 📚 相关文档

- [DeepSeek API 文档](https://platform.deepseek.com/docs)
- [Semantic Kernel 文档](https://learn.microsoft.com/en-us/semantic-kernel/)
- [AIService 本地部署指南](./AISERVICE_LOCAL_DEPLOYMENT_SETUP.md)

---

## 🆘 支持

如有问题，请联系开发团队或查看：
- DeepSeek 官方文档
- GitHub Issues
- 内部技术文档

---

**迁移完成时间：** 2025年1月29日  
**执行人：** AI Assistant  
**状态：** ✅ 配置完成，等待 API Key 和测试

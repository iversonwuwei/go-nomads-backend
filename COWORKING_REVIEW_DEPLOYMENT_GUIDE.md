# Coworking Review 功能 - 部署和测试指南

## ✅ 已完成的工作

### 1. 后端 API 开发
- ✅ Domain Layer: 实体和仓储接口
- ✅ Infrastructure Layer: Supabase 仓储实现
- ✅ Application Layer: 服务层和 DTOs
- ✅ API Layer: RESTful 控制器
- ✅ CacheService 集成: 自动更新评分缓存

### 2. 数据库迁移
- ✅ 创建 `coworking_reviews` 表
- ✅ 添加索引优化查询性能
- ✅ 配置行级安全策略 (RLS)
- ✅ 添加触发器自动更新时间戳

### 3. Flutter 前端
- ✅ Domain 实体和仓储
- ✅ 评论列表页（无限滚动）
- ✅ 添加/编辑评论页
- ✅ 详情页集成
- ✅ 依赖注入配置

---

## 📋 部署步骤

### 步骤 1: 数据库迁移（✅ 已完成）

你已经在 Supabase 中执行了迁移脚本：
```sql
src/Services/CoworkingService/Database/Migrations/004_create_coworking_reviews_table.sql
```

验证表是否创建成功：
```sql
SELECT * FROM information_schema.tables 
WHERE table_name = 'coworking_reviews';
```

### 步骤 2: 构建和部署 CoworkingService

```powershell
# 进入项目根目录
cd E:\Workspaces\WaldenProjects\go-nomads

# 构建 Docker 镜像
docker build -t coworking-service -f src/Services/CoworkingService/CoworkingService/Dockerfile .

# 停止旧容器（如果存在）
docker stop go-nomads-coworking-service go-nomads-coworking-service-dapr
docker rm go-nomads-coworking-service go-nomads-coworking-service-dapr

# 启动新容器
docker run -d --name go-nomads-coworking-service `
  --network go-nomads-network `
  -p 8004:8080 `
  -p 3514:3514 `
  -e "ASPNETCORE_ENVIRONMENT=Development" `
  -e "DAPR_HTTP_PORT=3514" `
  -e "DAPR_GRPC_PORT=50001" `
  -e "Supabase__Url=YOUR_SUPABASE_URL" `
  -e "Supabase__Key=YOUR_SUPABASE_KEY" `
  coworking-service

# 启动 Dapr Sidecar
docker run -d --name go-nomads-coworking-service-dapr `
  --network container:go-nomads-coworking-service `
  daprio/daprd:latest `
  ./daprd --app-id coworking-service --app-port 8080 `
  --dapr-http-port 3514 --dapr-grpc-port 50001 `
  --resources-path /components --config /configuration/config.yaml `
  --log-level info

# 查看日志
docker logs go-nomads-coworking-service -f
```

### 步骤 3: 验证服务健康状态

```powershell
# 检查健康状态
Invoke-RestMethod http://localhost:8004/health

# 查看 Swagger/Scalar 文档
Start-Process "http://localhost:8004/scalar/v1"
```

---

## 🧪 API 测试

### 方式 1: 使用快速测试脚本

```powershell
# 替换为实际的 Coworking ID
.\quick-test-review.ps1 -CoworkingId "your-coworking-id"
```

### 方式 2: 使用完整测试脚本

```powershell
# 编辑脚本，设置 Coworking ID
# $coworkingId = "your-coworking-id-here"

.\test-coworking-review-api.ps1
```

### 方式 3: 手动测试

#### 1. 获取评论列表
```powershell
$coworkingId = "your-id-here"
Invoke-RestMethod -Method GET -Uri "http://localhost:8004/api/v1/coworking/$coworkingId/reviews?page=1&pageSize=10"
```

#### 2. 添加评论
```powershell
$body = @{
    rating = 4.5
    title = "很棒的共享办公空间"
    content = "环境优美，设施齐全，网络速度快。咖啡免费，工作氛围很好。"
    visitDate = "2025-01-15"
    photoUrls = @(
        "https://example.com/photo1.jpg",
        "https://example.com/photo2.jpg"
    )
} | ConvertTo-Json

Invoke-RestMethod -Method POST `
  -Uri "http://localhost:8004/api/v1/coworking/$coworkingId/reviews" `
  -Body $body `
  -ContentType "application/json"
```

#### 3. 更新评论
```powershell
$reviewId = "your-review-id"
$body = @{
    rating = 5.0
    title = "更新：超棒的共享办公空间"
    content = "使用一段时间后，觉得更加喜欢这里了！强烈推荐！"
} | ConvertTo-Json

Invoke-RestMethod -Method PUT `
  -Uri "http://localhost:8004/api/v1/coworking/reviews/$reviewId" `
  -Body $body `
  -ContentType "application/json"
```

#### 4. 删除评论
```powershell
Invoke-RestMethod -Method DELETE `
  -Uri "http://localhost:8004/api/v1/coworking/reviews/$reviewId"
```

---

## 🔍 验证评分缓存更新

评论创建/更新/删除后，会自动调用 CacheService 更新评分缓存。

### 查看日志
```powershell
# CoworkingService 日志
docker logs go-nomads-coworking-service --tail 50 | Select-String "评分缓存"

# CacheService 日志
docker logs go-nomads-cache-service --tail 50 | Select-String "coworking"
```

### 验证缓存
```powershell
# 获取 Coworking 评分缓存
Invoke-RestMethod "http://localhost:8010/api/v1/cache/scores/coworking/$coworkingId"
```

---

## 📊 API 端点总览

| 端点 | 方法 | 描述 |
|------|------|------|
| `/api/v1/coworking/{id}/reviews` | GET | 获取评论列表（分页） |
| `/api/v1/coworking/{id}/reviews` | POST | 添加评论 |
| `/api/v1/coworking/reviews/{id}` | GET | 获取评论详情 |
| `/api/v1/coworking/reviews/{id}` | PUT | 更新评论 |
| `/api/v1/coworking/reviews/{id}` | DELETE | 删除评论 |
| `/api/v1/coworking/{id}/reviews/my-review` | GET | 获取当前用户的评论 |

---

## ⚠️ 注意事项

### 1. 认证要求
- 添加、更新、删除评论需要用户登录
- 只能修改/删除自己的评论
- 需要在请求头中包含认证信息

### 2. 数据验证
- **评分**: 1.0 - 5.0（精度到 0.5）
- **标题**: 5-100 字符
- **内容**: 20-1000 字符
- **照片**: 最多 5 张
- **防重复**: 每个用户对一个 Coworking 只能评论一次

### 3. 权限控制
- 普通用户：查看已验证的评论 + 自己的评论
- 用户：只能修改/删除自己的评论
- 管理员：可以验证/拒绝评论

---

## 🐛 故障排查

### 问题 1: 无法连接到 CoworkingService
```powershell
# 检查容器状态
docker ps | Select-String coworking

# 检查端口占用
netstat -ano | findstr :8004

# 查看容器日志
docker logs go-nomads-coworking-service --tail 100
```

### 问题 2: 评分缓存未更新
```powershell
# 检查 CacheService 是否运行
docker ps | Select-String cache

# 检查 Dapr 连接
docker logs go-nomads-coworking-service-dapr --tail 50

# 手动更新缓存
Invoke-RestMethod -Method PUT `
  -Uri "http://localhost:8010/api/v1/cache/scores/coworking/$coworkingId" `
  -Body (@{overallScore=4.5; statistics="{}"} | ConvertTo-Json) `
  -ContentType "application/json"
```

### 问题 3: 数据库连接失败
```powershell
# 检查 Supabase 环境变量
docker inspect go-nomads-coworking-service --format='{{range .Config.Env}}{{println .}}{{end}}' | Select-String Supabase

# 测试 Supabase 连接
# 在 Supabase SQL Editor 中执行
SELECT count(*) FROM coworking_reviews;
```

---

## 📱 Flutter 集成测试

### 1. 确保后端服务运行
```powershell
# 检查所有服务
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

### 2. 运行 Flutter 应用
```bash
cd df_admin_mobile
flutter run
```

### 3. 测试流程
1. 进入 Coworking 详情页
2. 点击评分区域，跳转到评论列表
3. 点击"+"按钮，添加评论
4. 填写评分、标题、内容
5. （可选）添加照片
6. 提交评论
7. 返回列表查看新评论
8. 长按评论进行编辑/删除

---

## 🎯 下一步优化建议

### 1. 功能增强
- [ ] 评论点赞/举报功能
- [ ] 评论回复功能（嵌套评论）
- [ ] 图片上传到 OSS
- [ ] 评论推送通知

### 2. 性能优化
- [ ] 评论列表添加 Redis 缓存
- [ ] 实现评论预加载
- [ ] 添加 CDN 加速图片

### 3. 管理功能
- [ ] 管理员审核界面
- [ ] 批量操作评论
- [ ] 导出评论数据

---

## 📞 技术支持

如遇问题，请检查：
1. 所有服务容器是否正常运行
2. 数据库表是否正确创建
3. 环境变量是否正确配置
4. 网络连接是否正常

日志位置：
- CoworkingService: `docker logs go-nomads-coworking-service`
- CacheService: `docker logs go-nomads-cache-service`
- Dapr Sidecar: `docker logs go-nomads-coworking-service-dapr`

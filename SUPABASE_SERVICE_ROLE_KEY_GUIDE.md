# 获取 Supabase Service Role Key 指南

## 当前问题
（已更新）用户城市内容相关表已关闭 RLS，因此默认 `anon` key 也能写入。这份文档保留供需要 service_role 的其他场景使用。

## 解决方案（如需使用 service_role）

切换到 `service_role` key,允许后端服务绕过 RLS 策略。

---

## 步骤 1：获取 Service Role Key

1. **打开 Supabase API 设置页面**：
   <https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/settings/api>

2. **找到 "Project API keys" 部分**

3. **复制 `service_role` key**：
   - 这是一个 **secret** key
   - 以 `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxjZmJhanJvY21qbHFuZGtyc2FvIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6...` 开头
   - **注意**: 是 `service_role` 不是 `anon`

---

## 步骤 2：更新配置文件

### 需要更新的文件

1. `src/Services/CityService/CityService/appsettings.json`
2. `src/Services/CityService/CityService/appsettings.Development.json`

### 替换内容

```json
"Supabase": {
    "Url": "https://lcfbajrocmjlqndkrsao.supabase.co",
    "Key": "<粘贴你复制的 service_role key>",
    "Schema": "public"
}
```

---

## 步骤 3：执行数据库迁移

打开 Supabase SQL Editor：
<https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/sql/new>

复制并执行 `database/migrations/add_updated_at_to_expenses_and_photos.sql`

---

## 步骤 4：重启 CityService

```powershell
cd E:\Workspaces\WaldenProjects\go-nomads\deployment
.\deploy-services-local.ps1
```

---

## 步骤 5：测试

从 Flutter 应用提交费用,应该能成功保存。

---

## 安全说明

⚠️ **Service Role Key 安全性**：

- **仅用于服务端**,不要暴露给前端
- 可以绕过所有 RLS 策略
- 后端已有 JWT 验证中间件保护 API 端点
- 用户身份通过 `UserContextMiddleware` 验证

---

## 当前配置对比

| 配置项 | anon key (当前) | service_role key (目标) |
|--------|----------------|------------------------|
| 用途 | 客户端应用 | 服务端应用 |
| RLS | 受限制 | 绕过所有策略 |
| JWT role | "anon" | "service_role" |
| 适用场景 | 浏览器/移动应用 | 微服务后端 |

---

## 完成后

✅ 后端可以正常写入 user_city_expenses  
✅ 后端可以正常写入 user_city_photos  
✅ 后端可以正常写入 user_city_reviews  
✅ 费用提交功能恢复正常  
✅ 照片上传功能恢复正常  

---

## 如果遇到问题

如果切换 key 后仍然报错:

1. 确认复制的是 `service_role` key (不是 `anon`)
2. 确认已重启 CityService
3. 确认执行了 `add_updated_at_to_expenses_and_photos.sql` 迁移
4. 查看 CityService 日志确认配置已加载


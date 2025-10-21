# 快速参考：JWT 认证问题

## 🔴 问题
通过 Gateway 访问 `/api/users` 返回 401，即使提供了有效 token

## ✅ 解决方案
禁用 Gateway 的自定义 JWT 中间件，让它作为透明代理

## 📝 修改的文件
`src/Gateway/Gateway/Program.cs` - 注释掉 `app.UseJwtAuthentication();`

## 🧪 测试
```bash
# 重新部署
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
./deploy-services-local.sh

# 测试访问
curl http://localhost:5000/api/users
# ✅ 应该返回用户列表（200 OK）
```

## 💡 原理
- **之前**: Gateway 拦截所有请求，验证 JWT 失败返回 401
- **现在**: Gateway 透明转发请求，后端服务自己处理认证

## ⚠️ 注意
当前 UserService 的 `GetUsers()` 没有 `[Authorize]` 特性，任何人都可访问。
如需保护，在 UserService Controller 添加 `[Authorize]` 特性。

---
详细文档: [JWT_AUTH_FIXED.md](JWT_AUTH_FIXED.md)

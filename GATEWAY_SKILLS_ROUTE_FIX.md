# Gateway Skills/Interests 路由修复

## 问题描述

Flutter 应用在调用 `/api/v1/skills/by-category` 和 `/api/v1/interests/by-category` API 时返回 404 错误。

### 错误日志
```
flutter: 🚀 REQUEST[GET] => http://127.0.0.1:5000/api/v1/skills/by-category
flutter: Headers: {Authorization: Bearer ...}
flutter: ❌ ERROR[404] => http://127.0.0.1:5000/api/v1/skills/by-category
flutter: ❌ Error getting skills by category: HttpException: 请求的资源不存在 (Status Code: 404)
```

## 根本原因

Gateway 的 `ConsulProxyConfigProvider` 只为 `user-service` 配置了以下路由：
- `/api/v1/users/*` (主路由)
- `/api/v1/auth/*` (认证路由)

但 `user-service` 实际上还处理：
- `/api/v1/skills/*` (技能 API)
- `/api/v1/interests/*` (兴趣 API)

这些路由没有被 Gateway 转发到 UserService，导致 404 错误。

## 解决方案

在 `src/Gateway/Gateway/Services/ConsulProxyConfigProvider.cs` 中为 `user-service` 添加 skills 和 interests 路由：

```csharp
// 在 user-service 的特殊处理中添加

// v1 API skills routes
var skillsRoute = new YarpRouteConfig
{
    RouteId = $"{serviceName}-skills-v1-route",
    ClusterId = $"{serviceName}-cluster",
    Match = new YarpRouteMatch
    {
        Path = "/api/v1/skills/{**remainder}"
    }
};
routes.Add(skillsRoute);

var skillsExactRoute = new YarpRouteConfig
{
    RouteId = $"{serviceName}-skills-v1-exact-route",
    ClusterId = $"{serviceName}-cluster",
    Match = new YarpRouteMatch
    {
        Path = "/api/v1/skills"
    }
};
routes.Add(skillsExactRoute);

// v1 API interests routes
var interestsRoute = new YarpRouteConfig
{
    RouteId = $"{serviceName}-interests-v1-route",
    ClusterId = $"{serviceName}-cluster",
    Match = new YarpRouteMatch
    {
        Path = "/api/v1/interests/{**remainder}"
    }
};
routes.Add(interestsRoute);

var interestsExactRoute = new YarpRouteConfig
{
    RouteId = $"{serviceName}-interests-v1-exact-route",
    ClusterId = $"{serviceName}-cluster",
    Match = new YarpRouteMatch
    {
        Path = "/api/v1/interests"
    }
};
routes.Add(interestsExactRoute);
```

## 部署

修改后需要重新部署 Gateway：

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
./deploy-services-local.sh
```

## 验证

修复后，测试 API：

```bash
# 测试 skills API
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  http://127.0.0.1:5000/api/v1/skills/by-category

# 测试 interests API
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  http://127.0.0.1:5000/api/v1/interests/by-category
```

应该返回 `{"success": true, "data": [...]}` 而不是 404。

## 影响范围

这个修复解决了以下功能：
- ✅ 技能列表加载
- ✅ 按类别获取技能
- ✅ 兴趣列表加载
- ✅ 按类别获取兴趣
- ✅ 用户技能/兴趣的添加、删除、更新操作

## 相关文件

- `src/Gateway/Gateway/Services/ConsulProxyConfigProvider.cs` - Gateway 路由配置
- `src/Services/UserService/UserService/API/Controllers/SkillsController.cs` - Skills API
- `src/Services/UserService/UserService/API/Controllers/InterestsController.cs` - Interests API
- `open-platform-app/lib/pages/profile_edit_page.dart` - Flutter 技能/兴趣底部抽屉

## 日期

2025-01-02

# 问题修复：502 Bad Gateway 错误

## 问题描述

Gateway 服务在尝试通过 YARP 反向代理访问后端服务时，返回 500 错误：

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Failed to get products: Response status code does not indicate success: 502 (Bad Gateway)."
}
```

## 根本原因

容器中存在环境变量 `HTTP_PROXY` 和 `HTTPS_PROXY`，导致所有 HTTP 请求被路由到一个不可用的代理服务器：

```bash
HTTP_PROXY=http://host.containers.internal:7897
HTTPS_PROXY=http://host.containers.internal:7897
```

这些代理设置导致 Gateway 服务无法直接连接到 Consul 服务注册中心，从而无法获取后端服务的配置信息。

## 诊断步骤

1. **检查服务状态** - 所有容器都在运行
2. **查看 Gateway 日志** - 发现 `Consul.ConsulRequestException: Unexpected response, status code BadGateway`
3. **测试网络连接** - 使用临时容器验证网络连通性正常
4. **检查环境变量** - 发现代理设置问题
5. **验证修复** - 清除代理环境变量后问题解决

## 解决方案

修改部署脚本 `deploy-services-local.sh`，在启动容器时显式清除代理环境变量：

```bash
$CONTAINER_RUNTIME run -d \
    --name "go-nomads-$service_name" \
    --network "$NETWORK_NAME" \
    -p "$app_port:8080" \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ASPNETCORE_URLS=http://+:8080 \
    -e HTTP_PROXY= \
    -e HTTPS_PROXY= \
    -e NO_PROXY= \
    "go-nomads-$service_name:latest" > /dev/null
```

## 验证

修复后，所有 API 端点正常工作：

### Gateway 健康检查
```bash
curl http://localhost:5000/health
# {"status":"healthy","timestamp":"2025-10-14T12:26:15.3819709Z"}
```

### 产品服务
```bash
curl http://localhost:5000/api/products
# 返回产品列表
```

### 用户服务
```bash
curl http://localhost:5000/api/users
# 返回用户列表
```

## 经验教训

1. **环境变量污染**: Podman/Docker 会继承宿主机的代理环境变量，可能影响容器内的网络连接
2. **显式配置**: 在部署脚本中显式设置或清除关键环境变量，避免隐式继承
3. **网络测试**: 使用临时容器可以快速验证网络层面的连通性
4. **日志分析**: 详细的日志对于诊断问题至关重要

## 预防措施

在未来的部署中：

1. 总是在部署脚本中显式设置环境变量
2. 为容器网络通信使用直接连接，不经过代理
3. 添加健康检查和启动探针来尽早发现问题
4. 文档化所有环境变量要求

## 状态

✅ **已解决** - 所有服务现在正常运行并可以通过 Gateway 访问

---

**修复日期**: 2025年10月14日  
**影响范围**: Gateway 及所有通过它访问的后端服务  
**修复版本**: deploy-services-local.sh v1.1

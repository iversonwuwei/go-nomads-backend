# Go Nomads 微服务快速启动指南

## 🚀 快速启动

### 前提条件
- Docker Desktop 已安装并运行
- .NET 8.0 SDK (用于本地开发)
- Visual Studio 2022 或 VS Code

### 1. 启动所有服务

```powershell
# 进入项目根目录
cd e:\Workspaces\WaldenProjects\go-nomads

# 构建并启动所有服务
docker-compose up -d --build
```

### 2. 检查服务状态

```powershell
# 查看所有服务状态
docker-compose ps

# 查看特定服务日志
docker-compose logs -f city-service
docker-compose logs -f user-service
```

### 3. 访问服务

#### 核心微服务 Swagger 文档
- **API Gateway**: http://localhost:5080/swagger
- **User Service**: http://localhost:5001/swagger
- **City Service**: http://localhost:5202/swagger
- **Coworking Service**: http://localhost:5203/swagger
- **Accommodation Service**: http://localhost:5204/swagger
- **Event Service**: http://localhost:5205/swagger
- **Innovation Service**: http://localhost:5206/swagger
- **Travel Planning Service**: http://localhost:5207/swagger
- **Ecommerce Service**: http://localhost:5208/swagger

#### 基础设施服务
- **RabbitMQ 管理界面**: http://localhost:5302 (admin/admin)
- **Grafana 监控**: http://localhost:5306 (admin/admin)
- **Prometheus**: http://localhost:5307
- **Zipkin 链路追踪**: http://localhost:5308

### 4. 测试 City Service API

```powershell
# 健康检查
curl http://localhost:5202/health

# 获取城市列表
curl http://localhost:5202/api/v1/cities

# 搜索城市
curl "http://localhost:5202/api/v1/cities/search?name=Chiang&pageNumber=1&pageSize=10"

# 获取推荐城市
curl http://localhost:5202/api/v1/cities/recommend?count=5
```

### 5. 停止服务

```powershell
# 停止所有服务
docker-compose down

# 停止并删除数据卷 (清空所有数据)
docker-compose down -v
```

---

## 🛠️ 本地开发

### 单独运行 City Service

```powershell
cd src\Services\CityService\CityService

# 还原包
dotnet restore

# 运行服务
dotnet run
```

服务将在 http://localhost:5202 启动

### 添加数据库迁移

```powershell
# 安装 EF Core 工具 (首次)
dotnet tool install --global dotnet-ef

# 创建迁移
dotnet ef migrations add InitialCreate

# 更新数据库
dotnet ef database update
```

---

## 📊 数据库连接

### PostgreSQL 连接信息
- **Host**: localhost
- **Port**: 5432
- **Username**: postgres
- **Password**: postgres

### 数据库列表
- `userservice_db` - 用户服务
- `cityservice_db` - 城市服务
- `coworkingservice_db` - 共享办公服务
- `accommodationservice_db` - 住宿服务
- `eventservice_db` - 活动服务
- `innovationservice_db` - 创新项目服务
- `travelplanningservice_db` - 旅行规划服务
- `ecommerceservice_db` - 电商服务

### 使用 pgAdmin 连接
```
Host: localhost
Port: 5432
Username: postgres
Password: postgres
```

---

## 🔐 JWT 认证测试

### 1. 注册用户 (User Service)
```powershell
curl -X POST http://localhost:5001/api/v1/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test@123",
    "username": "testuser"
  }'
```

### 2. 登录获取 Token
```powershell
curl -X POST http://localhost:5001/api/v1/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test@123"
  }'
```

### 3. 使用 Token 访问受保护的 API
```powershell
$token = "your_jwt_token_here"

curl -X POST http://localhost:5202/api/v1/cities `
  -H "Authorization: Bearer $token" `
  -H "Content-Type: application/json" `
  -d '{
    "name": "Bangkok",
    "country": "Thailand",
    "latitude": 13.7563,
    "longitude": 100.5018,
    "currency": "THB"
  }'
```

---

## 🐛 故障排查

### 服务无法启动

```powershell
# 查看详细日志
docker-compose logs city-service

# 重启特定服务
docker-compose restart city-service

# 重新构建服务
docker-compose up -d --build city-service
```

### 数据库连接失败

```powershell
# 检查 PostgreSQL 是否运行
docker-compose ps postgres

# 查看 PostgreSQL 日志
docker-compose logs postgres

# 重启 PostgreSQL
docker-compose restart postgres
```

### 端口冲突

如果某个端口已被占用,修改 `docker-compose.yml` 中的端口映射:

```yaml
ports:
  - "5202:5202"  # 如冲突，改为其它已登记的 5xxx 宿主机端口
```

---

## 📈 性能测试

### 使用 Apache Bench

```powershell
# 测试城市列表 API
ab -n 1000 -c 10 http://localhost:5202/api/v1/cities

# 测试健康检查
ab -n 10000 -c 100 http://localhost:5202/health
```

### 使用 k6

```javascript
// load-test.js
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 10,
  duration: '30s',
};

export default function() {
  let res = http.get('http://localhost:5202/api/v1/cities');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
}
```

运行测试:
```powershell
k6 run load-test.js
```

---

## 🎯 下一步

1. ✅ 所有核心微服务已创建
2. ✅ Docker Compose 已配置
3. ✅ City Service 完整实现
4. ⏳ 实现其他服务的完整功能
5. ⏳ 配置 API Gateway 路由
6. ⏳ 实现服务间通信
7. ⏳ 添加集成测试
8. ⏳ 部署到 Kubernetes

---

## 📚 相关文档

- [微服务架构总览](./MICROSERVICES_ARCHITECTURE.md)
- [City Service 详细文档](../services/city-service/README.md)
- [API 网关配置](../gateway/README.md)
- [部署指南](../../deployment/README.md)

---

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request!

## 📄 许可证

MIT License

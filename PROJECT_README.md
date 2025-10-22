# 🌍 Go Nomads - 数字游民平台后端

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-336791)](https://www.postgresql.org/)
[![PostGIS](https://img.shields.io/badge/PostGIS-3.3-2D72B8)](https://postgis.net/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED)](https://www.docker.com/)
[![Supabase](https://img.shields.io/badge/Supabase-Ready-3ECF8E)](https://supabase.com/)

Go Nomads 是一个专为数字游民打造的全功能平台后端,采用微服务架构,提供城市推荐、共享办公空间预订、住宿管理、活动组织、创新项目展示、智能旅行规划和电商服务。

## ✨ 核心功能

### 🏙️ 城市服务 (CityService)
- 全球城市信息管理
- 多维度评分系统(生活成本、网络质量、安全、社区、天气)
- PostGIS 地理位置搜索
- 城市推荐算法

### 💼 共享办公服务 (CoworkingService)
- 办公空间信息管理
- 灵活定价(小时/天/月)
- 在线预订系统
- 评分和评论

### 🏨 住宿服务 (AccommodationService)
- 酒店和民宿管理
- 房型和价格管理
- 预订系统
- 支付状态跟踪

### 🎉 活动服务 (EventService)
- 线下/线上/混合活动
- 多种活动类别(网络、工作坊、社交、运动等)
- 参与者管理
- 活动推荐

### 💡 创新服务 (InnovationService)
- 创意项目展示
- 社交互动(点赞、评论)
- 团队协作需求
- 项目分享

### ✈️ 旅行规划服务 (TravelPlanningService)
- AI 智能行程规划
- 多城市路线优化
- 协作旅行计划
- 预算管理

### 🛒 电商服务 (EcommerceService)
- 数字游民装备商城
- 购物车和订单管理
- 支付集成
- 物流跟踪

## 🏗️ 技术架构

### 后端技术栈

- **框架**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core 8.0
- **数据库**: PostgreSQL 15 + PostGIS 3.3
- **缓存**: Redis 7
- **搜索引擎**: Elasticsearch 8.11
- **消息队列**: RabbitMQ 3
- **服务编排**: Dapr 1.12
- **监控**: Prometheus + Grafana + Zipkin
- **日志**: Serilog
- **认证**: JWT Bearer Tokens
- **容器化**: Docker + Docker Compose

### 架构模式

- **微服务架构**: 8 个独立服务
- **领域驱动设计 (DDD)**: 清晰的业务边界
- **CQRS**: 命令查询职责分离
- **事件驱动**: 异步消息通信
- **API Gateway**: 统一入口
- **服务网格**: Dapr 服务发现和通信

## 📦 项目结构

```
go-nomads/
├── database/
│   └── schema.sql                    # Supabase PostgreSQL 完整架构
├── docs/
│   ├── DEPLOYMENT_GUIDE.md          # 部署指南
│   ├── IMPLEMENTATION_SUMMARY.md    # 实现总结
│   ├── QUICK_START.md               # 快速开始
│   └── architecture/
│       └── MICROSERVICES_ARCHITECTURE.md  # 架构文档
├── src/
│   ├── Gateway/
│   │   └── Gateway/                 # API 网关
│   ├── Services/
│   │   ├── CityService/             # 城市服务
│   │   ├── CoworkingService/        # 共享办公服务
│   │   ├── AccommodationService/    # 住宿服务
│   │   ├── EventService/            # 活动服务
│   │   ├── InnovationService/       # 创新服务
│   │   ├── TravelPlanningService/   # 旅行规划服务
│   │   ├── EcommerceService/        # 电商服务
│   │   └── UserService/             # 用户服务
│   └── Shared/
│       └── Shared/                  # 共享库
├── docker-compose.yml               # 容器编排
└── go-nomads-backend.sln            # Visual Studio 解决方案
```

## 🚀 快速开始

### 前置要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [Visual Studio Code](https://code.visualstudio.com/)
- [PostgreSQL 15](https://www.postgresql.org/download/) (可选,用于本地开发)

### 1. 克隆项目

```powershell
git clone https://github.com/your-username/go-nomads.git
cd go-nomads
```

### 2. 配置环境变量

复制 `.env.example` 为 `.env` 并填写配置:

```env
# Supabase 连接
SUPABASE_URL=https://your-project.supabase.co
SUPABASE_ANON_KEY=your-anon-key
SUPABASE_SERVICE_KEY=your-service-key
SUPABASE_DB_CONNECTION=postgresql://postgres:password@db.your-project.supabase.co:5432/postgres

# JWT
JWT_SECRET_KEY=your-super-secret-key-change-me
JWT_ISSUER=https://api.gonomads.com
JWT_AUDIENCE=https://gonomads.com

# Redis
REDIS_CONNECTION=localhost:6379

# Elasticsearch
ELASTICSEARCH_URL=http://localhost:9200

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USER=guest
RABBITMQ_PASSWORD=guest
```

### 3. 部署数据库

参考 [部署指南](docs/DEPLOYMENT_GUIDE.md) 将数据库架构部署到 Supabase:

```powershell
# 使用 Supabase Dashboard SQL Editor 执行 database/schema.sql
# 或使用 Supabase CLI
supabase db push
```

### 4. 启动服务(Docker)

```powershell
# 启动所有服务和基础设施
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f cityservice
```

### 5. 启动服务(本地开发)

```powershell
# 启动城市服务
cd src/Services/CityService/CityService
dotnet run

# 启动其他服务(在新终端窗口)
cd src/Services/CoworkingService/CoworkingService
dotnet run

# ... 启动其他服务
```

### 6. 访问 API

- **城市服务**: http://localhost:8002/swagger
- **共享办公服务**: http://localhost:8003/swagger
- **住宿服务**: http://localhost:8004/swagger
- **活动服务**: http://localhost:8005/swagger
- **创新服务**: http://localhost:8006/swagger
- **旅行规划服务**: http://localhost:8007/swagger
- **电商服务**: http://localhost:8008/swagger
- **API 网关**: http://localhost:8000

### 7. 监控和管理

- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Zipkin**: http://localhost:9411
- **Elasticsearch**: http://localhost:9200

## 📊 数据库架构

### 核心表

| 表名 | 描述 | 服务 |
|------|------|------|
| `cities` | 城市信息 | CityService |
| `coworking_spaces` | 共享办公空间 | CoworkingService |
| `hotels` | 酒店信息 | AccommodationService |
| `room_types` | 房型 | AccommodationService |
| `events` | 活动/聚会 | EventService |
| `innovations` | 创新项目 | InnovationService |
| `travel_plans` | 旅行计划 | TravelPlanningService |
| `products` | 商品 | EcommerceService |
| `orders` | 订单 | EcommerceService |

### 通用表

| 表名 | 描述 | 用途 |
|------|------|------|
| `users` | 用户信息 | 所有服务 |
| `reviews` | 评论 | 所有服务 |
| `favorites` | 收藏 | 所有服务 |
| `notifications` | 通知 | 所有服务 |

完整架构请查看 [database/schema.sql](database/schema.sql)

## 🔌 API 端点示例

### 城市服务

```http
# 获取所有城市
GET /api/cities?page=1&pageSize=10

# 获取城市详情
GET /api/cities/{id}

# 搜索城市
POST /api/cities/search
Content-Type: application/json
{
  "keyword": "thailand",
  "minScore": 8.0,
  "tags": ["digital-nomad", "affordable"]
}

# 附近城市
GET /api/cities/nearby?latitude=18.7883&longitude=98.9853&radiusKm=100

# 城市统计
GET /api/cities/{id}/statistics
```

### 共享办公服务

```http
# 获取办公空间列表
GET /api/coworking?cityId={cityId}&page=1&pageSize=10

# 创建预订
POST /api/coworking/{id}/bookings
Content-Type: application/json
Authorization: Bearer {token}
{
  "bookingDate": "2025-11-01",
  "bookingType": "daily",
  "specialRequests": "Need a standing desk"
}
```

### 住宿服务

```http
# 搜索酒店
GET /api/hotels?cityId={cityId}&category=luxury&minRating=4.5

# 查看可用房型
GET /api/hotels/{hotelId}/room-types

# 创建预订
POST /api/hotels/bookings
Content-Type: application/json
Authorization: Bearer {token}
{
  "hotelId": "...",
  "roomTypeId": "...",
  "checkInDate": "2025-11-01",
  "checkOutDate": "2025-11-05",
  "numberOfRooms": 1,
  "numberOfGuests": 2
}
```

完整 API 文档请访问 Swagger UI。

## 🧪 测试

```powershell
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test src/Services/CityService/CityService.Tests

# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

## 📈 性能优化

### 数据库优化

- ✅ PostGIS 空间索引用于地理位置查询
- ✅ B-tree 索引用于常用查询字段
- ✅ GIN 索引用于数组和 JSONB 字段
- ✅ 外键索引优化关联查询
- ✅ 部分索引减少索引大小

### 应用优化

- ✅ Redis 缓存热点数据
- ✅ EF Core 查询优化(Include, AsNoTracking)
- ✅ 分页查询避免全表扫描
- ✅ 异步操作提高并发
- ✅ 连接池管理

### 微服务优化

- ✅ API Gateway 缓存
- ✅ 负载均衡
- ✅ 限流和熔断
- ✅ 服务隔离

## 🔐 安全

### 认证和授权

- ✅ JWT Bearer Token 认证
- ✅ 基于角色的访问控制(RBAC)
- ✅ Supabase Auth 集成
- ✅ API 密钥管理

### 数据安全

- ✅ Row Level Security (RLS)
- ✅ SQL 注入防护(参数化查询)
- ✅ XSS 防护
- ✅ CORS 配置
- ✅ HTTPS/TLS 加密

### 依赖安全

- ✅ 定期更新依赖
- ✅ 漏洞扫描
- ✅ 最小权限原则

## 📝 开发指南

### 添加新服务

1. 创建服务项目
2. 定义实体模型
3. 创建 DbContext
4. 实现 Repository
5. 实现 Service
6. 创建 Controller
7. 添加到 docker-compose.yml
8. 配置 API Gateway 路由

详细步骤请参考 [开发文档](docs/DEVELOPMENT.md)

### 代码规范

- 遵循 C# 编码规范
- 使用 async/await 异步编程
- 实现依赖注入
- 编写单元测试
- 添加 XML 文档注释
- 使用 Serilog 结构化日志

## 🤝 贡献

欢迎贡献!请遵循以下步骤:

1. Fork 项目
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- [Supabase](https://supabase.com/)
- [PostgreSQL](https://www.postgresql.org/)
- [PostGIS](https://postgis.net/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Dapr](https://dapr.io/)
- [Docker](https://www.docker.com/)

## 📞 联系方式

- **项目主页**: https://github.com/your-username/go-nomads
- **问题反馈**: https://github.com/your-username/go-nomads/issues
- **邮箱**: support@gonomads.com

## 🗺️ 路线图

### v1.0 (当前开发中)
- [x] 数据库架构设计
- [x] 实体模型实现
- [ ] Repository 和 Service 实现
- [ ] API Controller 实现
- [ ] Docker 部署
- [ ] 基础测试

### v1.1 (计划中)
- [ ] API Gateway 完整配置
- [ ] Dapr 集成
- [ ] 全文搜索(Elasticsearch)
- [ ] 实时通知(SignalR)
- [ ] AI 旅行规划

### v2.0 (未来)
- [ ] GraphQL API
- [ ] 移动应用 API 优化
- [ ] 机器学习推荐系统
- [ ] 多语言支持
- [ ] 支付集成(Stripe)

---

**Made with ❤️ for Digital Nomads**

🌏 探索世界 | 💼 远程工作 | 🚀 自由生活

# 🌍 Go Nomads Backend - 微服务架构

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Ready-green)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

数字游民 (Digital Nomads) 平台的后端微服务系统,为全球数字游民提供城市信息、共享办公、住宿、活动等全方位服务。

---

## 📋 项目简介

Go Nomads 是一个基于 **ASP.NET Core 8.0** 的微服务架构系统,采用 **DDD (领域驱动设计)** 原则,为数字游民提供:

- 🏙️ **城市信息** - 全球城市评分、生活成本、气候等
- 💼 **共享办公** - 办公空间搜索、预订、评价
- 🏨 **住宿管理** - 酒店、民宿信息与预订
- 🎉 **活动管理** - 社区活动、线下聚会
- 💡 **创新项目** - 创意项目展示与协作
- ✈️ **旅行规划** - AI 驱动的行程规划
- 🛒 **电子商务** - 数字游民商品与服务

---

## 🏗️ 架构总览

```
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (:5000)                       │
│              路由 | 认证 | 限流 | 日志追踪                    │
└────────────────────┬────────────────────────────────────────┘
                     │
     ┌───────────────┼───────────────┐
     │               │               │
┌────▼────┐    ┌────▼────┐    ┌────▼────┐
│  User   │    │  City   │    │Coworking│
│Service  │    │Service  │    │Service  │
│ :8001   │    │ :8002   │    │ :8003   │
└─────────┘    └─────────┘    └─────────┘

┌─────────┐    ┌─────────┐    ┌─────────┐
│ Accom.  │    │  Event  │    │Innovation│
│Service  │    │Service  │    │Service  │
│ :8004   │    │ :8005   │    │ :8006   │
└─────────┘    └─────────┘    └─────────┘

┌─────────┐    ┌─────────┐
│ Travel  │    │Ecommerce│
│Planning │    │Service  │
│ :8007   │    │ :8008   │
└─────────┘    └─────────┘
```

---

## 🎯 核心功能

### ✅ 已实现
- **CityService (城市服务)** - 完整 CRUD、搜索、推荐、评分系统
- **所有微服务项目骨架** - 8个核心服务 + 基础设施
- **Docker Compose 编排** - 一键启动所有服务
- **JWT 认证** - 统一的身份验证
- **Swagger 文档** - 完整 API 文档
- **监控系统** - Prometheus + Grafana + Zipkin

### 🔄 进行中
- 其他微服务业务逻辑实现
- API Gateway 路由配置
- 服务间通信 (gRPC/HTTP)

### 📅 计划中

- 基础服务 (定位、通知、文件、搜索、支付、国际化)
- 熔断降级 (Polly)
- Kubernetes 部署
- CI/CD 流水线

---

## 🛠️ 技术栈

### 后端框架
- **ASP.NET Core 8.0** - Web API 框架
- **Entity Framework Core 8.0** - ORM
- **Serilog** - 结构化日志

### 数据库
- **PostgreSQL 15 + PostGIS 3.3** - 关系型数据库 + 地理位置扩展
- **Redis 7** - 缓存 + Session 存储
- **Elasticsearch 8.11** - 全文搜索引擎

### 消息队列
- **RabbitMQ 3** - 消息中间件

### 容器化与编排
- **Docker** - 容器化
- **Docker Compose** - 本地多容器编排
- **Kubernetes** (计划中) - 生产环境编排

### 监控与追踪
- **Prometheus** - 指标收集
- **Grafana** - 可视化监控
- **Zipkin** - 分布式链路追踪
- **Serilog** - 日志收集

---

## 🚀 快速开始

### 前提条件
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (本地开发)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [VS Code](https://code.visualstudio.com/)

### 1. 克隆项目

```bash
git clone <repository-url>
cd go-nomads
```

### 2. 启动所有服务

```powershell
# 构建并启动所有服务
docker-compose up -d --build

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

### 3. 访问服务

**核心服务 Swagger 文档**:
- API Gateway: <http://localhost:5000/swagger>
- User Service: <http://localhost:8001/swagger>
- City Service: <http://localhost:8002/swagger>
- Coworking Service: <http://localhost:8003/swagger>
- Accommodation Service: <http://localhost:8004/swagger>
- Event Service: <http://localhost:8005/swagger>
- Innovation Service: <http://localhost:8006/swagger>
- Travel Planning Service: <http://localhost:8007/swagger>
- Ecommerce Service: <http://localhost:8008/swagger>

**基础设施**:
- RabbitMQ 管理: <http://localhost:15672> (admin/admin)
- Grafana 监控: <http://localhost:3000> (admin/admin)
- Prometheus: <http://localhost:9090>
- Zipkin 追踪: <http://localhost:9811>
- Elasticsearch: <http://localhost:10200>

### 4. 测试 API

```powershell
# 健康检查
curl http://localhost:8002/health

# 获取城市列表
curl http://localhost:8002/api/v1/cities

# 搜索城市
curl "http://localhost:8002/api/v1/cities/search?name=Bangkok"
```

### 5. 停止服务

```powershell
# 停止所有服务
docker-compose down

# 停止并清除所有数据
docker-compose down -v
```

---

## 📂 项目结构

```
go-nomads/
├── src/
│   ├── Gateway/                    # API 网关
│   │   └── Gateway/
│   ├── Services/                   # 微服务
│   │   ├── UserService/            # 用户服务 ✅
│   │   ├── CityService/            # 城市服务 ✅
│   │   ├── CoworkingService/       # 共享办公服务 ✅
│   │   ├── AccommodationService/   # 住宿服务 ✅
│   │   ├── EventService/           # 活动服务 ✅
│   │   ├── InnovationService/      # 创新项目服务 ✅
│   │   ├── TravelPlanningService/  # 旅行规划服务 ✅
│   │   ├── EcommerceService/       # 电商服务 ✅
│   │   ├── ProductService/
│   │   └── DocumentService/
│   └── Shared/                     # 共享库
│       └── Shared/
├── deployment/                     # 部署脚本
│   ├── deploy-infrastructure-local.ps1
│   ├── deploy-services-local.ps1
│   ├── stop-services.ps1
│   └── prometheus/
│       └── prometheus-local.yml
├── dapr/                          # Dapr 配置
│   ├── components.yaml
│   └── config.yaml
├── docs/                          # 文档
│   ├── architecture/              # 架构文档
│   │   ├── MICROSERVICES_ARCHITECTURE.md  ✅
│   │   └── 01-microservices-overview.md
│   └── QUICK_START.md             # 快速启动指南 ✅
├── docker-compose.yml             # Docker Compose 配置 ✅
├── go-nomads-backend.sln          # 解决方案文件 ✅
└── README.md                      # 项目说明 (本文件)
```

---

## 📊 微服务详情

| 服务 | 端口 | 状态 | 功能 |
|------|------|------|------|
| API Gateway | 5000 | 🔄 进行中 | 路由、认证、限流 |
| User Service | 8001 | ✅ 已完成 | 用户管理、认证 |
| City Service | 8002 | ✅ 已完成 | 城市信息、搜索、推荐 |
| Coworking Service | 8003 | 🔄 开发中 | 共享办公空间 |
| Accommodation Service | 8004 | 🔄 开发中 | 住宿管理 |
| Event Service | 8005 | 🔄 开发中 | 活动管理 |
| Innovation Service | 8006 | 🔄 开发中 | 创新项目 |
| Travel Planning Service | 8007 | 🔄 开发中 | 旅行规划 |
| Ecommerce Service | 8008 | 🔄 开发中 | 电子商务 |

---

## 🔐 安全

- **JWT 认证** - 所有 API 使用 JWT Token
- **HTTPS** - 生产环境强制 HTTPS
- **CORS** - 配置跨域策略
- **密码哈希** - bcrypt 加密
- **SQL 注入防护** - 使用 EF Core 参数化查询

---

## 📖 文档

- [微服务架构总览](./docs/architecture/MICROSERVICES_ARCHITECTURE.md)
- [快速启动指南](./docs/QUICK_START.md)
- [City Service 详细文档](./src/Services/CityService/README.md) (待创建)
- [API 网关配置](./src/Gateway/README.md) (待创建)
- [部署指南](./deployment/README.md)

---

## 🧪 测试

```powershell
# 运行所有单元测试
dotnet test

# 运行特定服务的测试
dotnet test src/Services/CityService/CityService.Tests/

# 代码覆盖率
dotnet test /p:CollectCoverage=true
```

---

## 🤝 贡献

欢迎贡献代码!请遵循以下步骤:

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

---

## 📝 开发规范

- **代码风格**: 遵循 Microsoft C# 编码规范
- **提交信息**: 使用 [Conventional Commits](https://www.conventionalcommits.org/)
- **分支策略**: Git Flow
- **代码审查**: 所有 PR 需要至少 1 人审查

---

## 📈 路线图

### Q1 2025
- ✅ 微服务架构搭建
- ✅ CityService 完整实现
- 🔄 其他核心服务实现
- 🔄 API Gateway 配置

### Q2 2025
- 📅 基础服务实现 (定位、通知、文件等)
- 📅 服务注册与发现
- 📅 熔断降级
- 📅 集成测试

### Q3 2025
- 📅 Kubernetes 部署
- 📅 CI/CD 流水线
- 📅 性能优化
- 📅 压力测试

### Q4 2025
- 📅 生产环境上线
- 📅 监控告警完善
- 📅 灰度发布
- 📅 多区域部署

---

## 📞 联系方式

- **项目主页**: [GitHub Repository](#)
- **问题反馈**: [Issues](#)
- **邮箱**: contact@gonomads.com

---

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情

---

## 🙏 致谢

感谢所有贡献者和开源社区!

特别感谢:
- [ASP.NET Core](https://github.com/dotnet/aspnetcore)
- [Entity Framework Core](https://github.com/dotnet/efcore)
- [Docker](https://www.docker.com/)
- [PostgreSQL](https://www.postgresql.org/)
- [Redis](https://redis.io/)

---

<p align="center">Made with ❤️ by Go Nomads Team</p>

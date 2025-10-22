# Go Nomads 微服务架构总览

## 📊 项目结构

```
go-nomads-backend/
├── src/
│   ├── Gateway/                    # API 网关 (端口: 5000)
│   │   └── Gateway/
│   ├── Services/                   # 微服务集合
│   │   ├── UserService/            # 用户服务 (端口: 8001)
│   │   ├── CityService/            # 城市服务 (端口: 8002) ✅ NEW
│   │   ├── CoworkingService/       # 共享办公服务 (端口: 8003) ✅ NEW
│   │   ├── AccommodationService/   # 住宿服务 (端口: 8004) ✅ NEW
│   │   ├── EventService/           # 活动服务 (端口: 8005) ✅ NEW
│   │   ├── InnovationService/      # 创新项目服务 (端口: 8006) ✅ NEW
│   │   ├── TravelPlanningService/  # 旅行规划服务 (端口: 8007) ✅ NEW
│   │   ├── EcommerceService/       # 电商服务 (端口: 8008) ✅ NEW
│   │   ├── ProductService/         # 产品服务 (端口: 5002)
│   │   └── DocumentService/        # 文档服务
│   └── Shared/                     # 共享库
│       └── Shared/
├── deployment/                     # 部署脚本
├── dapr/                          # Dapr 配置
├── docker-compose.yml             # Docker Compose 配置 ✅ UPDATED
└── go-nomads-backend.sln          # 解决方案文件 ✅ UPDATED
```

## 🎯 核心微服务 (8个)

### 1️⃣ UserService - 用户服务
**端口**: 8001  
**数据库**: PostgreSQL  
**缓存**: Redis

**功能**:
- 用户注册/登录 (邮箱、手机、社交账号)
- JWT Token 认证
- 用户资料管理
- 密码重置/修改
- 用户设置与权限管理

**API 端点**:
```
POST   /api/v1/auth/register
POST   /api/v1/auth/login
POST   /api/v1/auth/logout
GET    /api/v1/users/{id}
PUT    /api/v1/users/{id}
DELETE /api/v1/users/{id}
```

---

### 2️⃣ CityService - 城市服务 ✅
**端口**: 8002  
**数据库**: PostgreSQL + PostGIS  
**缓存**: Redis  
**搜索**: Elasticsearch

**功能**:
- 城市信息管理 (CRUD)
- 城市搜索/筛选 (按地区、气候、生活成本)
- 城市评分系统 (整体、网络、安全、成本等)
- 城市标签管理
- 地理位置服务 (PostGIS)
- 城市推荐算法
- 城市统计信息

**已实现**:
- ✅ 完整的 Repository 层
- ✅ Service 层业务逻辑
- ✅ RESTful API Controller
- ✅ JWT 认证保护
- ✅ PostGIS 地理位置支持
- ✅ Redis 缓存
- ✅ Swagger 文档
- ✅ Dockerfile 配置

**API 端点**:
```
GET    /api/v1/cities                    # 获取城市列表 (分页)
GET    /api/v1/cities/{id}              # 获取城市详情
GET    /api/v1/cities/search            # 搜索城市 (多条件筛选)
GET    /api/v1/cities/recommend         # 推荐城市
GET    /api/v1/cities/{id}/statistics   # 城市统计数据
POST   /api/v1/cities                   # 创建城市 [需授权]
PUT    /api/v1/cities/{id}              # 更新城市 [需授权]
DELETE /api/v1/cities/{id}              # 删除城市 [需授权]
```

**数据模型**:
```csharp
public class City
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    public string? Region { get; set; }
    public string? Description { get; set; }
    public Point? Location { get; set; }        // PostGIS Geography
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Population { get; set; }
    public string? Climate { get; set; }
    public string? TimeZone { get; set; }
    public string? Currency { get; set; }
    public decimal? AverageCostOfLiving { get; set; }
    
    // 评分系统 (0-10)
    public decimal? OverallScore { get; set; }
    public decimal? InternetQualityScore { get; set; }
    public decimal? SafetyScore { get; set; }
    public decimal? CostScore { get; set; }
    public decimal? CommunityScore { get; set; }
    public decimal? WeatherScore { get; set; }
    
    public List<string> Tags { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

### 3️⃣ CoworkingService - 共享办公服务 ✅
**端口**: 8003  
**数据库**: PostgreSQL + PostGIS  
**缓存**: Redis

**功能**:
- 共享办公空间管理 (CRUD)
- 空间搜索/筛选 (按城市、价格、设施)
- 预订管理
- 空间评价系统
- WiFi 速度记录
- 空间收藏功能

**API 端点**:
```
GET    /api/v1/coworking
GET    /api/v1/coworking/{id}
POST   /api/v1/coworking
PUT    /api/v1/coworking/{id}
DELETE /api/v1/coworking/{id}
GET    /api/v1/coworking/search
GET    /api/v1/coworking/{id}/reviews
POST   /api/v1/coworking/{id}/reviews
GET    /api/v1/coworking/{id}/bookings
POST   /api/v1/coworking/{id}/bookings
```

---

### 4️⃣ AccommodationService - 住宿服务 ✅
**端口**: 8004  
**数据库**: PostgreSQL  
**缓存**: Redis

**功能**:
- 酒店/民宿管理 (CRUD)
- 住宿搜索/筛选
- 价格日历
- 预订管理
- 评价系统
- 收藏功能

**API 端点**:
```
GET    /api/v1/hotels
GET    /api/v1/hotels/{id}
POST   /api/v1/hotels
PUT    /api/v1/hotels/{id}
DELETE /api/v1/hotels/{id}
GET    /api/v1/hotels/search
GET    /api/v1/hotels/{id}/availability
POST   /api/v1/hotels/{id}/bookings
GET    /api/v1/hotels/{id}/reviews
```

---

### 5️⃣ EventService - 活动服务 ✅
**端口**: 8005  
**数据库**: PostgreSQL  
**缓存**: Redis  
**消息队列**: RabbitMQ

**功能**:
- 活动创建/管理 (CRUD)
- 活动报名系统
- 活动搜索/筛选
- 活动提醒通知 (RabbitMQ)
- 活动评价
- 活动日历

**API 端点**:
```
GET    /api/v1/events
GET    /api/v1/events/{id}
POST   /api/v1/events
PUT    /api/v1/events/{id}
DELETE /api/v1/events/{id}
GET    /api/v1/events/upcoming
POST   /api/v1/events/{id}/register
DELETE /api/v1/events/{id}/register
GET    /api/v1/events/{id}/attendees
```

---

### 6️⃣ InnovationService - 创新项目服务 ✅
**端口**: 8006  
**数据库**: PostgreSQL  
**缓存**: Redis

**功能**:
- 创意项目管理 (CRUD)
- 项目展示
- 项目评论/点赞
- 项目搜索
- 项目分类/标签
- 文件上传管理

**API 端点**:
```
GET    /api/v1/innovations
GET    /api/v1/innovations/{id}
POST   /api/v1/innovations
PUT    /api/v1/innovations/{id}
DELETE /api/v1/innovations/{id}
POST   /api/v1/innovations/{id}/like
POST   /api/v1/innovations/{id}/comments
POST   /api/v1/innovations/upload
```

---

### 7️⃣ TravelPlanningService - 旅行规划服务 ✅
**端口**: 8007  
**数据库**: PostgreSQL  
**缓存**: Redis  
**AI 引擎**: OpenAI API

**功能**:
- AI 旅行计划生成
- 行程管理 (CRUD)
- 路线优化
- 预算计算
- 景点推荐
- 行程分享

**API 端点**:
```
POST   /api/v1/travel-plans/generate
GET    /api/v1/travel-plans
GET    /api/v1/travel-plans/{id}
PUT    /api/v1/travel-plans/{id}
DELETE /api/v1/travel-plans/{id}
POST   /api/v1/travel-plans/{id}/optimize
GET    /api/v1/travel-plans/{id}/share
```

---

### 8️⃣ EcommerceService - 电商服务 ✅
**端口**: 8008  
**数据库**: PostgreSQL  
**缓存**: Redis

**功能**:
- 商品管理 (CRUD)
- 购物车
- 订单管理
- 支付集成
- 物流跟踪
- 优惠券系统

**API 端点**:
```
GET    /api/v1/products
GET    /api/v1/products/{id}
POST   /api/v1/cart
GET    /api/v1/cart
DELETE /api/v1/cart/{itemId}
POST   /api/v1/orders
GET    /api/v1/orders
GET    /api/v1/orders/{id}
POST   /api/v1/orders/{id}/pay
```

---

## 🗄️ 基础设施服务

### PostgreSQL + PostGIS
- **端口**: 5432
- **用途**: 主数据库 + 地理位置扩展
- **数据库列表**:
  - `userservice_db`
  - `cityservice_db`
  - `coworkingservice_db`
  - `accommodationservice_db`
  - `eventservice_db`
  - `innovationservice_db`
  - `travelplanningservice_db`
  - `ecommerceservice_db`

### Redis
- **端口**: 6379
- **用途**: 缓存 + Session 存储 + 分布式锁

### Elasticsearch
- **端口**: 9200, 9300
- **用途**: 全文搜索 (城市、住宿、活动等)

### RabbitMQ
- **端口**: 5672 (AMQP), 15672 (管理界面)
- **用途**: 消息队列 (活动通知、异步任务)
- **默认账号**: admin / admin

### Zipkin
- **端口**: 9411
- **用途**: 分布式链路追踪

### Prometheus
- **端口**: 9090
- **用途**: 监控指标收集

### Grafana
- **端口**: 3000
- **用途**: 监控可视化
- **默认账号**: admin / admin

---

## 🚀 快速启动

### 1. 启动所有服务
```bash
docker-compose up -d
```

### 2. 查看服务状态
```bash
docker-compose ps
```

### 3. 查看日志
```bash
# 查看所有服务日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f city-service
```

### 4. 停止所有服务
```bash
docker-compose down
```

### 5. 停止并删除数据卷
```bash
docker-compose down -v
```

---

## 🌐 服务端口映射

| 服务 | 容器端口 | 主机端口 | 协议 |
|------|---------|---------|------|
| API Gateway | 80 | 5000 | HTTP |
| UserService | 80 | 8001 | HTTP |
| CityService | 8002 | 8002 | HTTP |
| CoworkingService | 8003 | 8003 | HTTP |
| AccommodationService | 8004 | 8004 | HTTP |
| EventService | 8005 | 8005 | HTTP |
| InnovationService | 8006 | 8006 | HTTP |
| TravelPlanningService | 8007 | 8007 | HTTP |
| EcommerceService | 8008 | 8008 | HTTP |
| ProductService | 80 | 5002 | HTTP |
| PostgreSQL | 5432 | 5432 | TCP |
| Redis | 6379 | 6379 | TCP |
| Elasticsearch | 9200/9300 | 9200/9300 | HTTP/TCP |
| RabbitMQ | 5672 | 5672 | AMQP |
| RabbitMQ管理 | 15672 | 15672 | HTTP |
| Zipkin | 9411 | 9411 | HTTP |
| Prometheus | 9090 | 9090 | HTTP |
| Grafana | 3000 | 3000 | HTTP |

---

## 🔐 安全配置

### JWT 认证
所有服务使用统一的 JWT 配置:
```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration123456",
    "Issuer": "GoNomadsAPI",
    "Audience": "GoNomadsClient",
    "ExpiryMinutes": 60
  }
}
```

### CORS 策略
开发环境允许所有来源,生产环境需配置白名单。

---

## 📊 监控与日志

### 日志
所有服务使用 Serilog 记录日志:
- **控制台输出**: 实时查看
- **文件输出**: `logs/servicename-{Date}.txt`

### 监控
- **Prometheus**: 收集各服务指标
- **Grafana**: 可视化展示
- **Zipkin**: 分布式追踪

---

## 🛠️ 技术栈

### 后端框架
- **ASP.NET Core 8.0**
- **Entity Framework Core 8.0**

### 数据库
- **PostgreSQL 15** + **PostGIS 3.3** (地理位置)
- **Redis 7** (缓存)
- **Elasticsearch 8.11** (搜索)

### 消息队列
- **RabbitMQ 3**

### 容器化
- **Docker** + **Docker Compose**

### 监控
- **Prometheus** + **Grafana**
- **Zipkin** (链路追踪)
- **Serilog** (日志)

---

## 📈 下一步计划

### 待实现的基础服务 (6个)

1. **LocationService** - 定位服务 (端口: 9001)
   - GPS 定位
   - 地理编码/反编码
   - 距离计算
   - 附近搜索

2. **NotificationService** - 通知服务 (端口: 9002)
   - Push 通知
   - 邮件通知
   - 短信通知
   - 站内消息

3. **FileService** - 文件服务 (端口: 9003)
   - 文件上传/下载
   - 图片压缩
   - 视频处理
   - CDN 加速

4. **SearchService** - 搜索服务 (端口: 9004)
   - 全文搜索
   - 自动补全
   - 搜索建议
   - 热门搜索

5. **PaymentService** - 支付服务 (端口: 9005)
   - 支付网关集成
   - 订单支付
   - 退款管理
   - 账单管理

6. **I18nService** - 国际化服务 (端口: 9006)
   - 多语言管理
   - 翻译缓存
   - 语言包更新

### 架构优化

- [ ] 实现 API Gateway 路由配置
- [ ] 添加服务间通信 (gRPC/HTTP)
- [ ] 实现服务注册与发现 (Consul/Nacos)
- [ ] 添加熔断降级 (Polly)
- [ ] 实现配置中心
- [ ] 添加健康检查
- [ ] 实现自动扩缩容 (Kubernetes)

---

## 📝 开发指南

### 添加新微服务步骤

1. 创建项目骨架:
```bash
cd src/Services
dotnet new webapi -n YourService -o YourService/YourService --no-https
```

2. 添加必要的 NuGet 包
3. 实现 Models、DTOs、Repositories、Services、Controllers
4. 配置 Program.cs (数据库、认证、日志等)
5. 添加到解决方案:
```bash
dotnet sln add src/Services/YourService/YourService/YourService.csproj
```
6. 更新 docker-compose.yml
7. 创建 Dockerfile

---

## 🎯 总结

✅ **已完成**:
- 8 个核心业务微服务架构
- 完整的 CityService 实现
- Docker Compose 多服务编排
- 基础设施服务配置
- 监控与日志系统

📌 **架构特点**:
- **高内聚低耦合**: 每个服务职责清晰
- **可扩展性**: 服务可独立扩展
- **高可用性**: 多实例部署 + 熔断降级
- **易维护性**: 服务独立部署和更新
- **完整的技术栈**: 涵盖所有核心功能

🚀 **准备就绪**: 可以开始开发和测试各个微服务!

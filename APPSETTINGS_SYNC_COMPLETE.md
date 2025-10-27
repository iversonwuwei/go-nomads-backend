# 配置文件同步完成

## 概述
已将所有微服务的 `appsettings.json` 完整配置同步到 `appsettings.Development.json` 文件中,确保本地调试和 Docker 部署使用一致的配置。

## 同步时间
2025-01-XX

## 同步的服务列表

### 1. CityService ✅
- **文件位置**: `src/Services/CityService/CityService/appsettings.Development.json`
- **同步内容**:
  - Supabase 配置
  - ConnectionStrings (SupabaseDb, DefaultConnection, Redis)
  - JwtSettings 和 Jwt 配置
  - Serilog 配置
  - Weather API 配置 (OpenWeatherMap)
  - Consul 配置 (使用容器名称: `go-nomads-consul:8500`)

### 2. EventService ✅
- **文件位置**: `src/Services/EventService/EventService/appsettings.Development.json`
- **同步内容**:
  - Supabase 配置
  - ConnectionStrings (SupabaseDb)
  - Jwt 配置
  - Dapr 配置 (GrpcPort: 50001, HttpPort: 3505)
  - Serilog 配置
  - Consul 配置 (使用容器名称: `go-nomads-consul:8500`)

### 3. CoworkingService ✅
- **文件位置**: `src/Services/CoworkingService/CoworkingService/appsettings.Development.json`
- **同步内容**:
  - Supabase 配置
  - ConnectionStrings (SupabaseDb)
  - Jwt 配置
  - Dapr 配置 (GrpcPort: 50001, HttpPort: 3506)
  - Serilog 配置
  - Consul 配置 (使用容器名称: `go-nomads-consul:8500`)

### 4. UserService ✅
- **文件位置**: `src/Services/UserService/UserService/appsettings.Development.json`
- **同步内容**:
  - Kestrel 端点配置 (Grpc: 50002, Http: 8080)
  - Supabase 配置
  - ConnectionStrings (SupabaseDb)
  - Jwt 配置
- **注意**: 移除了旧的 Consul 配置 (localhost 地址)

### 5. ProductService ✅
- **文件位置**: `src/Services/ProductService/ProductService/appsettings.Development.json`
- **同步内容**:
  - AllowedHosts 配置
- **注意**: 原 appsettings.json 仅包含基础配置,已同步

### 6. DocumentService ✅
- **文件位置**: `src/Services/DocumentService/DocumentService/appsettings.Development.json`
- **同步内容**:
  - AllowedHosts 配置
  - Services 配置 (Gateway, ProductService, UserService URLs)
- **注意**: 移除了旧的 Consul 配置 (localhost 地址)

### 7. AccommodationService ✅
- **文件位置**: `src/Services/AccommodationService/AccommodationService/appsettings.Development.json`
- **同步内容**:
  - Supabase 配置
  - ConnectionStrings (SupabaseDb)
  - Jwt 配置

### 8. TravelPlanningService ✅
- **文件位置**: `src/Services/TravelPlanningService/TravelPlanningService/appsettings.Development.json`
- **同步内容**:
  - Supabase 配置
  - ConnectionStrings (SupabaseDb)
  - Jwt 配置

### 9. InnovationService ✅
- **文件位置**: `src/Services/InnovationService/InnovationService/appsettings.Development.json`
- **同步内容**:
  - Supabase 配置
  - ConnectionStrings (SupabaseDb)
  - Jwt 配置

### 10. EcommerceService ✅
- **文件位置**: `src/Services/EcommerceService/EcommerceService/appsettings.Development.json`
- **同步内容**:
  - Supabase 配置
  - ConnectionStrings (SupabaseDb)
  - Jwt 配置

## 关键配置项说明

### Consul 服务发现
- **地址**: `http://go-nomads-consul:8500`
- **原因**: Docker 容器部署需要使用容器名称进行服务发现
- **影响的服务**: CityService, EventService, CoworkingService

### Supabase 数据库
- **URL**: `https://lcfbajrocmjlqndkrsao.supabase.co`
- **连接字符串**: `Host=db.lcfbajrocmjlqndkrsao.supabase.co;Port=6543;...`
- **影响的服务**: 所有主要业务服务

### Dapr 配置
- **GrpcPort**: 50001 (标准端口)
- **HttpPort**: 3505 (EventService), 3506 (CoworkingService)
- **UseGrpc**: true
- **影响的服务**: EventService, CoworkingService

### JWT 认证
- **Issuer**: `https://lcfbajrocmjlqndkrsao.supabase.co/auth/v1`
- **Audience**: `authenticated`
- **AccessTokenExpirationMinutes**: 60
- **RefreshTokenExpirationDays**: 7
- **影响的服务**: 所有需要身份验证的服务

### Weather API (仅 CityService)
- **Provider**: OpenWeatherMap
- **ApiKey**: `e56757161fdf117eb32158ff0244eb87`
- **BaseUrl**: `https://api.openweathermap.org/data/2.5`
- **Language**: `zh_cn`

## 变更摘要

### 移除的配置
1. **localhost Consul 地址**: 旧配置使用 `http://localhost:8500`,已改为容器名称
2. **过时的 Consul 服务配置**: 部分服务的 ServiceAddress、ServicePort、ServiceVersion 配置已移除

### 新增的配置
1. **完整 Supabase 配置**: 为所有需要数据库访问的服务添加
2. **JWT 配置**: 统一的认证配置
3. **Dapr 配置**: 为微服务通信添加标准配置

## 验证步骤

### 1. 检查配置文件语法
```bash
# 使用 dotnet 工具验证 JSON 格式
dotnet build
```

### 2. 验证 Docker 容器启动
```bash
# 重新构建并启动服务
docker-compose down
docker-compose up -d
```

### 3. 验证服务健康检查
```bash
# CityService
curl http://localhost:8002/health

# EventService
curl http://localhost:8005/health

# CoworkingService
curl http://localhost:8006/health
```

### 4. 验证服务间通信
```bash
# 测试 Gateway -> CityService -> CoworkingService 流程
curl http://localhost:5000/api/v1/home/cities-with-coworking
```

## 后续工作

### 立即执行
1. ✅ 配置文件同步完成
2. ⏳ 重新构建 CityService Docker 镜像
3. ⏳ 重启所有服务容器
4. ⏳ 端到端测试 coworking_home 页面

### 未来优化
1. 考虑使用环境变量管理敏感配置 (API Keys, ConnectionStrings)
2. 将 Consul 地址配置化,支持本地调试和容器部署自动切换
3. 添加配置验证单元测试

## 注意事项

⚠️ **重要提醒**:
- 本次同步后,本地调试和 Docker 部署将使用完全相同的配置
- Consul 地址使用容器名称 `go-nomads-consul`,本地调试需要确保 Consul 容器运行
- 所有敏感信息 (数据库密码、API Keys) 已包含在配置文件中,请勿提交到公共代码仓库

## 相关文档
- [Docker Compose 配置](../../docker-compose.yml)
- [Dapr 组件配置](../../dapr/components.yaml)
- [服务部署脚本](../../deployment/)

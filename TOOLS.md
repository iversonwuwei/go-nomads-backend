# TOOLS.md - Local Notes

## 服务端口分配

| 服务 | 本地端口 | Docker 端口 |
|------|----------|------------|
| Gateway | 5000 | 80 |
| UserService | 8001 | 8001 |
| CityService | 8002 | 8002 |
| CoworkingService | 8003 | 8006 |
| EventService | 8004 | 8004 |
| AIService | 8005 | 8005 |
| MessageService | 8006 | 5005 |
| CacheService | 8007 | 8007 |
| SearchService | 8008 | 8008 |
| InnovationService | 8009 | 8009 |
| AccommodationService | 8010 | 8010 |
| ProductService | 8011 | 8011 |

## 基础设施

- Redis: `localhost:6379`
- RabbitMQ: `localhost:5672`（管理 `15672`）
- Elasticsearch: `localhost:9200`
- Supabase PostgreSQL: 外部托管

## Docker 镜像仓库

- SWR: `swr.ap-southeast-3.myhuaweicloud.com/go-nomads`

## 常用命令

```bash
# Aspire 启动
dotnet run --project src/GoNomads.AppHost

# 单服务启动
dotnet run --project src/Services/CityService/CityService

# Docker 基础设施
docker compose -f docker-compose-infras.yml up -d

# Docker 全部服务
docker compose -f docker-compose-services-swr.yml up -d
```
- Default speaker: Kitchen HomePod
```

## Why Separate?

Skills are shared. Your setup is yours. Keeping them apart means you can update skills without losing your notes, and share skills without leaking your infrastructure.

---

Add whatever helps you do your job. This is your cheat sheet.

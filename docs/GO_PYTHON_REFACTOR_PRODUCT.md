# Go + Python Backend Refactor Product Spec

## 1. Goal

将当前 .NET backend 按现有工程设计重构为 Go + Python 架构，并保持对 Web、Admin、Flutter、HarmonyOS 客户端的外部功能完全一致。

- Go 承担 API Gateway、所有业务 API、业务编排、高并发请求处理、消息消费、缓存、搜索、支付、配置、通知和实时通信入口。
- Python 仅承担 AI 图片生成 sidecar 服务，不承载业务 API、不持有业务状态、不直接对客户端开放公共业务能力。
- 对外 REST 路径、响应 envelope、鉴权行为、错误语义、消息事件、健康检查和实时推送能力必须保持兼容。

## 2. Current Baseline

本基线来自 2026-05-07 对 `src/Services` 和 `src/Gateway` 的只读扫描。

| Area | Current Count | Migration Rule |
| --- | ---: | --- |
| Services | 13 | 每个服务都必须有 Go 迁移归属或明确废弃依据；本次目标是不废弃。 |
| Controllers | 68 | 所有 Controller 路由必须进入契约清单。 |
| HTTP action attributes | 514 | Go 实现必须通过契约测试逐项覆盖。 |
| Hubs | 5 | 实时能力必须保持客户端可用，包含聊天、通知、AI 进度、活动。 |

现有服务包括：Gateway、UserService、CityService、CoworkingService、AccommodationService、EventService、InnovationService、AIService、MessageService、CacheService、SearchService、ProductService、ConfigService、DocumentService。

## 3. Product Scope

### In Scope

- 保留所有现有 REST API 路径和请求/响应结构。
- 保留 Gateway 的动态服务路由、JWT 验证、用户上下文透传、限流、HTTP Method Override、健康检查。
- 保留用户认证、用户资料、角色、技能、兴趣、会员、支付、法律协议、举报、旅行历史、访问地点、用户统计等能力。
- 保留城市、城市评分、地区页签、用户内容、照片、花费、点评、优缺点、城市管理员、版主申请/转让、GeoNames、指标、AI 城市图片触发等能力。
- 保留共享办公、住宿、活动、创新项目、商品、搜索、缓存、配置、消息通知、聊天、腾讯 IM、文档聚合等能力。
- 保留 AI 文本业务能力，但由 Go 实现业务编排；Python 只接收图片生成任务。
- 保留 AI 图片生成：通义万象任务创建、状态轮询、图片下载、Supabase Storage 上传、Redis 任务状态、RabbitMQ 进度/完成消息。
- 保留 RabbitMQ 消息契约，包括 AIProgressMessage、CityImageGeneratedMessage、AITaskCompletedMessage、AITaskFailedMessage、CityUpdatedMessage、CityRatingUpdatedMessage、CityReviewUpdatedMessage、CoworkingVerificationVotesMessage、SearchSyncMessages、UserUpdatedMessage、TravelPlanTaskMessage、DigitalNomadGuideTaskMessage、AIChatStreamMessages、ChatRoomOnlineStatusMessage。
- 保留 Supabase/PostgreSQL、Redis、RabbitMQ、Elasticsearch 的数据语义和配置来源。

### Out of Scope

- 不改变前端功能、路由命名或业务文案。
- 不把 Python 扩大成通用 AI 后端或业务服务。
- 不在迁移中顺手重设计数据库模型，除非某个字段无法等价映射，并需单独审批。
- 不删除现有 .NET 服务，直到对应 Go/Python 服务完成契约、数据、消息和回滚验证。

## 4. Compatibility Requirements

### API Compatibility

- 所有 `/api/v1/**`、`/api/**`、`/health`、Hub 路径保持原路径。
- 继续使用当前 `ApiResponse<T>` 风格：`success`/`message`/`data`/`errors` 或既有服务大小写差异必须按现有客户端兼容。
- 分页响应继续兼容当前 PaginatedResponse/totalCount 语义。
- 错误状态码不能收敛成统一 500；需保留 400、401、403、404、409、429 等语义。

### Auth Compatibility

- Gateway 继续验证 Supabase/JWT HS256 token。
- Gateway 继续向下游透传 `X-User-Id`、`X-User-Email`、`X-User-Role`。
- 当前公开路径和 GET 公开路径保持一致：认证、公开城市/酒店/共享办公/商品浏览、法律文档、App 配置、健康检查。
- `/api/v1/admin/**` 与 `/api/v1/reports` 继续视为管理员或受控权限路径。

### Realtime Compatibility

- 当前 Hub 路径包括 `/hubs/meetup`、`/hubs/notifications`、`/hubs/ai-progress`、`/hubs/chat`。
- 迁移时必须先确认客户端使用的是 SignalR 协议还是普通 WebSocket；若使用 SignalR，Go 实现必须提供协议兼容层，或以客户端版本门禁发布兼容迁移。

### AI Image Sidecar Boundary

- Go API 接收客户端请求，校验用户/城市/任务幂等，然后调用 Python sidecar。
- Python sidecar 不直接访问业务表；只允许访问图片供应商、对象存储和必要的短期任务状态接口。
- 图片生成完成后，Go 负责发布 RabbitMQ 业务消息和更新城市业务数据；Python 返回结构化结果，不发布业务事件。

## 5. Non-Functional Requirements

- Go 服务必须支持水平扩展、无状态 HTTP 处理、连接池、超时、重试、熔断和请求级 trace id。
- Python sidecar 必须限制并发、对图片供应商限流友好、支持任务超时和重试，并暴露健康检查。
- 所有服务必须暴露 `/health`；生产路径必须支持 OpenTelemetry traces、metrics、structured logs。
- Secrets 必须从环境变量、Secret Manager 或部署平台注入，不能在新实现中硬编码。
- 高风险路由迁移必须支持按路由灰度、快速回切 .NET 旧服务。

## 6. Delivery Gates

每个域迁移都必须同时满足：

1. API 契约测试通过：路径、方法、状态码、响应字段、分页、错误语义。
2. 数据等价测试通过：同一输入在 .NET 与 Go 产生同等数据库状态。
3. 消息等价测试通过：RabbitMQ exchange/queue/routing/payload 与现有消费者兼容。
4. 观测闭环通过：日志可定位用户、资源、任务、失败原因；metrics/traces 可关联 Gateway 和下游服务。
5. 回滚可执行：Gateway 路由可从 Go 服务切回 .NET 服务，数据无不可逆 schema 改动。

## 7. Acceptance Criteria

- Web、Admin、Flutter、HarmonyOS 现有关键链路无需改客户端即可通过。
- `dotnet` 旧服务与 Go/Python 新服务可在同一 Docker Compose/Kubernetes 环境中并行运行。
- Gateway 可以按服务或按路径切换到 Go 实现。
- Python sidecar 只暴露内部图片生成 API，不暴露公共业务 API。
- 全量契约清单中的路由均有 Go 处理器或明确兼容代理。
- AI 图片生成链路保持：城市 API 创建任务 -> Go 调用 Python -> 上传图片 -> Go 发布完成消息 -> City/Message 侧消费 -> 客户端收到进度或结果。

## 8. Rollback Strategy

- 采用 Strangler Fig 迁移：先让新 Go Gateway 代理旧 .NET 服务，再逐个路径切换到 Go 服务。
- 每个路由有 `backend_target=dotnet|go` 的可配置开关。
- 数据库 schema 首阶段不做破坏性迁移；新增表/列必须可空或旁路。
- Python sidecar 故障时，Go 返回任务失败状态并保留旧 AIService 图片生成路由回切能力。

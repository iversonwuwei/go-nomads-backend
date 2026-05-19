# Go + Python Backend Refactor Design

## 1. Target Architecture

```text
Clients
  -> Go Gateway
       -> Go User Domain
       -> Go City Domain
       -> Go Coworking Domain
       -> Go Accommodation Domain
       -> Go Event Domain
       -> Go Innovation Domain
       -> Go AI Text/Travel Domain
       -> Go Message/Realtime Domain
       -> Go Cache/Search/Product/Config/Document Domains
       -> Python Image Generation Sidecar
  -> PostgreSQL/Supabase, Redis, RabbitMQ, Elasticsearch, Object Storage
```

The Go backend owns public APIs and business state. The Python sidecar is an internal worker-like HTTP service used only for AI image generation.

## 2. Repository Layout

Proposed additive layout while keeping the current .NET code available for rollback:

```text
go-nomads-backend/
├── go-backend/
│   ├── go.mod
│   ├── cmd/
│   │   ├── gateway/
│   │   ├── user-service/
│   │   ├── city-service/
│   │   ├── coworking-service/
│   │   ├── accommodation-service/
│   │   ├── event-service/
│   │   ├── innovation-service/
│   │   ├── ai-service/
│   │   ├── message-service/
│   │   ├── cache-service/
│   │   ├── search-service/
│   │   ├── product-service/
│   │   ├── config-service/
│   │   └── document-service/
│   ├── internal/
│   │   ├── gateway/
│   │   ├── domains/
│   │   ├── transport/http/
│   │   ├── transport/ws/
│   │   ├── messaging/
│   │   ├── persistence/
│   │   ├── cache/
│   │   ├── search/
│   │   ├── auth/
│   │   ├── observability/
│   │   └── config/
│   ├── pkg/contracts/
│   └── tests/
├── python-sidecars/
│   └── image-generation/
│       ├── pyproject.toml
│       ├── app/
│       └── tests/
└── src/                         # Existing .NET services kept during migration
```

## 3. Go Service Layering

Each Go service follows the current backend DDD boundary:

```text
transport/http or transport/ws
  -> application use cases
       -> domain entities/value objects/repository interfaces
       -> infrastructure repositories/clients/messages
```

Rules:

- HTTP handlers only bind request, call application service, map response/error.
- Application layer owns business rules, idempotency, permission decisions and orchestration.
- Infrastructure layer owns Supabase/PostgreSQL, Redis, RabbitMQ, Elasticsearch, payment providers and sidecar clients.
- Shared contracts stay in `pkg/contracts` and must mirror existing C# DTO/message shapes.

## 4. Gateway Design

The Go Gateway replaces YARP behavior with equivalent route matching and reverse proxy support during migration.

Required features:

- Dynamic upstreams from `ServiceUrls__*` compatible environment keys.
- Current route prefixes and ordering from `ServiceUrlProxyConfigProvider`.
- JWT validation using current Supabase issuer/audience/secret configuration.
- User context propagation: `X-User-Id`, `X-User-Email`, `X-User-Role`.
- Public route rules and public GET prefixes equivalent to current Gateway.
- Rate limits equivalent to current policies: login, register, api, strict and per-IP concurrency.
- HTTP Method Override before routing.
- WebSocket/Hubs proxy or native handler support.
- `/health` and OpenAPI/diagnostic endpoint parity.

During migration, Gateway route targets must be configurable per prefix:

```yaml
routes:
  /api/v1/cities: go-city-service
  /api/v1/users: dotnet-user-service
  /api/v1/ai/images: go-ai-service
```

The additive Go Gateway exposes this as environment configuration so each path can move independently and roll back without changing clients:

- `GO_GATEWAY_ROUTE_TARGETS`: comma, semicolon, or newline separated `prefix=service-name` entries. More specific prefixes win by existing longest-prefix matching, and configured entries take precedence over built-in defaults for the same prefix.
- `GO_GATEWAY_UPSTREAMS`: comma, semicolon, or newline separated `service-name=http://host:port` entries. These register extra upstream names for dual-run targets such as `go-city-service` and `dotnet-city-service`.
- Existing `ServiceUrls__*` variables remain supported for the default .NET-compatible service names.

Example:

```bash
GO_GATEWAY_UPSTREAMS="go-city-service=http://go-city-service:5202,dotnet-city-service=http://city-service:5202"
GO_GATEWAY_ROUTE_TARGETS="/api/v1/cities=go-city-service;/api/v1/admin/city-reviews=dotnet-city-service"
```

## 5. Domain Migration Map

| Current Service | Go Ownership | Notes |
| --- | --- | --- |
| Gateway | `cmd/gateway`, `internal/gateway` | First service to implement; starts as proxy-only. |
| UserService | `cmd/user-service`, `internal/domains/user` | Auth, profile, roles, membership, payment, reports. |
| CityService | `cmd/city-service`, `internal/domains/city` | City data, rating, guide, nearby, user content, moderators. |
| CoworkingService | `cmd/coworking-service`, `internal/domains/coworking` | Spaces, reviews, bookings, verification, comments. |
| AccommodationService | `cmd/accommodation-service`, `internal/domains/accommodation` | Hotels, rooms, reviews, admin moderation. |
| EventService | `cmd/event-service`, `internal/domains/event` | Events, event types, join/follow/invitations, meetup realtime. |
| InnovationService | `cmd/innovation-service`, `internal/domains/innovation` | Projects, likes, comments, teams. |
| AIService | `cmd/ai-service`, `internal/domains/ai` | Text chat, travel plans, community, budget, visa, OpenClaw; image generation calls Python. |
| MessageService | `cmd/message-service`, `internal/domains/message` | Notifications, chats, Tencent IM, AI progress, WebSocket/Hubs. |
| CacheService | `cmd/cache-service`, `internal/domains/cache` | Score/cost cache APIs. |
| SearchService | `cmd/search-service`, `internal/domains/search` | Elasticsearch search and index sync. |
| ProductService | `cmd/product-service`, `internal/domains/product` | Product CRUD. |
| ConfigService | `cmd/config-service`, `internal/domains/config` | App config, option groups, static texts, snapshots. |
| DocumentService | `cmd/document-service`, `internal/domains/document` | API aggregation/spec endpoints. |

## 6. Python Image Sidecar

### Responsibilities

- Create DashScope/Wanx image synthesis tasks.
- Poll task status with bounded attempts and timeout.
- Download generated image bytes with retry.
- Upload images to Supabase Storage or return bytes/URLs to Go based on selected mode.
- Enforce concurrency limits for city image generation.
- Return deterministic error objects to Go.

### Non-Responsibilities

- No public client routes.
- No JWT business authorization.
- No city database update.
- No RabbitMQ business event publishing.
- No AI text chat, travel planning, visa, community, budget or OpenClaw logic.

### Internal API

```http
POST /internal/v1/images/generate
POST /internal/v1/images/city
GET  /internal/v1/images/tasks/{taskId}
GET  /health
```

The Go AI service preserves public routes:

```http
POST /api/v1/ai/images/generate
POST /api/v1/ai/images/city
POST /api/v1/ai/images/city/async
GET  /api/v1/ai/images/tasks/{taskId}
POST /api/v1/cities/{cityId}/generate-images
```

## 7. AI Image Flow

```text
Client/Admin
  -> Go Gateway
  -> Go CityService POST /api/v1/cities/{cityId}/generate-images
  -> Go AIService creates task state in Redis
  -> Python sidecar generates portrait + landscape images
  -> Python returns image result to Go
  -> Go AIService publishes AIProgressMessage and CityImageGeneratedMessage
  -> Go CityService updates city image fields
  -> Go MessageService pushes progress/result to realtime clients
```

Required parity details:

- Portrait size: `720*1280`, storage prefix `portrait/{cityId}`.
- Landscape size: `1280*720`, count 4, storage prefix `landscape/{cityId}`.
- Default bucket: `city-photos`.
- City request dedupe by `cityId`.
- City generation concurrency limit: 3.
- Task cache key compatibility: `task:image:{taskId}` with 24 hour expiry.
- Existing fallback user behavior must be replaced with explicit service user or authenticated user policy before production cutover.

## 8. Messaging Design

Use Go RabbitMQ publisher/consumer code that preserves exchange/queue naming compatible with current MassTransit consumers during dual-run.

Message contracts must be generated from C# shared messages into Go structs and JSON schema tests. During mixed mode, payloads must be accepted by both .NET and Go consumers.

Critical events:

- `AIProgressMessage`
- `CityImageGeneratedMessage`
- `AITaskCompletedMessage`
- `AITaskFailedMessage`
- `CityUpdatedMessage`
- `CityRatingUpdatedMessage`
- `CityReviewUpdatedMessage`
- `CoworkingVerificationVotesMessage`
- `SearchSyncMessages`
- `UserUpdatedMessage`
- `TravelPlanTaskMessage`
- `DigitalNomadGuideTaskMessage`
- `AIChatStreamMessages`
- `ChatRoomOnlineStatusMessage`

## 9. Data Access

- Prefer PostgreSQL driver + SQL/query builder for Go services.
- Preserve current Supabase schema and RLS assumptions.
- Do not change table names or JSON field names during phase 1.
- Use explicit row mapping and JSONB converters for fields currently serialized by .NET models.
- Add repository-level integration tests for pagination, nullable fields, JSONB and datetime UTC behavior.

## 10. Observability

All Go and Python services must emit:

- Structured JSON logs with `trace_id`, `span_id`, `service`, `user_id`, `resource_id`, `operation`, `status`, `error`.
- OpenTelemetry traces around Gateway -> service -> DB/cache/message/sidecar calls.
- Prometheus metrics for request count, duration, error count, queue publish/consume count, image task duration and sidecar failure count.
- Health checks for DB, Redis, RabbitMQ, Elasticsearch and Python sidecar dependencies.

## 11. Validation Strategy

### Contract Tests

- Generate OpenAPI from current .NET services.
- Generate OpenAPI from Go services.
- Diff paths, methods, request bodies, response bodies and status codes.
- Keep an allowlist only for intentional internal-only endpoints.

### Golden Behavior Tests

- Replay request fixtures against .NET and Go in isolated databases.
- Compare response envelope, status code, persisted rows and emitted messages.

### Load Tests

- Gateway proxy concurrency.
- City list/search and user profile read paths.
- Chat/notifications realtime connections.
- AI image async task burst with sidecar concurrency limit.

### Rollback Tests

- Flip route from Go to .NET without client change.
- Confirm task state and messages remain readable after route flip.

## 12. Implementation Phases

### Phase 0: Contract Freeze

- Commit API inventory and message inventory.
- Add contract tests around current .NET behavior.
- Externalize hardcoded secrets before introducing new runtimes.

### Phase 1: Go Gateway Proxy

- Implement Go Gateway with route parity.
- Proxy every current route to .NET services.
- Validate auth, rate limit, health, WebSocket proxy and method override.
- Add route-level target overrides and extra upstream registration so each prefix can be shifted between .NET and Go independently.

### Phase 2: Python Image Sidecar

- Implement internal HTTP sidecar. The first additive version can use the Python standard library to avoid new runtime dependencies; FastAPI can be introduced later if request validation, OpenAPI, and async worker ergonomics justify the dependency.
- Add Go AI client wrapper while old .NET AIService remains fallback.
- Validate image task status, upload paths and failure handling.

### Phase 3: Go AI Image Public Routes

- Move `/api/v1/ai/images/*` and `/api/v1/cities/{cityId}/generate-images` orchestration to Go.
- Keep text AI routes on .NET until contract tests exist.

### Phase 4: Domain-by-Domain Go Ports

- Migrate low-risk read-heavy domains first: Config, Cache, Product, Search.
- Then migrate City/Coworking/Accommodation/Event/Innovation.
- Then migrate User/Payment/Message/AI text flows.

#### Phase 4a: Config Public Read Slice

The first domain slice is ConfigService public app configuration because it is read-heavy, already published through immutable snapshots, and can roll back by route prefix.

- Go owns only `GET /api/v1/app/config` and `GET /api/v1/app/config/version` in this slice.
- Admin paths under `/api/v1/admin/config`, `/api/v1/admin/option-groups`, and `/api/v1/admin/static-texts` remain on .NET until write-path golden tests exist.
- Go reads `public.app_config_snapshots` directly and filters `is_published = true` and `is_deleted = false`, matching the current `SupabaseConfigSnapshotRepository.GetPublishedAsync()` behavior.
- Go preserves the response envelope and locale fallback: requested locale -> `zh-CN` -> empty static text map.
- Gateway canary uses `GO_GATEWAY_UPSTREAMS="go-config-service=http://go-config-service:5213"` and `GO_GATEWAY_ROUTE_TARGETS="/api/v1/app/config=go-config-service"`.
- Rollback removes the route target or points `/api/v1/app/config` back to `config-service`.

#### Phase 4b: Product Public Read Slice

The second domain slice is ProductService public read behavior because the current .NET service uses in-memory sample data, has no persistence or message side effects on reads, and already exposes a narrow public GET surface.

- Go owns only `GET /api/v1/products`, `GET /api/v1/products/{id}`, `GET /api/v1/products/user/{userId}`, `GET /api/v1/products/health`, and service `GET /health` in this slice.
- Product write paths `POST /api/v1/products`, `PUT /api/v1/products/{id}`, and `DELETE /api/v1/products/{id}` remain on .NET until auth and write-path parity tests exist.
- Go preserves the current sample product payload shape, success/error envelope, pagination fields, and page/pageSize normalization.
- Gateway canary uses `GO_GATEWAY_UPSTREAMS="go-product-service=http://go-product-service:5002"` and `GO_GATEWAY_ROUTE_TARGETS="/api/v1/products=go-product-service"`.
- Rollback removes the route target or points `/api/v1/products` back to `product-service`.

#### Phase 4c: Cache Read-Through Query Slice

The third domain slice is CacheService read-through query behavior because its read endpoints have a narrow DTO surface, reuse existing Redis-backed cache semantics, and can fall back to the current upstream CityService and CoworkingService calculations without introducing new write ownership.

- Go owns only `GET /api/v1/cache/costs/city/{cityId}`, `POST /api/v1/cache/costs/city/batch`, `GET /api/v1/cache/scores/city/{cityId}`, `POST /api/v1/cache/scores/city/batch`, `GET /api/v1/cache/scores/coworking/{coworkingId}`, `POST /api/v1/cache/scores/coworking/batch`, and service `GET /health` in this slice.
- Cache write and invalidation paths remain on .NET: `PUT /api/v1/cache/costs/city/{cityId}`, `DELETE /api/v1/cache/costs/city/{cityId}`, `PUT /api/v1/cache/scores/city/{cityId}`, `PUT /api/v1/cache/scores/coworking/{coworkingId}`, `DELETE /api/v1/cache/scores/city/{cityId}`, `POST /api/v1/cache/scores/city/invalidate-batch`, `DELETE /api/v1/cache/scores/coworking/{coworkingId}`, `POST /api/v1/cache/scores/coworking/invalidate-batch`, and `POST /api/v1/cache/scores/city/cleanup-zero-scores`.
- Go preserves the current DTO shape exactly: bare `ScoreResponseDto`, `BatchScoreResponseDto`, `CostResponseDto`, and `BatchCostResponseDto` responses without adding a new envelope.
- Go preserves current read-through semantics: Redis hit returns `FromCache=true`; cache miss calls the existing CityService or CoworkingService compatible endpoints, serializes statistics the same way as .NET, stores refreshed cache entries, and returns `FromCache=false`.
- Because the Gateway canary only switches by prefix, the Go Cache service must transparently proxy unsupported write and invalidation paths back to the existing .NET CacheService during mixed-mode rollout.
- Gateway canary uses `GO_GATEWAY_UPSTREAMS="go-cache-service=http://go-cache-service:5210"` and `GO_GATEWAY_ROUTE_TARGETS="/api/v1/cache=go-cache-service"`.
- Cache routes stay protected through the Go Gateway; canary validation must confirm missing JWT is still rejected and only the owned read-through paths are handled by Go.
- Rollback removes the route target or points `/api/v1/cache` back to `cache-service`.

#### Phase 4d: Search Public Query Slice

The fourth domain slice is SearchService public query behavior because `/api/v1/search/**` is already isolated from `/api/v1/index/**`, has no write-path ownership, and acts mainly as a thin Elasticsearch query wrapper with a stable response envelope.

- Go owns only `GET /api/v1/search`, `GET /api/v1/search/cities`, `GET /api/v1/search/coworkings`, `GET /api/v1/search/suggest`, and service `GET /health` in this slice.
- Index maintenance paths under `/api/v1/index/**` remain fully on .NET during Phase 4d, including health/stats, sync, and rebuild operations.
- Go preserves the current `ApiResponse<T>` envelope and `SearchResult<T>`, `UnifiedSearchResult`, and `SuggestResponse` DTO field names, including camelCase JSON, `totalPages`, `hasMore`, and optional `highlights`/`suggestions` fields.
- Go preserves current Elasticsearch-backed semantics for empty query, fuzzy multi-match query, pagination, and combined city/coworking search. Search query reads directly from the existing Elasticsearch indexes using the current `Elasticsearch__Url` and `IndexSettings__*` configuration keys.
- Gateway canary uses `GO_GATEWAY_UPSTREAMS="go-search-service=http://go-search-service:5215"` and `GO_GATEWAY_ROUTE_TARGETS="/api/v1/search=go-search-service"`.
- Search query routes remain public through the Go Gateway; canary validation must confirm `/api/v1/index/**` still resolves to the .NET SearchService and is not accidentally swept into the public slice.
- Rollback removes the route target or points `/api/v1/search` back to `search-service`.

#### Phase 4e: City Region Tabs Read Slice

The fifth domain slice starts CityService migration with the lowest-risk public read endpoint and keeps all complex city behavior on .NET.

- Go owns only `GET /api/v1/cities/region-tabs` and service `GET /health` in this slice.
- The Go implementation preserves `ApiResponse<List<CityRegionTabDto>>` envelope shape and field names (`key`, `label`, `cityCount`, `displayOrder`).
- Go reads existing `public.cities` and `public.countries` data and applies the same region bucket/order rules (`Asia`, `Europe`, `North America`, `South America`, `Oceania`, `Africa`, `Middle East`, fallback `Other`).
- All other `/api/v1/cities/**` paths remain on .NET in this slice; the Go City service proxies unsupported city paths to the current .NET CityService during mixed-mode canary.
- Gateway canary uses `GO_GATEWAY_UPSTREAMS="go-city-service=http://go-city-service:5202"` and `GO_GATEWAY_ROUTE_TARGETS="/api/v1/cities/region-tabs=go-city-service"`.
- Rollback removes the route target or points `/api/v1/cities/region-tabs` back to `city-service`.

### Phase 5: Decommission .NET

- Only after all contract tests, golden tests, load tests and rollback drills pass.
- Keep old images available for one release window.

## 13. Progress Baseline (2026-05-07)

The current migration status is additive and mixed-mode. The Go Gateway plus Python sidecar can replace selected prefixes, but full .NET decommission is not yet allowed.

| Service Domain | Current Ownership | Status | Replacement Scope Now |
| --- | --- | --- | --- |
| Gateway | Go | completed | `go-gateway` can front all routes and route by prefix to .NET or Go upstreams. |
| AI image | Go + Python | completed | `ai/images` public image routes are replaceable through Go + sidecar. |
| Config | Go (public read) + .NET (admin) | partial | replace `GET /api/v1/app/config` and `GET /api/v1/app/config/version`. |
| Product | Go (public read) + .NET (write) | partial | replace `GET /api/v1/products/**`; keep write APIs on .NET. |
| Cache | Go (query read-through) + .NET (write/invalidate) | partial | replace `/api/v1/cache` prefix with Go read ownership and write proxy fallback. |
| Search | Go (public query) + .NET (index maintenance) | partial | replace `/api/v1/search/**`; keep `/api/v1/index/**` on .NET. |
| City | Go (region-tabs) + .NET (other city APIs) | partial | replace `GET /api/v1/cities/region-tabs`; keep all other city routes on .NET. |
| Coworking | .NET | not started | not replaceable yet. |
| Accommodation | .NET | not started | not replaceable yet. |
| Event | .NET | not started | not replaceable yet. |
| Innovation | .NET | not started | not replaceable yet. |
| User/Auth/Payment | .NET | not started | not replaceable yet. |
| Message/Realtime | .NET | not started | not replaceable yet. |

### Why full replacement is blocked

- Major business prefixes still have no Go ownership: user, city, coworking, accommodation, event, innovation, message, and most AI text routes.
- Realtime and hub parity (`/hubs/chat`, `/hubs/notifications`, `/hubs/ai-progress`, `/hubs/meetup`) is not migrated.
- Write-path parity for Config/Product/Cache/Search index lifecycle is incomplete.
- Cross-service operational parity (contract diff, load, rollback drill per domain) is incomplete.

## 14. Progress-Driven Replacement Plan (to full .NET decommission)

### Wave A (already replaceable in overlay)

- Prefixes routed to Go by default in refactor overlay:
  - `/api/v1/app/config` -> `go-config-service`
  - `/api/v1/products` -> `go-product-service`
  - `/api/v1/cache` -> `go-cache-service`
  - `/api/v1/search` -> `go-search-service`
  - `/api/v1/cities/region-tabs` -> `go-city-service`
- AI image routes stay on `go-ai-service` via `ServiceUrls__AIImageService`.
- Rollback remains prefix-level and instant through `GO_GATEWAY_ROUTE_TARGETS`.

### Wave B (next replacement set)

- Implement Go ownership for City and Coworking public read + key write paths.
- Add canary route targets per prefix and rollback smoke scripts.
- Gate to pass before moving next wave:
  - contract diff clean for migrated prefixes
  - golden behavior tests for read and write
  - JWT/auth parity tests through Go Gateway
  - load test and rollback drill record

### Wave C

- Implement Accommodation, Event, Innovation.
- Include admin and moderation paths with idempotency and audit parity.

### Wave D (highest risk)

- Implement User/Auth/Payment and Message/Realtime.
- Cut over hub/realtime paths only after connection soak and reconnect behavior parity.

### Final Gate (Phase 5 hard gate)

All items below must be true before shutting down .NET service runtime:

1. Every Gateway route prefix has a Go owner with verified contract parity.
2. All critical write paths pass golden persistence + idempotency checks.
3. Realtime and message bus paths are parity-tested under load.
4. Prefix-level rollback drills are recorded and reproducible.
5. One release window runs with .NET as standby fallback only.

# Go + Python Refactor Runbook

## Current Phase

Phase 1 has started with additive, rollback-safe services:

- Go Gateway proxy-only entrypoint: `go-backend/cmd/gateway`.
- Go AI image public route entrypoint: `go-backend/cmd/ai-service`.
- Python image generation sidecar internal entrypoint: `python-sidecars/image-generation`.
- Compose overlay: `docker-compose.go-python-refactor.yml`.

The existing .NET services remain the source of truth. The Go Gateway initially proxies to them and can be removed without touching database state.

Current implementation limits:

- The Python sidecar is a dry-run implementation that returns compatible image DTOs without calling Wanx or Supabase Storage yet.
- The Go AI image service preserves `/api/v1/ai/images/**` response envelopes and calls the sidecar.
- Async task state uses Redis when `Redis__ConnectionString` is configured and falls back to memory for local dry-runs.
- RabbitMQ publishing for `AIProgressMessage` and `CityImageGeneratedMessage` is enabled when `RabbitMQ__HostName` or `RabbitMQ__Host` is configured; otherwise it uses a no-op publisher.
- The next migration gate is validating MassTransit consumers end-to-end against the Go-published envelopes before production traffic cutover.

## Local Validation

Run unit tests:

```bash
cd go-backend
go test ./...
go test ./internal/config -run 'TestGolden|TestGet' -count=1
go test ./internal/gateway -run 'TestConfigCanary|TestAdminConfigRouteRequiresAuth' -count=1
go test ./internal/product -run 'TestGolden|TestGet' -count=1
go test ./internal/gateway -run 'TestProductCanary|TestProductWriteRouteRequiresAuth' -count=1
go test ./internal/cache -run 'TestGolden|TestGet|TestBatch' -count=1
go test ./internal/gateway -run 'TestCacheCanary|TestCacheWriteRouteRequiresAuth' -count=1
go test ./internal/search -run 'TestGolden|TestSearch|TestSuggest' -count=1
go test ./internal/gateway -run 'TestSearchCanary|TestIndexRouteStaysOnDotnet' -count=1
go test ./internal/city -run 'TestRegionTabs|TestUnowned' -count=1
go test ./internal/gateway -run 'TestCityRegionTabsCanary|TestCityListRouteStaysOnDotnet' -count=1
go test ./internal/city -run 'TestRegionTabs|TestUnowned' -count=1
go test ./internal/gateway -run 'TestCityRegionTabsCanary|TestCityListRouteStaysOnDotnet' -count=1

cd ../python-sidecars/image-generation
../../.venv/bin/python -m unittest discover -s tests
```

The Config public read slice keeps golden JSON fixtures under `go-backend/internal/config/testdata`. These fixtures lock the external response envelope for:

- `GET /api/v1/app/config?locale=en-US`
- `GET /api/v1/app/config/version`
- locale fallback from unknown locale to `zh-CN`

The Gateway package also verifies that:

- `/api/v1/app/config` can be route-canary switched to the Go upstream.
- `/api/v1/app/config/version` follows the same canary prefix.
- `/api/v1/admin/config/**` still requires JWT and does not drift into the public whitelist.

The Product public read slice keeps golden JSON fixtures under `go-backend/internal/product/testdata`. These fixtures lock the external response envelope for:

- `GET /api/v1/products?page=1&pageSize=10`
- `GET /api/v1/products/{id}`
- `GET /api/v1/products/user/{userId}?page=1&pageSize=10`

The Gateway package also verifies that:

- `/api/v1/products` can be route-canary switched to the Go Product upstream.
- Product GET routes remain public through the Gateway.
- `POST /api/v1/products` still requires JWT and does not drift into the public whitelist.

The Cache read-through query slice keeps golden JSON fixtures under `go-backend/internal/cache/testdata`. These fixtures lock the external DTO shape for:

- `GET /api/v1/cache/costs/city/{cityId}`
- `POST /api/v1/cache/costs/city/batch`
- `GET /api/v1/cache/scores/city/{cityId}`
- `POST /api/v1/cache/scores/city/batch`
- `GET /api/v1/cache/scores/coworking/{coworkingId}`
- `POST /api/v1/cache/scores/coworking/batch`

The Gateway package also verifies that:

- `/api/v1/cache/**` can be route-canary switched to the Go Cache upstream.
- Cache query routes still require JWT through the Gateway and do not drift into the public whitelist.
- Cache write and invalidation paths are forwarded by the Go Cache service back to the .NET CacheService during the canary window.

The Search public query slice keeps golden JSON fixtures under `go-backend/internal/search/testdata`. These fixtures lock the external response envelope for:

- `GET /api/v1/search`
- `GET /api/v1/search/cities`
- `GET /api/v1/search/coworkings`
- `GET /api/v1/search/suggest`

The Gateway package also verifies that:

- `/api/v1/search/**` can be route-canary switched to the Go Search upstream.
- Search query routes remain public through the Gateway.
- `/api/v1/index/**` still routes to the .NET SearchService and is not accidentally captured by the canary.

Run RabbitMQ message smoke tests when a broker is available:

```bash
cd go-backend
GO_NOMADS_RABBITMQ_INTEGRATION=1 \
RabbitMQ__HostName=localhost \
RabbitMQ__Port=5672 \
RabbitMQ__UserName=walden \
RabbitMQ__Password=walden \
go test ./internal/ai -run TestRabbitMQPublisherPublishesMassTransitEnvelope -count=1
```

This smoke test publishes MassTransit-compatible envelopes to the same default exchanges used by the .NET consumers:

- `Shared.Messages:AIProgressMessage`
- `Shared.Messages:CityImageGeneratedMessage`

It is skipped unless `GO_NOMADS_RABBITMQ_INTEGRATION=1` is set.

Run the additive containers beside the current stack:

```bash
cd go-backend
mkdir -p bin
CGO_ENABLED=0 GOOS=linux go build -o bin/go-gateway ./cmd/gateway
CGO_ENABLED=0 GOOS=linux go build -o bin/go-ai-service ./cmd/ai-service
CGO_ENABLED=0 GOOS=linux go build -o bin/go-config-service ./cmd/config-service
CGO_ENABLED=0 GOOS=linux go build -o bin/go-product-service ./cmd/product-service
CGO_ENABLED=0 GOOS=linux go build -o bin/go-cache-service ./cmd/cache-service
CGO_ENABLED=0 GOOS=linux go build -o bin/go-search-service ./cmd/search-service
CGO_ENABLED=0 GOOS=linux go build -o bin/go-city-service ./cmd/city-service

cd ..
docker compose -f docker-compose.yml -f docker-compose.go-python-refactor.yml up -d --build go-gateway go-ai-service go-config-service go-product-service go-cache-service go-search-service go-city-service image-generation-sidecar
```

Endpoints:

- Go Gateway health: `http://localhost:5081/health`
- Go AI image service health: `http://localhost:5221/health`
- Go Config public read service is intentionally exposed only inside the Compose network to avoid colliding with the .NET ConfigService host port. Verify it through the Go Gateway canary route.
- Go Product public read service is intentionally exposed only inside the Compose network to avoid colliding with the .NET ProductService host port. Verify it through the Go Gateway canary route.
- Go Cache read-through query service is intentionally exposed only inside the Compose network to avoid colliding with the .NET CacheService host port. Verify it through the Go Gateway canary route.
- Go Search public query service is intentionally exposed only inside the Compose network to avoid colliding with the .NET SearchService host port. Verify it through the Go Gateway canary route.
- Go City region-tabs service is intentionally exposed only inside the Compose network to avoid colliding with the .NET CityService host port. Verify it through the Go Gateway canary route.
- Python sidecar health: `http://localhost:5222/health`
- Go AI image generation: `POST http://localhost:5221/api/v1/ai/images/generate`
- Go AI city image generation: `POST http://localhost:5221/api/v1/ai/images/city`
- Go AI async city image task: `POST http://localhost:5221/api/v1/ai/images/city/async`
- Go Config public app config via Gateway canary: `GET http://localhost:5081/api/v1/app/config?locale=zh-CN`
- Go Config public app config version via Gateway canary: `GET http://localhost:5081/api/v1/app/config/version`
- Go Product list via Gateway canary: `GET http://localhost:5081/api/v1/products?page=1&pageSize=10`
- Go Product detail via Gateway canary: `GET http://localhost:5081/api/v1/products/1`
- Go Product user list via Gateway canary: `GET http://localhost:5081/api/v1/products/user/1?page=1&pageSize=10`
- Go Cache city cost via Gateway canary: `GET http://localhost:5081/api/v1/cache/costs/city/{cityId}`
- Go Cache city cost batch via Gateway canary: `POST http://localhost:5081/api/v1/cache/costs/city/batch`
- Go Cache city score via Gateway canary: `GET http://localhost:5081/api/v1/cache/scores/city/{cityId}`
- Go Cache city score batch via Gateway canary: `POST http://localhost:5081/api/v1/cache/scores/city/batch`
- Go Cache coworking score via Gateway canary: `GET http://localhost:5081/api/v1/cache/scores/coworking/{coworkingId}`
- Go Cache coworking score batch via Gateway canary: `POST http://localhost:5081/api/v1/cache/scores/coworking/batch`
- Go Search unified query via Gateway canary: `GET http://localhost:5081/api/v1/search?query=nomad&type=all&page=1&pageSize=20`
- Go Search cities via Gateway canary: `GET http://localhost:5081/api/v1/search/cities?query=lisbon&page=1&pageSize=20`
- Go Search coworkings via Gateway canary: `GET http://localhost:5081/api/v1/search/coworkings?query=hub&page=1&pageSize=20`
- Go Search suggest via Gateway canary: `GET http://localhost:5081/api/v1/search/suggest?prefix=li&type=all&size=10`
- Go City region tabs via Gateway canary: `GET http://localhost:5081/api/v1/cities/region-tabs`
- Python dry-run image generation: `POST http://localhost:5222/internal/v1/images/generate`
- Python dry-run city image generation: `POST http://localhost:5222/internal/v1/images/city`

Run the Config canary smoke check after the overlay is up:

```bash
sh scripts/verify-go-config-canary.sh http://localhost:5081
sh scripts/verify-go-product-canary.sh http://localhost:5081
sh scripts/verify-go-cache-canary.sh http://localhost:5081 TOKEN
sh scripts/verify-go-search-canary.sh http://localhost:5081
sh scripts/verify-go-city-canary.sh http://localhost:5081
```

The scripts call the Go Gateway and verify the Config, Product, Cache, Search, and City canary endpoints return the expected envelope or DTO shape.

## Configuration

- `JWT_SECRET`, `JWT_ISSUER`, and `JWT_AUDIENCE` feed the Go Gateway `Jwt__*` settings.
- `ServiceUrls__*` stays compatible with the current .NET Gateway/AppHost service URL names.
- `ServiceUrls__AIImageService` narrows only `/api/v1/ai/images/**` to the Go AI image service; all other `/api/v1/ai/**` paths continue to use `ServiceUrls__AIService`.
- `GO_GATEWAY_UPSTREAMS` registers additional upstream names for dual-run targets. Use comma, semicolon, or newline separated `service-name=http://host:port` entries.
- `GO_GATEWAY_ROUTE_TARGETS` overrides route targets by prefix. Use comma, semicolon, or newline separated `prefix=service-name` entries. Configured prefixes can point traffic to a Go implementation or back to the .NET service without client changes.
- `GO_CONFIG_LISTEN_ADDRESS` defaults the Go Config public read service to `:5213`.
- `GO_PRODUCT_LISTEN_ADDRESS` defaults the Go Product public read service to `:5002`.
- `GO_CACHE_LISTEN_ADDRESS` defaults the Go Cache read-through query service to `:5210`.
- `GO_SEARCH_LISTEN_ADDRESS` defaults the Go Search public query service to `:5215`.
- `GO_CITY_LISTEN_ADDRESS` defaults the Go City canary service to `:5202`.
- `POSTGRES_CONNECTION_STRING`, `ConnectionStrings__Postgres`, or `ConnectionStrings__DefaultConnection` points Go Config at the existing PostgreSQL/Supabase database.
- `POSTGRES_CONNECTION_STRING`, `ConnectionStrings__Postgres`, or `ConnectionStrings__DefaultConnection` points Go City at the existing PostgreSQL/Supabase database.
- `REDIS_CONNECTION_STRING`, `Redis__ConnectionString`, or `ConnectionStrings__Redis` points Go Cache at the existing Redis instance.
- `Elasticsearch__Url` remains the authoritative SearchService Elasticsearch endpoint key; do not replace it with a `ConnectionStrings__*` variant.
- `IndexSettings__CityIndex` and `IndexSettings__CoworkingIndex` remain the authoritative SearchService index-name keys.
- `ServiceUrls__CityService` and `ServiceUrls__CoworkingService` stay compatible with the current service names used by Cache read-through calls.
- `IMAGE_SIDECAR_URL` points the Go AI image service to the Python sidecar.
- `IMAGE_SIDECAR_PUBLIC_BASE_URL` controls dry-run public image URL output.
- `IMAGE_SIDECAR_MAX_CITY_CONCURRENCY` defaults to `3`, matching the current AIService city image concurrency limit.

Example route canary:

```bash
GO_GATEWAY_UPSTREAMS="go-config-service=http://go-config-service:5213,dotnet-config-service=http://config-service:5213"
GO_GATEWAY_ROUTE_TARGETS="/api/v1/app/config=go-config-service;/api/v1/admin/config=dotnet-config-service"
```

Example Product route canary:

```bash
GO_GATEWAY_UPSTREAMS="go-product-service=http://go-product-service:5002,dotnet-product-service=http://product-service:5002"
GO_GATEWAY_ROUTE_TARGETS="/api/v1/products=go-product-service"
```

Example Cache route canary:

```bash
GO_GATEWAY_UPSTREAMS="go-cache-service=http://go-cache-service:5210,dotnet-cache-service=http://cache-service:5210"
GO_GATEWAY_ROUTE_TARGETS="/api/v1/cache=go-cache-service"
REDIS_CONNECTION_STRING="go-nomads-redis:6379,abortConnect=false"
GO_CACHE_DOTNET_UPSTREAM="http://cache-service:5210"
```

Example Search route canary:

```bash
GO_GATEWAY_UPSTREAMS="go-search-service=http://go-search-service:5215,dotnet-search-service=http://search-service:5215"
GO_GATEWAY_ROUTE_TARGETS="/api/v1/search=go-search-service"
Elasticsearch__Url="http://elasticsearch:9200"
IndexSettings__CityIndex="cities"
IndexSettings__CoworkingIndex="coworking_spaces"
```

Example City region-tabs canary:

```bash
GO_GATEWAY_UPSTREAMS="go-city-service=http://go-city-service:5202,dotnet-city-service=http://city-service:5202"
GO_GATEWAY_ROUTE_TARGETS="/api/v1/cities/region-tabs=go-city-service"
POSTGRES_CONNECTION_STRING="postgres://USER:PASSWORD@HOST:5307/DATABASE?sslmode=disable"
GO_CITY_DOTNET_UPSTREAM="http://city-service:5202"
```

Progress-driven replacement baseline in the refactor overlay:

```bash
GO_GATEWAY_ROUTE_TARGETS="/api/v1/app/config=go-config-service,/api/v1/products=go-product-service,/api/v1/cache=go-cache-service,/api/v1/search=go-search-service,/api/v1/cities/region-tabs=go-city-service"
```

This default reflects the currently completed Go slices and keeps non-migrated prefixes on .NET. Override this variable explicitly when running targeted canary, rollback, or domain-by-domain cutover drills.

Rollback the canary by removing the matching `GO_GATEWAY_ROUTE_TARGETS` entry or pointing the prefix back to the `dotnet-*` upstream.

The current Go Config service intentionally covers only public read paths. Keep Admin config paths on .NET until write-path contract and golden persistence tests are in place.

The current Go Product service intentionally covers only public read paths. Keep Product write paths on .NET until write-path auth and parity tests are in place.

The current Go Cache service intentionally covers only read-through query paths. Keep cache write, invalidation, and cleanup paths on .NET until write-path parity and mixed-route canary tests are in place.

The current Go Search service intentionally covers only public query paths. Keep `/api/v1/index/**` maintenance and rebuild operations on .NET until index lifecycle parity and operational rollback tests are in place.

The current Go City service intentionally covers only `GET /api/v1/cities/region-tabs`. Keep all other `/api/v1/cities/**` paths on .NET until broader city read/write parity and rollback tests are in place.

Recommended Config canary environment:

```bash
GO_GATEWAY_UPSTREAMS="go-config-service=http://go-nomads-go-config-service:5213"
GO_GATEWAY_ROUTE_TARGETS="/api/v1/app/config=go-config-service"
POSTGRES_CONNECTION_STRING="postgres://USER:PASSWORD@HOST:5307/DATABASE?sslmode=disable"
```

## Rollback

- Stop only the additive services:

```bash
docker compose -f docker-compose.yml -f docker-compose.go-python-refactor.yml stop go-gateway go-ai-service image-generation-sidecar
```

- Existing .NET Gateway on `5080` and all existing services remain unchanged.
- Removing `ServiceUrls__AIImageService` or stopping `go-ai-service` returns `/api/v1/ai/images/**` to the existing .NET AIService target.
- Removing a `GO_GATEWAY_ROUTE_TARGETS` entry restores the built-in default route target. Pointing the same prefix to a `dotnet-*` upstream performs an explicit route-level rollback during mixed Go/.NET operation.

## Next Migration Gate

Before switching any client traffic to Go Gateway, verify:

- Public GET routes proxy correctly without auth.
- Protected routes reject missing/invalid JWT and forward valid JWT claims as `X-User-*`.
- WebSocket/SignalR paths are exercised through the proxy.
- Route-by-route fallback to the existing .NET Gateway remains available.

## Full .NET Replacement Gate

Do not decommission .NET services until the following are all satisfied:

- All Gateway route prefixes in the contract baseline have Go ownership with contract parity evidence.
- All migrated write paths have golden persistence tests and idempotency/error-path verification.
- Realtime hub paths and message contracts pass load and reconnect stability checks.
- Prefix-level rollback drills are documented and reproducible in the same environment.
- A full release window has run with .NET as fallback only and no unresolved parity regressions.

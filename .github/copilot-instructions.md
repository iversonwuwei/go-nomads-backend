# Go Nomads Backend - Copilot Instructions

## Scope
- This file applies to the entire go-nomads-backend workspace.
- Keep instructions minimal and actionable. Link to existing docs instead of copying long sections.
- Default engineering execution should follow `docs/HARNESS_ENGINEERING_CHECKLIST.md` for requirement framing, validation closure, observability, and rollback safety.
- Delivery summaries should follow `docs/HARNESS_DELIVERY_TEMPLATE.md`.

## Build And Test
- Restore/build all services: `dotnet restore && dotnet build go-nomads-backend.sln`
- Run all tests: `dotnet test go-nomads-backend.sln`
- Run one service locally: `dotnet run --project src/Services/<ServiceName>/<ServiceName>/<ServiceName>.csproj`
- Run Aspire orchestration: `dotnet run --project src/GoNomads.AppHost/GoNomads.AppHost/GoNomads.AppHost.csproj`
- Docker local stack: `docker-compose up -d --build` and `docker-compose down`

## Architecture Boundaries
- Gateway entrypoint: `src/Gateway/Gateway`
- Service orchestration: `src/GoNomads.AppHost/GoNomads.AppHost`
- Shared defaults and observability: `src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults`
- Shared cross-service library: `src/Shared/Shared`
- Business services live under `src/Services/*`.

## Coding Conventions
- Use Microsoft C# conventions and async/await; async methods should end with `Async`.
- Preserve existing service layering and responsibilities:
  - Domain: entities/value objects/repository abstractions
  - Application: use cases, DTOs, service abstractions
  - Infrastructure: repository implementations, external integrations, message consumers
  - API: controllers and request/response contracts
- Prefer DI registration in each service Program.cs; do not bypass existing abstractions in Shared unless required.
- Keep changes minimal and local to the relevant service; avoid cross-service refactors unless explicitly requested.

## Project-Specific Pitfalls
- This repo targets .NET 10 (`net10.0`) in service projects; use a compatible SDK before building.
- RabbitMQ config keys are not fully uniform across services. Check `src/GoNomads.AppHost/GoNomads.AppHost/Program.cs` before changing env var names.
- SearchService and AIService use some non-standard config keys (for example Elasticsearch and RabbitMQ variants). Keep existing key names unless migrating all call sites together.

## Documentation Map (Link, Do Not Duplicate)
- Quick local startup and common operations: `docs/QUICK_START.md`
- Deployment and infra rollout: `docs/DEPLOYMENT_GUIDE.md`
- Supabase migration workflow: `docs/SUPABASE_MIGRATION_GUIDE.md`
- Migration status tracking: `docs/SUPABASE_CONVERSION_PROGRESS.md`
- Architecture overview: `docs/architecture/01-microservices-overview.md`
- Detailed microservice architecture: `docs/architecture/MICROSERVICES_ARCHITECTURE.md`

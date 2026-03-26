---
description: "Use when creating or editing service code under src/Services. Enforces DDD layering (Domain, Application, Infrastructure, API) and dependency direction."
applyTo: "src/Services/**"
---
# Backend Service Layering

## Layer Responsibilities

| Layer | Namespace suffix | Contains | May reference |
|-------|-----------------|----------|---------------|
| Domain | `.Domain` | Entities, value objects, repository interfaces, domain events | Nothing else |
| Application | `.Application` | Use-case services, abstractions (interfaces), DTOs, mapping | Domain |
| Infrastructure | `.Infrastructure` | Repository implementations, DbContext, MassTransit consumers, external HTTP/gRPC clients, configuration models | Domain, Application |
| API | (root or `.API`) | Controllers, request/response contracts, `Program.cs` DI wiring | Application (never Domain directly) |

## Rules

- **Dependency flows inward only**: API → Application → Domain. Infrastructure implements Domain/Application interfaces but is only wired via DI in `Program.cs`.
- **No cross-service references**: A service must never `ProjectReference` another service project. Use HTTP/gRPC or messaging instead.
- New files go into the layer matching their responsibility — do not place DTOs in Domain or entities in Application.
- Repository interfaces belong in **Domain**; their Supabase/EF implementations belong in **Infrastructure**.
- Controllers should be thin: delegate logic to Application services; do not put business rules in controllers.

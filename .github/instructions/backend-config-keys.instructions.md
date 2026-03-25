---
description: "Use when modifying RabbitMQ, Elasticsearch, Redis, or Supabase configuration keys, environment variables, or docker-compose env sections. Prevents accidental config key mismatches across services."
applyTo: src/GoNomads.AppHost/**, src/Services/**/Program.cs, src/Services/**/appsettings*.json, docker-compose*.yml
---
# Backend Config Key Conventions

## Known Non-Uniform Keys

| Service | RabbitMQ host key | RabbitMQ user key | Notes |
|---------|-------------------|-------------------|-------|
| Most services | `RabbitMQ__Host` | `RabbitMQ__Username` | Injected via `AddRabbitMqEnv` helper in AppHost |
| AIService | `RabbitMQ__HostName` | `RabbitMQ__UserName` | Non-standard — keep as-is |
| SearchService | `RabbitMQ__Host` | `RabbitMQ__Username` | Also uses `Elasticsearch__Url` (not ConnectionStrings) |

## Rules

- Before adding or renaming any infra config key, check `src/GoNomads.AppHost/GoNomads.AppHost/Program.cs` for the authoritative mapping.
- Do **not** unify non-standard key names in AIService or SearchService unless explicitly migrating all consumers at once.
- Secrets (API keys, passwords, private key paths) must come from environment variables or mounted files — never commit plaintext values in `appsettings.json`.
- When adding a new env var for a service, also add it to the relevant `docker-compose*.yml` and document the expected source (`.env` file, CI secret, mounted volume).

---
description: "Safely refactor backend service code: analyse impact, make minimal changes, run verification. Use when refactoring controllers, services, repositories, or shared code."
agent: "agent"
argument-hint: "Describe what to refactor (e.g., 'extract payment logic from PaymentController into a service')"
---
Perform a safe, minimal refactor following these steps:

1. **Scope analysis** — Identify every file that directly references the target symbol or interface. List them with file path and usage type (call site / implementation / test / config).
2. **Impact assessment** — Flag breaking changes: renamed types, changed method signatures, moved namespaces. Highlight any cross-service boundaries or Shared library impacts.
3. **Plan** — Propose the smallest set of edits that achieves the goal. Explain why each edit is necessary. Do not refactor surrounding code that is not part of the goal.
4. **Implement** — Apply edits while preserving existing code style, namespace conventions, and DI registration patterns.
5. **Verify** — Run `dotnet build go-nomads-backend.sln` to check compilation. If tests exist, run `dotnet test go-nomads-backend.sln`. Report results.
6. **Summary** — List files changed, symbols renamed/moved, and any manual follow-up needed (e.g., docker-compose env, documentation).

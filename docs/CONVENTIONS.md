# BudgetTracker — Conventions

How to add features to this codebase consistently. Phase 0 established the baseline
(PostgreSQL via Npgsql, base/display currency **SEK**); everything below is the pattern later
phases follow. **Guiding principle: keep it simple — don't add abstraction before it's needed.**

## Stack at a glance

- **BudgetTracker.Api** — .NET 9, FastEndpoints vertical slices + FluentValidation, EF Core
  (Npgsql/PostgreSQL) with repository + `IUnitOfWork`, Serilog, JWT bearer, Swagger.
- **BudgetTracker.Web** — Blazor WebAssembly + Bootstrap (`wwwroot/css/app.css`). Reuse the
  existing styling and shared components — **no new design system**.
- **BudgetTracker.Shared** — DTOs shared between Web and Api.
- **Database** — PostgreSQL 16 via `docker-compose.yml` (`budgettracker-postgres` / `-pgadmin`).

## Adding a vertical slice (the recipe)

Each feature is a thin vertical slice. Add the pieces in this order:

1. **Entity** — `Infrastructure/Persistence/Entities/<Name>.cs`, inherits `BaseEntity`
   (`Id` : `Guid`/`uuid`, `CreatedAt`, `UpdatedAt` — timestamps are auto-stamped in
   `AppDbContext.SaveChangesAsync`). User-owned records additionally carry the
   **owner/visibility** fields (see below).
2. **EF configuration** — `Infrastructure/Persistence/Configurations/<Name>Configuration.cs`
   (`IEntityTypeConfiguration<T>`): table name, lengths, defaults (`HasDefaultValue`), indexes,
   relationships. Register the `DbSet<>` on `AppDbContext`.
3. **Migration** — `dotnet ef migrations add <Name> --project src/BudgetTracker.Api --startup-project src/BudgetTracker.Api --output-dir Infrastructure/Persistence/Migrations`,
   then `dotnet ef database update`. Migrations live under
   `Infrastructure/Persistence/Migrations/` (not the EF default root `Migrations/`) — always pass
   `--output-dir` so new migrations land there. Confirm Postgres-native types (`uuid`, `text`,
   `timestamp with time zone`).
4. **Repository** — use the generic `IRepository<T>` for CRUD; add a specific
   `I<Name>Repository` only when a query needs it. All writes commit through `IUnitOfWork`.
5. **Endpoint + Validator** — `Features/<Area>/<Action>/<Action>Endpoint.cs` (FastEndpoints)
   plus `<Action>RequestValidator.cs` (FluentValidation). One folder per action, mirroring
   `Features/Auth/Login/` (`LoginEndpoint.cs` + `LoginRequestValidator.cs`).
6. **Shared DTO** — request/response in `BudgetTracker.Shared/DTOs/...`; never leak entities
   across the API boundary.
7. **Web page** — Blazor page under `Pages/<Area>/`, calling the API via `IApiClient`. Reuse
   existing components (`LoadingSpinner`, `ErrorAlert`) and layouts.

## Owner / visibility pattern

Every user-owned record (introduced from Phase 1, Sprint 1 via an `OwnedEntity` base) carries:

- `OwnerUserId` → the **internal** `User.Id` (a `Guid`).
- `Visibility` — `Individual` (private, the default) | `HouseholdShared` | `GroupShared`.
- optional `HouseholdId` when shared at the household level.

Queries must filter by **current user + household + visibility**. Individual finances are
**private by default** — only explicitly shared items are visible to other household members.

## Auth / identity

- **Never duplicate or rebuild auth.** Registration, login, refresh-token rotation, lockout,
  CSRF, etc. are already implemented and production-grade.
- Resolve the current **internal** `User.Id` from the request via `UserContextMiddleware` /
  `UserResolutionService` (`Infrastructure/Authentication`). The external auth provider
  (Supabase today) maps to the internal user through `UserExternalLogin`, so the provider is
  swappable — domain code only ever sees the internal `User.Id`.

## Currency

`User.PreferredCurrency` is a **per-user display preference** (defaults to **SEK**). It is
distinct from the app **base currency** (SEK) and from a transaction's **original currency + FX**
(Phase 2). For the MVP all three are effectively SEK — keep them as separate concepts so
multi-currency is a switch-on, not a refactor.

## Local development

```bash
docker compose up -d          # start Postgres + pgAdmin
dotnet ef database update --project src/BudgetTracker.Api --startup-project src/BudgetTracker.Api
dotnet run --project src/BudgetTracker.Api      # API
dotnet run --project src/BudgetTracker.Web      # Web
```

- Dev connection string lives in `appsettings.Development.json` (and the gitignored
  `appsettings.Local.json` for secrets):
  `Host=localhost;Port=5432;Database=BudgetTracker_Dev;Username=postgres;Password=postgres`.
- pgAdmin: http://localhost:5050 (`admin@budgettracker.local` / `admin`), server
  "BudgetTracker Dev" pre-registered.

## Testing

- **Unit tests** use EF Core InMemory — fast, no container.
- **Integration tests** spin up a **PostgreSQL 16** Testcontainer and reset state between tests
  with **Respawn** (`DbAdapter.Postgres`, schema `public`, ignoring `__EFMigrationsHistory`). The
  test host runs in the **`Testing`** environment (`appsettings.Testing.json`), which disables
  CSRF / lockout / rate limiting; `CustomWebApplicationFactory` registers a container-backed
  `DbContext` and a `Test` auth scheme. A container runtime (Docker Desktop) must be running;
  the endpoint is configured in `testcontainers.properties`.

# BudgetTracker

Personal/household budgeting app.

- **BudgetTracker.Api** — .NET 9, FastEndpoints vertical slices + FluentValidation, EF Core
  (Npgsql/PostgreSQL), repository + `IUnitOfWork`, Serilog, JWT bearer, Swagger.
- **BudgetTracker.Web** — Blazor WebAssembly + Bootstrap.
- **BudgetTracker.Shared** — DTOs shared between Web and Api.
- **Database** — PostgreSQL 16 via `docker-compose.yml`.

See [docs/CONVENTIONS.md](docs/CONVENTIONS.md) for how to add features consistently.

## Prerequisites

- .NET 9 SDK
- Docker Desktop (for the local Postgres + pgAdmin)
- EF Core tools: `dotnet tool install --global dotnet-ef` (only needed to run migrations)

## Local development setup

### 1. Start the database

```bash
docker compose up -d        # starts budgettracker-postgres (5432) + budgettracker-pgadmin (5050)
```

The `postgres` service auto-creates the **`BudgetTracker_Dev`** database on first start
(`POSTGRES_DB` in `docker-compose.yml`). Data persists in the `budgettracker-pgdata` volume
across restarts; use `docker compose down -v` to wipe it.

### 2. Apply the schema (migrations)

```bash
dotnet ef database update \
  --project src/BudgetTracker.Api \
  --startup-project src/BudgetTracker.Api
```

This creates the `Users`, `RefreshTokens`, `UserExternalLogins` and `__EFMigrationsHistory`
tables. On a brand-new database the first run logs one **harmless** error while EF probes for the
not-yet-existing `__EFMigrationsHistory` table — it then creates everything and prints `Done.`

> Without this step, login (and any DB call) fails with
> `Npgsql.PostgresException 3D000: database "BudgetTracker_Dev" does not exist` or with missing
> tables — the database/schema simply hasn't been created yet.

Migration files live under
[`src/BudgetTracker.Api/Infrastructure/Persistence/Migrations/`](src/BudgetTracker.Api/Infrastructure/Persistence/Migrations/)
(not the EF default root `Migrations/`). When adding new migrations, always pass
`--output-dir Infrastructure/Persistence/Migrations` — see [docs/CONVENTIONS.md](docs/CONVENTIONS.md).

### 3. Run the apps

```bash
dotnet run --project src/BudgetTracker.Api      # API + Swagger
dotnet run --project src/BudgetTracker.Web      # Blazor WASM front-end
```

### Connection string

Configured in `src/BudgetTracker.Api/appsettings.Development.json`:

```
Host=localhost;Port=5432;Database=BudgetTracker_Dev;Username=postgres;Password=postgres
```

### pgAdmin

- <http://localhost:5050>
- Login: `admin@budgettracker.local` / `admin`
- The "BudgetTracker Dev" server is pre-registered; on first expand it asks for the Postgres
  password once — enter `postgres`. (Inside the Docker network pgAdmin reaches the DB at host
  `postgres`, not `localhost`.)

## Port contention with other local projects ⚠️

The compose stack binds host ports **5432** (Postgres) and **5050** (pgAdmin). If another local
project (e.g. *Servoji*) runs its own Postgres/pgAdmin containers on the same ports, **only one
stack can run at a time** — and a running container from another project will accept connections
but won't contain `BudgetTracker_Dev`, producing the `3D000 database ... does not exist` error.

To switch to this project's stack:

```bash
docker stop servoji-postgres servoji-pgadmin     # free 5432 / 5050 (adjust names per project)
docker compose up -d                             # bring up BudgetTracker's own containers
```

To run both side-by-side instead, remap this project's ports in `docker-compose.yml` (e.g.
`5433:5432`) and update `Port=` in `appsettings.Development.json` to match.

## Useful commands

```bash
docker compose ps                 # status of this project's containers
docker compose logs -f postgres   # Postgres logs
docker compose down               # stop (keeps data)
docker compose down -v            # stop and wipe data volumes
```

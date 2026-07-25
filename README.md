# Grained — Children's Ministry Hub

Grained helps churches run Sunday School — classes, children, teachers, lessons, quizzes, badges,
attendance, and each child's *Growing in Christ* progress. This repo is the platform: a **.NET
API** and a **React PWA**. (Marketing landing page lives in `landing/`; brand art in `brand/`.)

## Architecture

```
Grained.Domain          Entities, enums, role constants — no external dependencies
Grained.Application     DTOs, form models, service interfaces + implementations, validation
                        (Clean Architecture "Application" layer) — shared by the API
Grained.Infrastructure  EF Core DbContext, migrations, Identity, seed data, invite tokens/email
Grained.Api             ASP.NET Core Web API (JWT auth) — the backend for the React app
Grained.Tests           xUnit tests (EF Core InMemory provider)
grained-web/            React 19 + TypeScript + Vite + Tailwind PWA — the admin & teacher app
landing/                Static marketing site (grained.org)
brand/                  Canonical logo / icon SVGs
```

The React app talks to `Grained.Api` over JWT bearer auth. Business logic lives once in
`Grained.Application`; the API is a thin layer of endpoints over those services. (The original
Blazor app, `Grained.AdminWeb`, has been retired now that the React port is at parity.)

Application services depend on `IApplicationDbContext` (defined in Application, implemented by
`ApplicationDbContext` in Infrastructure) rather than EF Core's `DbContext` directly — keeping the
dependency direction correct without a repository layer the MVP doesn't need.

## Roles

- **SuperAdmin** — provisions/onboards churches (seeded).
- **ChurchAdmin** — manages their church's class groups, children, teachers, lessons, badges;
  views reports/attendance.
- **Teacher** — read-only on children/lessons; takes attendance; views reports.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) · `dotnet-ef` (`dotnet tool install --global dotnet-ef`)
- [Node.js](https://nodejs.org) 20+ (for the React app)
- A PostgreSQL server

### This machine's local dev setup

`.NET 10` is at `~/.dotnet` and **Node** at `~/.local/node-v22.*` (both on `PATH` in `~/.zshrc`),
since Homebrew isn't installed here.

A user-owned PostgreSQL cluster is used (the system-wide one was never initialized):

- Data dir `~/pgdata` · Port **5433** · Superuser `cpk_admin` (trust auth, local dev only) · DB `christianplaybook`
- Connection string is in `Grained.Api/appsettings.json`.

```bash
# start / stop Postgres (does not auto-start on boot)
/Users/binsonmarkose/Projects/bin/pg_ctl -D ~/pgdata -o "-p 5433" -l ~/pgdata/logfile start
/Users/binsonmarkose/Projects/bin/pg_ctl -D ~/pgdata stop
```

## Run it

Two processes: the API and the web app.

```bash
# 1. API (:5200) — applies migrations + seeds on startup. Development lets the onboarding
#    flow surface the dev invite link.
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Grained.Api --urls http://localhost:5200

# 2. React app (:5173) — proxies /api to the API
cd grained-web && npm install && npm run dev
```

Open http://localhost:5173. On first run the DB is seeded with:

- **SuperAdmin** — `superadmin@grained.org` / `ChangeMe123!`
- Sample church "Grace Community Church" + **ChurchAdmin** — `admin@gracecommunity.org` / `ChangeMe123!`
- 4 class groups, 3 sample lessons, 6 badges

**Change these passwords before using this anywhere beyond local development.** The dev JWT signing
key in `Grained.Api/appsettings.json` must also move to a secret before deployment.

Migrations auto-apply on API startup; to run them manually:

```bash
dotnet ef database update --project Grained.Infrastructure --startup-project Grained.Api
```

## Tests

```bash
dotnet test Grained.Tests          # backend: services, validation, church isolation, attendance
cd grained-web && npm run e2e      # UI end-to-end (Playwright) — needs Postgres + API running
```

See `grained-web/README.md` for the front-end and e2e details.

## Notable implementation details

- **Church-level data isolation.** Every Application service method that touches church data takes
  an explicit `churchId` (from the caller's `ChurchId` JWT claim via `ICurrentUserService`) and
  filters/validates against it — enforced in the service layer, not just the UI. The API's
  authorization policies (`SuperAdmin` / `ChurchAdmin` / `Staff`) gate endpoints on top of that.
- **Church onboarding.** SuperAdmin provisions a church with name + admin email; the admin sets up
  their own account via a single-use, 7-day, hash-stored invite token (ASP.NET DataProtection).
  See the ticket flow — accept + activate happen in one transaction.
- **DbContext lifetime.** Application services are `Transient` and each resolves a fresh
  `IApplicationDbContext`, so unit-of-work stays per-operation. Identity's `UserManager` uses the
  standard scoped `ApplicationDbContext`.
- **Soft delete.** Churches, class groups, children, teachers, badges use an `IsActive` flag
  ("Disable", not "Delete").
- **Minimal PII.** Only an opaque `Guid` identifies a child in routes; list views keep sensitive
  fields out of URLs.

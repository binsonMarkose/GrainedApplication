# Grained Web (React PWA)

The React + TypeScript front-end for Grained — the admin & teacher app that will replace the
Blazor `Grained.AdminWeb` (per the migration decided in `GRAINED_PROJECT_BRIEF.md`).

## Stack

- **Vite + React 19 + TypeScript**
- **Tailwind CSS v4** (theme in `src/index.css` via `@theme` — grove/gold/leaf palette, Fraunces + Inter)
- **react-router-dom** for routing
- **vite-plugin-pwa** — installable, service-worker, manifest with the Grained icons
- Talks to the **Grained.Api** (.NET) over JWT bearer auth; the Vite dev server proxies `/api` → `http://localhost:5200`

## Run it (needs the API running too)

```bash
# 1. Postgres (see repo README) then the API:
dotnet run --project ../Grained.Api --urls http://localhost:5200

# 2. This app:
npm install        # first time
npm run dev        # http://localhost:5173
```

Log in with the seeded Church Admin: `admin@gracecommunity.org` / `ChangeMe123!`.

> Node was installed to `~/.local/node-v22.*` on this machine (added to `PATH` in `~/.zshrc`,
> same pattern as the local `~/.dotnet`). Open a new shell or `source ~/.zshrc` if `node` isn't found.

## Layout

```
src/
  main.tsx            Router + providers
  index.css           Tailwind + Grained @theme tokens
  types.ts            API DTO types (mirror Grained.Api)
  lib/api.ts          fetch wrapper: /api prefix, bearer token, ApiError
  auth/               AuthContext (login/logout/me) + ProtectedRoute
  components/         Logo, AppShell (sidebar+topbar), StatCard
  pages/              Login, Dashboard
```

## What's built

- ✅ Auth (JWT login, token persistence, `/me` re-hydration, role-aware protected routes)
- ✅ App shell (grove sidebar, role-filtered nav, logo, user menu) + modern themed **Dashboard**
- ✅ **All admin screens ported** on live API data:
  Dashboard · Churches (SuperAdmin) · Class Groups · Teachers · Children · Lessons + Lesson editor
  (details, memory verse, class assignment, quiz questions, publish) · Attendance · Badges · Reports (4 tabs)
- Teachers see a read-only subset (Children/Lessons view, Attendance, Reports); write actions are gated to ChurchAdmin, enforced in the API too.

### Not yet ported from Blazor / next up
- SuperAdmin church approval workflow, teacher self-set-password flow, and any niche edit screens.
- Finbuckle subdomain multi-tenancy (currently `churchId` from the JWT claim).
- Once React reaches parity, retire `Grained.AdminWeb`.

## Build

```bash
npm run build      # tsc + vite build -> dist/ (with service worker + manifest)
```

## UI tests (end-to-end)

Real browser tests with Playwright, in `e2e/`. They drive the actual app (login, dashboard,
list pages, mobile nav drawer, and the full church-onboarding flow). Configured to use your
installed **Google Chrome** (`channel: 'chrome'`) so there's no browser download.

**Prerequisites (the tests hit the real API + DB):**
1. Postgres running (see repo README).
2. The API running in Development on :5200 —
   `ASPNETCORE_ENVIRONMENT=Development dotnet run --project ../Grained.Api --urls http://localhost:5200`
   (Development is required so the onboarding test can read the dev invite link.)

The Vite dev server starts automatically (or is reused if already running).

```bash
npm run e2e          # run all UI tests (headless)
npm run e2e:ui       # interactive Playwright UI runner (watch/step through)
npm run e2e:report   # open the HTML report from the last run
```

Note: the onboarding test creates a throwaway church each run (fine against the dev DB).

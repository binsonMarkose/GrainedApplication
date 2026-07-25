# CLAUDE.md — Grained project state & working notes

> Auto-loaded by Claude Code each session. Keep it current. For founding/business/brand context
> see `GRAINED_PROJECT_BRIEF.md`; this file is the **technical handoff**.
> Last updated: 8 July 2026.

---

## 1. What this is

**Grained** — a multi-tenant SaaS for children's ministry ("where faith is ingrained"). This repo
is the **platform**: a .NET API + a React PWA. Product/brand context is in
`GRAINED_PROJECT_BRIEF.md`. Marketing site is in `landing/`; canonical logo art in `brand/`.

The repo folder is still named `ChristianPlaybook` (original name) but **everything is branded
Grained** — projects, namespaces, UI. Don't rename the repo folder mid-session (breaks paths).

Logo note: the **logo wordmark is Georgia, outlined to vector paths** in `brand/*-logo-*.svg`
(portable, no font dependency). Deliberate — the logo keeps Georgia; the app/site UI display font is
**Fraunces**. Don't switch the logo to Fraunces without asking. Design system lives in `design/`.

## 2. Architecture (current)

**Stack: ASP.NET Core Web API (.NET 10) + React 19 PWA. The old Blazor app was removed.**

```
Grained.Domain          Entities, enums, Roles constants — no external deps
Grained.Application     DTOs, form models, service interfaces + impls, validation (Clean Arch)
Grained.Infrastructure  EF Core DbContext, migrations, Identity, seed data, invite tokens, email senders
Grained.Api             Web API (JWT auth) — the backend. Runs migrate+seed on startup.
Grained.Tests           xUnit (EF Core InMemory)
grained-web/            React + TS + Vite + Tailwind v4 + vite-plugin-pwa — the admin & teacher app
landing/                Static marketing site (grained.org) + logo-animation demo
brand/                  grained-icon-bible.svg, grained-logo-horizontal/stacked(+ -dark).svg
```

- Business logic lives once in **`Grained.Application`**; the API is a thin layer of endpoints over
  those services. **New feature = an API endpoint (over an Application service) + a React page.**
- Services depend on `IApplicationDbContext` (Application) implemented by `ApplicationDbContext`
  (Infrastructure). Services are `Transient`, each resolving a fresh `IApplicationDbContext`.
- **Blazor `Grained.AdminWeb` was deleted** this session (React reached parity). A source backup
  tgz was left in the session scratchpad only — it is NOT in the repo.

## 3. Local dev environment (this machine)

Homebrew isn't installed; toolchains are user-local and on PATH via `~/.zshrc`:

- **.NET 10** → `~/.dotnet` (`DOTNET_ROOT`/`PATH`). `dotnet-ef` installed as a global tool.
- **Node 22** → `~/.local/node-v22.23.1-darwin-arm64/bin`. (`node`/`npm`.)
- **PostgreSQL** → user-owned cluster (system one was never initialized):
  - data dir `~/pgdata`, **port 5433**, superuser `cpk_admin` (trust auth, local only), DB `christianplaybook`
  - start/stop (does NOT auto-start on boot):
    ```bash
    /Users/binsonmarkose/Projects/bin/pg_ctl -D ~/pgdata -o "-p 5433" -l ~/pgdata/logfile start
    /Users/binsonmarkose/Projects/bin/pg_ctl -D ~/pgdata stop
    ```
- Connection string is in `Grained.Api/appsettings.json` (`Database=christianplaybook`). The DB name
  was deliberately NOT renamed from the old "christianplaybook" to avoid breaking the cluster.

## 4. Run it

Two processes. **Run the API in Development** so onboarding/reset flows surface their dev links.

```bash
# ensure Postgres is running (see above), then:

# API on :5200  (auto-applies migrations + seeds on startup)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Grained.Api --urls http://localhost:5200

# React app on :5173  (Vite dev proxies /api -> :5200)
cd grained-web && npm install && npm run dev
```

Open http://localhost:5173. Ports: **API 5200 · web 5173**. (Landing page previewed ad-hoc via
`python3 -m http.server 8088` in `landing/`.)

**Seeding.** Roles + the **SuperAdmin** (`superadmin@grained.org` / `ChangeMe123!`) are **always**
ensured on startup. The demo **sample data** — `admin@gracecommunity.org` / `ChangeMe123!` (Grace
Community Church) + 4 class groups, 3 lessons, 6 badges — is now **opt-in**: it's only seeded when
`SeedSampleData=true` (config/env) **and** the DB has no church yet. Default is **off**, so a wiped
DB stays clean (SuperAdmin only) across restarts. Set `SeedSampleData=true` (e.g.
`SeedSampleData=true dotnet run …` or in `appsettings.Development.json`) to get the demo church back.

## 5. Tests

```bash
dotnet test Grained.Tests          # 20 backend tests (services, validation, church isolation…)
cd grained-web && npm run e2e      # 6 Playwright UI tests — needs Postgres + API running
```

- e2e uses **system Google Chrome** (`channel: 'chrome'`, no browser download). Config:
  `grained-web/playwright.config.ts`; specs in `grained-web/e2e/`. `npm run e2e:ui` for the runner.
- e2e covers: admin login+dashboard, list pages render, bad-password, mobile hamburger drawer,
  password reset, full church onboarding. All non-destructive against the dev DB.

## 6. Auth model

- **JWT bearer** (no cookies). `POST /api/auth/login` returns `{ token, expiresAtUtc, user }`.
  Token carries `NameIdentifier`, `Email`, `FullName`, `ChurchId`, and role claims.
- `HttpContextCurrentUserService` (`ICurrentUserService`) reads those claims. Church data isolation:
  every church-scoped service method takes an explicit `churchId` from `RequireChurchId()`.
- Authorization policies (in `Grained.Api/Program.cs`): **`SuperAdmin`**, **`ChurchAdmin`**,
  **`Staff`** (= ChurchAdmin OR Teacher). List/read endpoints tend to be `Staff`; writes `ChurchAdmin`.
- Roles (`Grained.Domain.Common.Roles`): `SuperAdmin`, `ChurchAdmin`, `Teacher`.
- React: `src/auth/AuthContext.tsx` (login / `applySession` / logout, token in localStorage, `/me`
  rehydrate) + `ProtectedRoute`. Public routes (outside ProtectedRoute): `/login`,
  `/forgot-password`, `/reset-password`, `/accept-invite`.

## 7. Features built

**All admin screens are in React on live API data:** Dashboard · Churches (SuperAdmin) ·
Class Groups · Teachers · Children · Lessons + Lesson editor (details, memory verse, class
assignment, quiz questions, publish) · Events + Event editor · Fundraising + Campaign editor ·
Attendance · Badges · Reports (4 tabs). Teachers get a read-only subset; writes gated to ChurchAdmin
(UI + API). **Public (anonymous):** per-church storefront + event registration + campaign donation.

**Lesson authoring workflow** (teachers author, admins review/publish — Phase 1 of a shared-library
vision). `Lesson.Status` (`LessonStatus {Draft, InReview, Published}`) is the authoritative lifecycle;
`Lesson.IsPublished` is kept in lock-step (`== Status==Published`) so all existing "is it live?" reads
(dashboard, parent/teacher workspaces, reports) are untouched — **only ever change the two together,
via `LessonService`**. Migration `AddLessonAuthoringWorkflow` (backfills existing published → Status 2).
- **Authorship**: `AuthorUserId` + `AuthorName` (snapshot) stamped on create. Also added (nullable,
  **unused until Phase 2** — cross-church copy-on-import library): `SourceLessonId`, `OriginChurchId`.
  Plus `ReviewNote` (admin's send-back note) + `SubmittedAtUtc`.
- **Lifecycle**: teacher creates Draft → **submit** → InReview → admin **publish** (keeps the
  memory-verse + quiz gates) → Published. Admin can **send back** (InReview→Draft + note) or unpublish.
  A **teacher editing their own *published* lesson sends it back to InReview** (re-approval required) —
  by design (`OnContentEdited` in `LessonService`).
- **Permissions** (enforced in `LessonService`, not just endpoints): create/edit/submit/questions are
  now **`Staff`**; a teacher may only touch lessons **they authored** (`EnsureCanAuthor`); publish/
  unpublish/send-back stay **`ChurchAdmin`**; class-group assignment stays ChurchAdmin. Visibility: a
  teacher sees published lessons assigned to their classes **plus their own drafts**; admins see all and
  can filter `GET /api/lessons?status=InReview` for the review queue. New endpoints:
  `POST /{id}/submit` (Staff), `POST /{id}/send-back` (ChurchAdmin, `{note}`).
- React: `Lessons.tsx` (status pills, author column, "Pending review" banner+filter, teacher Submit,
  admin Publish/Send-back) + `LessonEditor.tsx` (author-scoped editing, review-note banner, submit/
  publish/send-back). Added `ICurrentUserService`/`LessonStatus` to `LessonService`. Tests:
  `Grained.Tests/Lessons/LessonAuthoringWorkflowTests.cs`. **Phase 2/3 (shared library copy-on-import +
  platform curation, contributor badges) are deferred bolt-ons.**
- **Mark-lesson-complete** (records `ChildProgress` for present kids via `POST /teacher/lessons/{id}/
  complete` → `TeacherWorkspaceService.MarkLessonCompletedAsync`) is a shared component
  `components/MarkLessonCompleteModal.tsx` used from the **teacher dashboard** and, most usefully, the
  **lesson detail/view page** (`LessonEditor.tsx`) — a teacher opens a lesson, teaches/runs the quiz,
  and hits **"✅ Mark as completed"** right there (shown for `Teacher` on a Published lesson assigned to
  their class). The modal picks the class (dropdown when >1 of their classes), loads the attendance
  roster for the date, and ticks who learned the memory verse. The **Lessons list is read-only** for
  this: a **"Taught"** column shows **"✅ {date}"** from the new `LastCompletedAtUtc` (most recent
  completion in the caller's scope), and the detail header shows the same pill. `LessonListItemDto`/
  `LessonDetailDto` carry `AssignedClassGroupIds` + `LastCompletedAtUtc` (`LessonService.CompletionDatesAsync`).
- **Per-group teaching order**: `LessonClassGroup.SortOrder` (migration `AddLessonSortOrder`, backfilled
  by lesson-created-date within each group) — order is **per assignment**, since a lesson can sit in
  several groups at different positions. `AssignToClassGroupAsync` appends to the end;
  `ReorderLessonsAsync(classGroupId, churchId, orderedLessonIds)` persists a new sequence
  (**`PUT /api/lessons/order`**, ChurchAdmin). `GetForChurchAsync(..., classGroupId)` filters to a
  group and returns lessons **in `SortOrder`**; teacher **and** parent workspaces now order their
  lesson lists by it too. React: `Lessons.tsx` has a **class-group dropdown**; picking a group swaps the
  DataTable for **`components/LessonReorderList.tsx`** — native HTML5 drag-and-drop **plus up/down
  arrows** (touch/a11y), saving on each change. **Teachers can reorder too** (endpoint is Staff;
  `ReorderLessonsAsync` guards a teacher to their assigned groups); their `/class-groups` is scoped so
  the dropdown only lists their classes. **Already-taught lessons sink to the bottom**: the group view
  scopes "taught" to *that* group (`GetForChurchAsync` passes `[classGroupId]` to `CompletionDatesAsync`)
  and the reorder list splits them into a static **"Already taught"** section below the draggable
  upcoming curriculum; the teacher dashboard also orders taught lessons last. Tests:
  `Grained.Tests/Lessons/LessonOrderTests.cs`.
- **Teach mode** (`components/TeachMode.tsx`) — a full-screen presenter view a teacher runs the class
  from. Launched by a **▶ Teach** button on the lesson detail page (`LessonEditor`) and on each teacher
  dashboard lesson card. Frontend-only: it fetches the lesson detail and splits it into steps —
  **Story (+ "big idea" moral) → Memory verse → each Quiz question → Activity → Prayer** — with big
  readable type (**A−/A+**), **tap-to-reveal** quiz answers, progress dots + arrow/keyboard nav,
  **Screen Wake Lock** (best-effort keep-awake), and a **"✓ Mark completed"** at the end (reuses
  `MarkLessonCompleteModal`; classes passed only when the caller can mark — teachers). No schema change.
  Follow-up idea: a **bilingual (English/Malayalam) toggle**, once Malayalam text is stored per lesson.

**Reports are teacher-scoped** (like the rest of the app): `ReportService` injects `ITeacherScope`
and every report method — child-progress, class-progress, attendance, lesson-completion, and the
per-child badge drill-down — filters to a plain Teacher's **assigned class groups** (admins/SuperAdmin
see the whole church, `scope == null`). Lesson-completion also only counts completions from children
in scope, and an out-of-scope child's badges 400 as "not found". Covered by `Grained.Tests/Reports/
ReportScopingTests.cs`.

**Growth path / seasons** (the "Growing in Christ" tree). Each child grows a tree through 7 stages
(Seed→Roots→Sprout→Sapling→Tree→Fruit→Harvest) over a **ministry year**, computed from timestamps of
their lessons/attendance/verses/badges. A "faithful Sunday" = 12 GP (attend 4 + lesson 4 + verse 4).
- **Ministry year varies per church**, so targets are **not fixed**: a `GrowthSeason` has admin-set
  `StartsOnUtc` + `EndsOnUtc`, and `GrowthLevels.StagesForWeeks(weeks)` **scales** the thresholds so
  Harvest = `weeks × 12` lands exactly at season end (at 52 weeks it reproduces the old 0…624 curve;
  the stage *shape* is fixed as faithful-Sundays-out-of-52). Migration `AddGrowthSeasonEndDate`
  (backfills each season's end = next season's start, or +1yr for the latest).
- `GrowthService`: per-season windows are `[StartsOnUtc, EndsOnUtc]` (no seasons → default calendar
  year, 52 wk); past seasons form the child's **forest**. `CreateSeasonAsync`/`UpdateSeasonAsync`
  (ChurchAdmin) validate end>start. `/api/growth/seasons` GET/POST/PUT. React `Growth.tsx` has
  start+end date pickers + a live weeks/Harvest preview; the summary DTO's `stageFloor`/`nextStageAt`
  carry the scaled targets so the parent/teacher progress bars adapt automatically. Tests:
  `Grained.Tests/Growth/GrowthScalingTests.cs`.

**Events** (ChurchAdmin): `Event` + `EventTicketType` entities (migration `AddEvents`). An event has
title, start/end date-time, location, description (for the event page), an **EnableTshirt** toggle,
and priced **ticket types** (new events pre-fill Adult/Student/Child/Senior citizen, all editable +
add/remove). Draft→Publish like Lessons (`IsPublished`; publish requires ≥1 ticket type), soft-disable
via `IsActive`. `EventService`/`IEventService`, `/api/events` endpoints (reads Staff, writes
ChurchAdmin), React `Events.tsx` list + `EventEditor.tsx`. Dates stored as UTC wall-clock
(`DateTime.SpecifyKind(..., Utc)`) and shown with `timeZone:'UTC'` so they round-trip unshifted.

**Public storefront + event registration** (anonymous). Building toward an EPP-style public product
list per church — see `CORE-FLOWS.md` for the EPP reference and the phased plan agreed with the
founder. **Payment is structure-first**: a shared `IPaymentGateway` seam (`Grained.Application/
Payments`) with a dev **`RecordPaymentGateway`** that marks payments **Paid instantly** (Provider
`"NoCard"`, ref `NOCARD-…`); a real **Stripe** gateway drops in behind the same interface later
(decision: **Stripe Connect** — store each church's `acct_` id, never raw bank details). **Payouts:
no bank fields anywhere** — that's deliberate (Stripe holds bank/KYC).
- Entities: `Payment` (+ `PaymentStatus` enum), `EventRegistration`, `EventRegistrationLine`
  (ticket name + unit price **snapshotted**, so editing an event's ticket types can't corrupt past
  bookings — hence no FK on `EventTicketTypeId`). Migration `AddEventRegistration`.
- `IPublicEventService`/`PublicEventService` (`Grained.Application/Public`) + **anonymous**
  `/api/public/*` endpoints (`churches/{slug}`, `events/{id}`, `events/{id}/register`), rate-limited
  by a new **`public`** fixed-window policy. Only published+active events of active churches; never
  leaks admin fields.
- React public pages (outside `ProtectedRoute`): `/p/:slug` storefront (`pages/public/Storefront.tsx`)
  and `/p/events/:id` registration (`pages/public/PublicEvent.tsx`), branded via `PublicShell`. The
  admin `EventEditor` shows a **copy-able public link** (`/p/events/{id}`) once published.
- **Storefront is keyed by `Church.Slug`** — seeded/renamed churches without a slug won't resolve;
  the seeder now sets one, and existing rows were backfilled (e.g. `peniel-pentecostal-church`).
- The storefront (`/api/public/churches/{slug}`) lists **both** published Events **and** Campaigns.

**Fundraising campaigns + public donation page** (ChurchAdmin authoring, anonymous donating). Reuses
the same `IPaymentGateway` seam.
- Entities: `Campaign` (title, description, optional `TargetAmount`, `LogoImageId`, `IsPublished` +
  `IsActive`; **raised amount is computed on read** from paid donations, never stored), `Donation`
  (name/email/amount/message/`IsNamePublic`, linked `Payment`), and **`StoredImage`** (logo bytes in
  the DB for now, served via **anonymous `GET /api/images/{id}`** — swap for object storage behind
  that URL later). Migration `AddFundraising`.
- Admin: `ICampaignService`/`CampaignService` (`Grained.Application/Fundraising`), `/api/campaigns`
  endpoints (reads Staff, writes ChurchAdmin) incl. **`POST /{id}/logo`** (multipart, `.DisableAntiforgery()`,
  ≤2 MB, image types only). React `Campaigns.tsx` list + `CampaignEditor.tsx` (logo upload appears
  after create; copy-able public link once published). Nav: **Fundraising** (🎗️, ChurchAdmin).
- Public: `IPublicCampaignService`/`PublicCampaignService` → `GET /api/public/campaigns/{id}` +
  `POST /api/public/campaigns/{id}/donate` (anonymous, `public` rate limiter). React
  `/p/campaigns/:id` donation page (`pages/public/PublicCampaign.tsx`) — amount presets + progress
  bar; storefront shows a Fundraising section.

**Hard delete (ChurchAdmin).** Class groups, teachers, children, events, campaigns and lessons each
have a `DeleteAsync` on their service + a `DELETE /api/{...}/{id}` endpoint (ChurchAdmin), sitting
alongside the existing soft-disable (`IsActive`). **Design: delete only when nothing important is linked, else
throw `ValidationException` telling the admin to disable instead** (surfaced as an error toast) —
never silently cascade away history. Guards: class group blocked by any Children/Attendance (both
Restrict FKs; teacher/lesson assignments cascade fine); event blocked by any EventRegistration;
campaign blocked by any Donation (and it also deletes the logo `StoredImage`); **lesson** blocked by
any ChildProgress or Attendance ("in use" — unpublish instead), its memory verse / quiz / class
assignments cascade. Child deletes cascade their badges/progress/attendance (parent account kept). Teacher: if the account is **solely** a
teacher it's removed via `UserManager.DeleteAsync` (login gone, profile+assignments cascade); if it's
also a ChurchAdmin or a linked parent, only the Teacher role + `TeacherProfile` are dropped and the
login stays. Authored lessons keep their `AuthorName` snapshot. React: a shared
**`ConfirmDialog`** (`components/ui.tsx`) + a red **Delete** row action on each list page. Tests:
`Grained.Tests/Deletion/DeleteGuardTests.cs`.
- **Still TODO** (next phases): SuperAdmin payout-account (Stripe Connect) management +
  select-on-create for events/campaigns; real Stripe (Checkout + webhooks); confirmation **emails**
  (registration/donation confirmations are not emailed yet — UI copy avoids promising one).

**Church onboarding / invites** (see `TICKET_church-onboarding-invite.md`):
- SuperAdmin provisions a church with **name + admin email** → church `Pending` + `Invitation`
  (only SHA-256 token hash stored) → invite "emailed".
- Public `/accept-invite?token=` page → admin sets name/church-details/password → transactional
  accept creates the ChurchAdmin, flips church `Active`, auto-logs-in.
- Tokens = ASP.NET **DataProtection** time-limited (7d), single-use; resend revokes the old.
- Endpoints: `POST /api/churches {name,adminEmail}`, `GET/POST /api/invites(+/accept)` [anonymous,
  rate-limited], `POST /api/churches/{id}/resend-invite`, `GET /api/churches?status=Pending`.
- **Default badges + lessons**: provisioning a church (`ChurchOnboardingService.CreateChurchWithInviteAsync`)
  seeds a **10-badge starter set** *and* a **20-lesson Nursery library** for it, so admins have content
  from day one. Catalogs: `Grained.Application/Badges/DefaultBadges.cs` (8 Standard @12 pts + 2
  Achievement @36 pts, **emoji** icons — the BadgeIcon medallion renders the raw string) and
  `Grained.Application/Lessons/DefaultLessons.cs` (20 lessons adapted from the IPC Sunday Schools
  "Nursery 1" book — story + moral + memory verse + activity + one quiz question each, `AgeGroup`
  "Nursery", `AuthorName` "IPC Sunday Schools Association", seeded **Published** and **unassigned** so
  admins assign them to a class group). The sample `DbSeeder` reuses the badge catalog. Backfill an
  existing church via **`POST /api/churches/{id}/seed-lessons`** (SuperAdmin, idempotent —
  `ILessonService.SeedDefaultLibraryAsync`). Admins can edit/unpublish/delete/reassign/add freely.
  Tests: `Grained.Tests/Onboarding/DefaultBadgesTests.cs` + `DefaultLessonsTests.cs`.
- **Repeatable badges**: `Badge.Repeatable` (migration `AddRepeatableBadges`) — effort/character
  badges can be awarded to a child **many times**; milestones only once. The `ChildBadge(ChildId,
  BadgeId)` index is **no longer unique**; one-time enforcement lives in the services (`BadgeService`
  + `TeacherWorkspaceService` gate the re-award only when `!Repeatable`). Each award is its own
  `ChildBadge` row with `AwardedAtUtc`, so growth/season windowing + points sum naturally per award.
  Default per tier (Standard repeatable, Achievement one-time), overridable via a toggle in the badge
  editor; backfill set existing Standard-repeatable. Child-badge lists (`ParentBadgeDto`,
  `TeacherWorkspaceBadgeDto`) are **aggregated by badge with a `Count`**, shown as an **"xN"** chip on
  the parent/teacher dashboards. Tests: `Grained.Tests/Badges/RepeatableBadgeTests.cs`.

**Announcements / messaging** (ChurchAdmin broadcasts, teachers/parents receive). A ChurchAdmin
writes a title + message and picks an audience — **Teachers, Parents, or Everyone** — and it reaches
those recipients as a **login pop-up** and an **Announcements tab**.
- Entities: `Announcement` (ChurchId, Title, Body, `AnnouncementAudience` enum {Teachers=0,
  Parents=1, Everyone=2}, `CreatedByUserId` + `CreatedByName` **snapshot**, `IsActive` for
  retract/restore) and `AnnouncementReceipt` (one row per user who read/dismissed; **absence = unread**,
  unique on (AnnouncementId, UserId)). Migration `AddAnnouncements`.
- `IAnnouncementService`/`AnnouncementService` (`Grained.Application/Announcements`). Delivery rule:
  a user receives an announcement if they're a **teacher or parent** and the audience matches (Everyone
  = teachers ∪ parents). **A pure ChurchAdmin/SuperAdmin is a sender, not a recipient** — their own
  inbox is empty (no self-popups). `AudienceLabel` is mapped **in memory** after the SQL projection
  (EF can't translate the switch). Admin list shows a **"Seen by N"** read count.
- Endpoints: **author** `/api/announcements` (ChurchAdmin) — GET list, POST create, POST `/{id}/active`
  (retract/restore); **recipient** `/api/my/announcements` (any authed) — GET inbox, POST `/{id}/read`,
  POST `/read-all`. Author name is snapshotted from the `full_name` JWT claim.
- Added `ICurrentUserService.IsParent` (+ impl + test fake) so the inbox can scope by role.
- React: admin `pages/Announcements.tsx` (nav **Messages** 📣, ChurchAdmin) — DataTable + compose modal
  (audience/title/body); recipient `pages/Inbox.tsx` (nav **Announcements** 📣 with unread badge, for
  Teacher in staff workspace + Parent in parent workspace); `components/AnnouncementPopup.tsx` (login
  pop-up, mounted in `AppShell` for recipients, steps through unread). `lib/announcements.ts` holds the
  shared fetch + a tiny pub/sub so the pop-up, tab, and nav badge stay in sync after a mark-read.

**Forgot / reset password:**
- Login has a "Forgot your password?" link → `/forgot-password` → `/reset-password?email=&token=`.
- `POST /api/auth/forgot-password` (always 200, no account-existence leak) + `POST /api/auth/reset-password`.
- Uses Identity's built-in reset token (single-use via security stamp). Rate-limited.

**Account settings** (self-service, all signed-in users). The sidebar footer's **name block links to
`/settings`** (`pages/Settings.tsx`). Sections: **Profile** (name + email), **Password** (current +
new), and **Church details** (ChurchAdmin only).
- `PUT /api/auth/me` (`UpdateProfileRequest`) — updates FullName + email; email change also updates
  `UserName` (login is by email) via `SetEmailAsync`/`SetUserNameAsync` with a uniqueness check, then
  **re-issues the JWT** and returns `LoginResponse` so the frontend `applySession()`s fresh claims.
- `POST /api/auth/change-password` (`ChangePasswordRequest`) — `UserManager.ChangePasswordAsync`;
  wrong current password → friendly 400. Both are `.RequireAuthorization()` (any signed-in user).
- `GET`/`PUT /api/churches/mine` (**ChurchAdmin**, `/api/churches/mine` group) — a church admin
  views/edits their **own** church via `IChurchService` (`RequireChurchId()`), separate from the
  SuperAdmin-only `/api/churches` CRUD. Non-admins get 403.

**Parent linking on child save (auto-link + promote).** Saving a child whose **parent email matches
an existing account** in the church links the child to it and **grants that account the `Parent` role**
(`ChildService.LinkParentAccountAsync`, called from Create/Update) — so a **teacher/admin becomes a
dual staff+parent account** and can switch to the Parent view. All siblings sharing the email are
linked. A brand-new email is *not* auto-provisioned here — that still needs the **Parent code** action
(`CreateOrResetParentCode`), which issues the login code. The React Children form warns first: on save
it calls **`GET /api/children/parent-lookup?email=`** (ChurchAdmin, → `ParentLookupResult{Exists,Name,
IsStaff,AlreadyParent}`) and, when the email matches an account that isn't already a parent, shows a
**`ConfirmDialog`** ("Link to an existing account?") before saving. Note: a promoted user must
**re-login** to get a JWT carrying the new `Parent` role before the workspace chooser appears. Tests:
`Grained.Tests/Children/ParentLinkTests.cs`.

**Force password change on first login.** Admin-provisioned teachers/parents get a temporary login
code, so `ApplicationUser.MustChangePassword` is set on `TeacherService.CreateAsync`/`ResetLoginCode`
and `ChildService.CreateOrResetParentCode` (new/reset codes — **not** dual-role staff who keep their
own login). Migration `AddMustChangePassword`. The flag rides the JWT (`must_change_password` claim)
and every `UserDto` (login/me/profile/set-password). On the web, `WorkspaceGate` shows
`pages/SetInitialPassword.tsx` before the app until it's cleared; `POST /api/auth/set-password`
(`SetPasswordRequest`, authenticated) sets a new password **without** the current one — allowed only
while the flag is set — then clears it and re-issues the token. Invited admins set their own password
on accept, so they're never flagged.

**Email is not wired to a provider yet.** `IInviteEmailSender` / `IPasswordResetEmailSender` have
**logging** dev implementations. In **Development only**, the create/resend/forgot responses also
return the raw link (`acceptUrl` / `resetUrl`) so the UI shows a copy-link box for testing. A real
provider (Resend/Brevo/Mailtrap) drops in behind those interfaces via config with no flow change;
the dev links simply stop being returned outside Development.

## 8. Front-end conventions (`grained-web/`)

- **Tailwind v4**, theme tokens in `src/index.css` `@theme`: colors `grove`, `grove-deep`, `gold`,
  `gold-soft`, `leaf`, `leaf-light`, `cream`, `cream-deep`, `ink`; `font-display` (Fraunces),
  default sans (Inter). Use these, not raw hex.
- **UI kit**: `src/components/ui.tsx` — `PageHeader, Button, Card, Field, Input, Textarea, Select,
  Checkbox, Table/Th/Td, Pill, Modal, ConfirmDialog, EmptyState, Loading, ErrorBanner`, and **`DataTable`**
  (renders a table on desktop, stacked cards on mobile — use it for list pages so nothing scrolls
  off a phone). `ClassGroups.tsx` is the reference list page.
- **Toasts**: `src/components/Toast.tsx` — `ToastProvider` (wraps the app in `main.tsx`) + `useToast()`
  → `toast.success(msg)` / `toast.error(msg)`. **Every mutating admin action** (create/update/publish/
  unpublish/enable/disable/logo/etc.) fires a success or error popup; error toasts pass the server's
  `ApiError.message`. Modal forms keep their inline `ErrorBanner` too.
- **`src/lib/api.ts`**: `api<T>(path, opts?)` — prefixes `/api`, attaches bearer token, throws
  `ApiError`. Types mirror the API DTOs in `src/types.ts`.
- App shell (`components/AppShell.tsx`): grove sidebar, **role-aware nav**, mobile hamburger drawer.
- **TS is strict** with `erasableSyntaxOnly` + `noUnusedLocals`: no TS parameter-properties, no
  enums, no unused imports/vars (build fails otherwise). `npm run build` = `tsc -b && vite build`.
- PWA: manifest + service worker via `vite-plugin-pwa`; Grained icons in `public/`.

## 9. Known gaps / decisions (do not "fix" without context)

- **Minimal-API endpoints don't enforce form-model DataAnnotations.** `[Required]`/`[Range]`/
  `IValidatableObject` on the `*FormModel`s are **not** auto-validated by minimal APIs, so inputs that
  pass the browser's HTML5 checks but violate a model rule (e.g. class-group `MaxAge < MinAge`) save
  without error — and the new error toast has nothing to show. Only **service-level** guards that
  `throw ValidationException` (→ 400 `{message}`) surface as errors. To make validation (and the error
  popups) reliable, enable .NET 10 minimal-API validation (`AddValidation()`) and map its
  `ValidationProblem` shape into `ApiError.message`, or add explicit service-side checks. Not yet done.
- **No Membership join table.** Identity is bound to a single church via `ApplicationUser.ChurchId`
  (+ ChurchAdmin role). The onboarding ticket wanted a `Membership` model for multi-church admins;
  that's a large cross-cutting refactor and was **deferred**. Consequence: re-inviting the same
  email to a *second* church is currently rejected.
- **API auto-migrates on every startup** (inherited from the old Blazor host; convenient for dev).
  For production, gate this behind `IsDevelopment()` / a deliberate deploy step. Left as-is per
  founder ("long way from prod").
- **Dev JWT signing key** is a placeholder in `Grained.Api/appsettings.json` — move to a secret
  before deploy.
- **CORS is an allowlist** (`Cors:AllowedOrigins` in config; env form `Cors__AllowedOrigins__0=…`).
  Empty + Development → falls back to `http://localhost:5173`, `http://127.0.0.1:5173`,
  `http://localhost:4173`. Empty + any other environment → **no CORS headers at all** (fail closed),
  which is correct when the PWA is served same-origin behind a reverse proxy; if the web app is ever
  hosted on a *different* host than the API, that list **must** be set or every browser call breaks.
  `AllowCredentials` is deliberately **off** — auth is a bearer token, never a cookie, so don't add
  it (it can't be combined with a wildcard anyway). Note `dotnet run` applies
  `Grained.Api/Properties/launchSettings.json`, which pins `ASPNETCORE_ENVIRONMENT=Development` —
  pass `--no-launch-profile` to actually test non-dev behaviour.
- Email provider (see §7), real payments/Stripe, Finbuckle subdomain tenancy, and **Grained Weekly**
  (from the brief) are **not built yet**. (The Growing-in-Christ tree / growth path **is** built — see
  the "Growth path / seasons" note above.)

## 10. Working rules

- Build features as **API endpoint + React page**. Reuse Application services; don't duplicate logic.
- Keep church isolation: church-scoped queries filter by `churchId` from the JWT claim.
- Match the Grained theme + UI kit; use `DataTable` for lists.
- Verify changes end-to-end (curl for API, `npm run e2e` / Playwright for UI) — that's how
  everything here was validated.
- No git in this repo yet; make backups before destructive changes.

## 11. Pointers

- `GRAINED_PROJECT_BRIEF.md` — founding/business/brand doc (north star, tiers, market).
- `grained-web/README.md` — front-end + e2e details.
- `landing/README.md` — marketing site deploy notes.
- `TICKET_church-onboarding-invite.md` — the onboarding spec (implemented with noted deviations).

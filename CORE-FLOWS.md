# Core Platform Flows — Grained

This document explains, in detail, how the foundational flows of the **Grained** platform (a
multi-tenant children's-ministry SaaS) work end to end:

1. [Church Creation & Onboarding](#1-church-creation--onboarding)
2. [User Creation & Authentication](#2-user-creation--authentication)
3. [Lesson Lifecycle (authoring & publishing)](#3-lesson-lifecycle-authoring--publishing)
4. [Ministry Delivery — Roster, Attendance, Progress & Badges](#4-ministry-delivery--roster-attendance-progress--badges)

It is written as a reference for engineers working on the `Grained.*` solution. File and line
references point at the code as of 2026-07. It is deliberately modeled on our reference read of the
**EasyPaymentsPlus (EPP)** `CORE-FLOWS.md`; a [Grained ↔ EPP concept map](#grained--epp-concept-map)
and a [quick file-map](#quick-file-map-reference) are at the end.

## Architecture at a glance

Grained is a **Clean Architecture** layered app — **no CQRS, no MediatR**. A React PWA talks to a
thin ASP.NET Core Web API over JWT; the API is a set of minimal-API endpoint groups over
Application services. The flow through the layers is always:

```
grained-web (React 19 + TS + Vite PWA)      ── fetch /api, Bearer JWT ──▶
  Grained.Api (minimal-API endpoint groups, auth policies)
    └─ Grained.Application (services + DTOs/form models + FluentValidation-style checks)
        └─ IApplicationDbContext (implemented by Infrastructure)
            └─ Grained.Infrastructure (EF Core DbContext, Identity, seed, invite tokens, email)
                └─ Grained.Domain (entities, enums, Roles) — no external deps
                    └─ PostgreSQL
```

| Project | Responsibility |
|---|---|
| `Grained.Domain` | EF entities (`Entities/*`), enums (`Enums/*`), `Common/Roles.cs`, `AuditableEntity`. No external deps. |
| `Grained.Application` | Business logic (`ChurchService`, `LessonService`, `AttendanceService`, …), DTOs + form models, `ValidationException`, service interfaces. Depends on `IApplicationDbContext`. |
| `Grained.Infrastructure` | `ApplicationDbContext` (EF Core + Npgsql), EF migrations, ASP.NET Identity stores, `DbSeeder`, invite-token (`InviteTokenService`) and email-sender implementations. |
| `Grained.Api` | The backend. Endpoint groups (`Endpoints/*`), JWT auth + policies, `JwtTokenService`, `HttpContextCurrentUserService`, rate limiting. Runs migrate+seed on startup. |
| `Grained.Tests` | xUnit + EF Core InMemory (services, validation, church isolation). |
| `grained-web/` | React admin & teacher SPA (Tailwind v4, `AuthContext`, `api.ts`). |

**Persistence:** EF Core against PostgreSQL, applied via **EF migrations** run on API startup
(`Grained.Api/Program.cs`; `DbSeeder.SeedAsync` migrates + seeds). **Validation** lives in the
Application layer as explicit guards that throw a single typed `ValidationException`
(`Grained.Application/Common/Exceptions/ValidationException.cs:5`), mapped to HTTP **400** by
middleware in `Grained.Api/Program.cs:99-107`. There is **no dedicated NotFound type** — "not found"
is thrown as `ValidationException("… not found.")` (contrast EPP's rich `*EppException` hierarchy).

### Multi-tenancy: church data isolation (cross-cutting)

Every church-scoped Application method takes an explicit `Guid churchId` and filters
`.Where(x => x.ChurchId == churchId)` on both reads and mutations. **There is no EF global query
filter** — isolation is by convention, enforced per method (and covered by
`Grained.Tests/DataIsolation/ChurchDataIsolationTests.cs`). Entities that lack a direct `ChurchId`
(`Attendance`, `ChildProgress`) reach the church through a navigation (`ClassGroup.ChurchId`,
`Lesson.ChurchId`). The `churchId` itself is never taken from the request body — it comes from the
authenticated user's JWT claim via `ICurrentUserService.RequireChurchId()`
(`Grained.Api/Auth/HttpContextCurrentUserService.cs:27-28`, throws if absent).

> **Key architectural note — Grained binds a user to ONE church; EPP does not.**
> This is the single most important divergence from the EPP model and is called out in full in
> [§2](#the-central-design-decision-one-church-per-user). EPP makes ASP.NET Identity's `TUser` the
> *membership* (`AccountOrganisationMembership`), so one person can belong to many orgs. Grained
> makes `TUser` the *person* (`ApplicationUser`) with a single `ChurchId`. If Grained ever needs
> "a teacher who moves between / serves at multiple churches," EPP's shape is the reference design.

---

## 1. Church Creation & Onboarding

A **Church** is the tenant entity in Grained — every class group, child, lesson, badge, and user
belongs to one. Unlike EPP (open self-service org signup), Grained churches are **provisioned by a
SuperAdmin and activated by the invited admin** via a tokenized invite.

### Schema

- **`Grained.Domain/Entities/Church.cs:6-27`** — `Church : AuditableEntity`. `Status`
  (`ChurchStatus`, default `Active`) `:16`, `Slug` `:17`, `CreatedByUserId` `:18`,
  `ActivatedAtUtc` `:19`, `Invitations` collection `:21`. Note `IsActive` (from `AuditableEntity`)
  is a separate disable flag, distinct from `Status`.
- **`Grained.Domain/Entities/Invitation.cs:7-24`** — `ChurchId` `:11`, `Email` (lower-cased) `:14`,
  `Role` (`MembershipRole`, default `ChurchAdmin`) `:15`, **`TokenHash` (SHA-256 of the raw token)**
  `:17`, `Status` `:18`, `ExpiresAtUtc` `:19`, `AcceptedAtUtc` `:23`. Only the hash is stored; the
  raw token lives only in the emailed link.
- **Enums** — `ChurchStatus` (`Pending, Active, Suspended`) `Enums/ChurchStatus.cs:3-8`;
  `InvitationStatus` (`Pending, Accepted, Expired, Revoked`) `Enums/InvitationStatus.cs:3-9`;
  `MembershipRole` (`ChurchAdmin, Teacher`) `Enums/MembershipRole.cs:5-9`.

### Flow: SuperAdmin provisions a church

```
POST /api/churches {name, adminEmail}            (SuperAdmin only)
  └─ ChurchOnboardingService.CreateChurchWithInviteAsync
       ├─ validate + normalize name/email
       ├─ create Church  (Status = Pending, unique Slug, IsActive = true)
       ├─ IssueInvitationAsync → InviteTokenService.CreateToken (DataProtection, 7-day)
       │      store Invitation (Status = Pending, TokenHash = SHA256(raw))
       └─ single SaveChangesAsync → returns CreatedInvite { …, RawToken }
  └─ BuildAcceptUrl(rawToken) → IInviteEmailSender.SendChurchAdminInviteAsync ("email")
  └─ 201 { status: "Pending", acceptUrl: IsDevelopment ? url : null }
```

1. **Endpoint** — `Grained.Api/Endpoints/ChurchEndpoints.cs`. Group `/api/churches`
   `.RequireAuthorization("SuperAdmin")` `:15` (all church endpoints are SuperAdmin-only).
   `POST ""` provision `:27-49` → `CreateChurchWithInviteAsync` `:36`, `BuildAcceptUrl` `:39`
   (`{App:WebBaseUrl ?? http://localhost:5173}/accept-invite?token=…`, `:83-87`), email `:40`,
   returns 201 with **dev-only** `acceptUrl = env.IsDevelopment() ? … : null` `:47`.
   `record CreateChurchRequest(Name, AdminEmail)` `:8`.
2. **Service** — `Grained.Application/Onboarding/ChurchOnboardingService.cs`.
   `CreateChurchWithInviteAsync` `:18-44`: normalizes email `:21-22`, validates (throws
   `ValidationException` `:25,:27`), creates `Church` `Status = Pending` `:33` with unique `Slug`
   `:34`, issues invite `:40`, one `SaveChangesAsync` `:42`. `IssueInvitationAsync` `:86-103` sets
   `ExpiresAtUtc = UtcNow + InviteLifetime` (**7 days**, `:16`, `:95`), `tokens.CreateToken` `:98`,
   `TokenHash = tokens.Hash(raw)` `:99`. Returns `CreatedInvite(…, RawToken)`
   (`OnboardingModels.cs:6`).
3. **Token service** — `Grained.Infrastructure/Onboarding/InviteTokenService.cs:11-41`. Uses an
   `ITimeLimitedDataProtector` (`"Grained.Invite.v1"`). `CreateToken` `:20-21` protects the
   invitation id with the lifetime (DataProtection signs + enforces expiry); `TryValidate` `:23-37`
   unprotects and catches `CryptographicException` → false for tampered/expired tokens; `Hash`
   `:39-40` = `Convert.ToHexString(SHA256.HashData(...))`.
4. **Email** — `Grained.Infrastructure/Onboarding/LoggingInviteEmailSender.cs:8-18` (dev/default)
   logs the accept URL instead of sending. Swap for a real provider (Resend/Brevo) behind
   `IInviteEmailSender` via config — no flow change.

### Flow: invited admin accepts → church goes Active + auto-login

```
GET  /api/invites?token=…      (anonymous, rate-limited)  → validate, 410 if bad/expired
POST /api/invites/accept        (anonymous, rate-limited)  → transactional activation
  ├─ tokens.TryValidate → 410 on fail
  ├─ reload invite+church; re-check Pending + not expired + TokenHash matches → 410 (single-use)
  ├─ if a user already exists for invite.Email → 409  (one admin ↔ one church, for now)
  ├─ [BeginTransaction]
  ├─ create ApplicationUser (EmailConfirmed, ChurchId = invite.ChurchId)
  ├─ userManager.CreateAsync(user, password)  → 400 on Identity/password failure
  ├─ AddToRoleAsync(user, Roles.ChurchAdmin)
  ├─ invite → Accepted (+AcceptedAtUtc);  church → Active (+ActivatedAtUtc)
  ├─ [Commit]
  └─ jwt.Create(user, roles) → 200 LoginResponse { token, expiresAtUtc, user }   (auto-login)
```

- **Endpoints** — `Grained.Api/Endpoints/InviteEndpoints.cs`. Group `/api/invites`
  `.RequireRateLimiting("invite")` `:20`. `GET ""` validate `.AllowAnonymous()` `:23-29` →
  `GetInviteInfoAsync`, **410 Gone** if invalid/expired `:27`. `POST /accept` `.AllowAnonymous()`
  `:33-92` is the transactional activation above (409 for existing user `:56-57`, 410 for bad token
  `:43-53`, 400 for password rules `:71-73`, auto-login `:88-91`).
  `record AcceptInviteRequest(Token, FirstName, LastName, Address?, Phone?, Password)` `:13-14`.
- **Validation (read side)** — `GetInviteInfoAsync` (`ChurchOnboardingService.cs:46-63`) validates
  the token and returns `InviteInfo(ChurchName, Email)` **without consuming it**, so the accept page
  can render "You're setting up {church}" before the user submits.
- **Resend revokes** — `POST /api/churches/{id}/resend-invite` (`ChurchEndpoints.cs:66-80`) →
  `ResendInviteAsync` (`ChurchOnboardingService.cs:65-82`): church must be `Pending`; **all
  outstanding `Pending` invites are set to `Revoked`** `:73-77` before a fresh one is issued.
- **Frontend** — the public `/accept-invite?token=` page is `grained-web/src/pages/AcceptInvite.tsx`
  (validates on load, collects name/details/password, calls `applySession` on success).

### SuperAdmin church management

`ChurchService` (`Grained.Application/Churches/ChurchService.cs`) backs the SuperAdmin Churches
screen: `GetAllAsync(includeInactive, status?)` `:10-22` (the `?status=Pending` filter),
`GetByIdAsync` `:24-28`, `CreateAsync` `:30-42`, `UpdateAsync` `:44-58`, `SetActiveAsync` `:60-66`.

### Rate limiting & DataProtection (`Grained.Api/Program.cs`)

`AddDataProtection()` `:72` (persists keys so invite tokens survive restarts). `AddRateLimiter`
`:75-84`: fixed-window **`"invite"`** policy — 1-minute window, 20 permits, no queue, 429 on reject.
Applied to the invite group and the anonymous auth endpoints.

---

## 2. User Creation & Authentication

### The central design decision: ONE church per user

ASP.NET Core Identity is configured with **`ApplicationUser` (the person) as `TUser`**, and each user
carries a **single nullable `ChurchId`**:

- `Grained.Domain/Entities/ApplicationUser.cs` — `ApplicationUser : IdentityUser<Guid>` `:5`,
  `FullName` `:7`, **`Guid? ChurchId` `:10`** (null for SuperAdmin), `Church` nav `:11`, `IsActive`
  `:13`, 1:1 `TeacherProfile?` `:16`.
- `Grained.Api/Program.cs:30-33` — `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<Guid>>()`.

**This is the inverse of EPP.** EPP registers Identity's `TUser` as
`AccountOrganisationMembership` — the *membership*, not the person — so one `Account` can hold many
memberships (one per org) and sign-in/lockout act on the membership. Grained has no `Membership`
join table: a user *is* bound to exactly one church.

**Consequence (known gap):** a teacher who leaves one church and joins another — or serves at two at
once — cannot be re-added under the same email. Email is globally unique (see the guard in
`TeacherService.CreateAsync` below and the `UserName = email` Identity default), and there is no
transfer path. Solving this properly means adopting EPP's membership shape:
`(UserId, ChurchId, Role, IsActive)` with a church-switcher and the JWT carrying the *active* church.
Deferred deliberately — see `CLAUDE.md` §9.

### Schema & roles

- `Grained.Domain/Entities/TeacherProfile.cs` — `TeacherProfile : AuditableEntity` `:5`,
  `ApplicationUserId`/nav `:7-8`, `ChurchId`/nav `:10-11`, `Bio` `:13`, `AssignedClassGroups` `:15`.
  1:1 with `ApplicationUser`.
- `Grained.Domain/Common/Roles.cs` — `SuperAdmin`, `ChurchAdmin`, `Teacher` `:5-7`, `All[]` `:9`.

### Authentication (JWT bearer, no cookies)

- **JWT setup** — `Grained.Api/Program.cs:41-58`: `MapInboundClaims = false` `:44`; validates
  Issuer/Audience/SigningKey/Lifetime; `NameClaimType = NameIdentifier` `:54`,
  `RoleClaimType = Role` `:55`, `ClockSkew = 30s` `:56`. Key from `JwtOptions`.
- **Token issuing** — `Grained.Api/Auth/JwtTokenService.cs` `Create(user, roles)` `:18-52`. Claims:
  `NameIdentifier`=Id `:25`, `Email` `:26`, `FullName` (custom `FullNameClaimType`) `:27`,
  `ChurchId` (custom `ChurchIdClaimType`, only if non-null) `:29-32`, one `Role` per role `:33-36`.
  HMAC-SHA256 `:47`. (Claim-type constants shared with
  `Grained.Infrastructure/Identity/ApplicationUserClaimsPrincipalFactory.cs:16-17`.)
- **Current user** — `Grained.Api/Auth/HttpContextCurrentUserService.cs`: `UserId` `:17`,
  `ChurchId` `:19`, `IsSuperAdmin/IsChurchAdmin/IsTeacher` `:21-25`, **`RequireChurchId()` `:27-28`**
  (throws `ValidationException` if no church claim).
- **Authorization policies** — `Grained.Api/Program.cs:59-65`: **`SuperAdmin`** → RequireRole
  SuperAdmin; **`ChurchAdmin`** → RequireRole ChurchAdmin; **`Staff`** → ChurchAdmin **OR** Teacher.
  Reads tend to be `Staff`; writes `ChurchAdmin`; provisioning `SuperAdmin`.
- **Password / uniqueness options** are **not explicitly configured** — ASP.NET Identity defaults
  apply (`RequireUniqueEmail` defaults false; password needs digit/lower/upper/non-alnum, min 6).
  Email uniqueness is instead enforced in code (below) and by the `UserName = email` unique index.

### Auth endpoints — `Grained.Api/Endpoints/AuthEndpoints.cs`

- **`POST /api/auth/login`** `:17-34` — `FindByEmailAsync`, reject if null or `!IsActive` `:23`,
  `CheckPasswordAsync` `:26`, `GetRolesAsync` `:29`, issue JWT `:30` → `LoginResponse`.
- **`GET /api/auth/me`** `:36-53` `.RequireAuthorization()` — rebuilds `UserDto` from claims (for
  the React `/me` rehydrate).
- **`POST /api/auth/forgot-password`** `:56-81` `.AllowAnonymous().RequireRateLimiting("invite")` —
  **always 200, no account-existence leak** `:75-80`; only generates an Identity reset token if the
  user exists and is active `:68-70`; **dev-only** `resetUrl` `:79`.
- **`POST /api/auth/reset-password`** `:84-103` — `ResetPasswordAsync` with Identity's single-use
  token `:94`; generic error on invalid/expired `:97-98`.

### The THREE ways a user record is created

1. **Seed** — `Grained.Infrastructure/Persistence/Seed/DbSeeder.cs` `SeedAsync` `:11-216`: creates
   roles `:18-22`, the **SuperAdmin** (`superadmin@grained.org`, `ChurchId` null) `:24-35`, and a
   sample **ChurchAdmin** (`admin@gracecommunity.org`, `ChurchId = church.Id`) `:49-58`. Idempotent.
2. **Invite-accept → ChurchAdmin** — `InviteEndpoints.cs` `POST /accept` `:33-92` (see §1): creates
   `ApplicationUser` with `ChurchId = invite.ChurchId` inside a transaction, `AddToRoleAsync(ChurchAdmin)`,
   activates the church, returns a JWT. Rejects a duplicate email with **409** `:56-57`.
3. **Teacher creation** — `Grained.Application/Teachers/TeacherService.cs` `CreateAsync` `:36-70`:
   **global duplicate-email guard** `FindByEmailAsync` → throws `"A user with this email already
   exists."` `:38-39`; builds `ApplicationUser` with `ChurchId = churchId` `:43-50`; generates a
   **temporary password** `GenerateTemporaryPassword()` (`$"Cpk{8hex}!1"`) `:52,:122-123`;
   `CreateAsync` `:53`; `AddToRoleAsync(Teacher)` `:57`; creates the 1:1 `TeacherProfile` `:59-61`;
   assigns class groups `:63-67`. `SetActiveAsync` `:98-108` is a **soft-deactivate** (flips both
   `profile.IsActive` and `user.IsActive`; login is blocked by the `!IsActive` check) — there is no
   hard delete, which is exactly why the email stays reserved.

### Frontend auth

- `grained-web/src/auth/AuthContext.tsx` — `AuthProvider` `:22-61`: boot rehydrate via `/auth/me`
  `:27-36`; `login` POSTs `/auth/login` and stores the token `:38-45`; `applySession(token, user)`
  for post-invite auto-login `:48-51`; `logout` `:53-56`.
- `grained-web/src/auth/ProtectedRoute.tsx:5-17` — Loading while booting, redirect to `/login` if no
  user. Public routes: `/login`, `/forgot-password`, `/reset-password`, `/accept-invite`.
- `grained-web/src/lib/api.ts` — `api<T>()` `:19-44`: prefixes `/api`, attaches
  `Authorization: Bearer` `:25`, clears the token on 401 `:31`, surfaces the server `message` as
  `ApiError` `:32-39`. Token in `localStorage['grained.token']`.

---

## 3. Lesson Lifecycle (authoring & publishing)

The **Lesson** is Grained's authored content unit — the closest analog to an EPP **Event**: an
organiser-authored object that moves through a draft → publish lifecycle and has attached
sub-structure (EPP: ticket types + questions; Grained: a memory verse + a quiz). Unlike EPP, a
Lesson is **not** a purchasable product and has no cart/payment pipeline.

### Schema (`Grained.Domain/Entities/`)

- **`Lesson.cs:3`** — `ChurchId` `:7`, `Title`, `BibleReference`, `Theme?`, `AgeGroup`,
  `StoryContent`, `LearningObjective?`, `Activity?`, `Prayer?`; **status is a bool `IsPublished`
  `:19`** — there is *no* Draft/Published enum (contrast EPP's `EventStatus`). Navs: `MemoryVerse?`
  `:23`, `Quiz?` `:24`, `AssignedClassGroups` `:25` (→ `LessonClassGroup`).
- **`Quiz.cs:3`** — 1:1 with Lesson (`LessonId` `:7`), `Title`, `Description?`, `Questions` `:13`.
- **`QuizQuestion.cs:5`** — `QuizId` `:9`, `QuestionText` `:12`, `QuestionType` `:13`,
  `int Points = 1` `:14`, `Options` `:16`.
- **`QuizOption.cs:3`** — `QuizQuestionId` `:7`, `OptionText` `:10`, `bool IsCorrect` `:11`.
- **`QuestionType`** enum — `Enums/QuestionType.cs:3`: `SingleChoice, TrueFalse, FillInTheBlank`.
- **`MemoryVerse.cs:3`** — 1:1 with Lesson: `VerseText`, `BibleReference`, `ShortExplanation?`.
- **`LessonClassGroup.cs:4`** — join Lesson↔ClassGroup (`LessonId`, `ClassGroupId`, `AssignedAtUtc`).
- Form models — `Grained.Application/Lessons/LessonModels.cs`: `LessonFormModel:44`,
  `MemoryVerseFormModel:78` (`IsProvided` `:89`), `QuizQuestionFormModel:103` (implements
  `IValidatableObject`, `Validate` `:117` enforces ≥1 correct option), `QuizOptionFormModel:92`.

### Lifecycle

```
Create (IsPublished = false, empty Quiz auto-created)
  → edit details / memory verse / quiz questions   (AddOrUpdateQuestion, RemoveQuestion)
  → assign to class groups                          (AssignToClassGroup)
  → Publish  ── 3 validation gates ──▶  IsPublished = true   ⇄  Unpublish
```

### Service — `Grained.Application/Lessons/LessonService.cs`

- `GetForChurchAsync(churchId, bool? publishedOnly)` `:10-28` — optional published filter `:18-19`.
- `GetDetailAsync(id, churchId)` `:30-39` — `ToDetailDto` `:238`; null → 404.
- `CreateAsync(churchId, LessonFormModel)` `:41-70` — creates the Lesson and **auto-creates an empty
  `Quiz`** titled `"{Title} Quiz"` `:54`; adds a `MemoryVerse` only if `model.MemoryVerse.IsProvided`
  `:57`.
- `UpdateAsync(churchId, LessonFormModel)` `:72-109` — church-scoped fetch `:79`; upserts/removes the
  memory verse `:92-106`.
- `PublishAsync(id, churchId)` `:111-131` — **three gates**: memory verse required `:119-120`, ≥1
  quiz question `:122-123`, every question has options with ≥1 correct `:125-126`; then
  `IsPublished = true` `:128`. `UnpublishAsync` `:133-140`.
- `AssignToClassGroupAsync` `:142-159` / `UnassignFromClassGroupAsync` `:161-172` — validate both the
  lesson and the class group belong to the church; assignment is idempotent `:152-155`.
- `AddOrUpdateQuestionAsync(lessonId, churchId, QuizQuestionFormModel)` `:174-225` — validates ≥1
  correct option `:176-177`; lazily creates the Quiz if missing `:184-189`; insert-vs-update by
  `model.Id` `:191`; on update, clears+removes old options `:208-209`. `RemoveQuestionAsync`
  `:227-236` (church-scoped via `q.Quiz.Lesson.ChurchId`).

> **Gap to note:** `UpdateAsync`, `AddOrUpdateQuestionAsync`, and `RemoveQuestionAsync` do **not**
> check `IsPublished`, so a **published lesson remains fully editable** — there is no post-publish
> edit lock (EPP, by contrast, throws `EventCannotBeEditedEppException` once an event leaves
> `Incomplete/Unpublished`). Worth a decision before real use.

### Endpoints — `Grained.Api/Endpoints/LessonEndpoints.cs` (base `/api/lessons`)

`GET ""` `:13` (`?publishedOnly`) · `GET /{id}` `:16` · `POST ""` `:19` · `PUT /{id}` `:25` ·
`POST /{id}/publish` `:32` · `POST /{id}/unpublish` `:38` · `POST /{id}/assign-class` `:44` ·
`POST /{id}/unassign-class` `:50` · `POST /{id}/questions` `:56` · `DELETE /{id}/questions/{qId}`
`:62`. Reads `Staff`; all writes `ChurchAdmin`.

### Supporting catalogue: Class Groups & Children

Lessons are delivered to **Class Groups**, which contain **Children** — Grained's "roster,"
analogous to EPP's product/catalogue setup that events hang off.

- **Class Groups** — `Grained.Application/ClassGroups/ClassGroupService.cs`:
  `GetAllForChurchAsync(churchId, includeInactive=false)` `:10-21` (DTO includes active child count
  `:19`), `GetByIdAsync` `:23-31`, `CreateAsync` `:33-46`, `UpdateAsync` `:48-62`, `SetActiveAsync`
  `:64-70` (soft). Endpoints `Grained.Api/Endpoints/ClassGroupEndpoints.cs` (base
  `/api/class-groups`). Entity `Grained.Domain/Entities/ClassGroup.cs:5` (`Name`, `MinAge`, `MaxAge`,
  `Description?`; navs `Children`, `Attendances`, `AssignedTeachers`, `AssignedLessons`).
  **Teacher↔ClassGroup** assignment lives in `TeacherService` (create `:63-66`, update-diff
  `:87-93`), via `TeacherClassGroup` (`Entities/TeacherClassGroup.cs:4`).
- **Children** — `Grained.Application/Children/ChildService.cs`:
  `GetForChurchAsync(churchId, ChildFilter)` `:10-33` (filters class group/active; Min/MaxAge applied
  in-memory since age is computed `:27-30`), `GetByIdAsync` `:35-41`, `CreateAsync` `:43-61`,
  `UpdateAsync` `:63-82`, `AssignClassGroupAsync` `:84-93`, `SetActiveAsync` `:95-101` (soft).
  `EnsureClassGroupBelongsToChurch` `:103-108` is the cross-church write guard. Entity
  `Grained.Domain/Entities/Child.cs:5` — **a child belongs to exactly one class group** (required
  `ClassGroupId` `:10`), plus `FirstName/LastName`, `DateOfBirth` (DateOnly), parent contact fields.
  Endpoints `Grained.Api/Endpoints/ChildrenEndpoints.cs` (base `/api/children`).

---

## 4. Ministry Delivery — Roster, Attendance, Progress & Badges

This is Grained's **runtime recording** side — the analog of EPP's event registration / fundraising
donation flows, but with **no cart, payment, or gateway**: teachers record attendance and progress
directly. All of it is child-centric.

### Schema (`Grained.Domain/Entities/`)

- **`Attendance.cs:3-19`** — `ChildId`, `ClassGroupId`, nullable `LessonId`, `AttendanceDate`
  (DateOnly `:16`), `IsPresent` `:17`, `Notes?` `:18`. **No direct `ChurchId`** — isolation via
  `ClassGroup.ChurchId`. No DB unique constraint on `(Child, ClassGroup, Date)` — de-dup is in
  service code.
- **`ChildProgress.cs:3-18`** — the **lesson-completion** record: `ChildId`, `LessonId`,
  `CompletedAtUtc` (DateTime?, null until completed `:13`), `QuizScore` (int? `:14`),
  `MemoryVerseCompleted`/`ActivityCompleted`/`PrayerCompleted` `:15-17`. Unique index
  `(ChildId, LessonId)` (`ApplicationDbContext.cs:202`).
- **`Badge.cs:5-16`** — `Badge : AuditableEntity`, `ChurchId` `:7`, `Name`, `Description`,
  `IconName`, `Criteria`, nav `ChildBadges` `:15`.
- **`ChildBadge.cs:3-14`** (award join) — `ChildId`, `BadgeId`, `AwardedAtUtc` `:13`. Unique index
  `(ChildId, BadgeId)` (`ApplicationDbContext.cs:189`). No direct `ChurchId`.

### Attendance flow — `Grained.Application/AttendanceTracking/AttendanceService.cs`

```
GET /api/attendance/roster?classGroupId&date   → GetRosterAsync
   validate class group ∈ church → load active children in group
   → merge existing Attendance rows (default IsPresent=false when none) → roster DTO
POST /api/attendance {classGroupId, date, lessonId?, entries[]}  → SaveAsync (upsert)
   validate class group ∈ church → keep only children actually in the church
   → per entry: insert new Attendance OR update IsPresent/Notes/LessonId → one SaveChanges
```

- `GetRosterAsync(churchId, classGroupId, date)` `:10-31` — church-validates the class group
  `:12-15`, loads active children `:17-20`, indexes existing attendance by `ChildId` `:22-24`,
  defaults `IsPresent=false` when absent `:29`.
- `SaveAsync(churchId, AttendanceSaveModel)` `:33-78` — upsert: validate `:35-38`, filter to
  in-church child ids `:41-44`, load existing rows `:46-49`, insert `:59-67` or update `:71-73`, one
  `SaveChangesAsync` `:77`.
- Models `AttendanceModels.cs` (`AttendanceRosterEntryDto:3`, `AttendanceEntryFormModel:10`,
  `AttendanceSaveModel:17` — `AttendanceDate` defaults today). Endpoints
  `Grained.Api/Endpoints/AttendanceEndpoints.cs` (`/api/attendance`, `Staff` `:10`): `GET /roster`
  `:12`, `POST ""` `:16`.

### Badges — `Grained.Application/Badges/BadgeService.cs`

CRUD only, all filtered by `ChurchId`: `GetForChurchAsync` `:10-19`, `GetByIdAsync` `:21-25`,
`CreateAsync` `:27-40`, `UpdateAsync` `:42-56`, `SetActiveAsync` `:58-64`. Endpoints
`Grained.Api/Endpoints/BadgeEndpoints.cs` (`/api/badges`, `ChurchAdmin` `:10`).

> **Gap to note — awarding is not implemented.** The `ChildBadge` table + unique index exist, but
> **no service or endpoint ever inserts a `ChildBadge`** — badges can be defined but not yet awarded
> to or listed for a child. This is the biggest missing piece in the delivery flow. (Compare EPP,
> where the post-payment `*TasksExecutor` is exactly where issuance happens.)

### Reports — `Grained.Application/Reports/ReportService.cs`

Four read-only aggregations (the Reports screen's four tabs — note there is **no badges tab**):

- `GetChildProgressReportAsync(churchId)` `:8-25` — per active child: completed count
  (`CompletedAtUtc != null`) + average non-null `QuizScore`.
- `GetClassProgressReportAsync(churchId)` `:27-47` — per class group: children, completions, and
  completion rate = completed / (children × published-lesson-count) × 100 `:41-45`.
- `GetAttendanceReportAsync(churchId, from, to)` `:49-69` — per class group over a date range:
  sessions, present, absent, rate%.
- `GetLessonCompletionReportAsync(churchId)` `:71-86` — per lesson: completions + avg quiz score.

Endpoints `Grained.Api/Endpoints/ReportEndpoints.cs` (`/api/reports`, `Staff` `:10`):
`GET /child-progress` `:12`, `/class-progress` `:15`, `/attendance?from&to` `:18`,
`/lesson-completion` `:21`.

### Dashboard — `Grained.Application/Dashboard/DashboardService.cs`

`GetSummaryAsync(churchId)` `:8-35` aggregates `totalChildren` `:10`, `totalClasses` `:11`,
`publishedLessons` `:12`, `totalTeachers` (active users with a `TeacherProfile`) `:13-14`,
`recentAttendance` (top 10, church via `ClassGroup.ChurchId`) `:16-23`, and `recentCompletions`
(top 10 completed `ChildProgress`, church via `Lesson.ChurchId`) `:25-32`. Endpoint
`Grained.Api/Endpoints/DashboardEndpoints.cs` `GET /api/dashboard` `:23` (any authenticated; 400 if
no church context `:17-18`).

---

## Grained ↔ EPP concept map

| Concept | EasyPaymentsPlus (EPP) | Grained |
|---|---|---|
| Tenant | `Organisation` (self-service signup) | `Church` (SuperAdmin-provisioned + invite) |
| Identity `TUser` | **`AccountOrganisationMembership`** (person↔org link) | **`ApplicationUser`** (the person) |
| Person ↔ tenant | **many-to-many** via memberships | **one-to-one** via `ApplicationUser.ChurchId` |
| Auth transport | Cookie (+ secondary pre-login cookie) | **JWT bearer** (localStorage) |
| Roles | OrganisationAdmin / Customer / Helpdesk… | SuperAdmin / ChurchAdmin / Teacher |
| Invite teammate | `Account.Invitations` + SMS/email verify | `Invitation` (DataProtection token, email only) |
| Authored object | `Event` (a purchasable `Product`) | `Lesson` (content; not purchasable) |
| Authored lifecycle | `EventStatus` enum, hard edit-lock | `bool IsPublished`, **no edit-lock** |
| Sub-structure | ticket types, questions, predefined fields | memory verse, quiz (questions/options) |
| Runtime recording | attendee ticket **purchase** (cart→gateway→ticket) | teacher records **attendance / progress** (no payment) |
| "Second object" | Fundraising **Campaign** + donations | child **progress + badges** (delivery, not money) |
| Post-completion issuance | `*TasksExecutor` issues tickets/donations | **not implemented** (`ChildBadge` award missing) |
| Validation | FluentValidation + rich `*EppException` types | inline guards + single `ValidationException` |
| Errors → HTTP | typed exceptions caught per controller | `ValidationException` → 400 (`Program.cs:99-107`) |
| Schema deploy | DbUp (`01-Init.sql`) | EF migrations on startup |
| Tenant isolation | (org-scoped queries) | explicit `.Where(ChurchId == churchId)`, no global filter |

## Known gaps surfaced by this map

1. **One-church-per-user** (`ApplicationUser.ChurchId`). No `Membership` table → a teacher/admin
   can't move between or serve at multiple churches under one email. EPP's `TUser = membership`
   shape is the reference fix. (§2; `CLAUDE.md` §9.)
2. **No post-publish edit lock on Lessons** — published lessons stay fully editable (§3).
3. **Badge awarding unimplemented** — `ChildBadge` schema exists but nothing inserts it (§4).
4. **API auto-migrates + seeds on every startup** — convenient for dev; gate behind a deliberate
   deploy step for production (`CLAUDE.md` §9).

## Quick file-map reference

| Concern | Primary file(s) |
|---|---|
| Layer/DI wiring | `Grained.Api/Program.cs`, `Grained.Application/DependencyInjection.cs`, `Grained.Infrastructure/DependencyInjection.cs` |
| Church schema | `Grained.Domain/Entities/Church.cs`, `Invitation.cs`, `Enums/{ChurchStatus,InvitationStatus,MembershipRole}.cs` |
| Church onboarding service | `Grained.Application/Onboarding/ChurchOnboardingService.cs`, `OnboardingModels.cs` |
| Invite token / email | `Grained.Infrastructure/Onboarding/InviteTokenService.cs`, `LoggingInviteEmailSender.cs` |
| Church endpoints | `Grained.Api/Endpoints/ChurchEndpoints.cs`, `InviteEndpoints.cs` |
| Church CRUD (SuperAdmin) | `Grained.Application/Churches/ChurchService.cs`, `ChurchModels.cs` |
| User entities | `Grained.Domain/Entities/ApplicationUser.cs`, `TeacherProfile.cs`, `Common/Roles.cs` |
| Identity / JWT / policies | `Grained.Api/Program.cs:30-65`, `Grained.Api/Auth/JwtTokenService.cs`, `HttpContextCurrentUserService.cs` |
| Auth endpoints | `Grained.Api/Endpoints/AuthEndpoints.cs` |
| User creation paths | `Grained.Infrastructure/Persistence/Seed/DbSeeder.cs`, `Grained.Api/Endpoints/InviteEndpoints.cs`, `Grained.Application/Teachers/TeacherService.cs` |
| Frontend auth | `grained-web/src/auth/AuthContext.tsx`, `ProtectedRoute.tsx`, `grained-web/src/lib/api.ts` |
| Lesson schema | `Grained.Domain/Entities/{Lesson,Quiz,QuizQuestion,QuizOption,MemoryVerse,LessonClassGroup}.cs`, `Enums/QuestionType.cs` |
| Lesson service/endpoints | `Grained.Application/Lessons/LessonService.cs`, `LessonModels.cs`, `Grained.Api/Endpoints/LessonEndpoints.cs` |
| Class groups / children | `Grained.Application/ClassGroups/ClassGroupService.cs`, `Grained.Application/Children/ChildService.cs` (+ endpoints) |
| Attendance | `Grained.Application/AttendanceTracking/AttendanceService.cs`, `Grained.Api/Endpoints/AttendanceEndpoints.cs` |
| Badges | `Grained.Application/Badges/BadgeService.cs`, `Grained.Api/Endpoints/BadgeEndpoints.cs` (award: **TODO**) |
| Progress / reports / dashboard | `Grained.Domain/Entities/ChildProgress.cs`, `Grained.Application/Reports/ReportService.cs`, `Grained.Application/Dashboard/DashboardService.cs` |
| Church isolation tests | `Grained.Tests/DataIsolation/ChurchDataIsolationTests.cs` |

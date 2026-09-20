# CARE HOME BACK OFFICE — COMPLETE MANUAL QA RUNBOOK

**Repository:** `C:\Users\HP\Downloads\CarehomeSystem`  
**Application:** Care Home Back Office (Angular 22 SPA + ASP.NET Core API + SQL Server)  
**Purpose:** Step-by-step manual QA you can execute with the app open beside this document.  
**Source of truth:** Current code and configuration in this repository (not older audit documents).

**Audience — fresh start (default path in this document)**

Use this runbook when you have **no organisation yet**: a new Docker volume, first login, or an empty platform. You will:

1. Start the stack (**Section 0**).  
2. Sign in as **PlatformAdmin** and **register your organisation** (**Phase 0 — Steps 1–5**).  
3. Sign in as the new **TenantAdmin**, set a permanent password (**Phase 0 — Steps 4–5**), then enter master data and invoices (**Phase 0 — Step 6**; field tables in `docs/demo/DEMO_DATA_ENTRY_CHECKLIST.md` Parts 3–6).  
4. Run the numbered QA tests (**QA-01** onward) and mark the **Master QA tracker**.

If someone else already created **Demo Care Group** and **INV-0001** / **INV-0002**, skip **Phase 0** and start at **Section 0** health checks + **QA-01**.

**How to use this runbook**

1. Complete **Section 0 (Prerequisites)** once per session.  
2. On a **fresh database**, complete **Phase 0** before any P0 billing or invoice tests.  
3. Follow **Recommended QA execution order**.  
4. For each test, mark **Status** in the **Master QA tracker** (⬜ / 🟢 / 🔴 / 🟡 / ⚪).  
5. After demo data exists, do **not** run destructive steps marked **DO NOT EXECUTE DURING DEMO DATA QA**.

---

## Recommended QA execution order

| Order | Area | Why this order |
|------:|------|----------------|
| 1 | Environment & health checks | Nothing else works if the stack is down |
| 2 | **Phase 0 — Register organisation (PlatformAdmin)** | Tenant does not exist until you create it |
| 3 | **Phase 0 — TenantAdmin password + master data** | Billing and invoices need companies, residents, rates |
| 4 | **Phase 0 — Generate INV-0001 / INV-0002** (checklist Part 4–6) | P0 invoice/PDF/payment tests assume these |
| 5 | Login & password change (QA-01) | Confirms JWT and tenant context after setup |
| 6 | Dashboard | Quick sanity of KPIs and links |
| 7 | Operations (Companies → Care Homes → Residents → Funding) | Verify what you entered in Phase 0 |
| 8 | Billing Setup (authorities, categories, nominals, templates) | Required for billing preview |
| 9 | Billing workspace (preview → exceptions → generate rules) | Core finance engine |
| 10 | Invoices (list → detail → PDF → payment) | Output verification |
| 11 | Email (simulated) | Workflow only in Development |
| 12 | Credit notes (preview-first; avoid unplanned generate) | Adjustments |
| 13 | Reports (all nine types + exports) | Reporting layer |
| 14 | Sage export & misc charges | Secondary finance flows |
| 15 | Audit & users | Admin-only surfaces |
| 16 | Role / authorization (TenantAdmin vs ReadOnly vs PlatformAdmin) | Security behaviour |
| 17 | Billing safety & negative tests | Edge cases and blocking rules |
| 18 | Navigation / UX / validation polish | P2 |
| 19 | End-to-end business workflow (QA-28) | Single finance-user journey |

---

## IMPORTANT — DO NOT DESTROY DEMO DATA

**Fresh start:** On a new volume you have **no** protected invoices yet — `docker compose down -v` only wipes an empty or in-progress database. Once you finish **Phase 0** and generate **INV-0001** / **INV-0002**, treat the rules below as mandatory.

### Never run during protected-data QA

| Action | Where | Effect |
|--------|--------|--------|
| `docker compose down -v` | Repository root | **Deletes** SQL volume `carehome-sql-data` and document volume `carehome-documents` (all tenants, invoices, PDFs) |
| Regenerate invoices for Aug 2026 when **INV-0001** / **INV-0002** already exist | Billing workspace | Duplicate billing / confusing demo state |
| **Generate credit note** without a planned narrative | Credit notes | Creates **CN-000x** permanently |
| **Mark unpaid** on **INV-0001** after it was marked paid for demo | Invoice detail | Breaks outstanding-report demo story |
| **Void** **INV-0001**, **INV-0002**, or **CN-0001** | Invoice detail | Irreversible demo damage |
| **Mark unpaid** on paid demo invoices just to test the button | Invoice detail | Use a throwaway invoice instead |
| Change organisation **Invoice prefix** / **Credit note prefix** on a populated tenant | Organisation settings | Can confuse invoice numbering expectations |
| **Confirm import** on misc charges CSV during demo QA | Misc charges | Creates billable misc rows |
| **Export CSV** on Sage (writes batch) | Sage export | Creates export history (usually acceptable; avoid if you need a clean Sage list) |

### Protected records (when demo data matches `docs/demo/DEMO_DATA_ENTRY_CHECKLIST.md`)

| Record | Rule |
|--------|------|
| **INV-0001** (Alex Morgan, Aug 2026, **£2,546.43**) | Do **not** void; do **not** regenerate August billing; after payment demo, do **not** mark unpaid |
| **INV-0002** (Jordan Blake, Aug 2026, **£2,657.14**) | Do **not** void; do **not** regenerate; credit note generate only if you accept **CN-0001** in the database |
| **CN-0001** (if present) | Do **not** void/delete |
| **Alex Morgan** (`RVH-001`), **Jordan Blake** (`RVH-002`) | Do **not** delete contracts/rates needed for billing demos |
| **Demo Care Group** tenant | Do **not** deactivate during tenant QA |

### Safe to create (with cleanup)

| Data | Notes |
|------|--------|
| Temporary resident **without** funding contract | For **missing contract** visibility test; archive afterward from Residents list |
| New company/care home on a **throwaway** rehearsal org | Only on non-demo tenant or after explicit reset |
| Platform org **Existing Organisation** | Migration artifact; deactivate only on **fresh** setup, not mid-QA on live demo DB |

### Recovery (non-destructive)

| Problem | Recovery |
|---------|----------|
| Bad login / stale JWT | User menu → **Sign out**; or clear site data for `http://localhost:4200` (localStorage key `carehome.auth`) |
| API error after partial action | Note URL and message; refresh page; do **not** repeat **Generate** until preview is re-checked |
| Wrong filter on list | Click **Clear** / reset filters |
| Accidental navigation | Breadcrumb **Home** or sidebar link |
| Need completely clean DB | **Only on isolated machine:** stop stack, then `docker compose down -v` and rebuild — **this wipes all data** |

---

## 0. Prerequisites

### 0.1 What you need before starting

| Item | Required? |
|------|-----------|
| Windows PC with browser (Chrome or Edge recommended) | Yes |
| **Docker Desktop** running (recommended full stack) | Yes for Docker path |
| OR: .NET 10 SDK, Node.js, SQL LocalDB (local dev path) | Alternative to Docker |
| Password manager entries (no secrets in this document) | Yes |
| Organisation already registered | **No** on fresh start — you create it in **Phase 0** |
| Demo / scripted master data and invoices (see **0.6**) | **No** until you complete **Phase 0**; **yes** before P0 billing/invoice QA |
| Second browser profile (optional, for ReadOnly user) | Optional |

### 0.2 Environment variables

Configuration is loaded from `docker-compose.yml`, image-baked `appsettings*.json`, and optional repository-root `.env`.  
Angular **does not** read runtime env vars; it proxies `/api` to the API container (Docker) or `localhost:5092` (`npm start`).

**Legend:** **Required to start** | **Optional** | **Production-only** | **Development/demo-only**

| Variable | Required? | Used by | Purpose | Example / expected | Safe to expose? |
|----------|-----------|---------|---------|-------------------|-----------------|
| *(none manually)* | No | Docker stack | Defaults suffice for standard local QA | — | — |
| `MSSQL_SA_PASSWORD` | Required to start (defaulted) | `carehome-sql`, `carehome-api` | SQL `sa` password; must match connection string | Default: `CareHomeDevSql2022@` or `<SECRET_FROM_LOCAL_ENV>` in `.env` | **No** if customized |
| `ACCEPT_EULA` | Required to start (auto) | SQL container | SQL Server license | `Y` | Yes |
| `MSSQL_PID` | Required to start (auto) | SQL container | SQL edition | `Developer` | Yes |
| `ASPNETCORE_ENVIRONMENT` | Required to start (auto) | API | Host profile | `Development` (**demo-only** behaviour) | Yes |
| `ASPNETCORE_URLS` | Required to start (auto) | API | Listen URL in container | `http://+:8080` | Yes |
| `ConnectionStrings__DefaultConnection` | Required to start (auto) | API / EF Core | Database | Contains `<SECRET_FROM_LOCAL_ENV>` password when customized | **No** |
| `Database__ApplyMigrations` | Required to start (auto) | API startup | Auto-migrate on empty DB | `true` | Yes |
| `DocumentStorage__RootPath` | Required to start (auto) | API | PDF / Sage file storage | `/app/App_Data/documents` (Docker volume) | Yes |
| `App__PublicUrl` | Required to start (auto) | API | Links in provisioning email body | `http://localhost:4200` | Yes |
| `Jwt:Key` / `Jwt__Key` | Defaulted in Development | API | JWT signing | Dev placeholder in `appsettings.Development.json`; prod `<SECRET_FROM_LOCAL_ENV>` | **No** in prod |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryHours`, `Jwt:ClockSkewMinutes` | Defaulted | API | Token validation | `CareHomeApi`, `CareHomeWeb`, `8`, `2` | Yes |
| `Cors:AllowedOrigins` / `Cors__AllowedOrigins__*` | Defaulted in Development | API | Browser CORS | `http://localhost:4200`, `http://127.0.0.1:4200` | Yes |
| `Https:Redirect` | Defaulted | API | HTTPS redirect | `false` in Development | Yes |
| `Email:Mode` / `Email__Mode` | Defaulted | API | Email delivery | `Development` = **simulated** (SMTP **not** required) | Yes |
| `Email:FromAddress`, `Email:FromName` | Defaulted | API | Sender metadata | `noreply@localhost`, `Care Home Billing` | Yes |
| `Email:Smtp:*` / `Email__Smtp__*` | Optional | API | Real SMTP | **Production-only** path when `Mode=Smtp` | **No** (password) |
| `Seed:AdminEmail` / `Seed__AdminEmail` | Development/demo-only | API | Platform admin bootstrap | `admin@localhost` | Yes |
| `Seed:AdminPassword` / `Seed__AdminPassword` | Development/demo-only | API | Platform admin password | `<DEMO_PLATFORM_ADMIN_PASSWORD>` — never paste real value here | **No** |
| `ConnectionStrings:DefaultConnection` (LocalDB) | Local non-Docker path | `appsettings.json` | Host-run API | LocalDB connection string | N/A |
| `ASPNETCORE_ENVIRONMENT=Production` | Production-only | API | Strict validation | Not for local QA | — |
| Azure / deploy script secrets | Production-only | Infra | Hosting | See `docs/AZURE_HOSTING.md` | **No** |

**TenantAdmin password** is **not** an environment variable; it comes from organisation creation (temporary password) and `/change-password`.

### 0.3 How to start the application

#### Path A — Docker (recommended)

**Step 0.3.1 — Open PowerShell**

Go to:

`C:\Users\HP\Downloads\CarehomeSystem`

**Step 0.3.2 — Optional `.env`**

Only if you must change SQL password: copy `.env.example` or `.env.demo.example` to `.env` and set `MSSQL_SA_PASSWORD=<SECRET_FROM_LOCAL_ENV>`.

**Step 0.3.3 — Start stack**

```powershell
docker compose up -d --build
```

| Service | Container | Host port | In Docker? |
|---------|-----------|-----------|------------|
| SQL Server | `carehome-sql` | `localhost,14333` | Yes |
| API | `carehome-api` | http://localhost:5092 | Yes |
| Angular (nginx + proxy) | `carehome-web` | http://localhost:4200 | Yes |

**Wait:** First build may take several minutes. After containers are **healthy**, allow **30–90 seconds** for the web container before opening the login page.

**Do not use** `docker compose down -v` if you need existing demo data.

#### Path B — Local API + local frontend

1. SQL: update `backend/CareHome.Api/appsettings.json` → `ConnectionStrings:DefaultConnection`.  
2. Apply migrations: `cd backend\CareHome.Api` → `dotnet ef database update`.  
3. API: `dotnet run --launch-profile http` → http://localhost:5092  
4. Frontend: `cd frontend\care-home-web` → `npm install` → `npm start` → http://localhost:4200  

Email remains **Development** mode via `appsettings.Development.json` when `ASPNETCORE_ENVIRONMENT=Development`.

### 0.4 Service health checks (before UI QA)

| # | Action | Expected result | PASS | FAIL |
|---|--------|-----------------|------|------|
| H-1 | Run `docker compose ps` | `carehome-sql` **healthy**, `carehome-api` **healthy**, `carehome-web` **running** | All match | Any container exited or unhealthy |
| H-2 | Open http://localhost:5092/health/live | JSON status healthy | Healthy | Error / connection refused |
| H-3 | Open http://localhost:5092/health/ready | JSON status healthy (SQL reachable) | Healthy | Unhealthy |
| H-4 | Open http://localhost:4200/health/ready | Same as H-3 via frontend proxy | Healthy | 502 / error |
| H-5 | Open http://localhost:4200/login | Title **Care Home Back Office**; fields **Email address**, **Password**; button **Sign in** | Page loads | Blank / endless load |
| H-6 | (Optional) `docker compose logs api --tail 30` | No repeated crash loop | Stable logs | Migration/SQL errors repeating |

### 0.5 Test accounts

**Fresh start:** Log in as **PlatformAdmin** first (**Phase 0 Step 1**), create the organisation, then use **TenantAdmin** (**Phase 0 Step 4**).

| Role | Email placeholder | Password placeholder | Login required? | What to test |
|------|-------------------|----------------------|-----------------|--------------|
| **PlatformAdmin** | `admin@localhost` | `<DEMO_PLATFORM_ADMIN_PASSWORD>` (Development seed) | Yes — **first** on empty DB | Register organisation; list tenants |
| **TenantAdmin** | `demo-admin@example.com` (email you set at org create) | Temporary password at create → `<DEMO_TENANT_ADMIN_PASSWORD>` after `/change-password` | Yes — **after** Phase 0 Step 2 | Full tenant workflow (P0) |
| **Administrator** | (if created in **Users**) | (set in UI) | Optional | Same admin nav as TenantAdmin in app |
| **ReadOnly** | e.g. `demo-viewer@example.com` (if created) | `<DEMO_VIEWER_PASSWORD>` | Optional | Read-only; writes blocked |
| **LocationManager** | (if created + homes assigned) | (set in UI) | Optional | Scoped care homes (404 outside scope) |

**Development seed password** is configured in `appsettings.Development.json` as `Seed:AdminPassword` — store the real value only in your password manager; do not write it in bug reports.

### 0.6 Scripted data targets (after Phase 0)

**Before organisation registration:** none of the rows below exist — that is expected.

**After Phase 0:** QA **P0 billing and invoice tests** assume the fictional dataset from `docs/demo/DEMO_DATA_ENTRY_CHECKLIST.md` / `docs/demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md`. **Verify in the UI** before P0 — do not assume an old database matches.

| Data | Expected (scripted demo) | Required before QA? |
|------|--------------------------|---------------------|
| Organisation | **Demo Care Group** (active); **Existing Organisation** inactive | Yes |
| TenantAdmin | `demo-admin@example.com` | Yes |
| Company | **Demo Care Ltd** | Yes |
| Care home | **River View House** (code e.g. `RIVER01`) | Yes |
| Resident A | **Alex Morgan**, ref **RVH-001**, rate **£575.00/week** | Yes |
| Resident B | **Jordan Blake**, ref **RVH-002**, rate **£600.00/week** | Yes |
| Funding authority | **Anytown Council** (and setup entities) | Yes |
| Invoice category | **General Care** (or equivalent active category) | Yes |
| Nominal code | **4000** linked | Yes |
| Invoice template | Active template for billing stream | Yes |
| Invoice **INV-0001** | Alex, Aug 2026, total **£2,546.43** | Yes for invoice/PDF/payment QA |
| Invoice **INV-0002** | Jordan, Aug 2026, total **£2,657.14**, **Not paid** | Yes |
| Payment state | **INV-0001** = **Paid** (after payment demo step) | Yes for report QA |
| Credit note **CN-0001** | Optional; only if you deliberately generated one | No |

If any **Required** row is missing after **Phase 0**, mark dependent tests **🟡 BLOCKED** and continue **Phase 0 Step 6** / `docs/demo/DEMO_DATA_ENTRY_CHECKLIST.md` **or** restore from backup — do **not** use `docker compose down -v` on a machine you need to keep.

**Billing period for scripted amounts:** **Period start** `2026-08-01`, **Period end** `2026-08-31`.

**Resident → Billing shortcut:** Opening billing from a resident profile suggests **previous calendar month** dates. If today is in **September 2026**, that matches August 2026 automatically.

### 0.7 If scripted data is missing (blocked P0)

Do **not** guess data. Either:

1. Complete **Phase 0** below and **`docs/demo/DEMO_DATA_ENTRY_CHECKLIST.md`** Parts 3–6, **or**  
2. Restore from backup per **`docs/operations/BACKUP_AND_RECOVERY_RUNBOOK.md`**, **or**  
3. Mark billing/invoice P0 tests **🟡 BLOCKED** and continue with login/navigation-only tests.

---

## Phase 0 — Fresh start: register organisation and prepare tenant

**Starting state (fresh Docker volume or first use):**

| Check | Expected |
|-------|----------|
| Stack healthy | Section **0.4** H-1–H-5 pass |
| PlatformAdmin login | `admin@localhost` + Development seed password (see **0.5**; value in `appsettings.Development.json` → store in password manager only) |
| **Existing Organisation** | Present in platform list (migration placeholder); you may deactivate it after creating your tenant |
| Your organisation | **Does not exist yet** — you will create it in Step 2 |
| Tenant users, companies, invoices | **None** |

Use **fictional** names and emails only (no real residents or councils). For P0 tests that reference **£2,546.43** / **INV-0001**, follow the exact values in **`docs/demo/DEMO_DATA_ENTRY_CHECKLIST.md`** (organisation name **Demo Care Group**, residents **Alex Morgan** / **Jordan Blake**, etc.). You may use a different organisation name for a smoke test, but invoice totals in this runbook will not match.

### Step 1 — PlatformAdmin sign-in

| # | Route | Action | Field | Value |
|---|-------|--------|-------|-------|
| 1 | `/login` | Open app | — | http://localhost:4200/login |
| 2 | `/login` | Sign in | Email address | `admin@localhost` |
| 3 | `/login` | Sign in | Password | `<DEMO_PLATFORM_ADMIN_PASSWORD>` |

**Pass:** Side nav shows **Organisations** only (platform scope).  
**Fail:** Cannot sign in → check API health (0.4), seed config, and that you are on Development stack.

### Step 2 — Register organisation

| # | Route | Action | Field | Value |
|---|-------|--------|-------|-------|
| 1 | `/platform/tenants` | Open list | — | Side nav → **Organisations** |
| 2 | `/platform/tenants/new` | Start create | — | **Add organisation** |
| 3 | `/platform/tenants/new` | Org details | Name | `Demo Care Group` *(or your org name)* |
| 4 | `/platform/tenants/new` | Org details | Trading name | Same as name |
| 5 | `/platform/tenants/new` | Org details | Address | `1 Demo Lane, Anytown, AN1 2BC` |
| 6 | `/platform/tenants/new` | Org details | Phone | `01234 567890` |
| 7 | `/platform/tenants/new` | Org details | Email | `info@demo-care-group.example` |
| 8 | `/platform/tenants/new` | Org details | Active | ✓ checked |
| 9 | `/platform/tenants/new` | First tenant admin | Admin email | `demo-admin@example.com` |
| 10 | `/platform/tenants/new` | First tenant admin | Admin display name | `Demo Administrator` |
| 11 | `/platform/tenants/new` | Save | — | **Save** |

**Pass:**

- Success screen **Organisation created**.  
- In **Development**, a banner shows **Temporary password:** — copy it to your password manager immediately (simulated email; it may not be shown again).  
- `/platform/tenants` lists your organisation as **Active**.

**Fail:** Validation errors, duplicate admin email, or API error — note message; do not retry **Save** blindly.

**Data impact:** Creates tenant, organisation settings defaults (e.g. **INV-** / **CN-** prefixes), and TenantAdmin user with `mustChangePassword`.

### Step 3 — Deactivate migration placeholder (recommended)

| # | Route | Action | Field | Value |
|---|-------|--------|-------|-------|
| 1 | `/platform/tenants` | Edit | — | **Existing Organisation** → **Edit** |
| 2 | `/platform/tenants/{id}` | Deactivate | Active | Uncheck **Active** |
| 3 | `/platform/tenants/{id}` | Save | — | **Save** |

**Pass:** **Existing Organisation** shows **Inactive**; your new org remains **Active**.  
**Skip if:** You are only testing platform create and will not enter billing data in tenant Id=1.

**Do not** create residents, contracts, or invoices under **Existing Organisation** — use your new tenant only.

### Step 4 — TenantAdmin first login and password

| # | Route | Action | Field | Value |
|---|-------|--------|-------|-------|
| 1 | Any | Sign out | — | User menu → **Sign out** |
| 2 | `/login` | Sign in | Email address | `demo-admin@example.com` |
| 3 | `/login` | Sign in | Password | Temporary password from Step 2 |
| 4 | `/change-password` | Set password | Temporary / current | Same temporary password |
| 5 | `/change-password` | Set password | New password | `<DEMO_TENANT_ADMIN_PASSWORD>` (≥12 chars, upper, lower, number, symbol) |
| 6 | `/change-password` | Set password | Confirm | Same as new password |
| 7 | `/change-password` | Submit | — | **Save password and continue** |

**Pass:** Redirect to `/dashboard`; side nav subtitle shows **Demo Care Group** (or your org name); sections **Dashboard**, **Operations**, **Billing Setup**, **Billing**, **Reporting**, **Administration** visible.  
**Fail:** Stuck on login or `/change-password` — sign out, verify email and passwords (QA-01e).

### Step 5 — Verify organisation settings (read-only)

| # | Route | Action | Expected |
|---|-------|--------|----------|
| 1 | `/settings/organisation` | Open | **Administration** → **Organisation Settings** |
| 2 | — | Read only | Invoice prefix **INV-**; credit note prefix **CN-**; payment terms **30**; currency **GBP** |

Do **not** change prefixes on a tenant you plan to bill — defaults are correct for scripted **INV-0001**.

### Step 6 — Enter master data (companies through rates)

**You are now the tenant admin.** The app has no companies, care homes, or residents until you add them.

Follow **`docs/demo/DEMO_DATA_ENTRY_CHECKLIST.md`**:

| Checklist part | What you enter |
|----------------|----------------|
| **Part 3 — Business setup** | **Demo Care Ltd**, **River View House**, **Anytown Council**, **Alex Morgan** (contract + **£575/week** rate), nominals/categories/templates as listed |
| **Part 4–5** | **Jordan Blake** and second resident billing setup |
| **Part 6** | Billing workspace → generate **INV-0001** then **INV-0002** for **Aug 2026** (`2026-08-01` – `2026-08-31`) |

**Checkpoint before QA-01 P0 billing tests:**

- [ ] **Demo Care Group** (or your org) active; TenantAdmin password is permanent (not temporary)  
- [ ] **Demo Care Ltd**, **River View House**, **Alex** / **Jordan** with funding and rates per checklist  
- [ ] **INV-0001** total **£2,546.43**; **INV-0002** total **£2,657.14** (**Not paid**)  
- [ ] After payment demo step: **INV-0001** marked **Paid** (see checklist / QA-033)

### Step 7 — Optional platform checks (after tenant works)

Sign out → PlatformAdmin → `/platform/tenants` — confirm your org **Active** (QA-03b). Sign out before tenant QA.

---

## Master QA tracker

Status key: ⬜ Not tested · 🟢 PASS · 🔴 FAIL · 🟡 BLOCKED · ⚪ SKIPPED

| ID | Area | Test | Priority | Data-changing? | Status |
|----|------|------|----------|----------------|--------|
| SETUP-01 | Onboarding | Phase 0 Step 1 — PlatformAdmin login | P0 | No | ⬜ |
| SETUP-02 | Onboarding | Phase 0 Step 2 — Register organisation | P0 | Yes | ⬜ |
| SETUP-03 | Onboarding | Phase 0 Steps 4–5 — TenantAdmin password + org settings | P0 | Yes | ⬜ |
| SETUP-04 | Onboarding | Phase 0 Step 6 — Master data per checklist Part 3–5 | P0 | Yes | ⬜ |
| SETUP-05 | Onboarding | Phase 0 Step 6 — Generate INV-0001 / INV-0002 (checklist Part 6) | P0 | Yes | ⬜ |
| QA-001 | Environment | Docker / stack health (H-1–H-5) | P0 | No | ⬜ |
| QA-002 | Login | Valid TenantAdmin login | P0 | No | ⬜ |
| QA-003 | Login | Invalid password | P1 | No | ⬜ |
| QA-004 | Login | Empty email / empty password | P1 | No | ⬜ |
| QA-005 | Login | Invalid email format | P2 | No | ⬜ |
| QA-006 | Login | PlatformAdmin login → Organisations only | P1 | No | ⬜ |
| QA-007 | Login | Forced password change flow | P1 | No | ⬜ |
| QA-008 | Dashboard | KPIs load | P0 | No | ⬜ |
| QA-009 | Dashboard | Links (residents, billing, outstanding report) | P1 | No | ⬜ |
| QA-010 | Organisation | Organisation settings read-only verify | P1 | No | ⬜ |
| QA-011 | Companies | Company list shows Demo Care Ltd | P0 | No | ⬜ |
| QA-012 | Care homes | River View House list + dashboard link | P0 | No | ⬜ |
| QA-013 | Residents | List shows Alex & Jordan | P0 | No | ⬜ |
| QA-014 | Resident profile | Alex funding summary £575/week | P0 | No | ⬜ |
| QA-015 | Resident → Billing | Preselect company/home/period banner | P0 | No | ⬜ |
| QA-016 | Funding authorities | List loads | P1 | No | ⬜ |
| QA-017 | Funding contract | Alex contract visible on profile | P0 | No | ⬜ |
| QA-018 | Rates | Rate history on profile | P0 | No | ⬜ |
| QA-019 | Invoice categories | List loads | P1 | No | ⬜ |
| QA-020 | Nominal codes | List loads | P1 | No | ⬜ |
| QA-021 | Invoice templates | List loads | P1 | No | ⬜ |
| QA-022 | Billing | Preview Aug 2026 — duplicate blocked (INV exists) | P0 | No | ⬜ |
| QA-023 | Billing | Missing contract visibility | P0 | Yes (temp resident) | ⬜ |
| QA-024 | Billing | Missing rate (negative) | P1 | Yes (test data) | ⬜ |
| QA-025 | Billing | Already fully billed message | P0 | No | ⬜ |
| QA-026 | Billing | Overlapping contracts blocked | P1 | Yes (test data) | ⬜ |
| QA-027 | Billing | No company selected — preview disabled | P2 | No | ⬜ |
| QA-028 | Billing | Invalid date range / empty period | P1 | No | ⬜ |
| QA-029 | Invoices | List filters | P1 | No | ⬜ |
| QA-030 | Invoices | Open INV-0001 detail | P0 | No | ⬜ |
| QA-031 | Invoices | Open INV-0002 detail | P0 | No | ⬜ |
| QA-032 | PDF | Download INV-0002 PDF | P0 | No | ⬜ |
| QA-033 | Payment | Mark paid (throwaway or scripted INV-0001) | P0 | Yes | ⬜ |
| QA-034 | Payment | Mark unpaid — **DO NOT on demo INV-0001** | P1 | Yes | ⬜ |
| QA-035 | Email | INV-0002 Email simulated success | P0 | Yes (audit) | ⬜ |
| QA-036 | Credit note | From INV-0002 — context prefilled | P0 | No | ⬜ |
| QA-037 | Credit note | Preview only (no generate) | P0 | No | ⬜ |
| QA-038 | Credit note | Generate — **optional / data-changing** | P2 | Yes | ⬜ |
| QA-039 | Reports | All 9 report types Run | P0 | No | ⬜ |
| QA-040 | Reports | CSV / Excel / PDF export buttons | P1 | No | ⬜ |
| QA-041 | Reports | Empty result handling | P1 | No | ⬜ |
| QA-042 | Audit | Load + filter entity types | P1 | No | ⬜ |
| QA-043 | Users | TenantAdmin can open Users | P1 | No | ⬜ |
| QA-044 | Roles | ReadOnly cannot write | P0 | No | ⬜ |
| QA-045 | Roles | PlatformAdmin tenant routes 403/forbidden | P1 | No | ⬜ |
| QA-046 | Sage export | Validate + export UI | P2 | Yes (export) | ⬜ |
| QA-047 | Misc charges | CSV preview invalid rows | P2 | No | ⬜ |
| QA-048 | Navigation | Sidebar + breadcrumbs + back links | P1 | No | ⬜ |
| QA-049 | Validation | Invoice not found | P1 | No | ⬜ |
| QA-050 | E2E | QA-28 full business workflow | P0 | Mixed | ⬜ |

---

## QA-01 — Login & authentication

### QA-01a — Valid TenantAdmin login

**Purpose**  
Confirm a tenant user reaches the dashboard with full navigation.

**Preconditions**  
- Application running (Section 0).  
- **Phase 0** complete (organisation registered, TenantAdmin password set, scripted data if you need P0 billing later).  
- **Demo Care Group** tenant exists; TenantAdmin password known as `<DEMO_TENANT_ADMIN_PASSWORD>`.

**Steps**  
1. Open http://localhost:4200/login  
2. Enter **Email address:** `demo-admin@example.com`  
3. Enter **Password:** `<DEMO_TENANT_ADMIN_PASSWORD>`  
4. Click **Sign in**  
5. If redirected to **Set your password**, complete change (QA-01c) then return here.

**Expected result**  
- URL becomes http://localhost:4200/dashboard (or `/change-password` first).  
- Side nav brand subtitle shows **Demo Care Group**.  
- Sections visible: **Dashboard**, **Operations**, **Billing Setup**, **Billing**, **Reporting**, **Administration**.

**PASS** — Dashboard loads; tenant name correct; no **Access denied**.  
**FAIL** — **Invalid email or password.**; blank screen; stuck on login.

**Data impact** — No database change.  
**Cleanup** — No cleanup required.

---

### QA-01b — Invalid password

**Purpose**  
Verify failed login shows a clear message without signing in.

**Preconditions** — Signed out (user menu → **Sign out**).

**Steps**  
1. Open http://localhost:4200/login  
2. Email: `demo-admin@example.com`  
3. Password: `WrongPassword!999`  
4. Click **Sign in**

**Expected result**  
- Red error: **Invalid email or password.** (API message).  
- Button returns to **Sign in** (not stuck on **Signing in...**).

**PASS** — Message shown; still on login.  
**FAIL** — Dashboard opens; no message; spinner never stops.

**Data impact** — None.  
**Cleanup** — None.

---

### QA-01c — Empty fields / invalid email

**Purpose** — Client-side validation on login form.

**Steps**  
1. Leave email empty, click **Sign in** → **Email address is required.**  
2. Enter `not-an-email`, tab out → **Please enter a valid email address.**  
3. Enter valid email, leave password empty → **Password is required.**

**PASS** — Mat-errors appear; no API call required for empty fields.  
**FAIL** — Form submits with empty values.

**Data impact** — None.

---

### QA-01d — PlatformAdmin scope

**Purpose** — Platform user sees organisations, not tenant billing.

**Steps**  
1. Sign out.  
2. Login **Email:** `admin@localhost` **Password:** `<DEMO_PLATFORM_ADMIN_PASSWORD>`  
3. Observe side nav.

**Expected result** — Only **Organisations** (no Dashboard until impersonation — not supported).  
4. Manually open http://localhost:4200/dashboard

**Expected result** — Either dashboard forbidden redirect or **Access denied** at `/forbidden` depending on route guards; operational APIs must not be usable.  
5. Open http://localhost:4200/companies

**Expected result** — **Access denied** page: *You do not have permission to view this page.* with **Back to dashboard** (platform user may still hit forbidden on tenant routes).

**PASS** — Tenant operational URLs do not show tenant data.  
**FAIL** — Companies or billing load for platform-only token.

**Data impact** — None.  
**Cleanup** — Sign out before tenant tests.

---

### QA-01e — Password change after temporary password

**Purpose** — New tenant admin must change password before use.

**Preconditions** — TenantAdmin still on temporary password after **Phase 0 Step 2**, or repeating the flow on a newly created org.

**Steps**  
1. Login with temporary password.  
2. URL `/change-password` — form **Set your password**.  
3. Fill **Temporary password**, **New password** (≥12 chars, upper, lower, number, symbol), **Confirm new password**.  
4. Submit.

**Expected result** — Redirect to dashboard; full nav available.

**PASS** — Can access `/billing` afterward.  
**FAIL** — Stuck on change-password with unclear error.

**Data impact** — Updates user password hash.  
**Cleanup** — Store new password in password manager.

---

## QA-02 — Dashboard

### QA-02a — Dashboard load

**Purpose** — KPIs and sections render.

**Preconditions** — Logged in as TenantAdmin.

**Steps**  
1. Click **Dashboard** or open http://localhost:4200/dashboard  
2. Wait for **Loading dashboard...** to finish.

**Expected result**  
- **What should I do next?** with **Start billing**, **View invoices**, **Outstanding report**.  
- **Operations** KPIs: care homes, current residents, available beds (numbers ≥ 0).  
- **Finance** KPIs: outstanding invoices, generated invoices.  
- **Recent invoices** table or empty message.

**PASS** — Data shown without **Unable to load** error.  
**FAIL** — Persistent error banner.

**Data impact** — None.

---

### QA-02b — Dashboard links

**Steps**  
1. Click **Current residents** KPI card → `/clients`  
2. Back → click **Outstanding report** → `/reports?report=outstanding`  
3. Click **Start billing** → `/billing`

**PASS** — Each link opens correct URL and page title.  
**FAIL** — 404 or wrong screen.

**Data impact** — None.

---

## QA-03 — Organisation (platform + settings)

### QA-03a — Organisation settings (TenantAdmin)

**Purpose** — Confirm prefixes and currency without changing demo values.

**Steps**  
1. **Administration** → **Organisation Settings** → `/settings/organisation`  
2. Read fields (do not save changes unless testing edit).

**Expected result (scripted demo)** — Name **Demo Care Group**; invoice prefix **INV-**; credit note prefix **CN-**; payment terms **30**; currency **GBP**.

**PASS** — Values match your prepared demo.  
**FAIL** — Wrong tenant or empty form error.

**Data impact** — None if you do not click **Save**.  
**Cleanup** — Do not change prefixes on populated demo.

---

### QA-03b — Platform organisations list

**Purpose** — PlatformAdmin can list tenants.

**Preconditions** — Logged in as `admin@localhost`. Fresh start: you should have completed **Phase 0 Step 2** (your org **Active**).

**Steps**  
1. Open http://localhost:4200/platform/tenants  
2. Confirm **Demo Care Group** (or your org) **Active** and **Existing Organisation** **Inactive** (recommended after Phase 0 Step 3).

**PASS** — List loads.  
**FAIL** — Error or missing org after setup.

**Data impact** — None.

---

## QA-04 — Companies

### QA-04a — Company list

**Steps**  
1. **Operations** → **Companies** → `/companies`  
2. Find **Demo Care Ltd**.

**PASS** — Company appears; click row or edit opens form.  
**FAIL** — Empty list while demo should exist → **🟡 BLOCKED**.

**Data impact** — None.

---

### QA-04b — Company form validation (negative)

**Steps**  
1. **Companies** → add **Companies** new route `/companies/new` if button available, or open new from list.  
2. Leave **Company name** empty → **Save**.

**Expected** — Required field validation; record not created.

**PASS** — Cannot save empty name.  
**FAIL** — Silent save or API 500.

**Data impact** — None if save blocked.  
**Cleanup** — Cancel; do not leave test companies on demo tenant unless intentional.

---

## QA-05 — Care homes

### QA-05a — Care home list & dashboard

**Steps**  
1. **Operations** → **Care Homes**  
2. Open **River View House** dashboard link → `/care-homes/{id}/dashboard`  
3. Confirm occupancy/resident links if shown.

**PASS** — Dashboard loads; resident links open profiles.  
**FAIL** — 404 for known home.

**Data impact** — None.

---

## QA-06 — Residents (list)

### QA-06a — Resident list

**Steps**  
1. **Operations** → **Residents** → `/clients`  
2. Search or scroll for **Alex Morgan** and **Jordan Blake**.

**PASS** — Both visible with references **RVH-001** / **RVH-002**.  
**FAIL** — Missing → blocked for billing QA.

**Data impact** — None.

---

### QA-06b — Add resident validation (negative)

**Steps**  
1. Click **Add resident** → `/clients/new`  
2. Click **Save** without required fields.

**Expected** — Form validation errors (care home, names, dates as required by form).

**PASS** — Save blocked.  
**FAIL** — Creates incomplete resident.

**Data impact** — None if blocked.  
**Cleanup** — If accidental save, archive from list (QA-06c).

---

## QA-07 — Resident profile

### QA-07a — Alex Morgan profile & funding

**Steps**  
1. Open **Alex Morgan** → `/clients/{id}`  
2. Read header: care home **River View House**, status.  
3. Tab **Funding contracts** — active **Anytown Council** / **General Care**.  
4. Tab **Rate history** — **£575.00** weekly.

**PASS** — Contract + rate visible.  
**FAIL** — “No rate recorded for the active contract.”

**Data impact** — None.

---

### QA-07b — Start billing from profile (improvement)

**Purpose** — Resident context preserved in billing workspace.

**Steps**  
1. On Alex profile, click **Start billing** (header) or **Billing** tab → **Open billing workspace**  
2. URL should include query params (`careHomeId`, `clientId`, `periodStart`, `periodEnd`).

**Expected result**  
- Blue banner: **Opened from resident profile: Alex Morgan. Company and care home are preselected.**  
- **Company** = **Demo Care Ltd**  
- **Care home** = **River View House**  
- **Period start/end** = previous calendar month (e.g. **2026-08-01** – **2026-08-31** if today is September 2026)

**PASS** — Banner + preselections correct.  
**FAIL** — Blank scope; wrong home.

**Data impact** — None.

---

## QA-08 — Funding authorities

**Steps**  
1. **Billing Setup** → **Funding Authorities**  
2. Confirm **Anytown Council** (or demo authority) exists.

**PASS** — List loads; edit opens form.  
**FAIL** — API error.

**Data impact** — None.

---

## QA-09 — Funding contracts (on resident profile)

### QA-09a — View contract

**Steps** — On Alex profile → **Funding contracts** → contract row shows start date, authority, category, **Active**.

**PASS** — Contract present for billing period.

---

### QA-09b — Missing authority (negative) — **DO NOT EXECUTE ON DEMO RESIDENTS**

**Purpose** — Required fields on add contract form.

**Safe alternative** — On `/clients/new` draft only; cancel before save.  
Or inspect validation on empty **Add contract** dialog without saving on Alex/Jordan.

**Expected** — Save blocked with validation.

---

## QA-10 — Rates

**Steps** — Jordan profile → **Rate history** → **£600.00** per week from admission date.

**PASS** — Rate effective for Aug 2026 billing.

**Negative (DO NOT on demo)** — Remove rate on Alex and preview billing → **MISSING_RATE** / headline **Cannot bill** / message **No applicable funding rate for the selected period.**

---

## QA-11 — Invoice categories, nominal codes, templates

For each screen (**Invoice Categories**, **Nominal Codes**, **Invoice Templates**):

1. Open list from **Billing Setup**.  
2. Confirm active **General Care**, nominal **4000**, at least one **invoice template**.

**PASS** — Lists load; inactive items hidden or marked.  
**FAIL** — Empty setup with billing exceptions **MISSING_TEMPLATE** / **MISSING_NOMINAL** on preview.

**Data impact** — None.

---

## QA-12 — Billing workspace (preview & generate)

### QA-12a — Preview when invoices already exist (duplicate protection)

**Purpose** — August 2026 must not silently double-bill.

**Preconditions** — **INV-0001** and **INV-0002** exist.

**Steps**  
1. **Billing** → **Billing Workspace**  
2. **Company:** Demo Care Ltd  
3. **Care home:** River View House  
4. **Invoice category:** All categories (or General Care)  
5. **Period start:** `2026-08-01` **Period end:** `2026-08-31`  
6. Click **Preview billing**

**Expected result**  
- Warning banner: **Already fully billed for the selected scope and period. Adjust the period or scope to preview billable residents.** OR **Generation blocked — resolve exceptions before creating invoices.**  
- Step 3 exceptions include code **ALREADY_FULLY_BILLED** / headline **Already billed** / message **Already fully billed for this period.**  
- **Generate invoices** disabled OR `canGenerate` false.

**PASS** — No new invoice created from this preview; generate blocked.  
**FAIL** — **Ready to generate** with full totals for already-invoiced period.

**Data impact** — None if you do not click **Generate invoices**.  
**Cleanup** — None.

---

### QA-12b — Missing contract visibility

**Purpose** — Residents without contracts appear in exceptions (not invisible).

**Preconditions** — River View House has at least one billable resident **without** active funding contract for Aug 2026.

**Safe setup (creates data)**  
1. **Residents** → **Add resident** at **River View House**  
2. Name: **QA No Contract** / ref: **RVH-QA-NC** / admission **2026-04-01** / status **Current**  
3. **Do not** add funding contract.  
4. Save profile.

**Steps**  
1. **Billing Workspace** — company **Demo Care Ltd**, care home **River View House**, period **2026-08-01** – **2026-08-31**  
2. Click **Preview billing**  
3. In **Step 2 — Preview**, read: `N resident(s) eligible · M require attention`  
4. In **Step 3 — Review**, find exception for **QA No Contract**  
5. Headline: **Cannot bill**  
6. Message: **No active funding contract for the selected period.** (code **MISSING_CONTRACT**)

**PASS** — Exception visible with resident name; generation blocked for that resident.  
**FAIL** — Resident absent from exceptions with no explanation.

**Data impact** — Creates resident.  
**Cleanup** — **Residents** → find **QA No Contract** → **Archive** (if status allows) or leave documented test data.

---

### QA-12c — Preview success path (Jordan only) — **only if INV-0002 NOT yet created**

**DO NOT EXECUTE DURING DEMO DATA QA** if **INV-0002** already exists.

If rehearsing on empty period: preview should show **Jordan Blake** **£2,657.14**, banner **Ready to generate — review the lines below.**

---

### QA-12d — Scope validation

**Steps**  
1. Leave **Company** at **Select company**  
2. Observe **Preview billing** button disabled.  
3. Select company; set **Period end** before **Period start**; preview.

**Expected** — API error or validation **Preview failed.** / business error (no successful generate).

**PASS** — No invoice created.  
**FAIL** — Generates with invalid range.

**Data impact** — None.

---

## QA-13 — Billing preview UI states

**Steps**  
1. Open `/billing` without preview → empty state **Select scope and preview billing**  
2. Click preview → **Previewing billing...** loading state  
3. After result → workflow steps **Scope → Preview → Review → Generate** highlight correctly.

**PASS** — User always sees loading/empty/error/success states.  
**FAIL** — Frozen UI.

---

## QA-14 — Billing exceptions (reference)

UI maps codes via `billing-exception.ts`:

| Code | Headline | Label |
|------|----------|-------|
| MISSING_CONTRACT | Cannot bill | No active funding contract for the selected period. |
| MISSING_RATE | Cannot bill | No applicable funding rate for the selected period. |
| MISSING_NOMINAL | Setup required | A nominal code must be configured before generating this invoice. |
| MISSING_TEMPLATE | Setup required | No invoice template is configured for this billing stream. |
| ALREADY_FULLY_BILLED | Already billed | Already fully billed for this period. |
| OVERLAPPING_FUNDING_CONTRACTS | Contract overlap | (API message with contract IDs/dates) |
| PARTIAL_PERIOD_BILLING | Partial period | Only unbilled dates in this period will be invoiced. |

**Resident outside occupancy** — Backend **skips** resident silently (no exception row). Document as known behaviour if discharge/admission excludes Aug 2026.

**Archived residents** — Excluded from billing query (`!IsArchived`).

---

## QA-15 — Invoice generation

**DO NOT EXECUTE DURING DEMO DATA QA** when **INV-0001/0002** already satisfy tests.

If executing on clean period:  
1. Preview with **Ready to generate**  
2. Click **Generate invoices**  
3. Green banner: `Generated 1 invoice(s). Total £…`  
4. Toast: **Invoice generated successfully.**  
5. **Open invoices** link → list shows new **INV-000x**.

**PASS** — Invoice appears in list with correct total.  
**FAIL** — Error banner; duplicate numbers.

**Data impact** — Creates invoice + lines.  
**Cleanup** — Void only throwaway invoices, not demo **INV-0001/0002**.

---

## QA-16 — Invoice list

**Steps**  
1. **Billing** → **Invoices**  
2. Filter **Payment status** → **Not paid** → **Search**  
3. Confirm **INV-0002** listed.  
4. Filter **Paid** → **INV-0001** after payment demo.  
5. Filter invoice number **INV-0002** → single row.

**PASS** — Filters work; empty state **No invoices found** when no match.  
**FAIL** — Wrong rows.

**Data impact** — None.

---

## QA-17 — Invoice detail

**Steps**  
1. Open **INV-0002** from list  
2. Breadcrumb: **Billing › Invoices › INV-0002**  
3. Hero: total **£2,657.14**, payment **Not paid**, resident link to Jordan  
4. Sections: Resident, Funding, Billing period, Invoice lines

**PASS** — Amounts match demo script.  
**FAIL** — Wrong period or resident.

**Negative** — Open http://localhost:4200/invoices/999999  
**Expected** — **Unable to load invoice.** (or not-found message).

**Data impact** — None.

---

## QA-18 — PDF

**Steps**  
1. On **INV-0002**, click **Download PDF**  
2. Wait **Preparing PDF...**  
3. PDF opens in new tab  
4. Spot-check: **INV-0002**, dates, total **£2,657.14**, organisation **Demo Care Group**, fictional bank details.

**PASS** — Valid PDF; amount matches screen.  
**FAIL** — **Unable to download PDF.** or zero-byte file.

**Data impact** — None (may cache in document store).

---

## QA-19 — Payment status

### QA-19a — Mark paid (demo INV-0001)

**DO NOT** mark unpaid afterward on demo data.

**Steps**  
1. Open **INV-0001**  
2. Click **Mark paid**  
3. Confirm dialog **Mark this invoice as paid?** → **Update**  
4. Toast: **Payment status updated.**  
5. Badge **Paid**

**PASS** — Payment status **Paid**.  
**FAIL** — Still **Not paid**.

**Data impact** — Updates payment status + audit entry.

---

### QA-19b — Mark unpaid (negative / alternate)

**DO NOT EXECUTE DURING DEMO DATA QA** on **INV-0001**.

Use a throwaway invoice or skip (⚪ SKIPPED).

---

## QA-20 — Email (simulated)

**Preconditions** — `Email:Mode=Development` (default Docker).

**Steps**  
1. Open **INV-0002**  
2. Click **Email**  
3. Wait **Sending...**

**Expected result**  
- Green banner: **Invoice email workflow completed successfully.**  
- In Angular dev mode, hint: **Development note: delivery is simulated; the send is recorded and audited.**  
- Toast with same success message.  
- **Do not** claim email arrived in a real inbox.

**PASS** — Success banner; no SMTP error.  
**FAIL** — **Email could not be sent.**

**Data impact** — Audit + possible invoice status **Sent**.  
**Cleanup** — None.

---

## QA-21 — Credit notes

### QA-21a — Navigation from invoice (context)

**Steps**  
1. Open **INV-0002**  
2. Click **Credit note**  
3. URL `/credit-notes?invoiceId=…&invoiceNumber=INV-0002&clientId=…&periodStart=2026-08-01&periodEnd=2026-08-31` (dates may vary)  
4. Blue info banner shows **Original invoice: INV-0002**, **Resident:** Jordan…, **Period:** …

**PASS** — Context banner populated; resident field prefilled.  
**FAIL** — Empty workspace.

**Data impact** — None.

---

### QA-21b — Preview without generate

**Steps**  
1. Enter **Reason:** `QA preview only — do not generate`  
2. Click **Preview**  
3. Section **Preview credit** shows table: **Original invoice**, **Remaining**, **Credit** columns  
4. **Do not** click **Generate credit note**

**Expected** — Preview amount shown (often **full remaining** on line — read **Credit** column).  
Warning if `!canGenerate`: **Preview cannot generate a credit note for the current selection.**

**PASS** — Preview completes; no new row in **Existing credit notes**.  
**FAIL** — Auto-generates without clicking.

**Data impact** — None.

---

### QA-21c — Generate credit note

**DO NOT EXECUTE DURING DEMO DATA QA** unless you accept **CN-0001** in database.

If executed: **Generate credit note** → toast **Credit note generated successfully.** → table **CN-0001**.

**Cleanup** — None (permanent record).

---

## QA-22 — Reports

For **each** report, on `/reports`:

1. Select report type.  
2. Set **From** / **To** when testing date-bound reports (use **2026-08-01** – **2026-08-31** for invoice reports).  
3. Click **Run report** → **Generating report...**  
4. Verify columns match UI labels in `reports.ts` (e.g. **Resident**, **Invoice**, **Total amount**, **Payment status**).  
5. Currency via **£** formatting on amount columns.

| Report (dropdown) | Date filter | Demo expectation |
|-------------------|-------------|------------------|
| Client census | Optional | Alex & Jordan rows |
| Current rates | Optional | £575 / £600 rates |
| Invoices by client | Aug 2026 | INV-0001 & INV-0002 amounts |
| Invoices by care home | Aug 2026 | Rows per home |
| Income by category | Aug 2026 | Category totals |
| Occupancy / availability | Optional | Capacity columns |
| Funding rate history | Optional | History rows |
| Billing exceptions | Optional | Logged exceptions if any |
| Payment status / outstanding | Optional | **INV-0002** unpaid; **INV-0001** absent if paid |

**Empty result** — After run, **No results** empty state.

**Exports** — Click **CSV**, **Excel**, **PDF** (downloads `{report}.csv/xlsx/pdf`).  
**PASS** — File downloads without error.  
**FAIL** — API error on export.

**Data impact** — None.

---

## QA-23 — Audit

**Preconditions** — TenantAdmin ( **Administration** → **Audit** requires `canManageUsers`).

**Steps**  
1. Open `/audit`  
2. Wait **Loading audit log...**  
3. Table columns: **Actor**, **Action**, **Entity**, **When**  
4. **Entity type** filter → **Invoice** → **Apply filter**  
5. **Clear** → all types  
6. Actor shows user id or **System**

**PASS** — Events appear after prior QA actions (login, invoice, payment, email).  
**FAIL** — **Unable to load audit log.**

**Note** — No UI pagination controls; page size 50 server-side.

**Data impact** — None.

---

## QA-24 — Users & role permissions

### QA-24a — Users list (TenantAdmin)

**Steps** — **Administration** → **Users** → list loads.

**PASS** — Page opens.  
**FAIL** — Redirect `/forbidden`.

---

### QA-24b — ReadOnly user

**Preconditions** — ReadOnly user created in **Users** (optional).

**Steps**  
1. Login as ReadOnly  
2. Open **Residents**, **Invoices**, **Billing** — views work.  
3. On invoice detail — **Mark paid**, **Email**, **Void**, **Generate invoices** buttons **absent** (`canWrite()` false).  
4. Attempt write via billing — no **Generate invoices** button.

**PASS** — Read-only UI; API returns 403 on writes if forced.  
**FAIL** — Write buttons visible and working.

**Data impact** — None.

---

## QA-25 — Navigation & UX

**Checklist (TenantAdmin)**  

| # | Action | Expected URL / page |
|---|--------|---------------------|
| 1 | Sidebar **Companies** | `/companies` |
| 2 | **Care Homes** | `/care-homes` |
| 3 | **Residents** | `/clients` |
| 4 | **Billing Workspace** | `/billing` |
| 5 | **Invoices** | `/invoices` |
| 6 | **Credit Notes** | `/credit-notes` |
| 7 | **Reports** | `/reports` |
| 8 | **Sage Export** | `/sage-exports` |
| 9 | Breadcrumb **Home** | `/dashboard` |
| 10 | Invoice → resident link | `/clients/{id}` |
| 11 | Care home dashboard **Open** | `/care-homes/{id}/dashboard` |

**PASS** — No **Not found** for valid links; back buttons return to list.  
**FAIL** — Broken router links.

---

## QA-26 — Validation & error handling

| Scenario | Steps | Expected |
|----------|-------|----------|
| API offline | Stop `carehome-api` container briefly | Red **Unable to load…** banners |
| Login failure | QA-01b | **Invalid email or password.** |
| Billing preview failure | Invalid body | **Preview failed.** |
| Report failure | Stop API during Run | **Unable to load report.** |
| Credit preview failure | Missing reason/period | **Preview failed.** or warning banner |

**PASS** — User-visible error, not silent failure.  
**FAIL** — Infinite spinner.

**Recovery** — Restart API: `docker compose start api`; refresh browser.

---

## QA-27 — Multi-tenancy & authorization

**Steps**  
1. TenantAdmin header subtitle = **Demo Care Group**  
2. All lists show only that tenant’s data  
3. PlatformAdmin without tenant context cannot operate `/companies` (forbidden)  
4. Deactivated tenant login → **Invalid email or password.** (generic)

**PASS** — Isolation observed.  
**FAIL** — Cross-tenant data visible.

---

## QA-28 — End-to-end business workflow

Execute as one narrative (mark **QA-050**). Use demo data; **do not** regenerate August invoices if **INV-0001/0002** exist.

| Step | Action | Expected | PASS if |
|------|--------|----------|---------|
| 1 | Login TenantAdmin | Dashboard | Subtitle **Demo Care Group** |
| 2 | Dashboard → **Current residents** | Client list | Alex & Jordan visible |
| 3 | Open **Alex Morgan** | Profile | £575/week; River View |
| 4 | **Start billing** | Billing workspace | Banner + Aug 2026 period |
| 5 | **Preview billing** | Preview | Already billed / blocked OR exceptions only (not re-generate) |
| 6 | **Invoices** → **INV-0001** | Detail | **£2,546.43**, **Paid** (if payment done) |
| 7 | **Download PDF** INV-0001 | PDF opens | Amount matches |
| 8 | Open **Jordan** → **INV-0002** | Detail | **£2,657.14**, **Not paid** |
| 9 | **Download PDF** INV-0002 | PDF | OK |
| 10 | **Email** on INV-0002 | Simulated success | QA-20 messages |
| 11 | **Credit note** from INV-0002 → **Preview** only | Preview table | No generate |
| 12 | **Reports** → outstanding | INV-0002 listed | INV-0001 not listed if paid |
| 13 | **Reports** → invoices by client (Aug 2026) | Two rows | Correct totals |
| 14 | **Audit** | Recent actions | Invoice/payment/email events |
| 15 | **Dashboard** | Return home | KPIs consistent |

**Workflow status:** ⬜ PASS / ⬜ FAIL / ⬜ BLOCKED  

---

## Billing safety tests (summary matrix)

| Test | Data needed | Button | Preview | Generate allowed? | Exception? |
|------|-------------|--------|---------|-------------------|------------|
| Missing contract | Resident without contract | Preview billing | Eligible count may be 0 for them; attention +1 | No | **MISSING_CONTRACT** / **Cannot bill** |
| Missing rate | Contract without rate | Preview billing | Blocked | No | **MISSING_RATE** |
| Overlapping contracts | Two overlapping active contracts same stream | Preview billing | Blocked | No | **OVERLAPPING_FUNDING_CONTRACTS** |
| Already billed | Aug 2026 with INV-0001/2 | Preview billing | Lines empty or blocked | No | **ALREADY_FULLY_BILLED** |
| Partial period | Partial coverage | Preview billing | Info | Maybe | **PARTIAL_PERIOD_BILLING** (Info) |
| Inactive/archived | Archived resident | Preview billing | Not in client set | No | N/A (excluded) |
| Outside occupancy | Discharge before period | Preview billing | Skipped | No | No row (known) |
| Zero-value | Rate £0 | Preview billing | Line £0.00 | Per `canGenerate` | — |
| Duplicate generate | Same period twice | Generate invoices | Second pass blocked | No | **ALREADY_FULLY_BILLED** |
| Multi-resident home | Alex + Jordan + QA resident | Preview | Multiple lines when not billed | When `canGenerate` | Per resident |
| Different authorities | Council vs private | Preview | Separate lines | When valid | — |
| Different rates | 575 vs 600 | Preview | Distinct amounts | When valid | — |

---

## Sage export (QA-46)

**Steps**  
1. **Reporting** → **Sage Export**  
2. Set **Date from/to** covering invoice dates  
3. Click **Validate** → eligible/blocked counts  
4. **Export CSV** (if **canExport** and `canWrite`)  
5. **Previous exports** table → **Download**

**PASS** — Validate runs; export downloads CSV.  
**FAIL** — Errors listed in preview.

**Data impact** — Creates export batch on **Export CSV**.

---

## Miscellaneous charges (QA-47)

**Steps**  
1. **Billing** → **Miscellaneous Charges**  
2. Upload invalid CSV → preview **Invalid** rows  
3. **Confirm import** disabled when invalid count > 0

**DO NOT** confirm import on demo tenant unless testing import intentionally.

**PASS** — Preview shows row errors.  
**FAIL** — Import accepts invalid rows.

---

## Appendix A — Reports: exact steps (each type)

Open http://localhost:4200/reports for all. After each **Run report**, mark PASS if table or **No results** empty state appears (not a red API error).

### A-1 Client census

1. **Report:** Client census  
2. Leave **From** / **To** blank (optional)  
3. Click **Run report**  
4. Verify columns: **Resident**, **Reference**, **Care Home**, **Status**, **Care Type**, **Admission Date**  
5. PASS if Alex/Jordan appear when demo data loaded  

### A-2 Current rates

1. **Report:** Current rates  
2. **Run report**  
3. Columns include **Company**, **Care home**, **Resident**, **Funding Authority**, **Category**, **Frequency**, **Amount**, **Effective From/To**  
4. PASS if Alex shows **£575.00** weekly and Jordan **£600.00** (currency via £ display component)  

### A-3 Invoices by client

1. **Report:** Invoices by client  
2. **From:** `2026-08-01` **To:** `2026-08-31`  
3. **Run report**  
4. Rows: **Invoice**, **Invoice Date**, **Billing period start/end**, **Resident**, **Total amount**, **Payment status**  
5. PASS: **INV-0001** **£2,546.43**, **INV-0002** **£2,657.14**  

### A-4 Invoices by care home

1. **Report:** Invoices by care home  
2. **From/To:** August 2026  
3. **Run report**  
4. PASS: rows grouped by **Care home** with invoice numbers and **Payment status**  

### A-5 Income by category

1. **Report:** Income by category  
2. **From/To:** August 2026  
3. **Run report**  
4. Columns: **Category**, **Amount**  
5. PASS: non-zero **General Care** (or your category name) total  

### A-6 Occupancy / availability

1. **Report:** Occupancy / availability  
2. **Run report**  
3. Columns: **Care Home**, **Company**, **Capacity**, **Current Residents**, **Available Beds**  
4. PASS: **River View House** capacity numbers sensible  

### A-7 Funding rate history

1. **Report:** Funding rate history  
2. **Run report** (optional date range)  
3. Columns: **Resident**, **Funding Authority**, **Effective From/To**, **Frequency**, **Amount**, **Notes**  
4. PASS: history rows for demo residents  

### A-8 Billing exceptions

1. **Report:** Billing exceptions  
2. **Run report**  
3. Columns: **Logged At**, **Severity**, **Code**, **Message**, **Resident**  
4. PASS: loads; may be empty if no logged exceptions  

### A-9 Payment status / outstanding

1. **Report:** Payment status / outstanding  
2. **Run report**  
3. Columns: **Invoice**, **Invoice Date**, **Due Date**, **Care home**, **Amount**, **Payment status**, **Overdue**  
4. PASS: **INV-0002** **Not paid** present; **INV-0001** absent when marked **Paid**  
5. Note: header totals; does **not** net credit notes  

### A-10 Export buttons (any report with rows)

1. After a successful run, click **CSV** → file `{report}.csv` downloads  
2. Click **Excel** → `{report}.xlsx`  
3. Click **PDF** → `{report}.pdf`  
4. PASS: each download starts without error (empty reports may still export headers)  

---

## Appendix B — Additional negative tests (quick reference)

| ID | Area | Steps | Expected | Data-changing? |
|----|------|-------|----------|----------------|
| N-01 | Login | Wrong email `nobody@example.com` | **Invalid email or password.** | No |
| N-02 | Billing | Preview with no company | Button disabled | No |
| N-03 | Billing | Generate without preview | No action | No |
| N-04 | Invoice | Void INV-0002 | **DO NOT** on demo | Yes |
| N-05 | Credit note | Preview with empty period | **Preview failed.** or warning | No |
| N-06 | Credit note | Generate without preview | Button disabled | No |
| N-07 | Reports | Run with future dates only | **No results** empty state | No |
| N-08 | Audit | ReadOnly opens `/audit` | **Access denied** | No |
| N-09 | Users | ReadOnly opens `/users` | **Access denied** | No |
| N-10 | Forbidden | TenantAdmin opens `/platform/tenants` without platform role | **Access denied** or hidden nav | No |
| N-11 | Misc | Upload non-CSV file | Preview error / no valid rows | No |
| N-12 | Sage | Export with blocked validation | **Export CSV** disabled | No |

---

## Bug report template

```
BUG-XXX
Title:
Severity: P0 / P1 / P2 / P3

Screen:
Preconditions:

Steps to reproduce:
1.
2.
3.

Expected result:

Actual result:

Evidence:
Screenshot / message / URL

Data affected:

Reproducible? Yes / No

Notes:
(no secrets)
```

---

## QA summary (fill after execution)

| Metric | Count |
|--------|------:|
| **Total tests** | 55 (50 QA + 5 SETUP) |
| **Passed** | |
| **Failed** | |
| **Blocked** | |
| **Skipped** | |

### P0

| Passed | Failed | Blocked |
|--------|--------|---------|
| | | |

### P1

| Passed | Failed | Blocked |
|--------|--------|---------|
| | | |

### P2

| Passed | Failed | Blocked |
|--------|--------|---------|
| | | |

### Business workflow (QA-28 / QA-050)

**Status:** PASS / FAIL / BLOCKED

### Critical defects

1.  
2.  

### Recommended fixes

**P0**  
-  

**P1**  
-  

**P2**  
-  

---

*Document generated from repository inspection (routes, templates, `docker-compose.yml`, `appsettings*.json`, billing/ auth implementation). On a fresh start, complete **Phase 0** before marking billing/invoice P0 PASS.*

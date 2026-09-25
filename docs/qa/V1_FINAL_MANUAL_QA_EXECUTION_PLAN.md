# V1 Final Manual QA Execution Plan

**Purpose:** Operator-ready manual test script for V1 finance workflow (Commercial Revenue **disabled**).  
**Method:** Derived from `docs/qa/*`, `docs/architecture/*`, and current Angular/API routes and copy (read-only code review; **not executed** in this preparation).  
**Pass criteria question:** Can a finance administrator complete the monthly billing workflow without developer assistance?

**Result codes:** `PASS` | `FAIL` | `BLOCKED` | `NOT APPLICABLE`

---

## 1. Environment prerequisites

### Frontend

| Item | Value / instruction |
|------|---------------------|
| Working directory | `frontend/care-home-web` |
| Install | `npm install` (once per machine) |
| Start command | `npm start` (runs `ng serve --hmr` per `package.json`) |
| Alternate (Docker UI dev overlay) | `npm run start:docker` with `proxy.conf.docker.json` when using `docker-compose.dev.yml` |
| Expected URL | `http://localhost:4200` (README; Angular default) |
| API base URL | **Relative** `/api` — no `environment.ts` API URL; `HttpClient` calls `/api/...` |
| Dev proxy | `proxy.conf.json` → `http://localhost:5092` (configured in `angular.json` `serve.proxyConfig`) |
| Feature flag (V1) | `COMMERCIAL_REVENUE_ENABLED = false` in `frontend/care-home-web/src/app/core/commercial-revenue.feature.ts` (compile-time; verify before QA) |
| Required frontend env vars | **None** in repository for local dev beyond Node/npm |

### Backend

| Item | Value / instruction |
|------|---------------------|
| Working directory | `backend/CareHome.Api` |
| Start command | `dotnet watch run --launch-profile http` (README) |
| API URL (local profile) | `http://localhost:5092` (`Properties/launchSettings.json`, profile `http`) |
| HTTPS profile (optional) | `https://localhost:7042` + `http://localhost:5092` (profile `https`) |
| Environment | `ASPNETCORE_ENVIRONMENT=Development` for local dev defaults |
| Health endpoints | `GET /health/live`, `GET /health/ready` (README; `Program.cs`) |
| Commercial Revenue | `Features:CommercialRevenueEnabled: false` in `appsettings.json` (must remain **false** for V1 QA) |

**Configuration (do not commit secrets):**

| Setting | Source | Notes |
|---------|--------|--------|
| `ConnectionStrings:DefaultConnection` | `appsettings.json` | Default: SQL Server LocalDB `CareHomeDb` |
| `Jwt:Key` | `appsettings.Development.json` (dev key present); **empty** in base `appsettings.json` | Production: **VERIFY IN LOCAL ENVIRONMENT** |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryHours` | `appsettings.json` | |
| `Cors:AllowedOrigins` | `appsettings.Development.json` | `http://localhost:4200`, `http://127.0.0.1:4200` |
| `Https:Redirect` | `false` in Development | |
| `Database:ApplyMigrations` | `true` in `appsettings.Development.json` | Auto-migrate on API startup in Development |
| `Seed:AdminEmail`, `Seed:AdminPassword` | Development: `admin@localhost` / `DevAdmin!12345` | Override: `Seed__AdminEmail`, `Seed__AdminPassword` |
| `Seed:MasterDataForEmptyTenants` | `false` in base; `true` in `appsettings.Production.json` | Optional demo company/care home/authority for empty tenants |
| `Email:Mode` | `Development` in dev | Simulated send; SMTP not required locally |
| `DocumentStorage:RootPath` | Empty → `App_Data/documents` under API (README) | Docker: `DocumentStorage__RootPath=/app/App_Data/documents` |
| `App:PublicUrl` | `http://localhost:4200` in Development | Used in emails/links where configured |
| `QuestPdf:LicenseType` | `Community` in `appsettings.json` | PDF generation |

**Docker Compose (optional full stack):** `docker compose up -d --build` from repo root — Frontend `4200`, API `5092`→container `8080`, SQL `localhost,14333`, password `${MSSQL_SA_PASSWORD:-CareHomeDevSql2022@}` per `docker-compose.yml` and README.

### Database

| Item | Value / instruction |
|------|---------------------|
| Engine | SQL Server / LocalDB (README, EF Core) |
| Connection string name | `ConnectionStrings:DefaultConnection` |
| Migrations | EF migrations in `backend/CareHome.Data/Migrations/`; apply via `dotnet ef database update` from `backend/CareHome.Api` **or** `Database:ApplyMigrations=true` on startup (Development) **or** `dotnet CareHome.Api.dll --apply-migrations` (`Program.cs`) |
| Do not use | `EnsureCreated` (README) |

### Authentication (test users)

| Role / account | Purpose | Credentials / setup |
|----------------|---------|---------------------|
| **PlatformAdmin** (seed) | Provision tenants only | Development seed: `admin@localhost` / `DevAdmin!12345` (`appsettings.Development.json`, README). Lands on `/platform/tenants` when no `tenantPublicId`. |
| **TenantAdmin** or **Administrator** | **Required for finance workflow** | Not auto-seeded with demo tenant. Create via **Organisations** → provision new tenant (email + temp password emailed) **or** **Administration → Users** after a tenant exists. Demo docs reference `demo-admin@example.com` — password **VERIFY IN LOCAL ENVIRONMENT** (not in repo). |
| **ReadOnly** | Read-only / blocked write tests | Create via Users UI if needed. |
| **LocationManager** | Scoped care-home tests | **VERIFY IN LOCAL ENVIRONMENT** if scope testing required. |
| Password change | `MustChangePassword` on new users | Redirect to `/change-password` until completed (`auth.guard`, `app.html` shell). |

**Finance QA recommendation:** Sign in as **TenantAdmin** or **Administrator** on an **active tenant** with sidebar tenant name visible (not platform-only Organisations screen).

### External dependencies

| Service | Required for V1 QA? | Notes |
|---------|---------------------|--------|
| SMTP | **No** (local) | `Email:Mode=Development` simulates invoice send; Organisation settings may mention SMTP stays in server config |
| Sage / export storage | **Yes** (for Sage tests) | CSV under document store (`DocumentStorage:RootPath` or `App_Data/documents`) |
| PDF (QuestPDF) | **Yes** (TEST 18) | Community license in config |
| SQL Server | **Yes** | |
| Commercial Revenue APIs | **No** | Gated off; nav hidden |

---

## 2. Startup procedure

| Step | Action | Expected result | Failure indicator |
|------|--------|-----------------|-------------------|
| 1 | Start database (LocalDB, SQL Server, or `docker compose` `sql` service) | SQL accepts connections | API migration/health fails |
| 2 | Verify connectivity | `sqlcmd` or API startup without connection errors | `Database is not reachable` on `/health/ready` |
| 3 | Apply migrations if not auto-applied | Schema current; no pending migration warnings in API log | EF migration exceptions; 500 on `/api/*` |
| 4 | Start API (`dotnet watch run --launch-profile http` or Docker `api`) | Listening on `http://localhost:5092` (or mapped port) | Process exit; bind errors |
| 5 | Verify API health | `GET /health/live` → healthy; `GET /health/ready` → healthy (includes SQL) | Non-200; unhealthy body |
| 6 | Start Angular (`npm start` in `frontend/care-home-web`) | Dev server on `http://localhost:4200` | Compile errors; proxy errors |
| 7 | Open `http://localhost:4200` | Redirect to `/login` when logged out | Blank page; CORS errors (check proxy/Cors) |
| 8 | Login as **tenant** finance user | Dashboard at `/dashboard`; V1 nav visible | Stuck on `/platform/tenants` only; 401 loop; Revenue nav visible |

---

## 3. Test data prerequisites

Minimum data chain for billing (per architecture audit):

1. **Organisation** defaults — `/settings/organisation` (invoice/credit prefixes, payment terms).
2. **Company** — at least one active company.
3. **Care home** — linked to company; bed capacity set.
4. **Billing setup** — funding authority, nominal code, invoice category, invoice template (tenant provision seeds categories `GENERAL_CARE`, `MISC`, etc.; Development seed adds templates for empty DB).
5. **Resident** — care home, admission date, care type; **Sage ID** recommended before Sage export tests.
6. **Funding contract** — authority, category, nominal, start date.
7. **Rate** — effective from, frequency, amount covering billing period.

**Optional seeds (environment-dependent):**

- **DevelopmentMasterDataSeeder** (first empty DB in Development): tenant **Demo Care Group**, company **Demo Care Ltd**, care home **Sunrise House**, sample authorities, templates — **no tenant login user** in seeder.
- **EmptyTenantMasterDataSeeder** when `Seed:MasterDataForEmptyTenants=true`: **Demo Care Ltd** / **River View House** / **Anytown Council** for tenants with categories but no companies.

**Billing period:** Use Billing Workspace prior-calendar-month suggestion or explicit period covering funded days.

**Sage FileMissing (TEST 25):** Requires an export batch with missing CSV on disk — may need DBA/ops simulation; **VERIFY IN LOCAL ENVIRONMENT** if not reproducible organically.

---

## 4. Core workflow

### TEST 01 — Login

**Prerequisites:** Steps in §2 complete; tenant **TenantAdmin** or **Administrator** credentials.

**Steps:**

1. Open `http://localhost:4200/login`.
2. Enter email and password; submit **Sign in**.

**Expected:**

1. Redirect to `/dashboard` (not `/platform/tenants` for tenant users).
2. Sidebar: **Dashboard**, **Operations**, **Billing Setup**, **Billing**, **Reporting**, **Administration**.
3. No **Revenue**, **Payments**, **Receivables**, **Banking**, etc.

**Failure indicators:** Endless 401; blank shell; commercial revenue nav visible; platform Organisations as only screen for finance tests.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 02 — Dashboard

**Prerequisites:** TEST 01 passed.

**Steps:**

1. Confirm **Dashboard** (`/dashboard`) loads KPIs and lists.
2. Click **Outstanding invoices** KPI card (links to `/reports` with `report=outstanding` when Commercial Revenue off).
3. Click a **care home** occupancy link and a **recent invoice** link.

**Expected:**

1. KPI shows £ outstanding and invoice count when data exists.
2. Reports opens with report type **Payment status / outstanding**.
3. Care home link: `/care-homes/{uuid}/dashboard`.
4. Invoice link: `/invoices/{uuid}`.

**Failure indicators:** KPI opens `/invoices` only; numeric-only entity URLs that fail on refresh.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 03 — Companies

**Prerequisites:** TEST 01 passed.

**Steps:**

1. **Operations → Companies** (`/companies`).
2. Observe list: search, pagination, row actions.
3. Open an existing company detail.

**Expected:**

1. Loading then table or empty state.
2. Detail URL `/companies/{uuid}` when `publicId` present.
3. Breadcrumbs **Companies → {name}**.

**Failure indicators:** 404 on detail; unparsed UUID.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 04 — Create company

**Prerequisites:** Write access (`canWrite()`).

**Steps:**

1. **Companies → Add company** (`/companies/new`).
2. Enter **Company name** (required); leave optional fields empty.
3. **Save**.

**Expected:**

1. Success toast.
2. Redirect to list; new company visible.
3. Save disabled while invalid or saving.

**Failure indicators:** Double company on double-click; stale form on re-open Add.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 05 — Care home

**Prerequisites:** At least one company.

**Steps:**

1. **Operations → Care Homes** (`/care-homes`) or company detail care homes section.
2. Open a care home dashboard from list.

**Expected:**

1. List loads; links to `/care-homes/{uuid}/dashboard`.
2. Dashboard shows occupancy/context widgets.

**Failure indicators:** Link uses numeric id only and fails.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 06 — Create care home

**Prerequisites:** Active company.

**Steps:**

1. **Add care home** (`/care-homes/new` or company detail **Add care home** with `?company={companyUuid}`).
2. Complete required fields (company, code, name, bed capacity).
3. **Save**.

**Expected:**

1. Success toast.
2. Redirect to **care home dashboard** `/care-homes/{uuid}/dashboard`.

**Failure indicators:** Redirect to list only; edit from dashboard 404.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 07 — Resident list

**Prerequisites:** TEST 01 passed.

**Steps:**

1. **Operations → Residents** (`/clients`).
2. Use search/filter if available; pagination.

**Expected:**

1. Label **Residents** in nav; route `/clients`.
2. List loads with care home/company context columns as implemented.

**Failure indicators:** Empty error with no message; wrong tenant data.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 08 — Create resident

**Prerequisites:** Active care home.

**Steps:**

1. **Add resident** (`/clients/new`, optionally `?careHome={uuid}`).
2. Fill required fields (care home, names, care type, admission date).
3. **Save**.

**Expected:**

1. Success; profile at `/clients/{uuid}`.

**Failure indicators:** Profile 404 after save.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 09 — Funding authority

**Prerequisites:** Write access.

**Steps:**

1. **Billing Setup → Funding Authorities** (`/funding-authorities`).
2. **Add** (`/funding-authorities/new`); complete required fields; save.
3. From list, **Edit** row action.

**Expected:**

1. Edit URL `/funding-authorities/{uuid}/edit` when `publicId` present.
2. Usage/deactivate patterns per Phase A (if shown).

**Failure indicators:** 404 from list edit link.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 10 — Funding contract

**Prerequisites:** Resident; funding authority; invoice category; nominal code.

**Steps:**

1. Resident profile → **Funding** tab → add contract (`/clients/{id}/funding/new`).
2. Select authority, invoice category, nominal code, start date; save.

**Expected:**

1. Return to funding tab; contract listed.
2. Overlap on save shows finance-facing message (resident / funding arrangement), not integer contract IDs.

**Failure indicators:** Wrong resident; “Contract IDs:” in message.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 11 — Rate

**Prerequisites:** Funding contract on resident.

**Steps:**

1. Add rate (`/clients/{id}/funding/rates/new` with contract context).
2. Set effective from, frequency, amount; save.

**Expected:**

1. Rate visible on contract/profile.
2. Profile may warn if rate missing before billing.

**Failure indicators:** Rate not persisted; wrong contract.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 12 — Billing scope

**Prerequisites:** Funded resident or care home with residents.

**Steps:**

1. **Billing → Billing Workspace** (`/billing`) or resident **Start billing** (query `company`, `careHome`, `client` UUIDs).
2. **Step 1 — Scope:** select company, care home, optional invoice category, period start/end.
3. Confirm scope banners when handoff from profile.

**Expected:**

1. Context banner names resident and/or care home/company when preselected.
2. Prior calendar month suggested when applicable (`suggestedBillingPeriod` behaviour per docs).

**Failure indicators:** Empty scope with no labels; wrong company/home after handoff.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 13 — Billing preview

**Prerequisites:** Valid scope (TEST 12).

**Steps:**

1. Click **Preview**.
2. Review workflow steps, ready/requires-attention banners, line preview.

**Expected:**

1. Scope becomes read-only until **Change**; amounts shown per preview API.
2. Exceptions use `billing-exception` labels (e.g. **Cannot bill**, **Overlapping funding**).

**Failure indicators:** Preview with no scope; silent zero lines without explanation.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 14 — Billing exceptions

**Prerequisites:** Data setup for at least one exception type (optional negative tests in §5).

**Steps:**

1. On preview, expand/read exception rows (missing contract, missing rate, overlap, already billed, partial period).
2. Read user-visible **message** text.

**Expected:**

1. Headlines and messages match `billing-exception.ts` fallbacks or API message.
2. Overlap: resident/authority/category/dates; **no** “Contract IDs:” in UI.

**Failure indicators:** Raw exception codes only; contract integer IDs in message.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 15 — Generate invoice

**Prerequisites:** Preview state **can generate** (no blocking exceptions).

**Steps:**

1. **Generate invoices** (or equivalent primary action on workspace).
2. Open **Billing → Invoices** (`/invoices`).

**Expected:**

1. Success feedback; new invoice(s) in list.
2. Duplicate protection: second generate for same scope/period shows **Already billed** or blocks (do not assert amounts).

**Failure indicators:** Generate with blocking exceptions; wrong tenant.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 16 — Invoice list

**Prerequisites:** At least one invoice.

**Steps:**

1. Review columns: number, dates, amounts, invoice status, **Payment status**.
2. Use filters (payment status, care home, etc. as implemented).
3. Select row(s); note bulk **Mark as paid** / **Mark as unpaid** (covered in §6).

**Expected:**

1. Payment filter labels **Paid** / **Unpaid** (not “Not paid”).
2. Pagination if volume warrants.

**Failure indicators:** Raw `NotPaid` label on screen.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 17 — Invoice detail

**Prerequisites:** Invoice from TEST 15.

**Steps:**

1. Open `/invoices/{uuid}`.
2. Confirm **Invoice** status badge separate from **Payment status** panel (V1).
3. Follow links to resident, care home, company.

**Expected:**

1. Breadcrumbs **Billing → Invoices → {number}**.
2. Payment panel heading **Payment status** with badge and actions (write user).
3. Cross-links use UUID routes where supported.

**Failure indicators:** “Legacy” payment wording; payment merged into invoice status only.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 18 — Download PDF

**Prerequisites:** TEST 17.

**Steps:**

1. Click **Download PDF** on invoice detail.
2. Open downloaded file.

**Expected:**

1. Button shows **Preparing PDF...** while loading.
2. PDF downloads/opens with invoice number and line content.

**Failure indicators:** 404; empty file.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 19 — Mark paid

**Prerequisites:** Unpaid invoice; write access; Commercial Revenue off.

**Steps:**

1. On invoice detail **Payment status**, confirm badge **Unpaid**.
2. Click **Mark as paid**.
3. Confirm dialog (**Update payment status**, **Mark as paid**); confirm.
4. Wait until button not **Updating...**.

**Expected:**

1. Toast success; badge **Paid**; action becomes **Mark as unpaid**.
2. Invoice status (Generated/Sent/etc.) unchanged.
3. **No Paid on / PaidAt field.**

**Failure indicators:** No detail action; double submit; invoice status becomes Paid.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 20 — Reopen paid invoice

**Prerequisites:** TEST 19 passed.

**Steps:**

1. Navigate to **Invoices** list or dashboard.
2. Reopen same invoice detail.

**Expected:**

1. Payment status still **Paid**; **Mark as unpaid** available.

**Failure indicators:** Reverts to Unpaid; action missing.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 21 — Mark unpaid

**Prerequisites:** Paid invoice (TEST 19–20).

**Steps:**

1. Click **Mark as unpaid** on detail.
2. Confirm; wait for completion.

**Expected:**

1. Badge **Unpaid**; **Mark as paid** returns.
2. Invoice document status unchanged.

**Failure indicators:** Must use list bulk only; no confirmation.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 22 — Credit note

**Prerequisites:** Invoice with creditable lines.

**Steps:**

1. Invoice detail → credit note icon (**Create credit note**) → `/credit-notes?invoiceId=…&clientId=…` (numeric query params possible).
2. Confirm resident/period/reason prefilled where implemented.
3. **Preview** → **Generate**.

**Expected:**

1. Success toast; form reset; list section refreshes.
2. Credit note list links invoice via UUID when `invoicePublicId` present.
3. Operator does not type internal IDs on form.

**Failure indicators:** Preview without reason allowed; list links `/invoices/42` only.

**Limitation:** Preview API does not require `InvoiceId`; handoff uses period + `clientId` query — document if multiple invoices per resident/period.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 23 — Reports

**Prerequisites:** Invoice/resident data.

**Steps:**

1. **Reporting → Reports** (`/reports`).
2. Select **Payment status / outstanding**; set date range; run.
3. Export if button present.

**Expected:**

1. Column headers use friendly labels (e.g. **Resident**, **Payment Status**).
2. Currency/date formatting; loading and empty states.

**Failure indicators:** Raw DTO property names in grid.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 24 — Sage export

**Prerequisites:** Invoice(s) with Sage ID and nominal on lines; write access.

**Steps:**

1. **Reporting → Sage Export** (`/sage-exports`).
2. Set **Date from** / **Date to** → **Validate**.
3. **Export CSV** when validation allows.
4. Download from **Previous exports**.

**Expected:**

1. Subtitle explains Sage 50 CSV; no “provisional mapping”.
2. Success banner; row shows invoices exported and CSV **available**.
3. CSV file downloads.

**Failure indicators:** Export with blocking validation errors; success without download.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 25 — Sage FileMissing / Retry

**Prerequisites:** Batch with unavailable CSV (see §3) **or** `BLOCKED` if cannot simulate.

**Steps:**

1. Locate row in **Previous exports** with unavailable CSV.
2. Read **CSV file** column and helper text.
3. If write user: **Retry CSV** → wait → download.

**Expected:**

1. Text equivalent to **Export recorded, but the CSV file is unavailable**; status not raw **FileMissing** alone.
2. **Invoices exported** still indicated.
3. Retry regenerates file without re-marking invoices exported (per UX fix report).

**Failure indicators:** Message implies export never happened; no Retry.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 26 — Audit

**Prerequisites:** **TenantAdmin** or **Administrator** (`/audit` guarded).

**Steps:**

1. **Administration → Audit** (`/audit`).
2. Filter by entity type/action; open a safe link (invoice, resident, company, care home).

**Expected:**

1. Entries for create/update actions from prior tests.
2. Links use UUID where entity supports `publicId`.

**Failure indicators:** Broken links; numeric-only paths that 404.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 27 — Care-home portal theme

**Prerequisites:** Care home; write access.

**Steps:**

1. Open care home dashboard → portal settings (`/care-homes/{uuid}/settings`).
2. Change **Portal colour**; **Save**.
3. Return to platform pages (dashboard, organisation settings); inspect global toolbar theme control.

**Expected:**

1. Settings subtitle: appearance applies only in that care home context.
2. Global shell has light/dark only — **no** care-home accent picker on platform admin pages.

**Failure indicators:** Accent picker on dashboard; portal theme treated as tenant-wide org setting.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 28 — UUID navigation

**Prerequisites:** Entities with `publicId`.

**Steps:**

1. From each primary list (companies, care homes, residents, invoices, funding authorities), copy detail URL; open in new tab.
2. Walk Back through company → care home → resident → invoice.

**Expected:**

1. UUIDs in path for core entities.
2. Legacy numeric bookmarks still load where API dual-key supported (per A3 docs).

**Finding (do not fix):** Edit URLs `/nominal-codes/{int}`, `/invoice-categories/{int}`, `/invoice-templates/{int}`.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 29 — Pagination/search

**Prerequisites:** Enough rows on companies, residents, invoices, credit notes, audit (create or seed).

**Steps:**

1. Change page size and page on **Companies**, **Invoices**, **Credit notes**, **Audit**.
2. Search on **Companies** (server-side).
3. On **Care homes**, note pagination behaviour when filtering (may client-slice full fetch per audit).

**Expected:**

1. Server-backed paging where API supports it.
2. Search debounce returns filtered companies.

**Failure indicators:** Browser hang loading entire care home set at scale.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

### TEST 30 — Validation failures

**Prerequisites:** Write access.

**Steps:**

1. **Companies → Add** → submit empty form.
2. **Credit notes** → preview without **reason**.
3. Optional: invalid dates on billing scope.

**Expected:**

1. Inline/required errors; submit blocked.
2. Credit note validation before API call.

**Failure indicators:** Silent 400 with no UI message.

**Record:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

## 5. Billing safety tests

| ID | Scenario | Setup | Expected | Failure indicators | Record |
|----|----------|-------|----------|-------------------|--------|
| **BS-A** | Missing funding contract | Resident without contract for period | Preview/generate blocked; **No active funding contract…** or API message | Invoice generated with £0 invented | |
| **BS-B** | Missing rate | Contract without rate covering period | **No applicable funding rate…**; no £0 assumption | Line with zero rate without message | |
| **BS-C** | Overlapping contracts | Two arrangements same authority+category overlapping period | Clear overlap message; billing blocked; **no contract integer IDs** in UI | “Contract IDs:”; billing proceeds | |
| **BS-D** | Duplicate billing | Re-run generate for same billed period | **Already fully billed** / block | Second invoice duplicate without warning | |
| **BS-E** | Invalid billing period | End before start or empty dates | Validation on workspace; no generate | API 500; silent generate | |
| **BS-F** | Care-home scope | Billing with single care home selected | Only residents in that home in preview lines | Residents from other homes included | |
| **BS-G** | Partial coverage | Period partially billed or partial admission | **Partial period** headline/message understandable | Full period billed without explanation | |

---

## 6. Payment tests

| ID | Scenario | Steps summary | Expected | Notes |
|----|----------|---------------|----------|-------|
| **PAY-01** | Detail mark paid | TEST 19 | Confirm, loading, toast, Paid badge | Do not verify PaidAt |
| **PAY-02** | Detail mark unpaid | TEST 21 | Confirm, loading, toast, Unpaid badge | |
| **PAY-03** | Reopen persistence | TEST 20, 26 | Status survives navigation/refresh | |
| **PAY-04** | Invoice status independence | During PAY-01/02 | Invoice status badge unchanged | |
| **PAY-05** | List bulk mark paid | Select unpaid → **Mark as paid** | Same dialog labels as detail | `invoice-list.ts` |
| **PAY-06** | List bulk mark unpaid | Select paid → **Mark as unpaid** | Buttons disabled while updating | |
| **PAY-07** | Read-only user | Login as **ReadOnly** only | Payment actions hidden or 403 on API | `NOT APPLICABLE` if no account |
| **PAY-08** | Dialog copy | Open confirm | Title **Update payment status**; explains status-only, not bank receipt when marking paid | No “Legacy” |

**Record each:** `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE`

---

## 7. Credit note tests

| ID | Check | Expected |
|----|-------|----------|
| **CN-01** | Resident context | Selected resident name/reference shown when handed off from invoice |
| **CN-02** | Period context | `periodStart` / `periodEnd` from query prefilled |
| **CN-03** | Invoice context | `invoiceNumber` shown; `invoiceId` in URL only (not labeled) |
| **CN-04** | Preview | Lines and amounts; validation errors use invoice number + description (not line PK) |
| **CN-05** | Generate | Success toast; form reset |
| **CN-06** | List refresh | New note in paginated list |
| **CN-07** | Invoice link | Opens `/invoices/{uuid}` when public id present |
| **CN-08** | Limitation | Multiple invoices same resident/period — operator must verify correct invoice scope manually |

---

## 8. Sage tests

| ID | Check | Expected |
|----|-------|----------|
| **SAGE-01** | Validate | Blocked count and errors list invoice number + description |
| **SAGE-02** | Missing Sage ID | Error **Sage ID is missing.** |
| **SAGE-03** | Successful export | Batch row: export recorded + CSV available + download |
| **SAGE-04** | FileMissing UX | Unavailable label + explanation; Retry path (TEST 25) |
| **SAGE-05** | No mapping change | CSV columns match existing Sage 50 export (spot-check only; do not alter) |

---

## 9. Navigation tests

For each entity, verify **List → View → Edit → Back** (where edit exists):

| Entity | List route | Detail | Edit | UUID in detail URL? | Numeric route finding |
|--------|------------|--------|------|---------------------|------------------------|
| Company | `/companies` | `/companies/:id` | `/companies/:id/edit` | Yes (when seeded) | — |
| Care home | `/care-homes` | `/care-homes/:id/dashboard` | `/care-homes/:id/edit` | Yes | — |
| Resident | `/clients` | `/clients/:id` | `/clients/:id/edit` | Yes | Path segment `/clients` (label Residents) |
| Funding authority | `/funding-authorities` | — | `/funding-authorities/:id/edit` | Yes | — |
| Nominal code | `/nominal-codes` | — | `/nominal-codes/:id/edit` | **No** | **Integer `:id`** |
| Invoice category | `/invoice-categories` | — | `/invoice-categories/:id/edit` | **No** | **Integer `:id`** |
| Invoice template | `/invoice-templates` | — | `/invoice-templates/:id/edit` | **No** | **Integer `:id`** |
| Invoice | `/invoices` | `/invoices/:id` | — | Yes | PDF/payment API uses numeric id internally |
| Credit note | `/credit-notes` | workspace | — | Invoice link UUID | Query `invoiceId`, `clientId` |
| User | `/users` | — | `/users/new`, edit as implemented | **VERIFY IN LOCAL ENVIRONMENT** | |
| Organisation | — | `/settings/organisation` | same | N/A | Not `/platform/tenants` |

---

## 10. Form tests

Apply checklist to **create/edit** forms (empty submit, required missing, optional omitted, invalid value, valid save, duplicate where applicable, redirect, list/detail verify, reopen form):

| Form | Route | Required highlights (see `docs/ux/FORM_FIELD_REQUIREMENTS_FINAL.md`) |
|------|-------|---------------------------------------------------------------------|
| Company | `/companies/new`, `.../edit` | Name required |
| Care home | `/care-homes/new`, `.../edit` | Company, code, name, bed capacity |
| Resident | `/clients/new`, `.../edit` | Care home, names, care type, admission; Sage ID may be required on update |
| Funding authority | `/funding-authorities/new`, `.../edit` | Per form markers |
| Funding contract | `/clients/:id/funding/new` | Authority, category, nominal, dates |
| Rate | `.../funding/rates/new` | Effective from, frequency, amount |
| Nominal / category / template | respective `/new`, `.../edit` | Code/name per form |
| Organisation settings | `/settings/organisation` | Organisation name |
| Billing workspace scope | `/billing` | Company, care home, period |
| Credit note workspace | `/credit-notes` | Reason for preview/generate |
| User | `/users/new` | Per form |
| Sage export | `/sage-exports` | Date from/to |

Record **only observable defects** (not theoretical).

---

## 11. Table/pagination tests

| Screen | Loading | Empty | Pagination | Search/filter | Row nav | Formatting notes |
|--------|---------|-------|------------|---------------|---------|------------------|
| Companies | ✓ | ✓ | Server | Server search | Detail link | Status badges |
| Care homes | ✓ | ✓ | Mixed API/client | Filter bar | Dashboard link | |
| Residents | ✓ | ✓ | Per implementation | Per implementation | Profile UUID | |
| Funding authorities | ✓ | ✓ | Per implementation | — | Edit | Usage column |
| Nominal codes | ✓ | ✓ | Per implementation | — | Edit (int URL) | |
| Invoice categories | ✓ | ✓ | Per implementation | — | Edit (int URL) | System default badges |
| Invoice templates | ✓ | ✓ | Per implementation | — | Edit (int URL) | |
| Invoices | ✓ | ✓ | Per implementation | Payment status, etc. | Detail UUID | £ amounts |
| Credit notes | ✓ | ✓ | ✓ | — | Invoice link | |
| Reports | ✓ | ✓ | N/A | Report type + dates | — | Friendly columns |
| Sage exports | ✓ | Empty history | Paged batches | Date range | Download/Retry | CSV availability column |
| Audit | ✓ | ✓ | ✓ | Entity/action filters | Entity links | Date/time |

---

## 12. Accessibility tests

Observable checks only (no redesign):

| ID | Check | Where |
|----|-------|-------|
| **A11Y-01** | Tab through login form | `/login` |
| **A11Y-02** | Focus visible on sidebar links | Main shell |
| **A11Y-03** | Invoice list row selection | `aria-label` on checkboxes (per A3) |
| **A11Y-04** | Icon actions on invoice detail | `aria-label` on PDF, credit note, more menu |
| **A11Y-05** | Billing status summary | `aria-live="polite"` on workspace if present |
| **A11Y-06** | Portal theme buttons | `aria-current` on selected swatch |
| **A11Y-07** | Form errors announced | Required fields + `mat-error` |
| **A11Y-08** | Disabled buttons during save | Payment, Sage export, forms |
| **A11Y-09** | Workflow steps | Visual steps may lack full SR text — note if observed |

---

## 13. Theme tests

| ID | Steps | Expected |
|----|-------|----------|
| **THEME-01** | Platform dashboard | No portal accent control in global header |
| **THEME-02** | `/care-homes/{uuid}/settings` | Accent grid + Save |
| **THEME-03** | Change accent → care home dashboard | Accent applies in care-home context per implementation |

---

## 14. Technical wording tests

Scan V1 finance screens; classify occurrences:

| Term / surface | Expected after UX fix | Classification guide |
|----------------|----------------------|------------------------|
| Payment status / Paid / Unpaid / Mark as paid / Mark as unpaid | Present | **CLEAR** |
| Sage ID | Resident, validation errors | **CLEAR** (business) |
| UUID / PublicId | Not as on-screen labels | **ACCEPTABLE** in URL only |
| `invoiceId`, `clientId` query on credit-notes | May appear in address bar | **ACCEPTABLE** (B) or **CONFUSING** if operator bookmarks |
| Nominal/category/template `/…/12/edit` | Integer in URL | **TECHNICAL LEAKAGE** (known, deferred) |
| FileMissing | Should not appear alone | **FAIL** if raw enum shown |
| Legacy / provisional / Contract IDs / DTO / API | Should be absent on finance screens | **TECHNICAL LEAKAGE** if found |
| ClientReference (misc CSV) | Column name in import errors | **ACCEPTABLE** (file contract) |
| SMTP in server configuration | Organisation settings | **ACCEPTABLE** (admin) |
| Development note: delivery simulated | Invoice send in dev | **ACCEPTABLE** |

Use `docs/qa/V1_TECHNICAL_WORDING_AUDIT.md` as reference baseline.

---

## 15. Financial safety tests

Observable UX risks (not calculation proofs):

| ID | Risk | Mitigation to verify in UI |
|----|------|----------------------------|
| **FS-01** | Wrong resident billed | Scope banners; single-resident handoff from profile |
| **FS-02** | Wrong care home | Care home filter on billing and lists |
| **FS-03** | Wrong period | Period readonly after preview; partial period messaging |
| **FS-04** | Duplicate invoice | BS-D / ALREADY_FULLY_BILLED |
| **FS-05** | Payment mistaken for cash | Mark paid dialog warns status-only |
| **FS-06** | Credit note wrong invoice | CN-08 limitation; operator verifies number |
| **FS-07** | Sage export state confusion | Separate “invoices exported” vs CSV availability |
| **FS-08** | Export failure recovery | Retry CSV without duplicate export marks |
| **FS-09** | Mark paid wrong invoice | Confirm dialog includes invoice number |
| **FS-10** | Void after Sage | More menu void — policy not enforced in UI; note operator confusion risk |

---

## 16. Final manual QA sign-off

| Tester | Environment (local/docker/staging) | Date | Core workflow (TEST 01–30) | Billing safety | Payments | Sage | Blockers noted |
|--------|-----------------------------------|------|----------------------------|----------------|----------|------|----------------|
| | | | | | | | |

**Per-test results:** Use `PASS` / `FAIL` / `BLOCKED` / `NOT APPLICABLE` in a spreadsheet or copy of this document.

**Explicit exclusions (do not test in V1 QA):**

- Commercial Revenue modules (Payments, Banking, Receivables, etc.)
- Revenue Assurance
- PaidAt / payment history fields
- Billing calculation correctness (beyond exception/block behaviour)
- Database schema changes

---

## Appendix A — Execution summary (preparation audit)

| Metric | Count / note |
|--------|----------------|
| **Core workflow tests** | **30** (TEST 01–30) |
| **Additional structured tests** | Billing safety **7**; Payment **8**; Credit note **8**; Sage **5**; Navigation matrix **11** entities; Form checklist **12** forms; Table matrix **12** screens; A11y **9**; Theme **3**; Financial safety **10** |
| **Approximate total discrete checks** | **~105** (including matrix rows once each) |
| **Requires live browser** | **All** core workflow tests; all payment/Sage/theme/navigation UUID checks; form validation; a11y spot checks |
| **Requires seeded data** | TEST 02–30 (except empty-state portions of TEST 03/07); BS-A–G; SAGE-01–03; CN-01–07 |
| **Requires external services** | SQL (required); PDF/QuestPDF (local); SMTP **not** required in Development; Sage FileMissing (TEST 25) may require **ops/DB simulation** |
| **Known limitations** | No PaidAt; payment is status flag only; credit note preview without InvoiceId contract; numeric edit URLs for nominal/category/template; credit note query numeric ids; Sage commits export marks before CSV write; misc CSV header `ClientReference`; care home list may client-page full fetch; platform admin seed ≠ tenant finance user |
| **Documented risks (post UX-fix)** | Misreading payment flag as bank reconciliation; Sage FileMissing misunderstanding (mitigated in UI copy); void-after-export policy unclear; credit note scope ambiguity with multiple invoices; optional demo seed varies by `MasterDataForEmptyTenants` |
| **Cannot verify without running app** | All PASS/FAIL outcomes; actual CSV column contents; PDF layout; email send in non-Development; production JWT/CORS; responsive layout on physical devices; performance at production data volume |

---

**MANUAL QA PLAN READY — NO CODE CHANGES**

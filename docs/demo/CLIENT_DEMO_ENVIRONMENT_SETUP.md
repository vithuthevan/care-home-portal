# Client Demo Environment Setup

**Date:** 15 September 2026  
**Scope:** Local Development demo environment only — no Azure deployment, no production configuration changes, no source code changes.  
**Context:** The client demo happens **before** the first Azure deployment. Use Docker Compose + `ASPNETCORE_ENVIRONMENT=Development` exclusively.  
**References:** `CLIENT_DEMO_READINESS.md`, `PRODUCTION_READINESS_ACTION_PLAN.md`, `AZURE_FIRST_DEPLOYMENT_READINESS.md`, `docs/EXISTING_CUSTOMER_MIGRATION.md`, `docs/UAT_CHECKLIST.md`

---

## 1. Purpose

This document is the operator runbook for preparing a **clean, repeatable** client demonstration environment. It explains how to:

- Stand up an isolated Development stack (Docker Compose preferred)
- Apply the EF Core migration chain to a **fresh** database
- Work around the migration artifact **Existing Organisation** (tenant Id=1) without modifying migrations
- Provision a client-branded demo organisation and **TenantAdmin** login
- Load a minimal but realistic fictional dataset covering all demo workflows
- Use **simulated email** (no real mail delivery)
- Reset to a known state before each rehearsal or demo day

### Demo capability coverage

| # | Capability | Where covered |
|---|------------|---------------|
| 1 | Login | §5, §9 |
| 2 | Tenant/company administration | §4, §6.1 |
| 3 | Resident/client management | §6.2 |
| 4 | Funding/contract management | §6.3 |
| 5 | Invoice generation | §6.4 |
| 6 | Invoice PDF generation | §6.4, §9 |
| 7 | Payment/status workflow | §6.4, §6.6 |
| 8 | Credit notes | §6.5 |
| 9 | Reports | §6.7 |
| 10 | User/role management | §5 |
| 11 | Audit functionality | §6.8, §9 |
| 12 | Simulated email behaviour | §7 |

### Priority labels used throughout

| Label | Meaning |
|-------|---------|
| **MUST DO BEFORE DEMO** | Demo will fail or expose internal artifacts without this |
| **SHOULD DO BEFORE DEMO** | Strongly recommended; avoids stalls and embarrassment |
| **OPTIONAL** | Enhances the narrative; skip if time is short |
| **DO NOT DO** | Out of scope, unsafe, or contradicts demo constraints |

---

## 2. Environment Architecture

### Recommended stack: Docker Compose

From the repository root, `docker-compose.yml` runs three services:

| Service | Container | Host URL / port | Role |
|---------|-----------|-----------------|------|
| **sql** | `carehome-sql` | `localhost,14333` | SQL Server 2022 Developer; database `CareHomeDb` |
| **api** | `carehome-api` | http://localhost:5092 | ASP.NET Core API, `ASPNETCORE_ENVIRONMENT=Development` |
| **web** | `carehome-web` | http://localhost:4200 | Angular dev server; proxies `/api` to the API container |

**Volumes (demo data lives here):**

| Volume | Contents |
|--------|----------|
| `carehome-sql-data` | SQL Server data files — **the demo database** |
| `carehome-documents` | Invoice/credit-note PDFs, Sage CSV exports |

**API startup behaviour** (`Program.cs`):

| Step | Condition | Action |
|------|-----------|--------|
| Migrations | `Database__ApplyMigrations=true` (set in Compose) | `MigrateAsync()` — full 12-migration chain |
| `IdentitySeeder` | Always | Creates roles; creates PlatformAdmin from `Seed:AdminEmail` / `Seed:AdminPassword` |
| `DevelopmentMasterDataSeeder` | Development **and** zero tenants | **Skipped on fresh migrate** — tenant Id=1 already exists |

**Development configuration sources:**

| Setting | Value (default) | Source |
|---------|-----------------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `docker-compose.yml` |
| `Database__ApplyMigrations` | `true` | `docker-compose.yml` |
| `Email__Mode` | `Development` | `appsettings.Development.json` (simulated send = success) |
| `Seed__AdminEmail` / `Seed__AdminPassword` | `admin@localhost` / `DevAdmin!12345` | `appsettings.Development.json` |
| `ConnectionStrings__DefaultConnection` | `Server=sql,1433;Database=CareHomeDb;…` | `docker-compose.yml` |
| `DocumentStorage__RootPath` | `/app/App_Data/documents` | `docker-compose.yml` + volume |
| `App__PublicUrl` | `http://localhost:4200` | `docker-compose.yml` |
| SQL SA password | `CareHomeDevSql2022@` | `.env.example` / `MSSQL_SA_PASSWORD` |

**OPTIONAL alternative:** LocalDB + `dotnet run` (API) + `npm start` (frontend). Use a **dedicated** database name (e.g. `CareHomeDemoDb`) so shared developer `CareHomeDb` is not polluted. Run `dotnet ef database update` manually; `Database__ApplyMigrations` is not set by default outside Compose.

**OPTIONAL:** Copy `.env.example` to `.env` at the repository root to override `MSSQL_SA_PASSWORD` for the SQL container. Default dev password is acceptable for local demo only.

**DO NOT DO:** Run with `ASPNETCORE_ENVIRONMENT=Production`, deploy to Azure, use `Email__AllowSimulationInProduction`, or point at a database containing real customer data.

---

## 3. Clean Database Setup

### What a fresh database contains after migrations

Latest migration: `20260911053954_UniqueMiscChargeDedupeIndex` (12-step chain from `InitialCreate`).

**Automatically created (system data — keep):**

- ASP.NET Identity roles: `PlatformAdmin`, `TenantAdmin`, `Administrator`, `LocationManager`, `ReadOnly` (+ legacy `SuperAdmin`)
- PlatformAdmin user (if seed credentials are configured)
- Tenant Id=1 **Existing Organisation** — migration compatibility artifact (`20260829072440_AddMultiTenancy`)
- Default invoice categories on tenant 1 (`GENERAL_CARE`, `OUTREACH`, `RENT`, `MISC`)
- Document sequences `INV-` / `CN-` on tenant 1
- `TenantSettings` defaults (GBP, 30-day terms) on tenant 1

**NOT created on a standard fresh database:**

- Demo Care Group / Sunrise House (`DevelopmentMasterDataSeeder` guard: `if (await dbContext.Tenants.AnyAsync()) return;`)
- Companies, care homes, clients, contracts, invoices, or demo tenant users

Historical company names **Sovereign Care Homes** / **Care Pro** are inserted by `InitialCreate` then deleted by `RemoveUnusedHistoricalCustomerSeedCompanies` when unused — they should **not** appear on a greenfield database.

### MUST DO BEFORE DEMO — Fresh database procedure (Docker)

```powershell
# From repository root. -v removes ONLY the Compose volumes (demo SQL + documents).
docker compose down -v
docker compose up -d --build
```

Wait until all services are healthy:

```powershell
docker compose ps
# api and sql should show "healthy"
```

Verify:

| Check | Command / URL | Expected |
|-------|---------------|----------|
| API live | `GET http://localhost:5092/health/live` | `{"status":"Healthy",…}` |
| API ready (SQL) | `GET http://localhost:5092/health/ready` | `{"status":"Healthy",…}` |
| Frontend | http://localhost:4200 | Login page |

**SHOULD DO BEFORE DEMO:** Clear browser site data for `localhost:4200` (or use a fresh browser profile). JWT is stored in `localStorage` key `carehome.auth`.

**DO NOT DO:** `docker compose down` without `-v` when you intend a clean reset — old SQL data persists. Do not drop or modify any database outside this local demo stack without explicit confirmation.

### OPTIONAL — Fresh database (LocalDB)

```powershell
cd backend\CareHome.Api
# Use a dedicated database name; drop only that database if resetting.
dotnet ef database update
```

Ensure `ASPNETCORE_ENVIRONMENT=Development` and `Seed` values are loaded from `appsettings.Development.json`.

---

## 4. Demo Organisation Setup

### Existing Organisation (tenant Id=1) — do not modify the migration

**Where it comes from:** Migration `20260829072440_AddMultiTenancy` inserts tenant Id=1, name `Existing Organisation`, fixed `PublicId` `9E4F2C11-7A8B-4D3E-9C10-1B2A3C4D5E6F`. This is required for upgrade compatibility; it is **not** demo branding.

**Required for demo functionality?** No. New organisations are provisioned independently with their own categories, sequences, and settings.

**Safest handling (recommended audit path):**

```
Fresh database
    → Migrations run (Existing Organisation created — expected)
    → Log in as PlatformAdmin
    → Create client-branded organisation (e.g. Demo Care Group)
    → Deactivate Existing Organisation
    → Log out; operate only inside the new organisation
```

| Action | Route | Notes |
|--------|-------|-------|
| View organisations | `/platform/tenants` | Existing Organisation may appear in the list |
| Deactivate artifact | Edit Existing Organisation → uncheck **Active** → Save | Prevents accidental use; users cannot log into an inactive tenant |
| Create demo org | `/platform/tenants/new` | See §5 for admin email and fields |

**DO NOT DO:** Delete tenant Id=1 via SQL unless you fully understand FK constraints — deactivation via the UI is safer. Do not run billing, create clients, or store demo data inside tenant 1.

### MUST DO BEFORE DEMO — Create the demo organisation

1. Log in at http://localhost:4200/login as **PlatformAdmin** (see §5).
2. Navigate to **Organisations** → **Add organisation**.
3. Enter organisation details:

| Field | Demo value |
|-------|------------|
| Name | `Demo Care Group` |
| Trading name | `Demo Care Group` |
| Address (optional) | `1 Demo Lane, Anytown, AN1 2BC` |
| Phone (optional) | `01234 567890` |
| Email (optional) | `info@demo-care-group.example` |
| Active | ✓ checked |
| Admin email | `demo-admin@example.com` |
| Admin display name | `Demo Administrator` |

4. Save. In Development, when email is simulated, the **Organisation created** screen shows: *"Email is simulated in Development. Temporary password: …"* (see `/platform/tenants/new`). The API returns `temporaryPassword` only when `ASPNETCORE_ENVIRONMENT=Development`; it is **not** written to application logs.
5. **Copy the temporary password** to a password manager — do not display it to the client.
6. Edit **Existing Organisation** → set **Inactive** → Save.
7. Log out.

**SHOULD DO BEFORE DEMO:** If showing organisation provisioning live, prepare a one-line explanation: *"Existing Organisation is a migration placeholder from the single-tenant upgrade; we use a dedicated demo organisation."*

---

## 5. Demo Users and Roles

### Role model

| Role | Tenant scope | Use in demo |
|------|--------------|-------------|
| **PlatformAdmin** | None (`TenantId = null`) | Organisation provisioning only (~2–3 min). **Cannot** access `/companies`, `/clients`, `/billing`, `/invoices` — returns 403 (`[RequireTenant]`). |
| **TenantAdmin** | Assigned organisation | **Primary demo login** — full tenant administration and all workflows. |
| **Administrator** | Assigned organisation | OPTIONAL — demo persona **Accountant** (finance operator with write access; not a separate role name). |
| **LocationManager** | Assigned care home(s) | OPTIONAL — permissions segment; scoped to one home. |
| **ReadOnly** | Assigned organisation | OPTIONAL — permissions segment; view only, writes blocked. |

### MUST DO BEFORE DEMO — PlatformAdmin (provisioning)

| Item | Value |
|------|-------|
| Email | `admin@localhost` |
| Password | `DevAdmin!12345` |
| Source | `appsettings.Development.json` → `Seed:AdminEmail` / `Seed:AdminPassword` |
| Organisation | None (platform scope) |

**DO NOT DO:** Present these credentials on screen to the client. Use only for setup before the main demo segment.

### MUST DO BEFORE DEMO — TenantAdmin (main workflow)

| Item | Value |
|------|-------|
| Email | `demo-admin@example.com` |
| Password | Temporary password from organisation create screen → **change once before demo** |
| Role | `TenantAdmin` (assigned automatically on org create) |
| Organisation | **Demo Care Group** (not Existing Organisation) |
| First-login behaviour | `MustChangePassword=true` — redirected to `/change-password` until completed |

**SHOULD DO BEFORE DEMO:** Complete the forced password change during rehearsal. Store the final password securely. Do not use first-login password change during the client session.

### SHOULD DO BEFORE DEMO — Additional demo users

Create from **Users** (`/users`) while logged in as TenantAdmin:

| Display name | Email | Role | Password |
|--------------|-------|------|----------|
| Demo Accountant | `demo-accountant@example.com` | Administrator | Set in UI at create time; complete password change if required |
| Demo Viewer | `demo-viewer@example.com` | ReadOnly | Set in UI at create time |

Tenant user creation sets the password in-app and does **not** send email. All users created here also have `MustChangePassword=true` — complete their first login password change **before** the permissions segment if you plan to log in as them.

**OPTIONAL:** LocationManager user scoped to `RIVER01` only — for the permissions mini-demo.

---

## 6. Demo Data

All data below is **fictional**. Create everything inside **Demo Care Group** (not tenant 1). Use the UI routes listed; see `docs/UAT_CHECKLIST.md` for field-level validation rules.

### 6.1 Master data

| Entity | Values |
|--------|--------|
| **Company** | Name: `Demo Care Ltd` |
| **Care home 1** | Code: `RIVER01`, Name: `River View House`, Beds: `24`, Company: Demo Care Ltd |
| **Care home 2** | Code: `MEADOW02`, Name: `Meadow Court`, Beds: `18`, Company: Demo Care Ltd |
| **Funding authority 1** | Code: `ATC-COUNCIL`, Name: `Anytown Council`, Type: Council, Billing: Monthly, Email: `billing@anytown-council.example` |
| **Funding authority 2** | Code: `PRIV-FUNDER`, Name: `Private Funder Ltd`, Type: Private, Billing: Weekly, Email: `invoices@private-funder.example` |
| **Nominal code** | Code: `4000`, Name: `Care income` |
| **Invoice categories** | Pre-seeded on org create — verify `GENERAL_CARE`, `MISC` exist; do not recreate |
| **Invoice template** | Name: `General Care Template`, Category: General Care, Bank: `00-00-00` / `00000000`, Contact email: `finance@demo-care-group.example`, Footer: fictional payment terms |

### 6.2 Residents (clients)

| Field | Resident A | Resident B | Resident C (optional) |
|-------|------------|------------|---------------------|
| Sage ID | `DEMO001` | `DEMO002` | `DEMO003` |
| Reference | `RVH-001` | `RVH-002` | `MC-001` |
| Name | `Alex Morgan` | `Jordan Blake` | `Sam Taylor` |
| Care home | River View House | River View House | Meadow Court |
| Admission | `2026-04-01` | `2026-05-15` | `2026-06-01` |
| Email | `alex.morgan@example.com` | `jordan.blake@example.com` | `sam.taylor@example.com` |
| Status | Current | Current | Current |

### 6.3 Funding contracts and rates

| Resident | Authority | Category | Nominal | Start | Rate |
|----------|-----------|----------|---------|-------|------|
| Alex Morgan | Anytown Council | General Care | 4000 | `2026-04-01` | £575.00 / week |
| Jordan Blake | Anytown Council | General Care | 4000 | `2026-05-15` | £600.00 / week |
| Sam Taylor (optional) | Private Funder Ltd | General Care | 4000 | `2026-06-01` | £550.00 / week |

### 6.4 Invoices (at least two)

**MUST DO BEFORE DEMO** — Pre-generate during rehearsal so the demo does not depend on live billing timing.

Suggested periods (adjust if demo date moves):

| Invoice | Resident | Period | Expected status | Payment status |
|---------|----------|--------|-----------------|----------------|
| INV-0001 | Alex Morgan | `2026-08-01` – `2026-08-31` | Finalized | **Paid** |
| INV-0002 | Jordan Blake | `2026-08-01` – `2026-08-31` | Finalized | **Not paid** |

**Workflow:**

1. `/billing` → select company, home, General Care, period → **Preview** → **Generate**.
2. Open each invoice → **Download PDF** (verify opens as `%PDF`).
3. Mark INV-0001 **Paid**; leave INV-0002 **Not paid**.
4. **OPTIONAL:** Email INV-0002 once (simulated) to show Sent status.

**SHOULD DO BEFORE DEMO:** Rehearse billing preview first. If you see `ALREADY_FULLY_BILLED`, the period was already invoiced — pick an unbilled month or reset the database.

### 6.5 Credit note (at least one)

| Field | Value |
|-------|-------|
| Source invoice | INV-0002 (Jordan Blake, August 2026) |
| Credit period | Same month, subset covering **one invoice only** |
| Reason | e.g. `Partial period adjustment — demo` |
| Expected number | `CN-0001` |

**Route:** `/credit-notes` → Preview → Generate → Download PDF → **OPTIONAL:** Send (simulated).

**Rules to remember:** One invoice per credit; amount cannot exceed remaining invoiced balance.

### 6.6 Payments and statuses

Demonstrate on invoice list and detail:

- **Paid** — INV-0001 (green / Paid badge)
- **Not paid** — INV-0002 (outstanding)
- **Sent** (optional) — after simulated email on INV-0002

### 6.7 Reports

With the dataset above, these should return meaningful rows:

| Report | Purpose |
|--------|---------|
| Outstanding invoices | Shows INV-0002 |
| Invoices by client | Both residents |
| Client census | Three current clients (if all created) |

**OPTIONAL:** Sage 50 export for a date range including August 2026 invoices.

### 6.8 Audit

After setup, `/audit` (TenantAdmin / Administrator) should list Create/Update actions from organisation setup, client creation, invoice generation, payment status changes, and email send attempts.

---

## 7. Email Simulation

### Configuration (DO NOT change for demo)

| Setting | Demo value | Effect |
|---------|------------|--------|
| `Email__Mode` | `Development` (default) | No SMTP connection; sends return **Success=true, Simulated=true** |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Required for simulated success |

Production email controls (`Email__Mode=Smtp`, `ProductionStartupValidator`, `Email__AllowSimulationInProduction`) are **not** used and must **not** be weakened for the demo.

### What happens when email is "sent"

`ConfigurableEmailSender` in Development:

- Logs: `EMAIL SIMULATED (Email:Mode=Development). To=… Subject=… Attachment=… No message was delivered.`
- Returns success with `Simulated=true`
- **No external delivery**

### Workflows affected

| Workflow | UI behaviour | API / data |
|----------|--------------|------------|
| Organisation create (welcome email) | Platform form shows temporary password when simulated | `CredentialsEmailSimulated=true`; password in API response **Development only** |
| Invoice send | Success message: *"Send completed (or simulated in development)."* | Invoice `Status` → `Sent`; `EmailSendLogs` row with `Success=1`, `Simulated=1` |
| Credit note send | Success (or same pattern) | `SentAt` set; `EmailSendLogs` recorded |
| Bulk invoice send | Bulk result includes `Simulated` outcome | Per-invoice logs |

### How to demonstrate email without a real inbox

1. State clearly: *"In this Development environment, email is simulated — the workflow completes and is audited, but no message leaves the machine."*
2. Click **Email** on an invoice → show success in the UI.
3. **OPTIONAL:** Show API logs: `docker compose logs api | Select-String "EMAIL SIMULATED"`
4. Show invoice status changed to **Sent** and an entry in **Audit**.
5. Use only fictional recipient addresses (`@example.com`, `demo@localhost`).

**DO NOT DO:** Configure real SMTP, use real funder email addresses, or set `Email__Mode=Smtp` for the demo.

---

## 8. Demo Reset Procedure

Repeatable process to return to a clean demo state. **Limited to the local Docker demo stack.**

### Step-by-step

| Step | Action | Priority |
|------|--------|----------|
| 1 | `docker compose down` | Stop demo environment |
| 2 | `docker compose down -v` | **Remove demo SQL + document volumes only** |
| 3 | `docker compose up -d --build` | Start clean stack; migrations apply automatically |
| 4 | Wait for healthy API/SQL | `docker compose ps` |
| 5 | Clear browser `localStorage` (`carehome.auth`) or use fresh profile | Avoid stale JWT |
| 6 | Log in as PlatformAdmin | Provisioning |
| 7 | Create **Demo Care Group** + TenantAdmin (`demo-admin@example.com`) | MUST DO |
| 8 | Deactivate **Existing Organisation** | MUST DO |
| 9 | Log out; log in as TenantAdmin; complete password change | MUST DO |
| 10 | Enter master data, clients, contracts, rates (§6) | MUST DO |
| 11 | Generate invoices, credit note, optional users | MUST DO |
| 12 | Run smoke checklist (§9) | MUST DO |

### Full reset command (single block)

```powershell
docker compose down -v
docker compose up -d --build
# Wait ~1–2 minutes, then open http://localhost:4200
```

**DO NOT DO:** Run `down -v` against any non-demo database. Do not use this procedure on Azure SQL, shared LocalDB, or production hosts.

### OPTIONAL — Reset without wiping documents

If you only need a fresh database but want to keep PDF files (unusual for demo):

```powershell
docker compose down
docker volume rm carehome_carehome-sql-data
docker compose up -d
```

Prefer full `down -v` for predictable demo state.

---

## 9. Pre-Demo Smoke Checklist

Run **same day**, **before** the client joins. Check each item in **Demo Care Group** as **TenantAdmin** unless noted.

### Environment

- [ ] **MUST** — `ASPNETCORE_ENVIRONMENT=Development` (Docker Compose default)
- [ ] **MUST** — `GET http://localhost:5092/health/live` and `/health/ready` → Healthy
- [ ] **MUST** — http://localhost:4200 loads login page
- [ ] **SHOULD** — Browser session cleared; no stale `carehome.auth`
- [ ] **SHOULD** — Demo run from a known git commit (no surprise WIP)

### Authentication

- [ ] **MUST** — TenantAdmin login → `/dashboard` (not `/forbidden`, not stuck on password change)
- [ ] **MUST** — Organisation name **Demo Care Group** in header
- [ ] **SHOULD** — PlatformAdmin login works (if showing provisioning)
- [ ] **SHOULD** — PlatformAdmin gets 403 on `/companies` (expected)
- [ ] **OPTIONAL** — ReadOnly login cannot save; write buttons hidden

### Tenant access

- [ ] **MUST** — **Existing Organisation** is **Inactive**
- [ ] **MUST** — No demo transactions visible under tenant 1
- [ ] **MUST** — No real customer names, emails, or UAT leftovers

### Residents

- [ ] **MUST** — At least 2 clients open on profile with funding tab visible
- [ ] **MUST** — Active contracts and rates cover the rehearsed billing period

### Contracts / billing

- [ ] **MUST** — Billing **preview** returns lines for rehearsed period
- [ ] **SHOULD** — Understand `ALREADY_FULLY_BILLED` if rehearsing generate live

### Invoices

- [ ] **MUST** — At least 2 finalized invoices exist (one Paid, one Not paid)
- [ ] **MUST** — Invoice detail shows correct client, period, amounts

### PDF

- [ ] **MUST** — Invoice PDF download opens valid PDF (`%PDF`)
- [ ] **SHOULD** — Credit note PDF downloads (if credit note created)

### Payments

- [ ] **MUST** — Payment status toggle works on a Not paid invoice
- [ ] **SHOULD** — Paid invoice shows Paid on list and detail

### Credit notes

- [ ] **MUST** — At least 1 credit note exists (or rehearsed create path)
- [ ] **SHOULD** — Original invoice unchanged in invoice list

### Reports

- [ ] **MUST** — At least one report runs without error (e.g. outstanding)
- [ ] **SHOULD** — Export downloads a file

### Audit

- [ ] **MUST** — `/audit` lists recent demo actions

### Email simulation

- [ ] **MUST** — `Email:Mode` is Development (no SMTP configured)
- [ ] **MUST** — Invoice send returns success (simulated)
- [ ] **SHOULD** — Recipient addresses are fictional only

### Permissions

- [ ] **SHOULD** — ReadOnly cannot mutate data
- [ ] **OPTIONAL** — LocationManager sees only assigned home

### Narrative (state to client if asked)

- [ ] Billing formulas and Sage mapping are implemented but **pending finance sign-off** (`docs/PRODUCTION_BUSINESS_SIGNOFF.md`)
- [ ] Email is **simulated** in this environment
- [ ] Azure deployment is **next phase** after this demo

---

## 10. Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Dashboard 403 / empty tenant | Logged in as **PlatformAdmin** | Log out; log in as **TenantAdmin** |
| Redirect to `/change-password` | First login; `MustChangePassword=true` | Complete password change before demo |
| Empty companies/clients after "fresh" start | `DevelopmentMasterDataSeeder` skipped; only Existing Organisation exists | Create Demo Care Group and manual data (§4, §6) |
| **Existing Organisation** visible | Normal after migrate | Deactivate; do not use for demo |
| Billing preview empty / errors | Missing rate, template, contract, or client outside period | Complete master data; check admission dates |
| `ALREADY_FULLY_BILLED` | Period already invoiced | Use new month or reset database |
| Invoice email fails | `Email:Mode=Smtp` without SMTP, or Production env | Use Development + `Email:Mode=Development` |
| Organisation create fails on email | Production without SMTP | Not applicable in Development demo |
| Login fails after many attempts | Rate limit 10/min/IP on `/api/auth/login` | Wait 1 minute; use correct password |
| Wrong user after role switch | JWT in `localStorage` | Log out; clear site data |
| PDF 500 | Document path not writable | Check `carehome-documents` volume; `docker compose logs api` |
| API not ready | SQL still starting | `docker compose logs sql api`; wait for health |
| Stale data after "reset" | Forgot `-v` on `down` | `docker compose down -v` and repeat |
| README implies Demo Care Group auto-seeds | Seeder guard + Existing Organisation | Follow this document, not README alone |

**Logs:**

```powershell
docker compose logs -f api
docker compose logs -f sql
docker compose logs api | Select-String "EMAIL SIMULATED"
```

---

## 11. What Must NOT Be Used During the Demo

### DO NOT DO — Infrastructure and environment

| Item | Why |
|------|-----|
| Azure deployment or `Deploy-Azure.ps1` | Demo is pre-Azure; local Development only |
| `ASPNETCORE_ENVIRONMENT=Production` | Blocks dev credentials; requires SMTP/secrets |
| Shared LocalDB / old Docker volumes without reset | Stale UAT data, wrong passwords |
| `Database__ApplyMigrations=true` on any production/Azure host | Financial DB migration discipline |
| Real customer database or PII | GDPR / confidentiality |

### DO NOT DO — Data and tenancy

| Item | Why |
|------|-----|
| **Existing Organisation** (tenant 1) for transactions | Migration placeholder; looks unfinished |
| Sovereign Care Homes / Care Pro / historical home names | Internal migration history (should not appear on fresh DB) |
| Real client names, NHS numbers, addresses, funder emails | Use fictional demo data only |
| Old UAT invoices, credit notes, test passwords | Reset database |

### DO NOT DO — Credentials and security

| Item | Why |
|------|-----|
| Display `admin@localhost` / `DevAdmin!12345` on screen | Dev-only platform login |
| Display SQL SA password or `.env` on screen | Security perception |
| Production secrets (JWT from Key Vault docs, SMTP passwords) | Not needed; risk of exposure |
| `Email__AllowSimulationInProduction` | Weakens production controls |

### DO NOT DO — Product expectations

| Item | Why |
|------|-----|
| Present billing/Sage rules as finance-approved | All PENDING in `docs/PRODUCTION_BUSINESS_SIGNOFF.md` |
| Demo general client document upload | Feature not in MVP — use invoice PDF, reports, Sage export |
| Promise live email delivery in this environment | Development simulates success only |

### MUST DO BEFORE DEMO — Summary

1. Reset Docker volumes (`docker compose down -v`)
2. Start Development stack; verify health
3. Deactivate Existing Organisation
4. Create Demo Care Group + TenantAdmin; complete password change
5. Load fictional demo data (§6)
6. Rehearse end-to-end as TenantAdmin
7. Run smoke checklist (§9)

### SHOULD DO BEFORE DEMO — Summary

1. Create ReadOnly (and optionally Administrator) users
2. Pre-generate invoices and one credit note
3. Verify PDF and simulated email once
4. Prepare billing/Sage disclaimer
5. Clear browser session

### OPTIONAL — Summary

1. Live organisation provisioning segment (PlatformAdmin)
2. LocationManager permissions demo
3. Sage export / misc CSV import
4. Second care home and third resident

---

## Related documents

| Document | Use |
|----------|-----|
| `CLIENT_DEMO_READINESS.md` | Risk assessment and demo flow narrative |
| `README.md` | Docker quick start (verify against seeder behaviour in §3) |
| `docs/UAT_CHECKLIST.md` | Detailed per-screen operator steps |
| `docs/EXISTING_CUSTOMER_MIGRATION.md` | Existing Organisation technical background |
| `AZURE_FIRST_DEPLOYMENT_READINESS.md` | Post-demo Azure preparation (out of scope here) |
| `PRODUCTION_READINESS_ACTION_PLAN.md` | Production blockers after pilot |

---

*This runbook reflects repository state as of 15 September 2026. No application code or production configuration was modified.*

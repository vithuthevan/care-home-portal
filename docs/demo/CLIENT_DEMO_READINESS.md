# Client Demo Readiness

**Date:** 15 September 2026  
**Scope:** Pre-demo review only — no Azure deployment, no source or configuration changes.  
**Context:** The client demo happens **before** the first Azure deployment. This report prepares a clean, predictable local (or single-host) demonstration environment.  
**References:** `AZURE_FIRST_DEPLOYMENT_READINESS.md`, `P0_EXECUTION_ROADMAP.md`, `docs/SYSTEM_VERIFICATION_REPORT.md`, `docs/EXISTING_CUSTOMER_MIGRATION.md`

---

## 1. Overall Assessment

**🟡 Requires preparation**

The application is functionally capable of a full end-to-end demonstration (login → organisation → master data → billing → invoice/PDF → payment status → reports → roles/audit). Core flows were verified in August 2026 against a fresh database (`docs/SYSTEM_VERIFICATION_REPORT.md`).

It is **not** demo-ready out of the box without operator preparation because:

1. A fresh migration chain always creates tenant Id=1 **Existing Organisation** — leftover multi-tenancy migration history, not intentional demo branding.
2. `DevelopmentMasterDataSeeder` (which would create **Demo Care Group** / **Sunrise House**) **does not run** when any tenant already exists — so README/Docker guidance implying automatic demo data is misleading on a greenfield database.
3. The main business UI requires a **TenantAdmin** (or equivalent tenant-scoped) login. The default **PlatformAdmin** (`admin@localhost`) cannot access companies, clients, billing, or invoices (`[RequireTenant]` returns 403).
4. Billing formulas and Sage CSV mapping remain **PENDING** business sign-off (`docs/PRODUCTION_BUSINESS_SIGNOFF.md`). Safe for a capability demo; not safe to present as finance-approved production rules.
5. There is no general client document upload feature — documents in scope are **invoice PDFs**, **report exports**, **Sage CSV exports**, and **misc-charge CSV imports**.

With a **reset database**, a **client-branded test organisation**, **pre-built demo data**, and a **rehearsed script**, the demo can proceed confidently. Without that preparation, the presenter risks exposing **Existing Organisation**, empty tenant data, or a mid-demo password-change interruption.

---

## 2. Demo Risks

| Priority | Area | Risk | Impact | Recommendation |
|----------|------|------|--------|----------------|
| **P0** | Seed / tenancy | Fresh DB always has tenant **Existing Organisation** (Id=1) | Client sees internal migration placeholder in Organisations list; looks unfinished | Create a **new** client-branded organisation; **deactivate** Existing Organisation in Platform → Organisations before the demo; never operate inside tenant 1 |
| **P0** | Demo data | `DevelopmentMasterDataSeeder` skipped when tenant 1 exists | Empty companies/clients/invoices despite README implying demo seed | Pre-populate the **new** organisation manually or via rehearsed live setup; do not rely on automatic Demo Care Group |
| **P0** | Auth roles | PlatformAdmin has no tenant context | 403 on dashboard/companies/billing if logged in as `admin@localhost` | Run the main demo as **TenantAdmin**; use PlatformAdmin only for organisation provisioning (first 2–3 minutes) |
| **P0** | Stale environment | Reusing LocalDB / Docker volumes from dev/UAT | Historical test orgs, residents, invoices, or passwords visible | Reset DB before demo (`docker compose down -v` or drop/recreate `CareHomeDb`); clear browser `localStorage` (`carehome.auth`) |
| **P1** | First login | New tenant admins have `MustChangePassword=true` | Demo stalls on forced password change | Pre-create tenant admin **before** demo; complete password change once; store the final password securely for demo day |
| **P1** | Email | Wrong `Email:Mode` or real SMTP misconfiguration | Invoice send fails loudly; or mail goes to real addresses | Keep `ASPNETCORE_ENVIRONMENT=Development` and `Email:Mode=Development` (simulated success, logged only); use `@example.com` / `demo@localhost` recipient addresses |
| **P1** | Credentials | Default dev password documented in README | Client perceives weak security; password visible in repo/docs | Explain these are **local-demo-only** credentials; do not display README passwords on screen; use a dedicated demo tenant admin |
| **P1** | Business rules | Billing/Sage rules PENDING sign-off | Client treats provisional formulas as approved | Open with “implemented for demo; finance sign-off pending”; avoid quoting live funder amounts as contractual |
| **P2** | Historical names | Migrations once inserted **Sovereign Care Homes** / **Care Pro** on tenant 1 | Confusing if companies reappear on tenant 1 | On fresh DB they are deleted when unused (`RemoveUnusedHistoricalCustomerSeedCompanies`); still avoid using tenant 1 |
| **P2** | Documents | No resident document repository | “Upload document” step fails if scripted generically | Demo **invoice PDF download**, **report PDF/Excel export**, or **misc-charge CSV import** instead |
| **P2** | Login rate limit | 10 attempts/minute per IP | Lockout during repeated login rehearsals | Rehearse once; use correct tenant admin; avoid spamming wrong passwords |
| **P2** | Session state | JWT in browser storage | Wrong user/tenant after role switches | Log out between PlatformAdmin and TenantAdmin segments; use separate browser profiles if helpful |
| **P3** | PDF fonts | QuestPDF on Linux without font packages (Azure concern) | Not applicable to local Docker/Windows demo if PDF already tested once | Verify one PDF download in the **actual demo environment** before the client arrives |
| **P3** | WIP code | Uncommitted local changes | Demo differs from known-good build | Demo from a tagged/known commit; avoid mixed working-tree changes (`P0_EXECUTION_ROADMAP` P0-6) |

---

## 3. Database / Seed Data Findings

### 3.1 Migration chain overview

Latest migration: `20260911053954_UniqueMiscChargeDedupeIndex` (12-step chain from `InitialCreate`).

Startup behaviour (`Program.cs`):

| Step | When | What |
|------|------|------|
| `Database:ApplyMigrations` | Only if config `true` (Docker sets this) | `MigrateAsync()` |
| `IdentitySeeder` | Every environment | Creates roles; creates PlatformAdmin if `Seed:AdminEmail` / `Seed:AdminPassword` set |
| `DevelopmentMasterDataSeeder` | **Development only**, and **only if zero tenants** | Would create Demo Care Group — **skipped on fresh migrate** |

Production does **not** auto-migrate at startup; the demo should use **Development** locally.

### 3.2 “Existing Organisation” — root cause

| Question | Finding |
|----------|---------|
| **Where created** | Migration `20260829072440_AddMultiTenancy.cs` — explicit SQL `INSERT INTO Tenants … Id=1, Name=N'Existing Organisation'` with fixed `PublicId` `9E4F2C11-7A8B-4D3E-9C10-1B2A3C4D5E6F` |
| **Why it exists** | Multi-tenancy was added to an existing single-tenant product. Tenant 1 is the **anchor** for backfilling `TenantId=1` on all pre-existing rows during upgrade. On a **greenfield** database it is an unavoidable side effect of replaying that migration — not product branding. |
| **Historical seed?** | Yes — migration compatibility artifact. Documented in `docs/EXISTING_CUSTOMER_MIGRATION.md` and `AZURE_FIRST_DEPLOYMENT_READINESS.md` §7. |
| **Required for functionality?** | **No** for demo or new customers. New organisations are created via `POST /api/platform/tenants` / Platform UI with their own Id, settings, invoice categories, and document sequences. Tenant 1 can be **deactivated** (`IsActive=false`) without blocking new tenants. |
| **Should it appear in client demo?** | **No.** Deactivate it or ensure the presenter never selects it. It may still appear in the Platform Organisations list unless hidden by narrative (“legacy migration placeholder”). |

**Related tenant-1 artifacts on fresh DB:**

- Default invoice categories (`GENERAL_CARE`, `OUTREACH`, `RENT`, `MISC`) from `AddFundingMasterData` → assigned to tenant 1 by `AddMultiTenancy`
- Document sequences `INV-` / `CN-` for tenant 1 from `AlignOperationalTenantSchema`
- `TenantSettings` (GBP, 30-day terms) for tenant 1
- **No** care homes from historical named-home seed (removed from `AddOperationalDomain`)
- **Sovereign Care Homes** / **Care Pro** inserted by `InitialCreate`, then **deleted** when unused by `RemoveUnusedHistoricalCustomerSeedCompanies`

### 3.3 Development seeder (not migration)

`DevelopmentMasterDataSeeder` (`IdentitySeeder.cs`) would create:

- Organisation: **Demo Care Group**
- Company: **Demo Care Ltd**
- Care home: **Sunrise House** (code `SUNRISE`)
- Funding authorities: Development NHS/Council/Private examples
- Invoice templates with placeholder bank details

**Guard:** `if (await dbContext.Tenants.AnyAsync()) return;` — because **Existing Organisation** already exists after migrate, this seeder **never runs** on a standard fresh database. This contradicts README line “Development login after first start” implying a ready demo org.

### 3.4 Default users and roles

| Item | Source | Demo note |
|------|--------|-----------|
| Roles | `IdentitySeeder` — PlatformAdmin, TenantAdmin, Administrator, LocationManager, ReadOnly (+ legacy SuperAdmin) | Required system data |
| Platform admin | `appsettings.Development.json`: `admin@localhost` / `DevAdmin!12345` | Platform provisioning only |
| Tenant admin | Created when provisioning organisation with admin email | Required for main demo |
| Production seed guard | `KnownDevelopmentCredentials` blocks dev credentials outside Development | Do not run demo with `ASPNETCORE_ENVIRONMENT=Production` and dev passwords |

### 3.5 Safest approach (no code changes yet)

**Recommended for this demo:**

1. **Reset** to an empty database (see §4).
2. Let migrations run (creates Existing Organisation — accept as internal artifact).
3. Log in as **PlatformAdmin**.
4. Create a **new organisation** with client-appropriate name (e.g. “Riverside Care Group”) and a dedicated admin email (`demo-admin@example.com`).
5. Copy the **temporary password** from the create-organisation screen (Development only returns it when email is simulated).
6. **Deactivate** Existing Organisation (Platform → Organisations → Edit → inactive).
7. Log out; log in as the **new TenantAdmin**; complete password change once.
8. Build or restore **demo business data** only inside the new organisation (§4).

**Do not** delete tenant 1 via SQL unless the operator understands FK constraints — deactivation via UI is safer.

**Post-demo / post-Azure (code or migration work — not for this demo):** consider a follow-up migration or seeder guard change so greenfield installs either skip inserting Existing Organisation or run demo seed into tenant 2. That is tracked for after the demo in §7.

---

## 4. Clean Demo Environment

### A. Required system / reference data (keep)

| Data | How it arrives | Demo action |
|------|----------------|-------------|
| ASP.NET Identity roles | `IdentitySeeder` at startup | None — automatic |
| Invoice category **definitions** per tenant | `DefaultInvoiceCategories` on organisation create | Automatic for new org |
| Document sequences per tenant | `TenantProvisioningService` | Automatic for new org |
| Tenant settings defaults (GBP, prefixes, payment terms) | Provisioning / migration for tenant 1 | Confirm in Organisation settings during demo |
| JWT / auth infrastructure | `appsettings.Development.json` | None if using Development profile |

### B. Demo / business data (create in the client-branded organisation)

Suggested minimal demo dataset:

| Entity | Suggested demo content |
|--------|------------------------|
| Organisation | Client-branded name; trading name; logo optional |
| Company | e.g. “Riverside Care Ltd” |
| Care home | e.g. code `RIVER01`, name “River View House”, beds 24 |
| Funding authority | e.g. “Anytown Council”, type Council, billing Monthly |
| Nominal code | e.g. `4000` — Care income |
| Invoice template | General Care — bank details **clearly fictional** (README uses `00-00-00`) |
| Client / resident | Fictional person; Sage ID `DEMO001`; admission date inside billing period |
| Funding contract | Active; General Care category; linked nominal |
| Rate | e.g. £575/week from admission date |
| Invoice | One finalized invoice for a completed billing period |
| Payment status | Demonstrate Paid / Not Paid on that invoice |
| Optional | Second user with ReadOnly role; LocationManager with home scope |
| Optional | One misc-charge CSV import; one Sage export preview |

### C. Data that must NOT appear in the demo

| Category | Examples | How to prevent |
|----------|----------|----------------|
| Migration placeholder tenant | **Existing Organisation** | Deactivate; do not use for transactions |
| Historical company names | Sovereign Care Homes, Care Pro | Avoid tenant 1; fresh DB deletes if unused |
| Historical care home names | Filsham, Ampersand, etc. | No longer inserted on fresh DB |
| Dev seeder labels | Demo Care Group, Development NHS Example | Seeder does not run; avoid creating these names unless intentional |
| Real customer PII | Real names, NHS numbers, addresses, funder emails | Use fictional demo names only |
| Real financial history | Old UAT invoices, test credit notes | Reset database |
| Dev credentials on screen | `admin@localhost`, `DevAdmin!12345` | Use dedicated demo tenant admin; explain dev-only platform login verbally |
| Production secrets | SMTP passwords, JWT keys from Key Vault docs | Not needed for Development demo |

### D. Recommended demo hosting configuration

**Preferred:** Docker Compose from repository root (`docker-compose.yml`).

| Setting | Demo value | Rationale |
|---------|------------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Enables simulated email; skips Production startup validator |
| `Database__ApplyMigrations` | `true` | Auto-applies migration chain on empty SQL volume |
| `Email__Mode` | `Development` (default) | Simulates send success; logs warning; no real mail |
| `ConnectionStrings__DefaultConnection` | Docker SQL service | Isolated from developer LocalDB clutter |
| `DocumentStorage__RootPath` | `/app/App_Data/documents` (Docker volume) | PDF persistence across restarts within the demo stack |
| `App__PublicUrl` | `http://localhost:4200` | Welcome email link text if org created live |
| SMTP | **Not required** | Do not configure real SMTP for demo |
| JWT key | Development default in `appsettings.Development.json` | Acceptable locally; never reuse in Production |

**Alternative:** LocalDB + `dotnet run` (Development profile) + `npm start` — ensure `dotnet ef database update` against a **dedicated** demo database name, not a shared `CareHomeDb` with months of dev data.

**Do not** weaken Production security for the demo (no `Email__AllowSimulationInProduction`, no Production profile with dev passwords).

---

## 5. Recommended Demo Flow

Estimated duration: 45–60 minutes with pre-built data; 75–90 minutes if building master data live.

### Segment A — Platform setup (PlatformAdmin, ~5 min) — optional live

| Step | Route / action | Notes |
|------|----------------|-------|
| 1. Login | `/login` as PlatformAdmin | Encrypted password transport; rate limit 10/min |
| 2. Organisations | `/platform/tenants` | Show multi-tenant model; **point out Existing Organisation is a migration artifact** if visible |
| 3. Deactivate artifact | Edit Existing Organisation → inactive | Prevents accidental use |
| 4. Create organisation | `/platform/tenants/new` | Client-branded name; admin email to a safe address |
| 5. Capture credentials | Create response | In Development, temporary password shown on screen when email simulated |
| 6. Log out | | Clear session |

**Failure points:** Production email mode without SMTP; creating org without admin email (API requires it).

### Segment B — Tenant operations (TenantAdmin, main demo)

| Step | Route / action | Failure points |
|------|----------------|----------------|
| 1. Login | `/login` | Forced redirect to `/change-password` if first login — **complete once before clients arrive** |
| 2. Dashboard | `/dashboard` | Empty widgets if no data — pre-build data |
| 3. Organisation settings | `/settings/organisation` | TenantAdmin/Administrator only |
| 4. Company | `/companies` | — |
| 5. Care home | `/care-homes`, dashboard | Unique code per tenant |
| 6. Funding authority | `/funding-authorities` | — |
| 7. Nominal code | `/nominal-codes` | Duplicate codes rejected |
| 8. Invoice categories | `/invoice-categories` | Pre-seeded GENERAL_CARE etc. on new org — show, don’t recreate |
| 9. Invoice template | `/invoice-templates` | Required before invoice email; bank details fictional |
| 10. Client | `/clients/new`, `/clients/:id` | Unique Sage ID and reference; email needed for invoice send |
| 11. Funding contract + rate | Client profile → Funding tab | Active contract; rate covering demo billing period |
| 12. Billing | `/billing` | Preview first; explain partial-period / ALREADY_FULLY_BILLED if rehearsed |
| 13. Generate invoice | Billing workspace | Concurrency: don’t double-click Generate |
| 14. Invoice detail | `/invoices/:id` | — |
| 15. PDF | Download PDF button | Verify `%PDF` opens; QuestPDF |
| 16. Email | Email button | Success with simulated mail in Development; show API/logs if asked |
| 17. Payment status | Mark Paid / Not Paid | Visible on list and detail |
| 18. Documents (substitute) | Invoice PDF + `/reports` export PDF/Excel + `/sage-exports` | No general client file upload exists |
| 19. Reports | `/reports` | e.g. outstanding, invoices-by-client, client-census |
| 20. Roles | `/users` | Create ReadOnly user; show forbidden write actions |
| 21. Audit | `/audit` | Admin-only; shows create/update actions from demo |

### Segment C — Permissions mini-demo (~5 min)

1. Log in as **ReadOnly** — confirm write buttons hidden and saves blocked.  
2. Optionally log in as **LocationManager** scoped to one care home — confirm data scope.

### Narrative caveats to state explicitly

- Billing proration and Sage mapping are **implemented but not finance-approved** (`docs/PRODUCTION_BUSINESS_SIGNOFF.md` — all PENDING).
- Email is **simulated** in this environment.
- Azure deployment and production SMTP are **next phase** after this demo.

---

## 6. Pre-Demo Checklist

Run **same day**, **before** the client joins.

### Environment

- [ ] Demo machine uses **Development** profile (Docker or local), not Production
- [ ] Database reset completed (`docker compose down -v` **or** new empty DB) and migrations applied successfully
- [ ] API health: `GET http://localhost:5092/health/live` and `/health/ready` → Healthy
- [ ] Frontend loads: `http://localhost:4200` — login page, not blank
- [ ] Browser: cleared site data / fresh profile; no stale JWT
- [ ] Demo run from a **known git commit** (no surprise WIP)

### Database / seed safety

- [ ] **Existing Organisation** deactivated (or presenter script ready to explain it)
- [ ] Client-branded organisation exists and is **Active**
- [ ] No Sovereign / Care Pro / old UAT companies visible in the demo tenant
- [ ] Demo business data exists in the **client organisation** (not tenant 1)
- [ ] No real customer names, emails, or financial figures from prior tests

### Login / auth

- [ ] TenantAdmin password change **already completed** (not first-login during demo)
- [ ] PlatformAdmin login works (if showing org provisioning)
- [ ] TenantAdmin login lands on `/dashboard` (not `/forbidden`)
- [ ] ReadOnly test user exists (if showing permissions)

### Core flows (smoke in demo tenant)

- [ ] Dashboard loads counts
- [ ] Client profile opens; funding contract and rate visible
- [ ] Billing **preview** returns lines for rehearsed period
- [ ] At least one **finalized invoice** exists (or generate once during rehearsal)
- [ ] **PDF download** opens valid PDF
- [ ] **Email send** returns success (simulated in Development)
- [ ] **Payment status** toggle works
- [ ] **Report** runs (e.g. outstanding) and export button downloads a file
- [ ] **Audit** list shows recent actions
- [ ] PlatformAdmin **cannot** open `/companies` (403 expected — optional check)

### Email / documents / security

- [ ] `Email:Mode` is **Development** (or unset → Development default) — no real SMTP
- [ ] Invoice recipient addresses are fictional (`@example.com` / `demo@localhost`)
- [ ] Document folder writable (invoice PDF regenerated if needed)
- [ ] Presenter knows **not** to show README passwords or `.env` SQL password on screen

### Session / UX

- [ ] Rehearsal completed end-to-end once with timing
- [ ] Tab/window zoom and resolution acceptable for screen share
- [ ] Error messages understood (missing template, missing rate, ALREADY_FULLY_BILLED)

---

## 7. Post-Demo Actions

### MUST fix before first Azure deployment (after demo)

| Item | Why |
|------|-----|
| Decide SMTP Path A vs Path B | Production refuses to start without SMTP or explicit simulation opt-in (`AZURE_FIRST_DEPLOYMENT_READINESS.md`) |
| Provision Azure with unique names, Key Vault secrets, `App__PublicUrl` | Not demo scope; required for pilot |
| Confirm **Existing Organisation** is not the pilot tenant | Same migration artifact appears on Azure greenfield DB |
| Business sign-off on billing rules and Sage mapping | P0 blocker for live funder invoicing (`P0_EXECUTION_ROADMAP.md` P0-1) |
| Tagged release build from known commit | P0-6 — pilot must match audited code |

### SHOULD fix before Azure (quality / operator experience)

| Item | Why |
|------|-----|
| Reconcile README vs actual seed behaviour (Demo Care Group vs Existing Organisation) | Prevents next operator repeating demo confusion |
| Document standard “demo reset” script (Docker volume wipe + seed checklist) | Repeatable sales demos |
| Rename or deactivate tenant 1 in pilot after first migrate | Operator hygiene |
| Restore drill / backup procedures | After Azure SQL exists — not before demo |

### Can wait until after demo (and optionally after Azure pilot)

| Item | Why |
|------|-----|
| Code/migration change to stop inserting **Existing Organisation** on greenfield | Requires careful migration design; existing customers depend on tenant 1 semantics |
| Fix `DevelopmentMasterDataSeeder` to run when only tenant 1 is present | Developer ergonomics; workaround is manual demo data |
| Remove historical `HasData` from old migrations | EF constraint — forward-only migrations preferred (`RemoveUnusedHistoricalCustomerSeedCompanies` pattern) |
| Client document upload feature | Not in current MVP |
| Application Insights, private endpoints, custom domain | GA hardening |

### Immediate post-demo (same day)

- [ ] Shut down or isolate demo stack if exposed on network
- [ ] Rotate any credential accidentally shared on screen (tenant admin password)
- [ ] Note client questions on billing/Sage for business sign-off backlog
- [ ] Capture list of UI gaps discovered during demo

---

## Priority summary

| Priority | Items |
|----------|-------|
| **MUST fix before demo** | Reset DB; create client-branded org; deactivate Existing Organisation; pre-build demo data; rehearse as TenantAdmin; Development email mode; clear browser session |
| **SHOULD fix before demo** | Complete password change before client; fictional PII only; prepare billing/Sage disclaimer; verify PDF once in demo environment; optional ReadOnly user |
| **Can wait until after demo** | Azure deploy; migration/seed refactor for tenant 1; business sign-off; README correction; production SMTP; code changes to seeder logic |

---

## Related documents

| Document | Use |
|----------|-----|
| `README.md` | Docker/local run; dev login (verify against §3.3 seeder guard) |
| `docs/UAT_CHECKLIST.md` | Detailed operator test steps |
| `docs/PRODUCTION_SMOKE_TEST.md` | Post-Azure smoke (not pre-demo) |
| `docs/EXISTING_CUSTOMER_MIGRATION.md` | Existing Organisation technical background |
| `AZURE_FIRST_DEPLOYMENT_READINESS.md` | Post-demo Azure preparation |
| `P0_EXECUTION_ROADMAP.md` | Production blockers after pilot |

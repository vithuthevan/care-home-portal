# Final Demo Data Readiness

**Date:** 17 September 2026  
**Session:** Local client-demo database inspection. **No data mutations.**  
**Environment:** Docker Compose Development only.  
**Sources of truth:** `FINAL_CLIENT_DEMO_UX_VERIFICATION.md`, `../demo/DEMO_DATA_PREPARATION_GUIDE.md`, `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md`, `../demo/DEMO_ENVIRONMENT_VARIABLES.md`, `../archive/CREDIT_NOTE_INVESTIGATION.md`.

**Not done in this session:** Azure/production, schema changes, billing/credit-note/auth/UI code changes, SMTP, destructive SQL, invoice total edits, credential storage.

---

## Environment confirmation

Confirmed **before** any data-change consideration.

| Check | Result |
|--------|--------|
| `ASPNETCORE_ENVIRONMENT` | **Development** (`carehome-api` container) |
| Database | Docker Compose `carehome-sql` → `CareHomeDb` (host `localhost:14333`). Connection string uses Compose service `sql`, **not** Azure SQL |
| Web | `http://localhost:4200` — login HTTP **200**; container healthy |
| API | `http://localhost:5092` — `/health/ready` **Healthy**, `/health/live` **Healthy** |
| Email | `Email:Mode=Development` — simulated; **SMTP not configured** |
| PlatformAdmin login | **Verified live** (`admin@localhost` → role `PlatformAdmin`; tenant business routes return **403**) |

---

## Current database state

Read-only SQL + PlatformAdmin organisation list. **No writes.**

### Organisations

| Id | Name | Active | Tenant relationship |
|----|------|--------|---------------------|
| 1 | Existing Organisation | Inactive | Migration placeholder. No demo TenantAdmin. |
| 2 | Demo Care Group | **Inactive** | Scripted org name. Owns `demo-admin@example.com`, **Alex Morgan**, master data **Demo Care Ltd** / **River View House** / **Anytown Council**. **No** funding contract, **no** invoices. |
| 1002 | Existing Organisation 1 | Inactive | Unrelated org. Resident **Vithursan Thevendran**. |
| **1004** | **Demo Care Group 1** | **Active** | **Current live tenant.** Owns `demo-admin1@example.com`, **Alexa Morgan**, **INV-0001**. Company name **Demo Care Limited**. |

Only **one** tenant is active: **1004**. Login for a tenant-scoped user is rejected when that user’s organisation `IsActive` is false (`AuthController` → HTTP 401, generic message).

### Users

Passwords are **not** displayed.

| Email | Display name | Role | Tenant / organisation | User active | MustChangePassword |
|-------|--------------|------|------------------------|-------------|-------------------|
| `admin@localhost` | Platform Administrator | PlatformAdmin | *(none)* | Yes | No |
| `demo-admin@example.com` | Demo Administrator | TenantAdmin | **2 — Demo Care Group (inactive)** | Yes | No |
| `demo-admin1@example.com` | Demo Administrator | TenantAdmin | **1004 — Demo Care Group 1 (active)** | Yes | No |
| `existingorganisation1@gmail.com` | Existing Organisation 1 Admin | TenantAdmin | 1002 (inactive) | Yes | No |

**ReadOnly:** **Not present.** No `demo-viewer@example.com` (or any `ReadOnly` role assignment).

Identity: `RequireUniqueEmail = true`. Emails are globally unique across tenants.

### Companies (demo-relevant)

| TenantId | Company Id | Name | Active |
|----------|------------|------|--------|
| 2 | 3 | Demo Care Ltd | Yes |
| 1002 | 1003 | Company 1 | Yes |
| **1004** | **1004** | **Demo Care Limited** | Yes |

Script expects **Demo Care Ltd** on the active demo org.

### Care homes (demo-relevant)

| TenantId | Id | Code | Name | Beds | Company |
|----------|-----|------|------|------|---------|
| 2 | 1 | RIVER01 | River View House | 24 | Demo Care Ltd |
| 1002 | 2 | CH-01 | Carehome 1 | 30 | Company 1 |
| **1004** | **3** | **RIVER01** | **River View House** | **24** | Demo Care Limited |

### Funding authorities (demo-relevant)

| TenantId | Code | Name | Type | Active |
|----------|------|------|------|--------|
| 2 | ATC-COUNCIL | Anytown Council | Council | Yes |
| 1002 | ATC-Council | Anytown Council | Council | Yes |
| **1004** | **ATC_COUNCIL** | **Anytown Council** | Council | Yes |

Invoice categories on tenant 1004 include seeded **General Care Invoice** (`GENERAL_CARE`). Prefixes: `INV-` / `CN-`, 30-day terms, GBP — all tenants.

### Residents

| Id | Name | Reference | Care home | Tenant | Status | Archived |
|----|------|-----------|-----------|--------|--------|----------|
| 1 | Alex Morgan | RVH-001 | River View House | **2** (inactive) | Current | No |
| 2 | Vithursan Thevendran | CDL01 | Carehome 1 | 1002 (inactive) | Current | No |
| **3** | **Alexa Morgan** | **RVH-001** | River View House | **1004 (active)** | Current | No |

**Jordan Blake:** **missing** (all tenants).

**Duplicate `RVH-001`:** yes, **across tenants** (Id 1 on tenant 2, Id 3 on tenant 1004). The **active** tenant has a **single** `RVH-001`.

Tenant 2 Alex matches the preparation guide (Ms, DOB 1948-03-12, Sage `DEMO001`, email `alex.morgan@example.com`, admission 2026-04-01, Residential) but has **no contract/rate**.  
Tenant 1004 Alexa is the billed resident (Sage `DEMO001`, same DOB/admission, email `alexa.morgan@example.com`).

### Contracts (Alex / Jordan)

| Resident | Tenant | Authority | Category | Start | End | Status |
|----------|--------|-----------|----------|-------|-----|--------|
| Alexa Morgan | 1004 | Anytown Council | General Care Invoice | 2026-04-01 | *(null — open-ended)* | Active |
| Alex Morgan | 2 | — | — | — | — | **No contract** |
| Jordan Blake | — | — | — | — | — | **Missing** |

### Rates

| Resident | Amount | Frequency | Effective from | Effective to |
|----------|--------|-----------|----------------|--------------|
| Alexa Morgan (tenant 1004) | **575.00** | Weekly | 2026-04-01 | *(null)* |
| Alex Morgan (tenant 2) | — | — | — | — |
| Jordan Blake | — | — | — | — |

### Invoices

| Invoice number | Resident (line snapshot) | Amount | Payment status | Invoice status | Billing period | Tenant |
|----------------|--------------------------|--------|----------------|----------------|----------------|--------|
| **INV-0001** | **Alexa Morgan** (`RVH-001`) | **2546.43** | **NotPaid** | Generated | 2026-08-01 → 2026-08-31 | 1004 |
| **INV-0002** | — | — | — | — | — | **Missing** |

INV-0001 PDF path exists: `tenants/…/invoices/invoice-INV-0001.pdf`. Live PDF open **not** verified (requires TenantAdmin).

### Credit notes

**None** (all tenants).

### Audit (tenant 1004, most recent)

| Entity | Action | Description |
|--------|--------|-------------|
| Invoice | Generate | Generated 1 invoice(s). |
| FundingRate | Create | Added funding rate. |
| ClientFundingContract | Create | Created funding contract. |
| Client | Create | Created client Alexa Morgan. |
| InvoiceTemplate / NominalCode / FundingAuthority / CareHome / Company / Tenant | Create | Master data + org provision |

No payment, email, or credit-note audit on tenant 1004.

---

## Account alignment

### Target

**Demo Care Group (active)** → TenantAdmin → **`demo-admin@example.com`**

### Actual

| Account | Bound organisation | Can sign in today? |
|---------|--------------------|--------------------|
| `demo-admin@example.com` | Tenant **2**, **inactive** | **No** — organisation inactive |
| `demo-admin1@example.com` | Tenant **1004**, **active** | **Yes, if operator password is known** — password **not available in this session** (not guessed) |

### Does the UI support the preferred account?

| Action | Supported in existing UI? | Notes |
|--------|---------------------------|--------|
| Create `demo-admin@example.com` on tenant 1004 | **No** | `/users` create is tenant-scoped, but email is **globally unique** |
| Deactivate `demo-admin@example.com` | **Only from tenant 2** | `/users` deactivate is same-tenant; that tenant has **only this user**, who **cannot deactivate themselves** |
| Assign / move user to another tenant | **No** | `UsersController.Update` does **not** change `TenantId`. Platform tenant **edit** does **not** re-bind admin email |
| Reactivate tenant 2 / deactivate tenant 1004 | **Yes** — PlatformAdmin **Edit organisation → Active** | This is a **supported** org operation, **not** a silent user-ownership SQL change |
| Provision a new org with `demo-admin@example.com` | **No** | Duplicate email on create |

**Not done:** no SQL `TenantId` move, no Identity hash rewrite, no `docker compose down -v`.

**STOP — preferred account vs active tenant cannot be completed through user-management UI alone.**

Operator choices that **do** exist (require explicit confirmation; **not** executed here):

1. **Keep tenant 1004** and present as TenantAdmin **`demo-admin1@example.com`** (rename org/company via UI after login). Closest to existing INV-0001. **Does not** meet scripted email.
2. **PlatformAdmin:** reactivate **Demo Care Group** (Id 2), deactivate **Demo Care Group 1** (Id 1004). Aligns **`demo-admin@example.com`** with the scripted org **without moving the user**. Then rebuild contracts/invoices on tenant 2. **Hides** existing INV-0001 on 1004. Still needs tenant-2 password.
3. **Clean volume reset** (`docker compose down -v`) and full runbook from empty DB. Destructive. Not run.
4. **Manual identity SQL** to free `demo-admin@example.com` then create it on 1004 via `/users`. **Requires explicit confirmation.** Not run.

---

## Data preparation performed

| Step | Status |
|------|--------|
| Confirm Development + Compose DB + localhost ports | **Done** |
| Inspect organisations, users, master data, residents, contracts, rates, invoices, credit notes, audit | **Done** (read-only) |
| PlatformAdmin login + tenant list | **Done** |
| TenantAdmin UI (edit Alexa→Alex, Jordan, billing generate, Mark paid, credit Preview, PDF, email, reports, audit, ReadOnly user) | **Not executed** — TenantAdmin password not available; PlatformAdmin is **403** on tenant routes |
| Direct SQL updates / deletes | **Not executed** |
| Credit note generate | **Not executed** (INV-0002 missing; would not force £600) |

**Archive/deactivate of stray residents:** UI can set status then **Archive** (Current residents cannot be archived until status is not Current). Extra resident **Vithursan Thevendran** lives on **inactive** tenant 1002 — not visible after login to 1004. **Do not delete via SQL.**

---

## Final expected dataset (script)

| Entity | Expected |
|--------|----------|
| Organisation | **Demo Care Group** (active) |
| Company | **Demo Care Ltd** |
| Care home | **River View House** (`RIVER01`) |
| Funder | **Anytown Council** |
| Residents | **Alex Morgan** `RVH-001`, **Jordan Blake** `RVH-002` |
| Alex | £575/week, contract **01/04/2026** open-ended, General Care |
| Jordan | £600/week; guide contract start **15/05/2026** (admission); task verify text also said 01/04/2026 — **use the preparation guide** for remaining fields |
| INV-0001 | Alex, **£2,546.43**, **Paid** — generated via Billing, marked paid in UI |
| INV-0002 | Jordan, **£2,657.14**, **Not paid** — sequential Preview → Generate |

---

## Invoice verification

| Item | Expected | Actual |
|------|----------|--------|
| INV-0001 | Exists | **Exists** on tenant 1004 |
| Resident | Alex Morgan | **Alexa Morgan** (invoice line snapshot) |
| Amount | £2,546.43 | **2546.43** (do **not** alter totals) |
| Payment | Paid | **NotPaid** |
| Period | August 2026 | 2026-08-01 – 2026-08-31 |
| INV-0002 | Jordan £2,657.14 Not paid | **Missing** |

Do **not** generate a second INV-0001. After Jordan exists, Preview August 2026 for River View House **before** Generate; expect **£2,657.14** and Alex skipped as already billed.

---

## Credit-note Preview amount

| Item | Result |
|------|--------|
| Generated CN | **None** |
| Preview amount | **Not recorded** — requires INV-0002 and TenantAdmin |
| Product behaviour | UI does **not** send `lineAmounts`. Preview for INV-0002 is expected to show **£2,657.14** (full remaining line), **not £600** |
| Presenter rule | Describe **whatever Preview shows**. Do **not** tell the client the system generated a £600 partial credit unless Preview shows £600.00 |

---

## Email simulation verification

| Item | Result |
|------|--------|
| SMTP | **Not configured** (correct for this demo) |
| Mode | Development simulation |
| Live send on INV-0002 | **Not verified** (invoice missing) |
| Expected UI wording | `Invoice email simulated successfully in this demonstration environment.` (implemented in invoice detail when API `simulated: true`) |
| Claim | Do **not** claim external delivery |

---

## Reports verification

| Report | Expected | Actual |
|--------|----------|--------|
| Outstanding | INV-0002 unpaid; INV-0001 absent if Paid | **Not verified** (INV-0002 missing; INV-0001 still NotPaid so it would currently appear **outstanding**) |
| Invoices by client (From `2026-08-01` To `2026-08-31`) | Alex INV-0001 £2,546.43; Jordan INV-0002 £2,657.14 | **Not verified** |

---

## Audit verification

| Item | Result |
|------|--------|
| TenantAdmin `/audit` access | **Not verified live** — `adminGuard` allows TenantAdmin; login blocked here |
| Present | Org/master data/client/contract/rate/INV-0001 generate on tenant 1004 |
| Missing vs demo | Payment change, email, credit-note, Jordan create, INV-0002 generate |

Audit implementation was **not** changed.

---

## Remaining blockers

1. **TenantAdmin credentials** — cannot complete UI data repair, Mark paid, billing generate, PDF, email, reports, or audit walkthrough without the operator password manager.
2. **Scripted account** — `demo-admin@example.com` is on **inactive** tenant **2**. Moving it to **1004** is **not** a supported UI operation; **STOP** rather than SQL.
3. **Active org branding** — **Demo Care Group 1** / **Demo Care Limited** / **Alexa Morgan**, not the scripted names.
4. **Jordan Blake, £600 rate, INV-0002** — absent.
5. **INV-0001** — amount correct, payment **NotPaid**; must use Invoice Detail **Mark Paid**.
6. **ReadOnly** account not created.
7. **Credit note / email / reports** — blocked on INV-0002 + TenantAdmin session.

---

## Final recommendation

**Not demo ready.**

The local Development stack is the correct target and is healthy. The **active** dataset is tenant **1004**, which already has a valid August **INV-0001** total of **£2,546.43** produced by billing (do not rewrite it). Gaps versus the walkthrough are **account/org naming**, **Alex vs Alexa**, **missing Jordan / INV-0002**, and **unpaid INV-0001**.

**Do not** proceed with identity SQL or a volume wipe unless the operator explicitly chooses option 3 or 4 under [Account alignment](#account-alignment).

**Minimum path (no wipe, no SQL):** operator logs in as **`demo-admin1@example.com`**, then via **normal UI only**:

1. Rename organisation (PlatformAdmin or Organisation Settings) to **Demo Care Group** if desired; rename company to **Demo Care Ltd**.
2. Edit client Id **3** first name **Alex** (keep `RVH-001`).
3. Create **Jordan Blake** per `../demo/DEMO_DATA_PREPARATION_GUIDE.md` (`RVH-002`, £600/week from **2026-05-15**).
4. Billing August 2026 → **Preview £2,657.14** → Generate **INV-0002**.
5. INV-0001 → **Mark Paid**.
6. INV-0002 → PDF; Email (confirm simulated wording); Credit note **Preview only** and record the amount (expect **£2,657.14**).
7. Reports (August 2026) and Audit.
8. Optional: `/users` → `demo-viewer@example.com` ReadOnly.

Until that session succeeds, treat the requirement matrix below as the live result.

---

## Requirement matrix

| Requirement | Expected | Actual | PASS/FAIL |
|-------------|----------|--------|-----------|
| Demo Care Group | Active org named Demo Care Group | Active name **Demo Care Group 1** (Id 1004); exact name on **inactive** Id 2 | **FAIL** |
| Active tenant | One clear demo tenant | **1004** active; 1, 2, 1002 inactive | **PASS** *(active exists)* |
| TenantAdmin | `demo-admin@example.com` on active Demo Care Group | **`demo-admin1@example.com`** on 1004; **`demo-admin@example.com`** on inactive 2 | **FAIL** |
| Alex | Alex Morgan `RVH-001` River View House / Anytown Council / General Care | **Alexa Morgan** `RVH-001` on 1004; true Alex on inactive tenant 2 | **FAIL** |
| Jordan | Jordan Blake `RVH-002` | **Missing** | **FAIL** |
| Alex rate | £575/week from 01/04/2026 open-ended | **575.00 Weekly** from 2026-04-01 (Alexa / 1004) | **PASS** *(amount/dates)* |
| Jordan rate | £600/week | **Missing** | **FAIL** |
| INV-0001 | Present | **Present** | **PASS** |
| INV-0001 amount | £2,546.43 | **2546.43** | **PASS** |
| INV-0001 payment status | Paid | **NotPaid** | **FAIL** |
| INV-0002 | Present | **Missing** | **FAIL** |
| INV-0002 amount | £2,657.14 | **N/A** | **FAIL** |
| INV-0002 payment status | Not paid | **N/A** | **FAIL** |
| Credit note | Preview recorded; no forced £600 | **No INV-0002 / no Preview** | **FAIL** |
| PDF | INV-0002 (and INV-0001) downloadable | Path on INV-0001 only; UI not verified | **FAIL** |
| Email simulation | Simulated success copy; no SMTP | Config OK; live send **not verified** | **FAIL** |
| Outstanding report | INV-0002 unpaid | **Not verified** | **FAIL** |
| Invoices-by-client report | Both invoices August 2026 | **Not verified** | **FAIL** |
| Audit | TenantAdmin sees generate / payment / email / CN | Entries exist; TenantAdmin access **not verified** | **FAIL** |

---

*Stopped after inspection and this report. No application source, schema, billing, credit-note, or authentication code was modified. No passwords were written to repository files.*

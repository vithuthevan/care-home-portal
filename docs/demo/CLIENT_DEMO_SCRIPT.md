# Client Demo Script

**Date:** 15 September 2026  
**Environment:** Local Docker Compose, `ASPNETCORE_ENVIRONMENT=Development`  
**Audience:** Care organisation stakeholders (finance, operations, management)  
**Presenter login:** **TenantAdmin** for the main demo; **PlatformAdmin** only for optional provisioning segment

---

## Demo startup procedure

Use this procedure for a **clean, isolated** demo environment. It affects **only** local Docker volumes on the demo machine.

### 1. Reset and start

From the repository root:

```powershell
docker compose down -v
docker compose up -d --build
```

`-v` removes the `carehome-sql-data` and `carehome-documents` volumes so each run starts with an empty database and document store.

Wait until services are healthy:

```powershell
docker compose ps
```

| Check | URL / command | Expected |
|-------|---------------|----------|
| API live | http://localhost:5092/health/live | `{"status":"Healthy",…}` |
| API ready | http://localhost:5092/health/ready | `{"status":"Healthy",…}` |
| Frontend | http://localhost:4200 | Login page |

**Optional:** Copy `.env.demo.example` to `.env` only if overriding `MSSQL_SA_PASSWORD`. Defaults work without a `.env` file.

Clear browser site data for `localhost:4200` (JWT key: `carehome.auth`) or use a fresh browser profile.

### 2. Database initialization

On first startup with an empty SQL volume:

1. SQL Server container starts and passes its healthcheck.
2. API container starts with `Database__ApplyMigrations=true`.
3. `Program.cs` runs `MigrateAsync()` — applies the full EF Core migration chain to `CareHomeDb`.
4. Migration `AddMultiTenancy` creates tenant Id=1 **Existing Organisation** (migration compatibility artifact — not demo branding).

No manual `dotnet ef database update` is required in Docker.

### 3. EF Core migrations

Handled automatically at API startup when `Database__ApplyMigrations=true` (set in `docker-compose.yml`). Do not run migrations manually unless using LocalDB outside Docker.

### 4. PlatformAdmin access

`IdentitySeeder` creates roles and the PlatformAdmin user from Development config:

| Field | Value |
|-------|-------|
| Email | `admin@localhost` |
| Password | `DevAdmin!12345` |
| Source | `appsettings.Development.json` → `Seed:AdminEmail` / `Seed:AdminPassword` |

Use PlatformAdmin **only for setup** — this account cannot access tenant business screens (`/companies`, `/clients`, `/billing`, etc.).

**Do not display these credentials to the client.**

### 5. Demo organisation creation

1. Log in at http://localhost:4200/login as PlatformAdmin.
2. Go to **Organisations** → **Add organisation**.
3. Create **Demo Care Group** (or client-branded name):

| Field | Demo value |
|-------|------------|
| Name | `Demo Care Group` |
| Trading name | `Demo Care Group` |
| Address | `1 Demo Lane, Anytown, AN1 2BC` |
| Phone | `01234 567890` |
| Email | `info@demo-care-group.example` |
| Active | ✓ |
| Admin email | `demo-admin@example.com` |
| Admin display name | `Demo Administrator` |

4. Save. In Development, the create screen shows the **temporary password** (email is simulated).
5. Copy the temporary password to a password manager — **do not show it on screen during the client session**.

### 6. Existing Organisation handling

Tenant Id=1 **Existing Organisation** is created by migrations. It is **not** the demo organisation.

1. Edit **Existing Organisation** → uncheck **Active** → Save.
2. Never create residents, contracts, or invoices in tenant 1.

If asked: *"Existing Organisation is a migration placeholder from the single-tenant upgrade; we operate in a dedicated demo organisation."*

### 7. TenantAdmin creation and password change

The TenantAdmin is created automatically when you provision the organisation (step 5).

1. Log out PlatformAdmin.
2. Log in as `demo-admin@example.com` with the temporary password.
3. Complete the forced **password change** at `/change-password`.
4. Confirm landing on `/dashboard` with **Demo Care Group** in the header.

**Complete this before the client arrives** — do not do first-login password change during the demo.

### 8. Demo data creation

Build fictional data inside **Demo Care Group** only. Pre-build during rehearsal.

#### Master data

| Entity | Values |
|--------|--------|
| Company | `Demo Care Ltd` |
| Care home 1 | Code `RIVER01`, `River View House`, 24 beds |
| Care home 2 | Code `MEADOW02`, `Meadow Court`, 18 beds |
| Funding authority 1 | `ATC-COUNCIL` — Anytown Council, Council, Monthly |
| Funding authority 2 | `PRIV-FUNDER` — Private Funder Ltd, Private, Weekly |
| Nominal code | `4000` — Care income |
| Invoice template | General Care, fictional bank `00-00-00` / `00000000` |

#### Residents

| | Resident A | Resident B |
|-|------------|------------|
| Sage ID | `DEMO001` | `DEMO002` |
| Reference | `RVH-001` | `RVH-002` |
| Name | Alex Morgan | Jordan Blake |
| Care home | River View House | River View House |
| Admission | 2026-04-01 | 2026-05-15 |

#### Contracts and rates

| Resident | Authority | Rate | Start |
|----------|-----------|------|-------|
| Alex Morgan | Anytown Council | £575/week | 2026-04-01 |
| Jordan Blake | Anytown Council | £600/week | 2026-05-15 |

#### Invoices (pre-generate)

| Invoice | Resident | Period | Status | Payment |
|---------|----------|--------|--------|---------|
| INV-0001 | Alex Morgan | Aug 2026 | Finalized | **Paid** |
| INV-0002 | Jordan Blake | Aug 2026 | Finalized | **Not paid** |

#### Credit note

- Source: INV-0002 (Jordan Blake)
- Reason: e.g. *Partial period adjustment — demo*
- Expected: `CN-0001`

#### Optional users (permissions segment)

| Name | Email | Role |
|------|-------|------|
| Demo Viewer | `demo-viewer@example.com` | ReadOnly |
| Demo Accountant | `demo-accountant@example.com` | Administrator |

---

## Demo flow

### 1. Introduction (1–2 minutes)

**Say:**

> This system supports care organisations that need to manage residents, funding contracts, invoicing, payments, and reporting in one place. Finance teams generate invoices from care contracts; managers maintain master data and residents; administrators control users and permissions. The core workflow is: set up the organisation → register residents and funding → generate invoices → record payments → report and audit.

Do not lead with Docker, Azure, or EF Core unless the client asks.

---

### 2. Login (2 minutes)

1. Open http://localhost:4200
2. Log in as **TenantAdmin** (`demo-admin@example.com`)
3. Point out **Demo Care Group** in the header (organisation context)
4. Land on **Dashboard** — brief orientation

**Do not** log in as PlatformAdmin for the main story.

---

### 3. Organisation (2 minutes)

Navigate to **Organisation settings** (`/settings/organisation`).

Show:

- Organisation name and trading details
- Currency (GBP), invoice prefixes, payment terms

Keep this short — confirm the system is configured for their operating model.

---

### 4. User management (3 minutes — if time permits)

Navigate to **Users** (`/users`).

Show:

- User list, roles, active status
- How a new user is added with a role

**Role explanation (brief):**

| Role | Scope | Demo note |
|------|-------|-----------|
| **PlatformAdmin** | Entire platform | Creates organisations only; no tenant data access |
| **TenantAdmin** | One organisation | Full admin for that organisation |
| **Administrator** | One organisation | Day-to-day operations and finance (e.g. accountant persona) |
| **ReadOnly** | One organisation | View only — writes blocked |

Skip if time is short; cover in the permissions segment (step 14) instead.

---

### 5. Resident / client (4 minutes)

Open **Alex Morgan** (`/clients/:id`).

Show:

- Personal and reference details (Sage ID, care home, admission date)
- **Funding** tab — active contract, authority, category, rate
- Status and contact details

**Business point:** The resident record is the anchor for all billing — contract and rate drive invoice lines.

---

### 6. Funding / contract (3 minutes)

From Alex’s profile or **Funding authorities**, show:

- Contract linked to Anytown Council
- Start date, category (General Care), nominal code
- Effective-dated **rate** (£575/week)

**Business point:** When the billing period runs, the system uses the active contract and rate to calculate invoice lines. Billing formulas are implemented for demonstration; **finance stakeholder sign-off on proration rules is still pending**.

---

### 7. Invoice (5 minutes)

**Option A (pre-built):** Open **INV-0001** and **INV-0002** from `/invoices`.

**Option B (live):** `/billing` → select company, home, category, period → **Preview** → **Generate**.

Show on invoice detail:

- Invoice number
- Resident name
- Billing period
- Line items and amounts
- Status (Finalized)
- Payment status

**Business point:** Invoices move from generation → review → send → payment tracking. This replaces manual spreadsheet billing.

---

### 8. Invoice PDF (2 minutes)

On an invoice detail page, click **Download PDF**.

Open the PDF and show:

- Organisation branding area
- Resident and period
- Line items and total
- Bank/payment details (fictional for demo)

**Business point:** This is what the funder receives.

---

### 9. Payment (2 minutes)

On invoice list or detail:

- Show **INV-0001** — **Paid**
- Show **INV-0002** — **Not paid** / outstanding
- Toggle payment status on INV-0002 (or mark paid then revert during rehearsal only)

**Business point:** Finance can track what is collected vs outstanding without a separate spreadsheet.

---

### 10. Credit note (3 minutes)

Open `/credit-notes` or the credit note linked to INV-0002.

Show:

- Credit note number (`CN-0001`)
- Linked invoice and resident
- Adjustment reason and amount
- Effect on outstanding balance

**Scenario:** *"Jordan Blake was overcharged for part of August — we issue a credit note to correct the record."*

---

### 11. Email (2 minutes — simulation only)

On INV-0002, click **Email** (or Send).

**Say clearly:**

> In this Development environment, email is **simulated**. The workflow completes, the invoice is marked Sent, and the action is audited — but **no message leaves this machine**. In production we will connect real SMTP.

Show:

- Success message in the UI
- Invoice status → **Sent**

**Do not** claim the client received an email. **Do not** configure real SMTP for the demo.

---

### 12. Reports (3 minutes)

Open `/reports`. Run **two** high-value reports:

1. **Outstanding invoices** — shows INV-0002
2. **Invoices by client** — both residents

**Optional:** Export to Excel/PDF if time permits.

Do not walk through every report.

---

### 13. Audit / history (2 minutes)

Open `/audit`.

Show recent entries: client created, invoice generated, payment status changed, email send (simulated).

**Say:**

> Every significant action is recorded — who did what and when. That supports accountability, traceability, and financial review.

---

### 14. Permissions (3 minutes — if time permits)

1. Log out TenantAdmin.
2. Log in as **Demo Viewer** (ReadOnly).
3. Open a resident and an invoice — show read access.
4. Attempt a save or create — show blocked / buttons hidden.
5. Log back in as TenantAdmin.

---

### 15. Closing (2 minutes)

**Summarize the story:**

> Collect residents and funding → manage contracts and rates → generate invoices → send (or simulate) → record payments and credit notes → report and audit.

**Ask the client:**

- What workflow is missing?
- What would you change?
- Which reports are most important?
- Are the billing rules correct? *(pending finance sign-off)*
- Are the user roles correct?
- What should be prioritized before launch?

---

## Demo timing

### 30-minute demo

| Section | Minutes | Notes |
|---------|---------|-------|
| 1. Introduction | 2 | Problem, users, workflow |
| 2. Login + dashboard | 2 | TenantAdmin only |
| 3. Organisation | 2 | Settings glance |
| 4. User management | — | **Skip** |
| 5. Resident | 4 | Alex Morgan profile + funding |
| 6. Funding / contract | 2 | Fold into resident if tight |
| 7. Invoice | 5 | Pre-built invoices |
| 8. PDF | 2 | Download and show |
| 9. Payment | 2 | Paid vs outstanding |
| 10. Credit note | 3 | CN-0001 |
| 11. Email (simulated) | 2 | State simulation clearly |
| 12. Reports | 3 | Outstanding + by client |
| 13. Audit | 2 | Recent actions |
| 14. Permissions | — | **Skip** |
| 15. Closing | 3 | Summary + questions |
| **Total** | **~29** | Buffer for transitions |

### 15-minute short demo

Prioritize the revenue cycle — skip organisation admin, users, audit, and permissions.

| Section | Minutes | What to show |
|---------|---------|--------------|
| Introduction | 1 | One-sentence problem + workflow |
| Login + dashboard | 1 | TenantAdmin, Demo Care Group |
| Resident + funding | 3 | Alex Morgan — profile and contract/rate |
| Invoice + PDF | 4 | INV-0002 detail + PDF download |
| Payment + credit note | 3 | Outstanding status + CN-0001 |
| Email (simulated) | 1 | Send with simulation disclaimer |
| Reports | 1 | Outstanding invoices only |
| Closing | 1 | One question: billing rules + priorities |
| **Total** | **~15** | |

---

## Narrative guardrails

| Topic | What to say |
|-------|-------------|
| Billing / proration | Implemented for demo; **pending finance sign-off** |
| Sage export mapping | Provisional — see business sign-off |
| Email | **Simulated** in this environment |
| Azure / production | **Next phase** after this demo |
| Existing Organisation | Migration placeholder — deactivated |
| Client document upload | Not in current MVP — use invoice PDFs and reports |

---

## Related documents

| Document | Use |
|----------|-----|
| `DEMO_PRE_FLIGHT_CHECKLIST.md` | Same-day checks and recovery |
| `DEMO_ENVIRONMENT_VARIABLES.md` | Configuration reference |
| `CLIENT_DEMO_ENVIRONMENT_SETUP.md` | Detailed setup and troubleshooting |
| `docs/UAT_CHECKLIST.md` | Field-level screen steps |

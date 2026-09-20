# Client Demo Operator Runbook

## Environment

Development / Docker Compose / Simulated Email

## Golden Rule

Never use:

- Production
- Azure
- real SMTP
- real customer data
- shared development database

This runbook prepares a **local client demo** from a **clean machine state**. It does **not** modify application code, schema, migrations, or production/Azure configuration.

**Related source documents:** `DEMO_DATA_PREPARATION_GUIDE.md`, `CLIENT_DEMO_SCRIPT.md`, `CLIENT_DEMO_ENVIRONMENT_SETUP.md`, `CLIENT_DEMO_READINESS.md`, `DEMO_ENVIRONMENT_VARIABLES.md`, `DEMO_PRE_FLIGHT_CHECKLIST.md`, `CLIENT_DEMO_DRY_RUN_REPORT.md`

---

## How to use this document

1. Work **Part 1 → Part 16** in order on a rehearsal day (allow **60–90 minutes** for full data entry).
2. Complete **Part 16** again on demo day before the client joins.
3. Keep **Part 14** or **Part 15** open during the presentation.
4. Use **Part 17** only if something fails during the session (no destructive commands while the client is watching).

**Placeholder credentials** (set in your password manager; never show on screen):

| Placeholder | Used for |
|-------------|----------|
| `<DEMO_PLATFORM_ADMIN_PASSWORD>` | PlatformAdmin (`admin@localhost`) — from `Seed:AdminPassword` in Development config |
| `<DEMO_TENANT_ADMIN_PASSWORD>` | TenantAdmin (`demo-admin@example.com`) — after org create + password change |
| `<DEMO_VIEWER_PASSWORD>` | Optional ReadOnly user (`demo-viewer@example.com`) |

---

# PART 1 — Starting the application

## 1.1 Prerequisites

1. Install **Docker Desktop** and ensure it is running.
2. Open a terminal at the **repository root** (folder containing `docker-compose.yml`).
3. Optional: copy `.env.demo.example` to `.env` only if you must override `MSSQL_SA_PASSWORD`. Defaults work without `.env`.

## 1.2 Reset and start (clean demo database)

Run from the repository root:

```powershell
docker compose down -v
docker compose up -d --build
```

### What `docker compose down -v` does

| Effect | Detail |
|--------|--------|
| Stops containers | Stops `carehome-sql`, `carehome-api`, and `carehome-web` |
| Removes containers | Next `up` creates fresh containers |
| **`-v` removes Compose volumes** | Deletes **`carehome-sql-data`** (SQL Server data — entire `CareHomeDb`) and **`carehome-documents`** (invoice/credit-note PDFs and Sage export files) |

On disk, Docker may show these as `carehome_carehome-sql-data` and `carehome_carehome-documents` (project name `carehome` + volume key). They are the same volumes defined in `docker-compose.yml`.

**Why this is safe for the local demo:** Data lives only in those Docker volumes on your demo machine. Wiping them gives a predictable empty database and document store for rehearsal.

**What must NOT be run against production/shared environments:**

- Never run `docker compose down -v` against Azure, shared SQL, or any non-isolated database.
- Never point Compose at production connection strings or run with `ASPNETCORE_ENVIRONMENT=Production` for this demo.

### What `docker compose up -d --build` does

| Step | Detail |
|------|--------|
| Builds images | Rebuilds API and Angular (`web`) images if needed |
| Starts SQL | `carehome-sql` on host port **14333** |
| Starts API | `carehome-api` on **http://localhost:5092** (`ASPNETCORE_ENVIRONMENT=Development`, `Database__ApplyMigrations=true`) |
| Starts web | `carehome-web` on **http://localhost:4200** (proxies `/api` and `/health` to the API container) |
| Applies migrations | API runs EF Core migrations on empty SQL volume |
| Seeds PlatformAdmin | `IdentitySeeder` creates roles and `admin@localhost` when Development seed is configured |

**First build** may take several minutes. The Angular dev server inside `carehome-web` can take **30–90 seconds** after containers are up before the login page responds.

### What happens on first API start (no manual migrations)

1. SQL container passes its healthcheck.
2. API runs `MigrateAsync()` because `Database__ApplyMigrations=true` in Compose.
3. Migration `AddMultiTenancy` creates tenant Id=1 **Existing Organisation** (expected artifact).
4. `DevelopmentMasterDataSeeder` **does not** create Demo Care Group when tenant 1 already exists — you create the demo org manually (Part 2).

## 1.3 Health verification

### Container status

```powershell
docker compose ps
```

| Service | Container name | Expected |
|---------|----------------|----------|
| sql | `carehome-sql` | **healthy** |
| api | `carehome-api` | **healthy** |
| web | `carehome-web` | **running** (no healthcheck in Compose) |

### API health endpoints (from `Program.cs`)

| Endpoint | Tag | Purpose |
|----------|-----|---------|
| `GET http://localhost:5092/health/live` | `live` | Process is up |
| `GET http://localhost:5092/health/ready` | `ready` | SQL Server connectivity (`SqlReadyHealthCheck`) |

**Expected:** JSON with `"status":"Healthy"` (or equivalent healthy response).

PowerShell check:

```powershell
Invoke-RestMethod http://localhost:5092/health/live
Invoke-RestMethod http://localhost:5092/health/ready
```

Via the frontend proxy (same checks through `carehome-web`):

- `http://localhost:4200/health/live`
- `http://localhost:4200/health/ready`

### Frontend

| URL | Expected |
|-----|----------|
| http://localhost:4200 | Redirects to login or shows **Care Home Back Office** sign-in |
| http://localhost:4200/login | Login form: **Email address**, **Password**, button **Sign in** |

### Document storage

There is **no separate HTTP health URL** for documents. Storage is the Docker volume mounted at `/app/App_Data/documents` in the API container (`DocumentStorage__RootPath`). Verify by downloading an invoice PDF after generation (Part 9).

### Browser session (recommended before login)

Clear site data for `localhost:4200` or use a fresh profile. JWT is stored in `localStorage` key **`carehome.auth`**.

---

# PART 2 — Platform Admin

Complete this **before** any tenant business data. PlatformAdmin **cannot** access tenant routes such as `/companies`, `/clients`, or `/billing` (API returns 403).

## 2.1 Open the frontend and log in

| Step | Action | Expected result |
|------|--------|-----------------|
| 1 | Open http://localhost:4200/login | Login page: **Care Home Back Office** |
| 2 | **Email address:** `admin@localhost` | — |
| 3 | **Password:** `<DEMO_PLATFORM_ADMIN_PASSWORD>` | — |
| 4 | Click **Sign in** | Side nav shows only **Organisations** (platform scope) |

## 2.2 Organisation management

| Step | Action | Expected result |
|------|--------|-----------------|
| 1 | Side nav → **Organisations** or open http://localhost:4200/platform/tenants | List includes **Existing Organisation** (migration artifact) |
| 2 | Click **Add organisation** or open http://localhost:4200/platform/tenants/new | Page title **Add organisation** |

## 2.3 Create Demo Care Group

| Field | Value | Required |
|-------|-------|----------|
| Name | `Demo Care Group` | Yes |
| Trading name | `Demo Care Group` | Optional |
| Address | `1 Demo Lane, Anytown, AN1 2BC` | Optional |
| Phone | `01234 567890` | Optional |
| Email | `info@demo-care-group.example` | Optional |
| Active | ✓ checked | Yes |
| Admin email | `demo-admin@example.com` | Yes |
| Admin display name | `Demo Administrator` | Optional |

Click **Save**.

| Expected result |
|-----------------|
| **Organisation created** screen appears |
| Message: login details sent to `demo-admin@example.com` |
| In Development, banner: **Email is simulated in Development. Temporary password:** … |
| Copy the **temporary password** to your password manager — **do not show on screen during the client demo** |

Click **Back to organisations** when done.

## 2.4 Record TenantAdmin credentials

| Item | Value |
|------|-------|
| Email | `demo-admin@example.com` |
| Temporary password | From create screen (store securely) |
| Final password | You will set `<DEMO_TENANT_ADMIN_PASSWORD>` at first login (Part 3) |

## 2.5 Deactivate Existing Organisation

| Step | Action | Expected result |
|------|--------|-----------------|
| 1 | On http://localhost:4200/platform/tenants, find **Existing Organisation** | Status may show **Active** |
| 2 | Click **Edit** (route `/platform/tenants/{id}`) | **Edit organisation** form |
| 3 | Uncheck **Active** | — |
| 4 | Click **Save** | Status **Inactive** in list |

**Rule:** Never create residents, contracts, or invoices in **Existing Organisation** (tenant Id=1).

---

# PART 3 — Tenant Admin

## 3.1 Log out PlatformAdmin

| Step | Action | Expected result |
|------|--------|-----------------|
| 1 | Header → user menu (avatar) → **Sign out** | Login page |

## 3.2 Log in as TenantAdmin

| Step | Action | Expected result |
|------|--------|-----------------|
| 1 | http://localhost:4200/login | Login page |
| 2 | **Email address:** `demo-admin@example.com` | — |
| 3 | **Password:** temporary password from Part 2 | Redirect to **Set your password** (`/change-password`) |

## 3.3 First-login password change

Complete **before the client arrives** — not during the demo.

| Field | Value |
|-------|-------|
| Temporary password | Organisation create temporary password |
| New password | `<DEMO_TENANT_ADMIN_PASSWORD>` |
| Confirm new password | Same |

Password rules (UI): at least **12 characters** with upper, lower, number, and symbol.

Click **Save password and continue**.

| Expected result |
|-----------------|
| Full app shell loads (side nav with Dashboard, Operations, Billing, etc.) |

## 3.4 Confirm organisation and dashboard

| Check | Where | Expected |
|-------|-------|----------|
| Organisation name | Side nav brand subtitle | **Demo Care Group** |
| Dashboard | http://localhost:4200/dashboard | Page loads (counts may be zero until data entry) |
| Organisation settings | **Administration** → **Organisation Settings** → `/settings/organisation` | Name **Demo Care Group**; **Invoice prefix** `INV-`; **Credit note prefix** `CN-`; **Payment terms** 30 days; **Currency** GBP (£) — confirm only; do not change prefixes |

---

# PART 4 — Demo data

Enter **all** data in **Demo Care Group** only. Use fictional values from `DEMO_DATA_PREPARATION_GUIDE.md` — do not invent new data.

**Recommended entry order** (dependencies):

1. Company & care homes  
2. Funding authorities  
3. Nominal code & invoice template  
4. Verify invoice categories  
5. Resident **Alex** → contract & rate → **INV-0001** (Part 5)  
6. Resident **Jordan** → contract & rate → **INV-0002** (Part 6)  
7. Credit note (Part 7)  
8. Mark INV-0001 paid (Part 8)  
9. Optional: second care home, Sam Taylor, extra users  

---

## 4.1 Company

| Step | Navigation | Action |
|------|------------|--------|
| 1 | **Operations** → **Companies** → `/companies` | Click **Add Company** → `/companies/new` |

| Field | Value | Required | Expected result |
|-------|-------|----------|-----------------|
| Company name | `Demo Care Ltd` | Yes | Company appears in list |

Click **Save**.

---

## 4.2 Care homes

| Step | Navigation | Action |
|------|------------|--------|
| 1 | **Operations** → **Care Homes** → `/care-homes` | **Add Care Home** → `/care-homes/new` |

### River View House (required)

| Field | Value | Required |
|-------|-------|----------|
| Company | `Demo Care Ltd` | Yes |
| Care home code | `RIVER01` | Yes |
| Care home name | `River View House` | Yes |
| Bed capacity | `24` | Optional |

Click **Save**.

### Meadow Court (optional — for Sam Taylor)

| Field | Value |
|-------|-------|
| Company | `Demo Care Ltd` |
| Code | `MEADOW02` |
| Name | `Meadow Court` |
| Bed capacity | `18` |

---

## 4.3 Funding authorities

**Billing Setup** → **Funding Authorities** → `/funding-authorities` → **Add Authority** → `/funding-authorities/new`.

### Anytown Council (required)

| Field | Value | Required |
|-------|-------|----------|
| Code | `ATC-COUNCIL` | Yes |
| Name | `Anytown Council` | Yes |
| Type | `Council` | Yes |
| Contact name | `Adult Social Care Billing` | Optional |
| Phone | `01234 567001` | Optional |
| Email | `billing@anytown-council.example` | Optional |
| Address | `Adult Social Care, Anytown Council, Civic Centre, Anytown, AN1 1AA` | Optional |
| Billing frequency | `Monthly` | Yes |

Save.

### Private Funder Ltd (optional)

| Field | Value |
|-------|-------|
| Code | `PRIV-FUNDER` |
| Name | `Private Funder Ltd` |
| Type | `Private` |
| Email | `invoices@private-funder.example` |
| Contact name | `Accounts Payable` |
| Phone | `01234 567002` |
| Address | `Finance Department, Private Funder Ltd, 100 Commerce Park, Anytown, AN2 2BB` |
| Billing frequency | `Weekly` |

---

## 4.4 Nominal code

**Billing Setup** → **Nominal Codes** → `/nominal-codes` → add ( **Add** / new form).

| Field | Value | Required |
|-------|-------|----------|
| Code | `4000` | Yes |
| Name | `Care income` | Yes |

---

## 4.5 Invoice categories (verify only)

**Billing Setup** → **Invoice Categories** → `/invoice-categories`

| Code | Name | Action |
|------|------|--------|
| `GENERAL_CARE` | General Care | Confirm present — **do not recreate** |
| `MISC` | Miscellaneous | Confirm present |

---

## 4.6 Invoice template

**Billing Setup** → **Invoice Templates** → `/invoice-templates`

Scroll to **Add category default template** (inline form on the list page).

| Field | Value | Required |
|-------|-------|----------|
| Name | `General Care Template` | Yes |
| Category | `General Care` | Yes |
| Footer | `Payment due within 30 days. Bank details are fictional for demonstration purposes only.` | Optional |
| Bank account | `Demo Care Group Client Account` | Optional |
| Sort code | `00-00-00` | Optional |
| Account number | `00000000` | Optional |
| Contact name | `Demo Finance Team` | Optional |
| Contact email | `finance@demo-care-group.example` | Optional |

Click **Save template**.

**Note:** The UI does not expose contact job title or contact phone (those appear only in the data guide). The funding contract form does not link a template; at invoice generation, the **most specific matching template** for the category is used (see page subtitle).

---

## 4.7 Residents

**Operations** → **Clients** → **Add Client** → `/clients/new`

New clients default to status **Current** on create (no status field on the add form).

### Resident A — Alex Morgan (create first; before Jordan)

| Section | Field | Value | Required |
|---------|-------|-------|----------|
| Location | Care home | `Demo Care Ltd - River View House` (or `River View House`) | Yes |
| Client identification | Sage ID | `DEMO001` | Yes |
| Client identification | Client reference number | `RVH-001` | Yes |
| Personal | Title | `Ms` | Optional |
| Personal | First name | `Alex` | Yes |
| Personal | Last name | `Morgan` | Yes |
| Personal | Date of birth | `1948-03-12` | Optional |
| Care | Care type | `Residential` | Yes |
| Care | Admission date | `2026-04-01` | Yes |
| Contact | Email address | `alex.morgan@example.com` | Optional |
| Contact | Phone | `07700 900101` | Optional |
| Contact | Notes | `Previous address (fictional): 14 Willow Close, Anytown, AN1 3DE. Demo resident — not real.` | Optional |

Click **Save** → profile opens at `/clients/{id}`.

### Funding contract — Alex

Tab: **Funding contracts**

| Field | Value | Required |
|-------|-------|----------|
| Authority | `Anytown Council` | Yes |
| Category | `General Care` | Yes |
| Nominal | `4000` | Yes |
| Start | `2026-04-01` | Yes |
| End | *(leave blank)* | Optional |

Click **Save contract**.

Tab: **Rate history** → **Add rate**

| Field | Value | Required |
|-------|-------|----------|
| Contract | `Anytown Council / General Care` | Yes |
| From | `2026-04-01` | Yes |
| To | *(blank)* | Optional |
| Frequency | `Weekly` | Yes |
| Amount | `575.00` | Yes |

Click **Add rate**.

| Expected result |
|-----------------|
| Contract **Active**; rate £575.00/week visible |

### Resident B — Jordan Blake (create only after INV-0001 — Part 5)

Same flow at `/clients/new`:

| Field | Value |
|-------|-------|
| Care home | River View House |
| Sage ID | `DEMO002` |
| Client reference number | `RVH-002` |
| Title | `Mr` |
| First name | `Jordan` |
| Last name | `Blake` |
| Date of birth | `1952-07-22` |
| Care type | `Nursing` |
| Admission date | `2026-05-15` |
| Email address | `jordan.blake@example.com` |
| Phone | `07700 900102` |
| Notes | `Previous address (fictional): 8 Meadow Lane, Anytown, AN1 4FG. Demo resident — not real.` |

Contract: Anytown Council, General Care, nominal `4000`, start `2026-05-15`.  
Rate: Weekly **£600.00** from `2026-05-15`.

### Resident C — Sam Taylor (optional)

| Field | Value |
|-------|-------|
| Care home | Meadow Court |
| Sage ID | `DEMO003` |
| Client reference number | `MC-001` |
| Title | `Ms` *(UI has no `Mx`; use Ms or leave title blank)* |
| First name / last name | Sam / Taylor |
| DOB | `1960-11-05` |
| Care type | Residential |
| Admission date | `2026-06-01` |
| Email / phone / notes | Per `DEMO_DATA_PREPARATION_GUIDE.md` |
| Funder | Private Funder Ltd, rate £550/week from `2026-06-01` |

---

## 4.8 Billing configuration summary

| Item | Demo value |
|------|------------|
| Billing period | `2026-08-01` to `2026-08-31` |
| Invoice date (system) | `2026-08-31` (period end) |
| Due date (system) | `2026-09-30` (30-day terms) |

**DEMO ONLY — NOT FINANCE APPROVED:** Line amount ≈ `(weekly rate ÷ 7) × eligible days` in August (31 days).

| Resident | Weekly rate | Expected August line |
|----------|-------------|----------------------|
| Alex Morgan | £575.00 | **£2,546.43** |
| Jordan Blake | £600.00 | **£2,657.14** |

---

# PART 5 — Alex / INV-0001

**Critical:** Generate this invoice **before** creating **Jordan Blake**. If both residents exist with the same company, care home, funder, and category, one billing run creates **one invoice with two lines** (billing groups by Company + Care Home + Funding Authority + Invoice Category).

## 5.1 Generate

| Step | Navigation | Action |
|------|------------|--------|
| 1 | **Billing** → **Billing Workspace** → `/billing` | — |
| 2 | **Step 1 — Select billing scope** | |
| | Company | `Demo Care Ltd` |
| | Care home | `River View House` *(select the home, not “All care homes”)* |
| | Invoice category | `General Care` |
| | Period start | `2026-08-01` |
| | Period end | `2026-08-31` |
| 3 | Click **Preview** | **Step 2** shows **Alex Morgan** only; **Step 4** line amount **£2,546.43**; banner **Ready to generate** |
| 4 | Click **Generate** | Green banner: `Generated 1 invoice(s). Total £2,546.43.` |

## 5.2 Verify INV-0001

**Billing** → **Invoices** → `/invoices` → open **INV-0001**.

| Check | Expected |
|-------|----------|
| Invoice number | `INV-0001` |
| Status badge | `Generated` |
| Payment status | `Not paid` *(until Part 8)* |
| Resident (line) | Alex Morgan |
| Sage ID / reference | `DEMO001` / `RVH-001` |
| Billing period | `2026-08-01` to `2026-08-31` |
| Line amount / total | **£2,546.43** |
| Funder | Anytown Council |
| Care home | River View House / Demo Care Ltd |

**Do not** create Jordan or run a second August bill until INV-0001 is verified. Payment status is set in Part 8.

---

# PART 6 — Jordan / INV-0002

## 6.1 Why sequential billing is required

The billing engine **groups** all eligible lines that share:

- Company  
- Care home  
- Funding authority  
- Invoice category  

into **one invoice per group**. Alex and Jordan at River View House with Anytown Council and General Care would **combine** into a single invoice if both exist at generate time.

A **second generate** for the same period skips Alex’s already-billed days and bills only Jordan.

## 6.2 Create Jordan (if not already done)

Follow §4.7 Resident B **after** INV-0001 exists.

## 6.3 Generate

Same as Part 5, same period and filters:

| Field | Value |
|-------|-------|
| Company | Demo Care Ltd |
| Care home | River View House |
| Invoice category | General Care |
| Period | `2026-08-01` – `2026-08-31` |

**Preview** should show **Jordan Blake** only (Alex skipped as already billed).  
**Generate** → expect **1 invoice**, total **£2,657.14**.

## 6.4 Verify INV-0002

| Check | Expected |
|-------|----------|
| Invoice number | `INV-0002` |
| Resident | Jordan Blake (`DEMO002` / `RVH-002`) |
| Period | August 2026 |
| Total | **£2,657.14** |
| Payment status | **Not paid** — leave unchanged for demo |

---

# PART 7 — Credit note CN-0001

**Scenario (demo narrative):** Partial adjustment on Jordan’s August invoice — 7 days not billable.

| Item | Demo target |
|------|-------------|
| Credit note number | `CN-0001` (first credit in tenant) |
| Source invoice | **INV-0002** |
| Resident | Jordan Blake |
| Credit period | `2026-08-25` – `2026-08-31` |
| Reason | `Partial period adjustment — 7 days not billable (demo)` |
| **Target credit amount** | **£600.00** *(per demo data guide)* |

## 7.1 UI steps

| Step | Navigation | Action |
|------|------------|--------|
| 1 | Open Jordan’s profile `/clients/{id}` | Note numeric **client id** from the browser URL (e.g. `/clients/2` → `2`) |
| 2 | **Billing** → **Credit Notes** → `/credit-notes` | — |
| 3 | **Client ID (optional)** | Enter Jordan’s numeric id |
| 4 | **Period start** | `2026-08-25` |
| 5 | **Period end** | `2026-08-31` |
| 6 | **Reason** | `Partial period adjustment — 7 days not billable (demo)` |
| 7 | Click **Preview** | Table: Invoice **INV-0002**, **Credit** column |
| 8 | If **Can generate** (no red exceptions), click **Generate** | Toast: *Credit note generated successfully.* |
| 9 | **Existing credit notes** table | **CN-0001**, original invoice **INV-0002**, total |

Optional: click **PDF** on the credit note row.

## 7.2 Verification

| Check | Expected |
|-------|----------|
| Credit note number | `CN-0001` |
| Linked invoice | INV-0002 |
| Reason | As entered |
| INV-0002 | Unchanged header total **£2,657.14** |
| INV-0002 payment status | Still **Not paid** |

### ⚠️ Application behaviour vs demo guide (£600)

`DEMO_DATA_PREPARATION_GUIDE.md` targets **£600.00** (7 × £600/week ÷ 7). The **Credit Notes** screen does **not** send per-line credit amounts to the API (`LineAmounts` exists in the API only). When `LineAmounts` is omitted, the API credits the **full remaining** balance on each matched invoice line.

The credit period filters which lines are *eligible* (service period overlaps the credit period); it does **not** prorate the credit amount by days.

**Before you click Generate:**

1. Confirm the Preview **Credit** column shows **£600.00** if you need to match the demo financial script.  
2. If Preview shows **£2,657.14** (full remaining), the current UI cannot produce a £600 partial credit — **do not** tell the client the amount is £600 without adjusting the narrative.

Net balance after a **£600** credit (finance story only): £2,657.14 − £600.00 = **£2,057.14** — the **Outstanding** report still shows invoice **header** totals (Part 11).

---

# PART 8 — Payment (INV-0001)

Payment is a **status flag** (`Paid` / `NotPaid`) on the invoice — not a separate payment ledger.

## 8.1 Demonstrate Paid on INV-0001

| Step | Action | Expected |
|------|--------|----------|
| 1 | `/invoices` → open **INV-0001** | Invoice detail loads |
| 2 | Click **Mark Paid** | Toast: *Payment status updated.* |
| 3 | Refresh if needed | Payment status badge **Paid** (green) |

## 8.2 Verify outstanding picture

| Invoice | Payment status | Outstanding for reports |
|---------|----------------|-------------------------|
| INV-0001 | **Paid** | Excluded from **Payment status / outstanding** report |
| INV-0002 | **Not paid** | Included |

There is no separate “outstanding amount” field after payment — use invoice **total** and payment badge. Optional on detail: **Mark unpaid** restores `Not paid`.

---

# PART 9 — PDF (INV-0002)

## 9.1 Download

| Step | Action |
|------|--------|
| 1 | `/invoices` → open **INV-0002** |
| 2 | Click **Download PDF** |
| 3 | PDF opens in a new browser tab |

| Expected |
|----------|
| File opens; header starts with `%PDF` if inspected |

## 9.2 What to point out on the PDF

| Area | Call out |
|------|----------|
| Organisation / company | **Demo Care Group** / **Demo Care Ltd** |
| Invoice metadata | **INV-0002**, date **31 Aug 2026**, due **30 Sep 2026** |
| Care home | River View House (`RIVER01`) |
| Funder / recipient | Anytown Council / template contact details |
| Line | Jordan Blake, `DEMO002`, service period August 2026, weekly rate **£600.00**, days, amount **£2,657.14** |
| Total | **£2,657.14** |
| Bank details | **Fictional** `00-00-00` / `00000000` — demo only |
| Footer | Payment terms from template |

**Business point:** PDF is a **snapshot** — later edits to the resident do not change this document.

Also confirm **INV-0001** PDF downloads once (paid example).

---

# PART 10 — Email simulation (INV-0002)

Development uses **`Email:Mode=Development`** — no SMTP; no message leaves the machine.

## 10.1 UI steps

| Step | Action |
|------|--------|
| 1 | Open **INV-0002** detail |
| 2 | Click **Email** |

## 10.2 What the UI shows

| Element | Text / behaviour |
|---------|------------------|
| Green info banner | `Send completed (or simulated in development).` |
| Toast | `Email queued/sent successfully.` *(easy to misread as real delivery)* |
| Invoice status badge | May change to **Sent** after refresh |

## 10.3 What to tell the client

> In this **Development** environment, email is **simulated**. The workflow completes, the invoice can show **Sent**, and the action is audited — but **no email is delivered** to any inbox. Production will use real SMTP.

**Do not** describe simulated delivery as actual delivery.

Optional operator check (not during client screen share):  
`docker compose logs api | Select-String "EMAIL SIMULATED"`

---

# PART 11 — Reports

**Reporting** → **Reports** → `/reports`

## 11.1 Outstanding invoices

| Field | Value |
|-------|-------|
| Report | `Payment status / outstanding` |
| From / To | Leave blank or wide range *(report excludes **Paid**)* |

Click **Run**.

| Expected | Notes |
|----------|-------|
| **INV-0002** listed | Not paid |
| **INV-0001** absent | Paid |

**Known warning:** This report shows invoice **header totals**. It does **not** net **CN-0001**. INV-0002 may still show **£2,657.14** after a credit note — explain that credits are separate **CN** documents.

## 11.2 Invoices by client

| Field | Value |
|-------|-------|
| Report | `Invoices by client` |
| From | `2026-08-01` |
| To | `2026-08-31` |

Click **Run**.

| Expected rows |
|---------------|
| Alex Morgan → INV-0001 → **£2,546.43** |
| Jordan Blake → INV-0002 → **£2,657.14** |

**Presenter tip:** Empty grid usually means **date filters were not set**.

Optional: **CSV**, **Excel**, or **PDF** export buttons on the same page.

---

# PART 12 — Audit

**Administration** → **Audit** → `/audit` (TenantAdmin / Administrator).

| Step | Action |
|------|--------|
| 1 | Open `/audit` | Recent events listed (newest first) |
| 2 | Optional: filter **Entity type** e.g. `Invoice`, `CreditNote`, `Client` → **Filter** |

## Demo actions that should appear (wording may vary)

| Entity | Typical actions |
|--------|-----------------|
| CreditNote | Generate CN-0001 |
| Invoice | Create INV-0001, INV-0002; Update payment status; Send (simulated email) |
| Client | Create Alex, Jordan |
| ClientFundingContract / rates | Contract and rate creates |
| InvoiceTemplate, NominalCode, FundingAuthority, CareHome, Company | Master data creates |
| ApplicationUser | Demo Viewer (if created) |
| Tenant | Demo Care Group provisioned (if audited at platform level) |

**Demo line:** *Every significant action is recorded — who did what and when.*

---

# PART 13 — ReadOnly (optional)

Skip if short on time.

| Step | Action | Expected |
|------|--------|----------|
| 1 | Create user: **Administration** → **Users** → `/users` | |
| | Email | `demo-viewer@example.com` |
| | Display name | `Demo Viewer` |
| | Role | `ReadOnly` |
| | Password | `<DEMO_VIEWER_PASSWORD>` |
| 2 | Sign out TenantAdmin; complete first-login password change for viewer if required | |
| 3 | Log in as viewer | Dashboard loads |
| 4 | Open a **Client** profile and **INV-0002** | Data visible |
| 5 | Confirm no **Save**, **Mark Paid**, **Generate**, or **Add Client** | Writes blocked / buttons hidden |
| 6 | Sign out; log back in as TenantAdmin | |

---

# PART 14 — Exact client presentation flow (~30 minutes)

Story: *“Let’s follow a resident through the care organisation’s financial workflow.”*

| # | WHAT I DO | WHAT I SAY | WHAT THE CLIENT SHOULD UNDERSTAND |
|---|-----------|------------|-----------------------------------|
| 1 | Open demo; log in as TenantAdmin | This system supports residents, funding, invoicing, payments, and reporting in one place. | One system for finance and operations. |
| 2 | Point to **Demo Care Group** in nav; glance at **Dashboard** | You always work inside your organisation’s data. | Multi-tenant isolation. |
| 3 | **Administration** → **Organisation Settings** | Here are currency, invoice numbering, and payment terms. | Configurable for their model. |
| 4 | **Operations** → **Clients** → **Alex Morgan** → **Details** + **Funding contracts** + **Rate history** | Alex lives at River View House; the council funds care under a contract and weekly rate. | Resident record drives billing. |
| 5 | Briefly show **Anytown Council** under Funding Authorities (optional) | Funders are set up once and reused. | Master data supports many residents. |
| 6 | **Billing** → **Invoices** → **INV-0001** | August billing for Alex — amount from the contract rate. | Invoices are generated from contracts, not spreadsheets. |
| 7 | **Download PDF** on INV-0002 (or show pre-opened PDF) | This is what the funder receives. Bank details here are fictional for the demo. | Professional invoice output. |
| 8 | Show INV-0001 **Paid**, INV-0002 **Not paid** | Finance tracks collected vs outstanding. | Simple payment tracking. |
| 9 | **Billing** → **Credit Notes** → **CN-0001** | Jordan was overcharged for part of August — we issue a credit without rewriting the original invoice. | Adjustments are controlled and traceable. |
| 10 | INV-0002 → **Email**; read green banner aloud | Email is **simulated** today — workflow and audit only. | No false claim of delivery. |
| 11 | **Reporting** → **Reports** → outstanding + invoices by client (August dates) | Management sees exposure and revenue by resident. | Reporting for finance review. |
| 12 | **Administration** → **Audit** | Every important action is logged. | Accountability. |
| 13 | Close: ask about workflows, reports, billing rules, roles | Billing rules are implemented for demo; **finance sign-off is pending**. | Honest path to production. |

---

# PART 15 — 15-minute version

| Minutes | WHAT I DO | WHAT I SAY (short) |
|---------|-----------|-------------------|
| 1 | Login + Dashboard | One place for care billing from resident to report. |
| 1 | Alex profile + contract/rate | Resident + funder contract drives the invoice. |
| 4 | INV-0002 detail + **Download PDF** | Funder-facing invoice; demo bank details only. |
| 3 | Paid vs not paid + **CN-0001** | Collections and corrections. |
| 1 | **Email** on INV-0002 + simulation disclaimer | Workflow only — no real email. |
| 1 | **Outstanding** report | Who still owes money. |
| 1 | One question on priorities and billing rules | Capture feedback. |

Skip: platform provisioning, users, organisation deep-dive, audit (unless high priority).

---

# PART 16 — Pre-demo checklist

### Infrastructure

- [ ] Docker running  
- [ ] API healthy (`GET http://localhost:5092/health/live` → Healthy)  
- [ ] Frontend healthy (http://localhost:4200 → login page)  
- [ ] Database healthy (`GET http://localhost:5092/health/ready` → Healthy)  

### Organisation

- [ ] Demo Care Group exists  
- [ ] Existing Organisation inactive  

### Users

- [ ] TenantAdmin works  
- [ ] Password changed  

### Data

- [ ] Alex  
- [ ] Jordan  
- [ ] Contracts  
- [ ] INV-0001  
- [ ] INV-0002  
- [ ] CN-0001  

### Financial

- [ ] INV-0001 = £2,546.43  
- [ ] INV-0002 = £2,657.14  
- [ ] CN-0001 = £600.00 *(confirm in Preview/Existing credit notes — see Part 7 warning)*  

### Presentation

- [ ] PDF works  
- [ ] Email simulation works  
- [ ] Reports work  
- [ ] Audit works  

### Security

- [ ] No real PII  
- [ ] No passwords visible  
- [ ] No secrets visible  
- [ ] No Azure/production configuration shown  

---

# PART 17 — Recovery during demo

**During the client session:** prefer refresh, restart single service, or switch to **pre-built** invoices — **no** `docker compose down -v`.

| Symptom | Safe recovery |
|---------|----------------|
| API not responding | `docker compose ps`; `docker compose restart api`; wait for healthy; refresh browser |
| Frontend not responding | `docker compose restart web`; wait 30–90s; refresh |
| Database unavailable | `/health/ready` Unhealthy — wait 1–2 min; `docker compose logs sql`; do not drop DB during demo |
| Login failure | Confirm TenantAdmin email; wait 1 min if rate-limited (10 attempts/min/IP); check password manager |
| Stale browser session | Sign out; clear `localStorage` key `carehome.auth`; log in again |
| PDF failure | `docker compose logs api`; retry download; use a previously saved PDF only if needed |
| Wrong tenant / 403 on business pages | Logged in as **PlatformAdmin** — sign out; use **TenantAdmin** |
| Billing `ALREADY_FULLY_BILLED` | Open existing INV-0001/0002 — do not regenerate live |
| Live generate fails | Navigate to `/invoices` — continue with pre-built data; explain setup was done earlier |

**After the client leaves** (isolated demo machine only): full reset per Part 1.

---

# Final Go / No-Go

🟢 **GO** — all critical checks pass: health endpoints Healthy, TenantAdmin dashboard, INV-0001/INV-0002 amounts and payment states correct, PDF and simulated email work, reports and audit usable.

🟡 **CONDITIONAL** — minor issues remain (e.g. credit amount preview ≠ £600 but narrative adjusted; toast wording on email; outstanding report vs credit note explained).

🔴 **NO-GO** — a critical workflow fails: cannot log in as TenantAdmin, cannot open invoices, wrong organisation active, sequential billing produced wrong invoice numbers, API/SQL not healthy, or simulated email fails in Development.

---

*Runbook aligned to repository UI routes and behaviour as verified against source (September 2026). No application code or configuration was modified.*

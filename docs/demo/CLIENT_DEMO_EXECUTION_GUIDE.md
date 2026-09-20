# Client Demo — Final Step-by-Step Execution Guide

**Purpose:** Literal operator procedure. Open this document beside the application and follow in order.  
**Environment:** Local Docker Compose · `Development` · simulated email · fictional data · tenant **Demo Care Group** only  
**Application base URL:** http://localhost:4200  
**API (operator checks only — not on client screen):** http://localhost:5092  

**UI naming (use the app, not old doc labels):**

| Older docs may say | Application shows |
|--------------------|-------------------|
| Residents | **Operations → Clients** (`/clients`) |
| Funding tab | **Funding contracts** on client profile |
| Invoice “Finalized” | Status badge **Generated** (+ separate **Paid** / **Not paid**) |

**Password rule:** Never show passwords on screen during the client demo. Use your password manager.

| Account | Email | Password |
|---------|-------|----------|
| PlatformAdmin (prep only) | `admin@localhost` | `<use password stored in password manager>` (Development PlatformAdmin from configured development credentials) |
| TenantAdmin (main demo) | `demo-admin@example.com` | `<use password stored in password manager>` (set at org create → `/change-password` during Phase A) |
| ReadOnly (optional) | `demo-viewer@example.com` | `<use password stored in password manager>` |

**Do not run** `docker compose down -v` after demo data is prepared (wipes SQL + PDF volumes).

---

# PHASE A — PREPARE DEMO ENVIRONMENT (before the client arrives)

Complete Phase A on a **rehearsal day** (allow 60–90 minutes for full data entry). Re-run **Phase A final check** immediately before the client joins.  
**The client demonstration (Phase B) must not require creating organisations, residents, or invoices.**

---

## STEP 1 — Check Docker

### Screen

PowerShell (private window — not shared with client)

### URL

N/A (terminal)

### Account

N/A

### Username

N/A

### Password

N/A

### Action

1. Open **PowerShell** (private).
2. Change directory to the repository root (folder containing `docker-compose.yml`).
3. Run:

```powershell
docker compose ps
```

4. Run:

```powershell
Invoke-RestMethod http://localhostNhL%WU5#$YhPb@:5092/health/live
Invoke-RestMethod http://localhost:5092/health/ready
```

*(Alternatively open in a browser: `http://localhost:5092/health/live` and `http://localhost:5092/health/ready`.)*

### Enter

| Item | Value |
|------|-------|
| Command | `docker compose ps` |
| Health URLs | `http://localhost:5092/health/live` and `http://localhost:5092/health/ready` |

### Expected result

| Container | Expected state |
|-----------|----------------|
| `carehome-sql` | **healthy** |
| `carehome-api` | **healthy** |
| `carehome-web` | **running** (Compose may not show a healthcheck for web) |

Both health endpoints return status **Healthy**.

### Verify

- [ ] `carehome-sql` healthy  
- [ ] `carehome-api` healthy  
- [ ] `carehome-web` running  
- [ ] `/health/live` → Healthy  
- [ ] `/health/ready` → Healthy  

### If this fails

| Symptom | Action |
|---------|--------|
| Containers missing or exited | From repo root: `docker compose up -d --build`. Wait 1–3 minutes; re-run this step. **Do not** use `docker compose down -v` if demo data already exists. |
| API not Healthy | Wait 1–2 minutes for SQL; `docker compose logs api`; `docker compose restart api`; re-check health. |
| First-time clean stack | Only when you **intentionally** want empty DB: `docker compose down -v` then `docker compose up -d --build` (then continue from Step 2). |

### Continue when

All containers and both health checks pass.

### Checkpoint

**STOP** if not Healthy. **GO** when all checks pass.

---

## STEP 2 — Open application

### Screen

Care Home Back Office — Login

### URL

http://localhost:4200/login

### Account

None yet

### Username

N/A

### Password

N/A

### Action

1. Open http://localhost:4200/login in your browser.
2. *(Recommended)* Clear site data for `localhost:4200` or use a fresh profile (JWT key: `localStorage` → `carehome.auth`).

### Enter

N/A

### Expected result

Page title/branding: **Care Home Back Office** with **Email address**, **Password**, button **Sign in**.

### Verify

- [ ] Login form visible  
- [ ] URL is `/login`  

### If this fails

| Symptom | Action |
|---------|--------|
| Connection refused | Confirm `carehome-web` running (Step 1); wait 30–90s after web container start; retry. |
| Wrong page | Try http://localhost:4200 (should redirect to login). |

### Continue when

Login page loads correctly.

### Checkpoint

**STOP** if login page does not load. **GO** otherwise.

---

## STEP 3 — Login as Platform Administrator

### Screen

Login → Platform **Organisations**

### URL

http://localhost:4200/login → after sign-in: http://localhost:4200/platform/tenants

### Account

PlatformAdmin

### Username

`admin@localhost`

### Password

`<use password stored in password manager>`  
*(Development PlatformAdmin — from configured development credentials / `Seed:AdminPassword` in Development config.)*

### Action

1. Enter **Email address:** `admin@localhost`.
2. Enter PlatformAdmin password from password manager.
3. Click **Sign in**.

### Enter

| Field | Value |
|-------|-------|
| Email address | `admin@localhost` |
| Password | From password manager |

### Expected result

Side navigation shows **Organisations** only (platform scope). You can open **Organisations** / `/platform/tenants`.

You **cannot** use this account for the main client workflow (no tenant Dashboard, Clients, or Billing).

### Verify

- [ ] Nav shows **Organisations**  
- [ ] `/companies` or `/clients` would return forbidden if attempted  

### If this fails

| Symptom | Action |
|---------|--------|
| Invalid login | Confirm email; wait 1 minute (rate limit); verify password manager entry for Development PlatformAdmin. |
| Full tenant menu appears | Wrong user — sign out and use `admin@localhost`. |

### Continue when

Platform **Organisations** list is available.

### Checkpoint

**GO** — do **not** use PlatformAdmin for Phase B demo narrative.

---

## STEP 4 — Create Demo Care Group

### Screen

Add organisation → Organisation created (success)

### URL

http://localhost:4200/platform/tenants/new

### Account

PlatformAdmin

### Username

`admin@localhost`

### Password

*(Still logged in — no re-entry)*

### Action

1. Side nav → **Organisations** or open http://localhost:4200/platform/tenants.
2. Click **Add organisation**.
3. Complete every required field below.
4. Click **Save**.

### Enter

| Field | Exact value |
|-------|-------------|
| Name | `Demo Care Group` |
| Trading name | `Demo Care Group` |
| Address | `1 Demo Lane, Anytown, AN1 2BC` |
| Phone | `01234 567890` |
| Email | `info@demo-care-group.example` |
| Active | ✓ checked |
| Admin email | `demo-admin@example.com` |
| Admin display name | `Demo Administrator` |

### Expected result

- Screen **Organisation created**.
- Message that login details were sent to `demo-admin@example.com`.
- In **Development**, a banner shows **Email is simulated in Development. Temporary password:** …  
- **Copy the temporary password to your password manager immediately.**

**SAVE THIS PASSWORD SECURELY. Do not expose it on screen during the client demo.**

4. Click **Back to organisations** when done.

### Verify

- [ ] Success screen appeared  
- [ ] Temporary password saved in password manager  

### If this fails

| Symptom | Action |
|---------|--------|
| Save error / validation | Fix highlighted fields; retry. |
| Duplicate org name | If **Demo Care Group** already exists from prior rehearsal, skip create and verify Step 5 instead. |
| Any other failure | **STOP** — do not continue until organisation is created. |

### Continue when

**Demo Care Group** exists and temporary TenantAdmin password is stored.

### Checkpoint

**STOP** if organisation creation fails. **GO** when password is saved.

---

## STEP 5 — Verify Demo Care Group

### Screen

Organisations list

### URL

http://localhost:4200/platform/tenants

### Account

PlatformAdmin

### Username

`admin@localhost`

### Password

N/A

### Action

1. Open the organisation list.
2. Locate **Demo Care Group**.
3. Confirm status **Active**.
4. Record TenantAdmin email: **`demo-admin@example.com`**.

### Enter

N/A

### Expected result

| Item | Expected |
|------|----------|
| Organisation | **Demo Care Group** |
| Status | **Active** |
| TenantAdmin email | `demo-admin@example.com` (created with org) |

### Verify

- [ ] **Demo Care Group** exists  
- [ ] **Active**  
- [ ] TenantAdmin **`demo-admin@example.com`** exists (via org create)  
- [ ] Temporary password saved securely  

### If this fails

Missing org → return to Step 4. Wrong status → **Edit** → check **Active** → **Save**.

### Continue when

All four verify items checked.

### Checkpoint

**STOP CHECKPOINT** — do not continue until:

- [ ] Demo Care Group exists  
- [ ] Active  
- [ ] TenantAdmin exists  
- [ ] Temporary password saved securely  

---

## STEP 6 — Deactivate Existing Organisation

### Screen

Edit organisation → Organisations list

### URL

http://localhost:4200/platform/tenants → **Edit** on **Existing Organisation** → `/platform/tenants/{id}`

### Account

PlatformAdmin

### Username

`admin@localhost`

### Password

N/A

### Action

1. On **Organisations** list, find **Existing Organisation** (migration placeholder — tenant Id=1).
2. Click **Edit**.
3. Uncheck **Active**.
4. Click **Save**.
5. Return to list.

### Enter

| Field | Value |
|-------|-------|
| Active | **OFF** (unchecked) |

### Expected result

**Existing Organisation** shows status **Inactive** in the list.

**Do NOT delete it. Do NOT create demo data in Existing Organisation.**

### Verify

- [ ] **Demo Care Group** = **Active**  
- [ ] **Existing Organisation** = **Inactive**  

### If this fails

Save blocked → retry; confirm you are PlatformAdmin.

### Continue when

Both organisations show correct active flags.

### Checkpoint

**STOP CHECKPOINT:**

- [ ] Demo Care Group = Active  
- [ ] Existing Organisation = Inactive  

---

## STEP 7 — Logout PlatformAdmin

### Screen

Any platform screen → Login

### URL

After sign out: http://localhost:4200/login

### Account

PlatformAdmin → none

### Username

N/A

### Password

N/A

### Action

1. Header → user chip (avatar) → menu → **Sign out**.

### Expected result

Login page at `/login`.

### Verify

- [ ] Signed out  
- [ ] Login form visible  

### If this fails

Clear `localStorage` key `carehome.auth`; reload `/login`.

### Continue when

On login page.

### Checkpoint

**GO**

---

## STEP 8 — Login TenantAdmin (first time)

### Screen

Login → Set your password

### URL

http://localhost:4200/login → redirect to http://localhost:4200/change-password

### Account

TenantAdmin (first login)

### Username

`demo-admin@example.com`

### Password

`<use password stored in password manager>` *(temporary password from Step 4 organisation create screen)*

### Action

1. **Email address:** `demo-admin@example.com`.
2. **Password:** temporary password from Step 4.
3. Click **Sign in**.

### Expected result

Redirect to **Set your password** at `/change-password` (forced first-login change).

### Verify

- [ ] URL is `/change-password`  
- [ ] Not `/forbidden`  

### If this fails

Wrong password → use saved temporary password from Step 4. Rate limited → wait 1 minute.

### Continue when

Password change page loads.

### Checkpoint

**GO**

---

## STEP 9 — Complete forced password change

### Screen

Set your password

### URL

http://localhost:4200/change-password

### Account

TenantAdmin

### Username

`demo-admin@example.com`

### Password

| Field | Value |
|-------|-------|
| Current / temporary password | Temporary password from Step 4 |
| New password | `<use password stored in password manager>` *(choose final TenantAdmin password — UI requires ≥12 chars, upper, lower, number, symbol)* |
| Confirm new password | Same as new password |

### Action

1. Enter fields above.
2. Click **Save password and continue**.

### Expected result

Full tenant app shell: side nav **Dashboard**, **Operations**, **Billing Setup**, **Billing**, **Reporting**, **Administration**. Brand subtitle: **Demo Care Group**.

### Verify

- [ ] Not stuck on `/change-password`  
- [ ] Side nav shows **Demo Care Group**  

### If this fails

Password policy error → strengthen password per UI rules.

### Continue when

Dashboard-capable shell loads.

### Checkpoint

**GO** — complete this **before** the client arrives, **not** during Phase B.

---

## STEP 10 — Verify Demo Care Group tenant

### Screen

Dashboard + Organisation Settings

### URL

http://localhost:4200/dashboard  
http://localhost:4200/settings/organisation

### Account

TenantAdmin

### Username

`demo-admin@example.com`

### Password

`<use password stored in password manager>` *(final TenantAdmin password)*

### Action

1. Confirm side nav brand subtitle **Demo Care Group**.
2. **Administration** (expand) → **Organisation Settings**.
3. Scroll and **read only** — do not change prefixes.

### Enter

N/A (verify only)

### Expected result

| Setting | Expected |
|---------|----------|
| Organisation name | **Demo Care Group** |
| Currency | GBP (£) |
| Invoice prefix | `INV-` |
| Credit note prefix | `CN-` |
| Payment terms | **30** days |

### Verify

- [ ] **Demo Care Group** in nav  
- [ ] `/companies` and `/clients` load (not 403)  
- [ ] Prefixes and terms match table  

### If this fails

403 on business routes → sign out; confirm not logged in as PlatformAdmin.

### Continue when

Tenant context and settings confirmed.

### Checkpoint

**GO**

---

## STEP 11 — Create Demo Care Ltd

### Screen

Add Company → Companies list

### URL

http://localhost:4200/companies/new → save → http://localhost:4200/companies

### Account

TenantAdmin

### Action

1. **Operations** (expand) → **Companies**.
2. Click **Add Company**.
3. Enter company name.
4. Click **Save**.

### Enter

| Field | Value |
|-------|-------|
| Company name | `Demo Care Ltd` |

### Expected result

**Demo Care Ltd** appears on `/companies`.

### Verify

- [ ] Company listed  

### If this fails

Validation error → fill required fields; retry.

### Continue when

Company exists.

### Checkpoint

**GO**

---

## STEP 12 — Create River View House

### Screen

Add Care Home → Care Homes list

### URL

http://localhost:4200/care-homes/new → http://localhost:4200/care-homes

### Account

TenantAdmin

### Action

1. **Operations** → **Care Homes**.
2. Click **Add Care Home** (navigates to new form).
3. Enter fields.
4. Click **Save**.

### Enter

| Field | Value |
|-------|-------|
| Company | `Demo Care Ltd` |
| Care home code | `RIVER01` |
| Care home name | `River View House` |
| Bed capacity | `24` |

### Expected result

Table row: **RIVER01**, **River View House**, company **Demo Care Ltd**, capacity **24**.

### Verify

- [ ] Row visible on `/care-homes`  

### If this fails

Company missing → complete Step 11.

### Continue when

River View House exists.

### Checkpoint

**GO**

---

## STEP 13 — Create Anytown Council

### Screen

Add Authority → Funding Authorities list

### URL

http://localhost:4200/funding-authorities/new → http://localhost:4200/funding-authorities

### Account

TenantAdmin

### Action

1. **Billing Setup** (expand) → **Funding Authorities**.
2. Click **Add Authority**.
3. Enter fields.
4. Save.

### Enter

| Field | Value |
|-------|-------|
| Code | `ATC-COUNCIL` |
| Name | `Anytown Council` |
| Type | `Council` |
| Contact name | `Adult Social Care Billing` |
| Phone | `01234 567001` |
| Email | `billing@anytown-council.example` |
| Address | `Adult Social Care, Anytown Council, Civic Centre, Anytown, AN1 1AA` |
| Billing frequency | `Monthly` |
| Active | ✓ (if shown) |

### Expected result

**Anytown Council** in list.

### Verify

- [ ] Authority listed  

### Continue when

Authority exists.

### Checkpoint

**GO**

---

## STEP 14 — Billing master data (nominal, categories, template)

Complete **before** Alex’s invoice. Order below matches dependencies.

### 14A — Nominal code

**URL:** `/nominal-codes/new`

| Field | Value |
|-------|-------|
| Code | `4000` |
| Name | `Care income` |

Click **Save**. Expect code on `/nominal-codes`.

### 14B — Invoice categories (verify only)

**URL:** `/invoice-categories`

Confirm present — **do not recreate:**

| Code | Name |
|------|------|
| `GENERAL_CARE` | General Care |
| `MISC` | Miscellaneous |

### 14C — Invoice template

**URL:** `/invoice-templates` — scroll to **Add category default template**

| Field | Value |
|-------|-------|
| Name | `General Care Template` |
| Category | `General Care` |
| Footer | `Payment due within 30 days. Bank details are fictional for demonstration purposes only.` |
| Bank account | `Demo Care Group Client Account` |
| Sort code | `00-00-00` |
| Account number | `00000000` |
| Contact name | `Demo Finance Team` |
| Contact email | `finance@demo-care-group.example` |

Click **Save template**.

### Verify (Step 14)

- [ ] Nominal **4000** exists  
- [ ] Categories verified  
- [ ] Template saved  

### Checkpoint

**GO**

---

## STEP 15 — Create Alex Morgan

### Screen

Add Client → Client profile

### URL

http://localhost:4200/clients/new → `/clients/{id}`

### Account

TenantAdmin

### Action

1. **Operations** → **Clients** → **Add Client**.
2. Enter all fields.
3. Click **Save**.

### Enter

| Field | Value |
|-------|-------|
| Care home | `Demo Care Ltd - River View House` or **River View House** |
| Sage ID | `DEMO001` |
| Client reference number | `RVH-001` |
| Title | `Ms` |
| First name | `Alex` |
| Last name | `Morgan` |
| Date of birth | `1948-03-12` |
| Care type | `Residential` |
| Admission date | `2026-04-01` |
| Email address | `alex.morgan@example.com` |
| Phone | `07700 900101` |
| Notes | `Previous address (fictional): 14 Willow Close, Anytown, AN1 3DE. Demo resident — not real.` |

### Expected result

Profile opens; header **Alex Morgan**; status **Current**; subtitle includes **RVH-001 · DEMO001 · River View House**.

### Verify

- [ ] Alex exists  
- [ ] **Jordan Blake does not exist yet** (required for separate INV-0001)  

### Checkpoint

**GO**

---

## STEP 16 — Create Alex funding contract

### Screen

Client profile → tab **Funding contracts**

### URL

`/clients/{id}` (Alex)

### Action

1. Open tab **Funding contracts**.
2. Add contract fields.
3. Click **Save contract**.

### Enter

| Field | Value |
|-------|-------|
| Authority | `Anytown Council` |
| Category | `General Care` |
| Nominal | `4000` |
| Start | `2026-04-01` |
| End | *(blank)* |

### Expected result

Contract status **Active**.

### Verify

- [ ] Active contract on profile  

### Checkpoint

**GO**

---

## STEP 17 — Create Alex rate

### Screen

Client profile → tab **Rate history** (or rate section on funding tab)

### URL

`/clients/{id}` (Alex)

### Action

1. **Add rate**.
2. Enter fields.
3. Click **Add rate**.

### Enter

| Field | Value |
|-------|-------|
| Contract | `Anytown Council / General Care` |
| From | `2026-04-01` |
| To | *(blank)* |
| Frequency | `Weekly` |
| Amount | `575.00` |

### Expected result

Rate **£575.00** weekly visible.

### Verify

- [ ] Rate displayed  

### Checkpoint

**GO**

---

## STEP 18 — Verify billing setup (Alex preview)

### Screen

Billing Workspace — Step 1–4

### URL

http://localhost:4200/billing

### Action

1. **Billing** → **Billing Workspace**.
2. **Step 1 — Select billing scope** — enter scope below.
3. Click **Preview** only (do not Generate yet).

### Enter

| Field | Value |
|-------|-------|
| Company | `Demo Care Ltd` |
| Care home | `River View House` *(not “All care homes”)* |
| Invoice category | `General Care` |
| Period start | `2026-08-01` |
| Period end | `2026-08-31` |

### Expected result

- **Step 2 — Preview** lists **Alex Morgan** only.  
- **Step 4** total / line amount **£2,546.43**.  
- Banner **Ready to generate** (or equivalent).

*(DEMO ONLY — NOT FINANCE APPROVED: proration formula for demonstration.)*

### Verify

- [ ] Alex only (no Jordan)  
- [ ] **£2,546.43**  

### If this fails

Fix contract/rate/master data (Steps 11–17). **STOP** if amount wrong.

### Checkpoint

**STOP** if preview ≠ **£2,546.43** for Alex-only. **GO** when correct.

---

## STEP 19 — Generate Alex invoice

### Screen

Billing Workspace

### URL

http://localhost:4200/billing

### Action

1. Same scope as Step 18.
2. Click **Preview**.
3. Click **Generate**.

### Expected result

Green banner similar to: `Generated 1 invoice(s). Total £2,546.43.`

### Verify

- [ ] One invoice generated  
- [ ] Total **£2,546.43**  

### Checkpoint

**GO**

---

## STEP 20 — Verify INV-0001

### Screen

Invoice detail

### URL

http://localhost:4200/invoices → open **INV-0001**

### Action

Open **INV-0001** and read header, badges, lines.

### Expected result

| Check | Expected |
|-------|----------|
| Invoice number | `INV-0001` |
| Resident | **Alex Morgan** (`DEMO001` / `RVH-001`) |
| Period | `2026-08-01` – `2026-08-31` (August 2026) |
| Total | **£2,546.43** |
| Status badge | **Generated** |
| Payment badge | **Not paid** |
| Funder | Anytown Council |
| Care home | River View House / Demo Care Ltd |

### Verify

All rows in table above.

### If this fails

Wrong total or wrong resident → **STOP**. Do not create Jordan until fixed.

### Checkpoint

**CRITICAL BILLING CHECKPOINT — STOP**

Do **not** create Jordan until **all** are true:

- [ ] Invoice **INV-0001**  
- [ ] Resident **Alex Morgan**  
- [ ] Period **August 2026**  
- [ ] Amount **£2,546.43**  
- [ ] Status **Generated**  
- [ ] Payment **Not paid**  

---

## STEP 21 — Verify £2,546.43 (duplicate confirmation)

Same as Step 20 — confirm header total and line total both **£2,546.43**.

### Checkpoint

**GO** only if amount exact.

---

## STEP 22 — Download and verify INV-0001 PDF

### Screen

INV-0001 detail

### URL

`/invoices/{id}` for INV-0001

### Action

Click **Download PDF**.

### Expected result

PDF opens in new tab; total **£2,546.43**; invoice **INV-0001**; fictional bank **00-00-00** / **00000000**.

### Verify

- [ ] PDF opens  
- [ ] Amount matches  

### If this fails

Retry download; check API healthy (Step 1).

### Checkpoint

**GO** — **DO NOT create Jordan until PDF OK.**

---

## STEP 23 — Create Jordan Blake

### Screen

Add Client → profile

### URL

http://localhost:4200/clients/new

### Action

**Add Client** → enter fields → **Save**.

### Enter

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

### Expected result

Profile **Jordan Blake**; **RVH-002 · DEMO002**.

**Note client id** from URL `/clients/{id}` (e.g. `2`) for credit note Step 33.

### Checkpoint

**GO**

---

## STEP 24 — Create Jordan funding contract

### URL

Jordan `/clients/{id}` → **Funding contracts**

### Enter

| Field | Value |
|-------|-------|
| Authority | `Anytown Council` |
| Category | `General Care` |
| Nominal | `4000` |
| Start | `2026-05-15` |
| End | *(blank)* |

**Save contract** → **Active**.

### Checkpoint

**GO**

---

## STEP 25 — Create Jordan rate

### Enter

| Field | Value |
|-------|-------|
| Contract | `Anytown Council / General Care` |
| From | `2026-05-15` |
| To | *(blank)* |
| Frequency | `Weekly` |
| Amount | `600.00` |

**Add rate** → **£600.00**/week.

### Checkpoint

**GO**

---

## STEP 26 — Preview Jordan August billing

### URL

http://localhost:4200/billing

### Enter (Step 1)

Same as Step 18: Demo Care Ltd, River View House, General Care, `2026-08-01` – `2026-08-31`.

### Action

Click **Preview**.

### Expected result

- **Jordan Blake** only (Alex skipped as already billed).  
- Line total **£2,657.14**.  
- Ready to generate.

### Verify

- [ ] Jordan only  
- [ ] **£2,657.14**  

### Checkpoint

**STOP** if wrong. **GO** when correct.

---

## STEP 27 — Generate Jordan invoice

### Action

**Preview** → **Generate** (same scope).

### Expected result

`Generated 1 invoice(s). Total £2,657.14.`

### Checkpoint

**GO**

---

## STEP 28 — Verify INV-0002

### URL

http://localhost:4200/invoices → **INV-0002**

### Expected result

| Check | Expected |
|-------|----------|
| Invoice number | `INV-0002` |
| Resident | **Jordan Blake** |
| Period | August 2026 |
| Total | **£2,657.14** |
| Status | **Generated** |
| Payment | **Not paid** |

### Checkpoint

**JORDAN CHECKPOINT — STOP**

Verify all before continuing:

- [ ] **INV-0002**  
- [ ] **Jordan Blake**  
- [ ] **August 2026**  
- [ ] **£2,657.14**  
- [ ] Payment **Not paid**  

---

## STEP 29 — Verify £2,657.14

Confirm header and line **£2,657.14**.

### Checkpoint

**GO**

---

## STEP 30 — Download and verify INV-0002 PDF

### Action

**Download PDF** on INV-0002.

### Expected result

Valid PDF; total **£2,657.14**; **INV-0002**.

### Verify

- [ ] PDF opens  

### Checkpoint

**GO**

---

## STEP 31 — Payment status preparation (rehearsal)

**For Phase B** you will **Mark Paid** on INV-0001 **live** in front of the client.  
Before the client joins, INV-0001 must show **Not paid** unless you will skip the live payment step.

### Option A — Recommended for standard Phase B script

1. Leave **INV-0001** as **Not paid** after generation.  
2. Leave **INV-0002** as **Not paid**.

### Option B — If you marked INV-0001 Paid during rehearsal to test reports

1. Open **INV-0001** → click **Mark unpaid**.  
2. Confirm badge **Not paid**.

### Verify both invoices

| Invoice | Payment status before client |
|---------|------------------------------|
| INV-0001 | **Not paid** (for live Mark Paid in Phase B) |
| INV-0002 | **Not paid** |

### Checkpoint

**GO**

---

## STEP 32 — Verify payment statuses (quick check)

Open INV-0001 and INV-0002; confirm badges match Step 31.

### Checkpoint

**GO**

---

## STEP 33 — Credit note workflow (understand + optional generate)

### Screen

Credit Notes workspace

### URL

http://localhost:4200/credit-notes

### Account

TenantAdmin

### Action

1. Note Jordan’s numeric **Client ID** from `/clients/{id}` URL.
2. **Billing** → **Credit Notes**.
3. Enter:

| Field | Value |
|-------|-------|
| Client ID (optional) | Jordan’s numeric id |
| Period start | `2026-08-25` |
| Period end | `2026-08-31` |
| Reason | `Partial period adjustment — 7 days not billable (demo)` |

4. Click **Preview**.
5. Read **Credit** column for **INV-0002**.

### OUTCOME A — Preview Credit = **£600.00**

1. If **Generate** is enabled and no blocking exceptions → click **Generate**.  
2. Expect toast *Credit note generated successfully.*  
3. **Existing credit notes** table → **CN-0001**, linked to **INV-0002**, total **£600.00**.  
4. Optional: **PDF** on that row.  
5. INV-0002 header total remains **£2,657.14**; payment still **Not paid**.

### OUTCOME B — Preview Credit = **£2,657.14** (full remaining)

1. **STOP. Do not click Generate.**  
2. The UI does **not** expose partial line amounts; the API defaults to full remaining balance when `lineAmounts` is omitted.  
3. **Do not** use SQL/API hacks to force £600.  
4. For Phase B: use **Preview only** or show **Existing CN-0001** only if you got Outcome A on another machine — **never claim £600** unless Preview or CN list shows **£600.00**.

**Client-friendly wording if they ask (Outcome B):**

> “The credit note screen filters which invoice lines are in scope by period, but it doesn’t yet let us type a partial amount in the browser. The API supports partial credits, but this workspace doesn’t send per-line amounts — so Preview defaults to the full remaining balance on the line. We wouldn’t generate that in production without matching your finance rules; it’s on our enhancement list to expose partial amounts in the UI.”

### Verify

- [ ] You know which outcome you have  
- [ ] You will not generate full **£2,657.14** credit in front of client without explanation  

### Checkpoint

**GO** (either safe CN-0001 at £600 or documented limitation)

---

## STEP 34 — Verify reports

### URL

http://localhost:4200/reports

### 34A — Payment status / outstanding

| Field | Value |
|-------|-------|
| Report | `Payment status / outstanding` |
| From / To | Leave blank (or wide range) |

Click **Run**.

**Expected before client demo (both invoices Not paid):** **INV-0001** and **INV-0002** may both appear.  
**Expected after Phase B Mark Paid on INV-0001:** **INV-0002** only.

*(Outstanding report shows invoice **header** totals; does not net credit notes.)*

### 34B — Invoices by client

| Field | Value |
|-------|-------|
| Report | `Invoices by client` |
| From | `2026-08-01` |
| To | `2026-08-31` |

Click **Run**.

**Expected rows:**

| Client | Invoice | Amount |
|--------|---------|--------|
| Alex Morgan | INV-0001 | £2,546.43 |
| Jordan Blake | INV-0002 | £2,657.14 |

Empty grid → set August dates and **Run** again.

### Checkpoint

**GO**

---

## STEP 35 — Verify audit

### URL

http://localhost:4200/audit

### Action

1. **Administration** → **Audit**.
2. Optional: **Entity type** `Invoice`, `Client`, or `CreditNote` → **Filter**.

### Expected result

Recent entries for company/care home/client creates, invoice generates, etc. Subtitle: append-only records.

### Verify

- [ ] Demo actions visible  

### Checkpoint

**GO**

---

## STEP 36 — Email simulation verified (rehearsal)

### URL

INV-0002 detail `/invoices/{id}`

### Action

1. Open **INV-0002**.  
2. Click **Email** once (rehearsal).

### Expected result

- Green banner: **Send completed (or simulated in development).**  
- Toast: **Email queued/sent successfully.**  
- After refresh, status may show **Sent**.

**No real email leaves the machine** (`Email:Mode=Development`).

Optional off-screen: `docker compose logs api` and look for simulated email log — not during client share.

### Verify

- [ ] Simulation banner appeared  
- [ ] You will re-state simulation during Phase B  

### Checkpoint

**GO**

---

## STEP 37 — Optional ReadOnly user (Phase B optional segment)

### URL

http://localhost:4200/users

### Enter

| Field | Value |
|-------|-------|
| Display name | `Demo Viewer` |
| Email | `demo-viewer@example.com` |
| Role | `ReadOnly` |
| Password | Set in UI; store in password manager |

Complete first-login password change for viewer **before** client demo if using Step 20 in Phase B.

### Checkpoint

**OPTIONAL GO**

---

# PHASE A — FINAL CHECK (before client joins)

Check every item:

### Infrastructure

- [ ] Docker healthy (`carehome-sql`, `carehome-api` healthy; `carehome-web` running)  
- [ ] API healthy (`/health/live`, `/health/ready`)  
- [ ] Frontend healthy (`/login` loads)  

### Organisation

- [ ] **Demo Care Group** active  
- [ ] **Existing Organisation** inactive  

### Access

- [ ] TenantAdmin works (`demo-admin@example.com`)  
- [ ] Password change completed (not `/change-password` on login)  
- [ ] Side nav **Demo Care Group**  

### Master data

- [ ] **Demo Care Ltd**  
- [ ] **River View House** (`RIVER01`, 24 beds)  
- [ ] **Anytown Council**  
- [ ] Nominal **4000**, template **General Care Template**  

### Residents & billing

- [ ] **Alex Morgan** + contract + **£575.00**/week  
- [ ] **Jordan Blake** + contract + **£600.00**/week  
- [ ] **INV-0001** = **£2,546.43**, PDF works  
- [ ] **INV-0002** = **£2,657.14**, PDF works  
- [ ] **INV-0001** payment **Not paid** (for live Mark Paid in Phase B)  
- [ ] **INV-0002** payment **Not paid**  

### Presentation readiness

- [ ] Reports verified  
- [ ] Audit verified  
- [ ] Credit note behaviour understood (Outcome A or B)  
- [ ] Email simulation verified  
- [ ] No IDE, terminal, Docker, `.env`, or passwords on shared screen  

### Final verdict

- **GO** — all critical items pass.  
- **NO-GO** — cannot log in as TenantAdmin, wrong amounts, missing invoices, or API/SQL unhealthy.

---

# PHASE B — CLIENT DEMO — LIVE PRESENTATION

**IMPORTANT — during the client demo DO NOT:**

- Create organisations, residents, or invoices  
- Change configuration unnecessarily  
- Open Docker, terminal, source code, Azure, Key Vault, `.env`  
- Show passwords  
- Use real customer information  
- Click **Void** on invoices  
- Run `docker compose down -v`  

Everything must already exist from Phase A.

**Presenter account:** `demo-admin@example.com`  
**Password:** `<use password stored in password manager>`

---

# STEP 1 — Login as TenantAdmin

### Screen

Login

### URL

http://localhost:4200/login

### Account

TenantAdmin

### Username

`demo-admin@example.com`

### Password

`<use password stored in password manager>`

### Action

1. Enter email and password.  
2. Click **Sign in**.

### Show the client

- **Care Home Back Office** branding (**CH** mark).  

### Say

“Thank you for joining. Today I’ll show how your teams could manage residents and the full financial path around them — from organisation setup and funding contracts through billing, invoices, payments, adjustments, and reporting — all in one place. Everything you’ll see is fictional demo data on our local development environment, so we can explore the workflow safely.”

### Expected result

Full app shell; brand subtitle **Demo Care Group**; land on **Dashboard** (`/dashboard`). Not `/change-password`.

### Verify

- [ ] **Demo Care Group** in side nav  
- [ ] Dashboard loads  

### If this fails

**STOP** — sign out; clear `carehome.auth` if needed; confirm password manager; use Phase A recovery (refresh, re-login). Say calmly: “Let me refresh — the data is already in the system.”

### Continue when

Dashboard visible as TenantAdmin.

---

# STEP 2 — Verify Dashboard

### Screen

Dashboard

### URL

http://localhost:4200/dashboard

### Account

TenantAdmin

### Username

`demo-admin@example.com`

### Password

*(session active)*

### Action

1. Stay on Dashboard 30–60 seconds.  
2. Glance at KPI cards and **Recent invoices** table.

### Show the client

- **Care Homes**, **Current Clients**, **Available Beds**, **Outstanding Invoices** (count + £).  
- **Recent invoices** with links to **INV-0001** / **INV-0002** if populated.  

### Say

“This gives us a quick operational and financial view of the organisation.”

### Expected result

Demo data visible (non-zero KPIs if data ready; recent invoices listed).

### Verify

- [ ] KPIs / recent invoices show demo data  

### If this fails

If empty → Phase A incomplete (**STOP** off-screen after session). If slow → wait 5–10s; single refresh.

### Continue when

Data populated enough to tell the story.

---

# STEP 3 — Organisation Settings

### Screen

Organisation Settings

### URL

http://localhost:4200/settings/organisation

### Account

TenantAdmin

### Action

1. **Administration** (expand) → **Organisation Settings**.  
2. Scroll — **do not click Save** unless fixing a mistake off-hours.

### Show the client

- Name **Demo Care Group**  
- **Invoice prefix** `INV-`  
- **Credit note prefix** `CN-`  
- **Payment terms** 30 days  
- **Currency** GBP (£)  

### Say

“Each care group runs in its own isolated organisation. These settings control how money is presented — currency, how invoice and credit note numbers are prefixed, and standard payment terms.”

### Expected result

Settings display as above.

### Verify

- [ ] Values match rehearsal  
- [ ] No accidental edits  

### If this fails

Wrong tenant → signed in as PlatformAdmin (**STOP** — sign out → TenantAdmin).

### Continue when

Settings confirmed.

---

# STEP 4 — Care Home

### Screen

Care Homes list

### URL

http://localhost:4200/care-homes

### Action

1. **Operations** → **Care Homes**.  
2. Point to **River View House** row.

### Show the client

- **Code** `RIVER01`  
- **Care Home** **River View House**  
- **Company** **Demo Care Ltd**  
- **Capacity** **24**  

### Say

“Organisations typically have operating companies and the care homes themselves. River View House sits under Demo Care Ltd — that structure drives which company name appears on invoices.”

### Expected result

Row visible as above.

### Verify

- [ ] RIVER01 row present  

### If this fails

Navigate again from Operations menu.

### Continue when

Row shown.

---

# STEP 5 — Alex Morgan (client profile)

### Screen

Client profile — **Details**

### URL

http://localhost:4200/clients → open **Alex Morgan** → `/clients/{id}`

### Action

1. **Operations** → **Clients**.  
2. Open **Alex Morgan**.  
3. Tab **Details** (default).

### Show the client

- Header **Alex Morgan**  
- Subtitle **RVH-001 · DEMO001 · River View House**  
- Status **Current**  
- Care type **Residential**, admission **2026-04-01**  

### Say

“Every billing line ties back to a person in care. The resident record holds identity, placement, and references your finance system needs — here Sage ID and an internal reference — so invoices stay unambiguous.”

### Expected result

Profile matches demo data.

### Verify

- [ ] RVH-001, DEMO001 visible  

### Continue when

Profile displayed.

---

# STEP 6 — Alex funding contract and rate

### Screen

Client profile — **Funding contracts** / **Rate history**

### URL

Same Alex `/clients/{id}`

### Action

1. Tab **Funding contracts**.  
2. Show contract and **Rate history** (**£575.00** weekly).

### Show the client

- Authority **Anytown Council**  
- Category **General Care**  
- Nominal **4000**  
- Start **2026-04-01**, status **Active**  
- Rate **£575.00** weekly from **2026-04-01**  

### Say

“The council funds Alex under a contract for General Care, with a weekly rate and effective dates. When we run billing for a period, the system uses the active contract and rate — not a one-off spreadsheet cell. The August amount you’ll see is calculated for this demo — your finance team would still sign off the exact proration rules before production.”

### Expected result

Active contract and rate visible.

### Verify

- [ ] £575/week shown  

### Continue when

Funding visible.

---

# STEP 7 — Billing Preview (August 2026)

### Screen

Billing Workspace

### URL

http://localhost:4200/billing

### Action

1. **Billing** → **Billing Workspace**.  
2. **Step 1** — enter scope below.  
3. Click **Preview** only — **do not click Generate**.

### Enter

| Field | Value |
|-------|-------|
| Company | `Demo Care Ltd` |
| Care home | `River View House` |
| Invoice category | `General Care` |
| Period start | `2026-08-01` |
| Period end | `2026-08-31` |

### Show the client

- Steps 2–4 preview UI, or message that August is **already invoiced** / generation blocked / no remaining billable lines.

### Say

“This is where finance selects scope and previews what would be invoiced before anything is committed. August has already been invoiced for this home — that’s why there may be nothing left to generate. The invoices we’ll open next are the result of this same preview workflow during setup.”

*(If preview still showed lines in a fresh mistake scenario, do **not** generate live — open existing invoices instead.)*

### Expected result

You explained preview intent; **no duplicate invoices** created.

### Verify

- [ ] **Generate** was **not** clicked  

### If this fails

Billing error → open **INV-0001** instead; do not regenerate.

### Continue when

Preview step complete without generate.

---

# STEP 8 — Invoice INV-0001

### Screen

Invoice detail

### URL

http://localhost:4200/invoices → **INV-0001**

### Action

1. **Billing** → **Invoices**.  
2. Open **INV-0001**.

### Show the client

- **INV-0001**, badges **Generated** and **Not paid**  
- Total **£2,546.43**  
- Line **Alex Morgan**, period August 2026  
- Funder **Anytown Council**, references **DEMO001** / **RVH-001**  

### Say

“Here is Alex’s August invoice — produced from her contract and rate.”

### Expected result

Totals and resident match Phase A.

### Verify

- [ ] **£2,546.43**  

### Continue when

Invoice detail correct.

---

# STEP 9 — INV-0001 PDF

### Screen

INV-0001 detail → PDF tab

### URL

`/invoices/{id}` (INV-0001)

### Action

Click **Download PDF**.

### Show the client

- PDF layout, **INV-0001**, due date **30 Sep 2026** (30-day terms), line total **£2,546.43**, fictional bank details.

### Say

“This is the customer-facing document the funder receives. It’s a snapshot at generation time. Bank details here are fictional for the demo only.”

### Expected result

PDF opens; total matches screen.

### Verify

- [ ] PDF opens  
- [ ] Total matches  

### If this fails

Retry **Download PDF**; fallback to rehearsal copy only if needed — say “same document as in the system.”

### Continue when

PDF shown.

---

# STEP 10 — Mark INV-0001 Paid

### Screen

INV-0001 detail

### URL

`/invoices/{id}` (INV-0001)

### Action

1. Close PDF tab.  
2. Click **Mark Paid**.  
3. **Do not** click **Void**.

### Show the client

- Toast **Payment status updated.**  
- Payment badge **Paid** (green).  
- Status **Generated** unchanged.  
- Total still **£2,546.43**.  

### Say

“Payment status is tracked on the invoice itself — paid versus still outstanding — so collections can see what’s settled without maintaining a separate shadow spreadsheet. This is a status flag for workflow and reporting, not a full payment gateway.”

### Expected result

**Paid** badge visible.

### Verify

- [ ] **Paid** on INV-0001  

### Continue when

Payment updated.

---

# STEP 11 — Jordan Blake

### Screen

Client profile

### URL

http://localhost:4200/clients → **Jordan Blake**

### Action

Open **Jordan Blake** → **Details** briefly → **Funding contracts** / rates.

### Show the client

- **RVH-002 · DEMO002**  
- **Nursing**, admitted **2026-05-15**  
- Contract **Anytown Council**, rate **£600.00** weekly  

### Say

“The same model scales to every resident — Jordan is in nursing care at the same home with his own references and council contract, but a different weekly rate.”

### Expected result

Parallel structure visible.

### Verify

- [ ] Contract and £600 rate visible  

### Continue when

Brief profile shown.

---

# STEP 12 — Invoice INV-0002

### Screen

Invoice detail

### URL

http://localhost:4200/invoices → **INV-0002**

### Action

Open **INV-0002**.

### Show the client

- **INV-0002**, **Generated**, **Not paid**  
- Total **£2,657.14**, line **Jordan Blake**  

### Say

“Jordan’s August invoice is still outstanding — this is what finance would chase. We deliberately keep one paid and one unpaid example to show reporting and collections.”

### Expected result

**£2,657.14**, **Not paid**.

### Verify

- [ ] Amount and payment badge correct  

### Continue when

INV-0002 shown.

---

# STEP 13 — Credit Note

### Screen

Credit Notes

### URL

http://localhost:4200/credit-notes

### Action

**Path 1 — CN-0001 already exists from Phase A (Outcome A, £600):**

1. Scroll **Existing credit notes**.  
2. Show **CN-0001**, original invoice **INV-0002**, reason, total **£600.00**.  
3. Optional: **PDF** on row.

**Path 2 — No CN yet or Outcome B (£2,657.14 preview):**

1. Enter Client ID (optional), period `2026-08-25` – `2026-08-31`, reason `Partial period adjustment — 7 days not billable (demo)`.  
2. Click **Preview** only.  
3. Read **Credit** column.  
4. If Credit = **£2,657.14** → **DO NOT CLICK GENERATE.**

### Show the client

- Link between adjustment and **INV-0002**.  
- Preview **Credit** / **Remaining** columns if using Path 2.

### Say

“Jordan was billed for part of August that shouldn’t have been charged — we issue a credit note linked to the original invoice instead of editing history. Credits are separate controlled documents — the invoice header total stays as issued.”

If Outcome B and client asks about partial £600, use **exact wording**:

> “The credit note screen filters which invoice lines are in scope by period, but it doesn’t yet let us type a partial amount in the browser. The API supports partial credits, but this workspace doesn’t send per-line amounts — so Preview defaults to the full remaining balance on the line. We wouldn’t generate that in production without matching your finance rules; it’s on our enhancement list to expose partial amounts in the UI.”

Also note if relevant: Outstanding report still shows invoice **header** totals and does **not** net credit notes.

### Expected result

No false **£600** claim unless Preview or CN list shows **£600.00**.

### Verify

- [ ] Did not generate full credit without explanation  
- [ ] Did not claim £600 unless supported by Preview/CN list  

### If this fails

Stay on list or preview — do not SQL/API fix during demo.

### Continue when

Credit story told safely.

---

# STEP 14 — Email simulation (INV-0002)

### Screen

INV-0002 detail

### URL

`/invoices/{id}` (INV-0002)

### Action

Click **Email**.

### Show the client

- Green banner: **Send completed (or simulated in development).**  
- Toast **Email queued/sent successfully.**  
- Status may become **Sent** after refresh.

### Say

“Email delivery is **simulated** in today’s **Development** demonstration. The workflow completes, the system records the send, and audit captures it — but **no message leaves this machine**. Production will use your configured email service.”

### Expected result

Simulation banner shown; you stated clearly **no inbox delivery**.

### Verify

- [ ] Disclaimer spoken  
- [ ] Did not claim council received email  

### Continue when

Email step complete.

---

# STEP 15 — Outstanding Report

### Screen

Reports

### URL

http://localhost:4200/reports

### Action

1. Report: **Payment status / outstanding** (`outstanding`).  
2. Leave **From** / **To** blank unless narrowing.  
3. Click **Run**.

### Show the client

- Grid includes **INV-0002** / Jordan.  
- **INV-0001** absent (marked Paid in Step 10).  

### Say

“Finance can see everything still marked unpaid. Alex’s invoice dropped off once we marked it paid; Jordan’s remains because it’s still outstanding.”

### Expected result

Unpaid invoice(s) only.

### Verify

- [ ] INV-0002 listed  
- [ ] INV-0001 not listed  

### If this fails

Empty → widen dates; confirm payment badges on invoices.

### Continue when

Report matches payment flags.

---

# STEP 16 — Invoices by Client

### Screen

Reports

### URL

http://localhost:4200/reports

### Action

1. Report: **Invoices by client** (`invoices-by-client`).  
2. **From** `2026-08-01`, **To** `2026-08-31`.  
3. Click **Run**.

### Show the client

- Alex Morgan → **INV-0001** → **£2,546.43**  
- Jordan Blake → **INV-0002** → **£2,657.14**  

### Say

“Management can see revenue by resident for a period — useful for home managers and finance reviewing who was billed and how much.”

### Expected result

Both rows present.

### Verify

- [ ] Both residents and amounts  

### If this fails

Empty grid → set August 2026 dates → **Run** again.

### Continue when

Both rows visible.

---

# STEP 17 — Audit

### Screen

Audit log

### URL

http://localhost:4200/audit

### Action

1. **Administration** → **Audit**.  
2. Optional filter **Entity type** `Invoice` or `Client` → **Filter**.

### Show the client

- Columns **When**, **Entity**, **Action**, **Description**  
- Recent creates/updates (clients, invoices, payment, email, credit note if any)  
- Subtitle append-only  

### Say

“Every significant action is recorded — who did what and when. That supports accountability, traceability, and financial review.”

### Expected result

Recent demo actions visible.

### Verify

- [ ] Entries exist  

### Continue when

Audit screen shown.

---

# STEP 18 — Optional ReadOnly

### Screen

Login → Clients / Invoices (view only)

### URL

http://localhost:4200/login

### Account

ReadOnly — `demo-viewer@example.com`

### Username

`demo-viewer@example.com`

### Password

`<use password stored in password manager>`

### Action

1. User menu → **Sign out**.  
2. Sign in as viewer.  
3. Open **Clients** → Jordan; **Invoices** → **INV-0002**.  
4. Confirm no **Mark Paid**, **Email**, **Generate**, **Add Client**, **Save** on write actions.  
5. **Sign out** → sign in again as **`demo-admin@example.com`**.

### Say

“Some staff only need visibility — carers, auditors, or board viewers — without ability to change financial records.”

### Expected result

Data visible; writes blocked/hidden.

### Verify

- [ ] Restored TenantAdmin session after  

Skip in 15-minute demo.

---

# STEP 19 — Final summary

### Screen

Dashboard or Audit (no required navigation)

### URL

`/dashboard` or `/audit`

### Action

No clicks required.

### Say

“We followed one resident and their money end to end: resident record → funding contract and rate → billing preview → invoice → PDF to the funder → payment status → outstanding invoice → credit note capability → reports → audit trail. The value is one controlled system instead of disconnected spreadsheets, with permissions and history built in. Next step is your feedback on rules, reports, integrations, and roles so we can align production with how you actually work.”

### Expected result

Client understands end-to-end journey.

### Verify

- [ ] Summary delivered  

---

# STEP 20 — Client questions

### Screen

None

### Action

Ask — do not answer for them; capture notes.

### Say

Ask any of:

1. How do you currently manage resident billing?  
2. Which billing rules are mandatory for go-live?  
3. How should partial-period billing work?  
4. How should partial credit notes work?  
5. What payment workflows are required?  
6. What reports do finance teams need daily / monthly?  
7. What Sage integration or nominal mapping is required?  
8. Which roles should different staff have?  
9. What information should be visible to each role?  
10. What documents need to be exported (PDF, CSV, Sage)?  
11. What email workflows are required?  

### Expected result

Requirements captured.

### Verify

- [ ] Notes taken  

---

# FINAL DEMO FLOW (compact)

1. Login  
2. Dashboard  
3. Organisation  
4. Care Home  
5. Alex  
6. Funding  
7. Billing Preview  
8. INV-0001  
9. PDF  
10. Payment (Mark Paid)  
11. Jordan  
12. INV-0002  
13. Credit Note  
14. Email  
15. Reports (Outstanding)  
16. Reports (Invoices by client)  
17. Audit  
18. ReadOnly (optional)  
19. Summary  
20. Questions  

---

# FINAL ONE-PAGE CHECKLIST

## BEFORE CLIENT ARRIVES

- [ ] Docker + API + frontend healthy  
- [ ] Demo Care Group active; Existing Organisation inactive  
- [ ] TenantAdmin login + password change done  
- [ ] Alex, Jordan, contracts, rates, INV-0001/0002 amounts correct  
- [ ] INV-0001 **Not paid** (for live Mark Paid)  
- [ ] INV-0002 **Not paid**  
- [ ] Both PDFs work  
- [ ] Credit note Outcome A or B understood  
- [ ] Email simulation rehearsed  
- [ ] Reports + audit spot-checked  
- [ ] Second monitor for this guide; no secrets on shared screen  

## DURING DEMO

- [ ] TenantAdmin only (main story)  
- [ ] Show dashboard → org → home → Alex → funding → preview (no generate) → INV-0001 → PDF → Mark Paid  
- [ ] Jordan → INV-0002 → credit note (safe path) → email (simulation disclaimer)  
- [ ] Outstanding + invoices-by-client + audit  
- [ ] Optional ReadOnly → back to TenantAdmin  

## DO NOT DO

- [ ] Create orgs/residents/invoices live  
- [ ] Show Docker, terminal, Azure, `.env`, passwords, source  
- [ ] Generate duplicate August invoices  
- [ ] Generate full credit if Preview shows £2,657.14 without explanation  
- [ ] Claim simulated email was delivered  
- [ ] `docker compose down -v`  
- [ ] Void invoices  

## IF SOMETHING FAILS

- [ ] Single browser refresh; wait 5–10s  
- [ ] Sign out → TenantAdmin login  
- [ ] Open existing INV-0001 / INV-0002 — do not regenerate  
- [ ] PDF retry or rehearsal copy  
- [ ] Operator fixes API off-screen: `docker compose restart api` — **not** while client watches destructive commands  
- [ ] Calm line: “Let me refresh — the data is already in the system.”  

---

*Guide aligned to repository demo documents and UI routes in `frontend/care-home-web/src/app/app.routes.ts` (September 2026). No application code or configuration was modified.*

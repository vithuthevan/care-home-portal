# Demo Data Entry Checklist

**Purpose:** Manually prepare the **already running** local Docker Compose Development stack for the client demo via the UI only.

**Base URL:** http://localhost:4200  
**Assumed starting state:** Stack healthy; PlatformAdmin login works; **Existing Organisation** is Active; **Demo Care Group** does not exist; **TenantAdmin** does not exist; 0 clients, invoices, and credit notes.

**Sources:** `DEMO_DATA_PREPARATION_GUIDE.md`, `CLIENT_DEMO_OPERATOR_RUNBOOK.md`, `CLIENT_DEMO_SCRIPT.md`, `CLIENT_DEMO_ENVIRONMENT_SETUP.md`, `CLIENT_DEMO_READINESS.md` (routes verified against `frontend/care-home-web/src/app/app.routes.ts`).

---

## Safety — DO NOT

| Do not | Why |
|--------|-----|
| Use **Azure**, **Production**, or `Deploy-Azure.ps1` | Demo is local Development only |
| Configure **real SMTP** or change `Email__Mode` away from Development | Risk of real mail / startup failures |
| Enter **real customer** names, NHS numbers, addresses, or funder emails | Privacy / GDPR |
| Display **passwords**, **SQL SA** credentials, **JWT** secrets, or `.env` on screen | Security / client perception |
| Run **`docker compose down -v`** after demo data is entered | Wipes SQL + document volumes |
| Create residents, contracts, or invoices in **Existing Organisation** (tenant Id=1) | Migration placeholder only |
| Force a **£600** credit note via API if the UI preview shows the full line | Document limitation instead |

Store PlatformAdmin and TenantAdmin passwords in a **password manager**. Use placeholders below during rehearsal notes.

| Account | Email | Password source |
|---------|-------|-----------------|
| PlatformAdmin | `admin@localhost` | Development seed → `<DEMO_PLATFORM_ADMIN_PASSWORD>` |
| TenantAdmin | `demo-admin@example.com` | Temporary password at org create → `<DEMO_TENANT_ADMIN_PASSWORD>` after `/change-password` |

---

## How to read each row

| Column | Meaning |
|--------|---------|
| **Step** | Checklist step number (fixed sequence) |
| **Screen / route** | Where you are in the app |
| **Action** | What to click or do |
| **Field** | Form label or control (— if N/A) |
| **Value** | What to enter |
| **Expected result** | Pass criteria before continuing |

---

# PART 1 — Platform setup

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 1 | `/login` | Open app and sign in | Email address | `admin@localhost` | Login page **Care Home Back Office** loads |
| 1 | `/login` | Sign in | Password | `<DEMO_PLATFORM_ADMIN_PASSWORD>` | Side nav shows **Organisations** only (platform scope) |
| 2 | `/platform/tenants` | Navigate | — | Side nav → **Organisations** | List includes **Existing Organisation** |
| 3 | `/platform/tenants/new` | Open create form | — | **Add organisation** | Page title **Add organisation** |
| 3 | `/platform/tenants/new` | Enter org details | Name | `Demo Care Group` | — |
| 3 | `/platform/tenants/new` | Enter org details | Trading name | `Demo Care Group` | — |
| 3 | `/platform/tenants/new` | Enter org details | Address | `1 Demo Lane, Anytown, AN1 2BC` | — |
| 3 | `/platform/tenants/new` | Enter org details | Phone | `01234 567890` | — |
| 3 | `/platform/tenants/new` | Enter org details | Email | `info@demo-care-group.example` | — |
| 3 | `/platform/tenants/new` | Enter org details | Active | ✓ checked | — |
| 3 | `/platform/tenants/new` | Enter admin | Admin email | `demo-admin@example.com` | — |
| 3 | `/platform/tenants/new` | Enter admin | Admin display name | `Demo Administrator` | — |
| 3 | `/platform/tenants/new` | Save | — | Click **Save** | **Organisation created** screen; Development banner shows **Temporary password:** (simulated email) |
| 4 | `/platform/tenants/new` (success) | Capture credential | Temporary password | Copy to password manager | Password stored securely; not shown during client demo |
| 5 | `/platform/tenants` | Deactivate artifact | — | **Edit** on **Existing Organisation** → `/platform/tenants/{id}` | **Edit organisation** form opens |
| 5 | `/platform/tenants/{id}` | Deactivate | Active | Uncheck **Active** | — |
| 5 | `/platform/tenants/{id}` | Save | — | Click **Save** | **Existing Organisation** shows **Inactive** in list |

### ✓ Checkpoint 1 — Demo Care Group created

**Must be true before continuing:**

- [ ] **Demo Care Group** appears in `/platform/tenants` and is **Active**
- [ ] TenantAdmin temporary password is saved
- [ ] **Existing Organisation** is **Inactive**

---

# PART 2 — TenantAdmin

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 6 | Any | Log out | — | Header user menu → **Sign out** | `/login` |
| 7 | `/login` | Sign in | Email address | `demo-admin@example.com` | — |
| 7 | `/login` | Sign in | Password | Temporary password from step 4 | Redirect to `/change-password` (**Set your password**) |
| 8 | `/change-password` | Change password | Current / temporary password | Organisation create temporary password | — |
| 8 | `/change-password` | Change password | New password | `<DEMO_TENANT_ADMIN_PASSWORD>` | Meets UI rules (≥12 chars, upper, lower, number, symbol) |
| 8 | `/change-password` | Change password | Confirm new password | Same as new password | — |
| 8 | `/change-password` | Submit | — | **Save password and continue** | Full tenant shell loads (Dashboard, Operations, Billing, etc.) |
| 9 | `/dashboard` | Verify tenant | — | Side nav brand subtitle | **Demo Care Group** |
| 9 | `/settings/organisation` | Verify settings | Invoice prefix | `INV-` | Confirm only — do not change |
| 9 | `/settings/organisation` | Verify settings | Credit note prefix | `CN-` | Confirm only |
| 9 | `/settings/organisation` | Verify settings | Payment terms | 30 days | Confirm only |
| 9 | `/settings/organisation` | Verify settings | Currency | GBP (£) | Confirm only |

### ✓ Checkpoint 2 — TenantAdmin works

**Must be true before continuing:**

- [ ] Login lands on `/dashboard` (not `/forbidden`, not stuck on `/change-password`)
- [ ] Header / nav shows **Demo Care Group**
- [ ] `/companies` and `/clients` load (not 403)

---

# PART 3 — Business setup

Enter **all** data in **Demo Care Group** only. Create **Alex** before **Jordan** (Jordan comes in Part 5).

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 10 | `/companies/new` | Add company (prerequisite for care home) | Company name | `Demo Care Ltd` | Company listed at `/companies` |
| 10 | `/care-homes/new` | Add care home | Company | `Demo Care Ltd` | — |
| 10 | `/care-homes/new` | Add care home | Care home code | `RIVER01` | — |
| 10 | `/care-homes/new` | Add care home | Care home name | `River View House` | — |
| 10 | `/care-homes/new` | Add care home | Bed capacity | `24` | River View House appears at `/care-homes` |
| 10 | `/funding-authorities/new` | Add authority | Code | `ATC-COUNCIL` | — |
| 10 | `/funding-authorities/new` | Add authority | Name | `Anytown Council` | — |
| 10 | `/funding-authorities/new` | Add authority | Type | `Council` | — |
| 10 | `/funding-authorities/new` | Add authority | Billing frequency | `Monthly` | — |
| 10 | `/funding-authorities/new` | Add authority | Email | `billing@anytown-council.example` | — |
| 10 | `/funding-authorities/new` | Add authority | Contact name | `Adult Social Care Billing` | — |
| 10 | `/funding-authorities/new` | Add authority | Phone | `01234 567001` | — |
| 10 | `/funding-authorities/new` | Add authority | Address | `Adult Social Care, Anytown Council, Civic Centre, Anytown, AN1 1AA` | — |
| 10 | `/funding-authorities/new` | Save | Active | ✓ | Anytown Council in list |
| 11 | `/clients/new` | Add client | Care home | `Demo Care Ltd - River View House` (or **River View House**) | — |
| 11 | `/clients/new` | Add client | Sage ID | `DEMO001` | — |
| 11 | `/clients/new` | Add client | Client reference number | `RVH-001` | — |
| 11 | `/clients/new` | Add client | Title | `Ms` | — |
| 11 | `/clients/new` | Add client | First name / Last name | `Alex` / `Morgan` | — |
| 11 | `/clients/new` | Add client | Date of birth | `1948-03-12` | — |
| 11 | `/clients/new` | Add client | Care type | `Residential` | — |
| 11 | `/clients/new` | Add client | Admission date | `2026-04-01` | — |
| 11 | `/clients/new` | Add client | Email address | `alex.morgan@example.com` | — |
| 11 | `/clients/new` | Add client | Phone | `07700 900101` | — |
| 11 | `/clients/new` | Add client | Notes | `Previous address (fictional): 14 Willow Close, Anytown, AN1 3DE. Demo resident — not real.` | — |
| 11 | `/clients/new` | Save | — | **Save** | Profile opens at `/clients/{id}`; status **Current** |
| 12 | `/clients/{id}` → tab **Funding contracts** | Add contract | Authority | `Anytown Council` | — |
| 12 | `/clients/{id}` | Add contract | Category | `General Care` | — |
| 12 | `/clients/{id}` | Add contract | Nominal | `4000` | — |
| 12 | `/clients/{id}` | Add contract | Start | `2026-04-01` | — |
| 12 | `/clients/{id}` | Add contract | End | *(blank)* | — |
| 12 | `/clients/{id}` | Save contract | — | **Save contract** | Contract shows **Active** |
| 12 | `/clients/{id}` → tab **Rate history** | Add rate | Contract | `Anytown Council / General Care` | — |
| 12 | `/clients/{id}` | Add rate | From | `2026-04-01` | — |
| 12 | `/clients/{id}` | Add rate | To | *(blank)* | — |
| 12 | `/clients/{id}` | Add rate | Frequency | `Weekly` | — |
| 12 | `/clients/{id}` | Add rate | Amount | `575.00` | Rate **£575.00** visible on contract |
| 13 | `/nominal-codes/new` | Add nominal | Code | `4000` | — |
| 13 | `/nominal-codes/new` | Add nominal | Name | `Care income` | Nominal listed |
| 13 | `/invoice-categories` | Verify only | — | `GENERAL_CARE` / **General Care**, `MISC` / **Miscellaneous** | Present — **do not recreate** |
| 13 | `/invoice-templates` | Add template (inline form) | Name | `General Care Template` | — |
| 13 | `/invoice-templates` | Add template | Category | `General Care` | — |
| 13 | `/invoice-templates` | Add template | Bank account | `Demo Care Group Client Account` | — |
| 13 | `/invoice-templates` | Add template | Sort code | `00-00-00` | — |
| 13 | `/invoice-templates` | Add template | Account number | `00000000` | — |
| 13 | `/invoice-templates` | Add template | Contact name | `Demo Finance Team` | — |
| 13 | `/invoice-templates` | Add template | Contact email | `finance@demo-care-group.example` | — |
| 13 | `/invoice-templates` | Add template | Footer | `Payment due within 30 days. Bank details are fictional for demonstration purposes only.` | — |
| 13 | `/invoice-templates` | Save | — | **Save template** | Template active on list |
| 14 | `/billing` | Preview Alex-only bill | Company | `Demo Care Ltd` | — |
| 14 | `/billing` | Preview | Care home | `River View House` *(not “All care homes”)* | — |
| 14 | `/billing` | Preview | Invoice category | `General Care` | — |
| 14 | `/billing` | Preview | Period start / end | `2026-08-01` / `2026-08-31` | — |
| 14 | `/billing` | Preview | — | Click **Preview** | Step 2 shows **Alex Morgan** only; line **£2,546.43**; banner **Ready to generate** |

### ✓ Checkpoint 3 — Alex contract ready

**Must be true before continuing:**

- [ ] Alex exists (`DEMO001` / `RVH-001`) at River View House
- [ ] Active contract + weekly rate **£575.00** from `2026-04-01`
- [ ] Company, care home, funder, nominal **4000**, **General Care** template exist
- [ ] Billing **Preview** for Aug 2026 shows Alex and **£2,546.43**
- [ ] **Jordan Blake does not exist yet** (required for separate INV-0001)

---

# PART 4 — First invoice

**Stop rule:** Do **not** proceed to Part 5 if INV-0001 is wrong.

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 15 | `/billing` | Generate | — | Same scope as step 14 → **Preview** then **Generate** | Green banner: `Generated 1 invoice(s). Total £2,546.43.` |
| 16 | `/invoices` | Open invoice | — | Open **INV-0001** | Invoice detail loads |
| 16 | `/invoices/{id}` | Verify | Invoice number | `INV-0001` | — |
| 16 | `/invoices/{id}` | Verify | Status | `Generated` | — |
| 16 | `/invoices/{id}` | Verify | Resident line | Alex Morgan (`DEMO001` / `RVH-001`) | — |
| 16 | `/invoices/{id}` | Verify | Period | `2026-08-01` – `2026-08-31` | — |
| 17 | `/invoices/{id}` | Verify total | Total | **£2,546.43** | Header total matches line |
| 18 | `/invoices/{id}` | Download PDF | — | **Download PDF** | PDF opens; file begins with `%PDF` |
| 18 | `/invoices/{id}` | Verify PDF | Total on PDF | **£2,546.43** | Matches preview (DEMO ONLY proration — not finance-approved) |

### ✓ Checkpoint 4 — INV-0001 verified

**Must be true before continuing:**

- [ ] Exactly **INV-0001** for Alex, August 2026, total **£2,546.43**
- [ ] Payment status still **Not paid** until Part 7
- [ ] **Do not create Jordan until this checkpoint passes**

---

# PART 5 — Second resident

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 19 | `/clients/new` | Add client | Care home | River View House | — |
| 19 | `/clients/new` | Add client | Sage ID | `DEMO002` | — |
| 19 | `/clients/new` | Add client | Client reference number | `RVH-002` | — |
| 19 | `/clients/new` | Add client | Title | `Mr` | — |
| 19 | `/clients/new` | Add client | First / Last name | `Jordan` / `Blake` | — |
| 19 | `/clients/new` | Add client | Date of birth | `1952-07-22` | — |
| 19 | `/clients/new` | Add client | Care type | `Nursing` | — |
| 19 | `/clients/new` | Add client | Admission date | `2026-05-15` | — |
| 19 | `/clients/new` | Add client | Email / Phone | `jordan.blake@example.com` / `07700 900102` | — |
| 19 | `/clients/new` | Add client | Notes | `Previous address (fictional): 8 Meadow Lane, Anytown, AN1 4FG. Demo resident — not real.` | Profile at `/clients/{id}` |
| 20 | `/clients/{id}` → **Funding contracts** | Add contract | Authority / Category / Nominal / Start | Anytown Council, General Care, `4000`, `2026-05-15` | Contract **Active** |
| 20 | `/clients/{id}` → **Rate history** | Add rate | Frequency / Amount / From | Weekly, `600.00`, `2026-05-15` | **£600.00**/week on contract |
| 21 | `/billing` | Preview Jordan-only | Same filters as Part 4 | Aug 2026, River View, General Care | Preview shows **Jordan Blake** only (Alex skipped as billed); line **£2,657.14** |

### ✓ Checkpoint 5 — Jordan contract ready

**Must be true before continuing:**

- [ ] Jordan exists (`DEMO002` / `RVH-002`) with active contract and **£600.00**/week from `2026-05-15`
- [ ] Billing preview for Aug 2026 shows Jordan only and **£2,657.14**

---

# PART 6 — Second invoice

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 22 | `/billing` | Generate | — | **Preview** → **Generate** (same period) | `Generated 1 invoice(s). Total £2,657.14.` |
| 23 | `/invoices` | Open invoice | — | **INV-0002** | Jordan Blake, August 2026 |
| 24 | `/invoices/{id}` | Verify total | Total | **£2,657.14** | — |
| 25 | `/invoices/{id}` | Download PDF | — | **Download PDF** | Valid PDF; total **£2,657.14**; fictional bank `00-00-00` / `00000000` |

### ✓ Checkpoint 6 — INV-0002 verified

**Must be true before continuing:**

- [ ] **INV-0002** for Jordan only; total **£2,657.14**
- [ ] Payment status **Not paid** (leave until after Part 7 for INV-0001 payment demo)

### ✓ Checkpoint 7 — PDF verified

**Must be true before continuing:**

- [ ] INV-0001 and INV-0002 PDFs both download and open as valid PDFs
- [ ] Amounts on PDFs match **£2,546.43** and **£2,657.14**

---

# PART 7 — Payment

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 26 | `/invoices/{id}` (INV-0001) | Record payment | — | Click **Mark Paid** | Toast: **Payment status updated.** |
| 27 | `/invoices/{id}` (INV-0001) | Verify | Payment status badge | `Paid` | Status badge **Paid** |
| 28 | `/reports` | Outstanding behaviour | Report | `Payment status / outstanding` | Click **Run** (From/To optional) |
| 28 | `/reports` | Verify grid | — | — | **INV-0002** listed; **INV-0001** absent (paid) |

---

# PART 8 — Credit note

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 29 | `/credit-notes` | Open workflow | — | **Billing** → **Credit Notes** | Credit note workspace loads |
| 29 | `/clients/{id}` (Jordan) | Note client id | URL | e.g. `/clients/2` → id `2` | For optional **Client ID** filter |
| 30 | `/credit-notes` | Set filters | Client ID (optional) | Jordan’s numeric id | — |
| 30 | `/credit-notes` | Set filters | Period start / end | `2026-08-25` / `2026-08-31` | — |
| 30 | `/credit-notes` | Set filters | Reason | `Partial period adjustment — 7 days not billable (demo)` | — |
| 30 | `/credit-notes` | Preview | — | Click **Preview** | Table row: invoice **INV-0002**, **Credit** column visible |
| 31 | `/credit-notes` | Decide generate | Credit column | **£600.00** | If **£600.00** → click **Generate** → **CN-0001** in **Existing credit notes** |
| 31 | `/credit-notes` | **STOP if wrong** | Credit column | **£2,657.14** (full remaining) | **Do not Generate.** Mark limitation: UI cannot partial-credit £600; do not use API. Adjust demo narrative. |
| 32 | — | Compliance | — | — | Never force £600 through API or SQL |

**After CN-0001 (if generated):** INV-0002 header total remains **£2,657.14**; payment status stays **Not paid**.

---

# PART 9 — Email

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 33 | `/invoices/{id}` (INV-0002) | Open invoice | — | From `/invoices` | INV-0002 detail |
| 34 | `/invoices/{id}` | Send simulated email | — | Click **Email** | Green banner: `Send completed (or simulated in development).` |
| 35 | `/invoices/{id}` | Verify UI | Toast | — | `Email queued/sent successfully.` *(workflow only — not real delivery)* |
| 35 | `/invoices/{id}` | Verify status | Status badge | `Sent` | May show **Sent** after refresh |
| 36 | — | Confirm safety | — | — | No real email sent (`Email:Mode=Development`); optional operator check: `docker compose logs api` for `EMAIL SIMULATED` — not on client screen |

---

# PART 10 — Reports

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 37 | `/reports` | Outstanding | Report | `Payment status / outstanding` | **Run** → **INV-0002** present |
| 38 | `/reports` | By client | Report | `Invoices by client` | — |
| 38 | `/reports` | By client | From / To | `2026-08-01` / `2026-08-31` | **Run** — empty grid usually means dates not set |
| 39 | `/reports` | Verify rows | — | — | Alex → INV-0001 **£2,546.43**; Jordan → INV-0002 **£2,657.14** |

**Note:** Outstanding report uses invoice **header** totals; it does not net credit notes.

### ✓ Checkpoint 8 — Reports verified

**Must be true before continuing:**

- [ ] Outstanding shows unpaid INV-0002 only
- [ ] Invoices by client shows both residents for August 2026 with correct amounts

---

# PART 11 — Audit

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 40 | `/audit` | Open audit | — | **Administration** → **Audit** | Recent events, newest first |
| 41 | `/audit` | Verify entries | — | Filter optional (Invoice, Client, CreditNote) | Creates for clients, invoices, payment update, email send, credit note (if generated), master data |

### ✓ Checkpoint 9 — Audit verified

**Must be true before continuing:**

- [ ] Demo actions visible (invoice create, payment status update, simulated send, etc.)

---

# PART 12 — Optional

| Step | Screen / route | Action | Field | Value | Expected result |
|------|----------------|--------|-------|-------|-----------------|
| 42 | `/users` | Create viewer (if not done) | Display name / Email / Role | `Demo Viewer` / `demo-viewer@example.com` / `ReadOnly` | User created; complete first-login password change before demo |
| 42 | Any | Log out TenantAdmin | — | **Sign out** | `/login` |
| 42 | `/login` | Sign in as ReadOnly | Email / Password | `demo-viewer@example.com` / `<DEMO_VIEWER_PASSWORD>` | Dashboard loads read-only |
| 43 | `/clients/{id}`, `/invoices/{id}` | Demonstrate restriction | — | Open resident and invoice | Read access works |
| 43 | Same | Attempt write | — | No save / create where writes blocked | Restricted access demonstrated |
| 43 | `/login` | Restore presenter | — | Sign out → TenantAdmin login | Ready for client demo as `demo-admin@example.com` |

---

# Demo Data Ready?

Check every item after completing Parts 1–11 (Part 12 optional).

- [ ] Demo Care Group
- [ ] Existing Organisation inactive
- [ ] TenantAdmin working
- [ ] Alex
- [ ] Jordan
- [ ] Contracts
- [ ] INV-0001
- [ ] INV-0002
- [ ] Payment (INV-0001 **Paid**)
- [ ] PDF
- [ ] Email simulation (INV-0002)
- [ ] Reports
- [ ] Audit

**Final verdict:** If every required box above is checked **and** INV-0001 = **£2,546.43**, INV-0002 = **£2,657.14**, use **🟢 READY FOR CLIENT DEMO**. If any required item fails (including credit-note £600 limitation blocking your narrative), use **🔴 NOT READY** and fix data or adjust script before the client joins.

---

*Checklist aligned to application routes and UI as of 16 September 2026. No application code or configuration was modified.*

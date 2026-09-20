# Demo Data Preparation Guide

**Date:** 15 September 2026  
**Scope:** Manual UI data entry for the client demonstration only  
**Environment:** Fresh local Docker Compose, `ASPNETCORE_ENVIRONMENT=Development`  
**Organisation:** **Demo Care Group** only — never **Existing Organisation**

This guide defines the exact fictional dataset to enter through the application UI before the client demo. It does **not** modify source code, schema, migrations, configuration, or infrastructure, and does **not** insert data automatically.

**Related documents:** `CLIENT_DEMO_SCRIPT.md`, `CLIENT_DEMO_ENVIRONMENT_SETUP.md`, `CLIENT_DEMO_READINESS.md`, `DEMO_ENVIRONMENT_VARIABLES.md`, `DEMO_PRE_FLIGHT_CHECKLIST.md`, `docs/UAT_CHECKLIST.md`

---

## Before you start

1. Reset the demo stack (see `CLIENT_DEMO_SCRIPT.md` → Demo startup procedure).
2. Complete organisation provisioning and TenantAdmin first-login **before** entering business data.
3. Enter **all** data below inside **Demo Care Group** only.
4. Store all passwords in a password manager. **Do not display credentials on screen during the client session.**

---

## 1. Demo organisation

### Existing Organisation — do not use

| Item | Detail |
|------|--------|
| Name | **Existing Organisation** |
| Tenant Id | `1` |
| Origin | Migration compatibility artifact (`AddMultiTenancy`) — not demo branding |
| Required action | Edit → uncheck **Active** → Save |
| Rule | **Never** create residents, contracts, invoices, or users in this tenant |

If asked: *"Existing Organisation is a migration placeholder from the single-tenant upgrade; we operate in a dedicated demo organisation."*

### Demo Care Group — create via PlatformAdmin

| Field | Value |
|-------|-------|
| Route | `/platform/tenants/new` |
| Name | `Demo Care Group` |
| Trading name | `Demo Care Group` |
| Address | `1 Demo Lane, Anytown, AN1 2BC` |
| Phone | `01234 567890` |
| Email | `info@demo-care-group.example` |
| Active | ✓ |
| Admin email | `demo-admin@example.com` |
| Admin display name | `Demo Administrator` |

After save, copy the **temporary password** from the Development create screen. Log out PlatformAdmin.

### Organisation settings to verify (TenantAdmin)

Route: `/settings/organisation`

| Setting | Expected default | Demo action |
|---------|------------------|-------------|
| Currency | GBP (£) | Confirm only |
| Invoice prefix | `INV-` | Confirm only |
| Credit note prefix | `CN-` | Confirm only |
| Payment terms | 30 days | Confirm only |

Do not change prefixes — first invoice must be `INV-0001` and first credit note `CN-0001`.

---

## 2. Demo users

Minimum users for the full demo story. Use **placeholder passwords** in rehearsal notes; set real passwords only in the UI and password manager.

### PlatformAdmin (provisioning only)

| Field | Placeholder |
|-------|-------------|
| Purpose | Create **Demo Care Group** and deactivate **Existing Organisation** |
| Email | `admin@localhost` |
| Password | `<DEMO_PLATFORM_ADMIN_PASSWORD>` |
| Source | Development seed (`Seed:AdminEmail` / `Seed:AdminPassword` in `appsettings.Development.json`) |
| Scope | Platform only — **cannot** access `/companies`, `/clients`, `/billing`, `/invoices` |
| Client demo | **Do not** use for the main narrative |

### TenantAdmin (main demonstration account)

| Field | Value |
|-------|-------|
| Purpose | Primary presenter login for the full demo |
| Email | `demo-admin@example.com` |
| Password | `<TENANT_ADMIN_PASSWORD>` (set at org create → change at `/change-password` **before** the client arrives) |
| Role | `TenantAdmin` (assigned automatically on organisation create) |
| Organisation | **Demo Care Group** |
| First login | Complete forced password change during rehearsal — not during the client session |

### ReadOnly (optional — permissions segment)

| Field | Value |
|-------|-------|
| Purpose | Demonstrate view-only access |
| Display name | `Demo Viewer` |
| Email | `demo-viewer@example.com` |
| Role | `ReadOnly` |
| Password | `<DEMO_VIEWER_PASSWORD>` |
| Route | `/users` → Add user |
| Note | Complete first-login password change before the permissions segment |

### Optional: Administrator (accountant persona)

| Field | Value |
|-------|-------|
| Display name | `Demo Accountant` |
| Email | `demo-accountant@example.com` |
| Role | `Administrator` |
| Password | `<DEMO_ACCOUNTANT_PASSWORD>` |

---

## 3. Residents (clients)

All people below are **fictional**. Do not use real names, NHS numbers, or real addresses.

> **Application note:** The client record does not have a dedicated address field. Residence is represented by **Care home**. Store the fictional home address in **Notes** for presenter reference.

### Resident A — Alex Morgan (primary demo resident)

| Field | Value |
|-------|-------|
| Route | `/clients/new` |
| Sage ID | `DEMO001` |
| Client reference | `RVH-001` |
| Title | `Ms` |
| First name | `Alex` |
| Last name | `Morgan` |
| Date of birth | `1948-03-12` |
| Care type | `Residential` |
| Care home | River View House (`RIVER01`) |
| Admission date | `2026-04-01` |
| Status | `Current` |
| Email | `alex.morgan@example.com` |
| Phone | `07700 900101` |
| Notes | `Previous address (fictional): 14 Willow Close, Anytown, AN1 3DE. Demo resident — not real.` |

**Funding relationship (summary):** Active council contract with Anytown Council from admission date; see §5.

### Resident B — Jordan Blake (outstanding invoice / credit note resident)

| Field | Value |
|-------|-------|
| Sage ID | `DEMO002` |
| Client reference | `RVH-002` |
| Title | `Mr` |
| First name | `Jordan` |
| Last name | `Blake` |
| Date of birth | `1952-07-22` |
| Care type | `Nursing` |
| Care home | River View House (`RIVER01`) |
| Admission date | `2026-05-15` |
| Status | `Current` |
| Email | `jordan.blake@example.com` |
| Phone | `07700 900102` |
| Notes | `Previous address (fictional): 8 Meadow Lane, Anytown, AN1 4FG. Demo resident — not real.` |

**Funding relationship (summary):** Active council contract with Anytown Council from admission date; see §5.

### Resident C — Sam Taylor (optional third resident)

| Field | Value |
|-------|-------|
| Sage ID | `DEMO003` |
| Client reference | `MC-001` |
| Title | `Mx` |
| First name | `Sam` |
| Last name | `Taylor` |
| Date of birth | `1960-11-05` |
| Care type | `Residential` |
| Care home | Meadow Court (`MEADOW02`) |
| Admission date | `2026-06-01` |
| Status | `Current` |
| Email | `sam.taylor@example.com` |
| Phone | `07700 900103` |
| Notes | `Previous address (fictional): 22 Brook Street, Anytown, AN2 5HI. Demo resident — not real.` |

**Funding relationship (summary):** Active private contract with Private Funder Ltd; demonstrates a second funder type and care home.

---

## 4. Funders (funding authorities)

Route: `/funding-authorities`

### Funding authority 1 — Anytown Council

| Field | Value |
|-------|-------|
| Code | `ATC-COUNCIL` |
| Name | `Anytown Council` |
| Type | `Council` |
| Billing frequency | `Monthly` |
| Email | `billing@anytown-council.example` |
| Contact name | `Adult Social Care Billing` |
| Phone | `01234 567001` |
| Address | `Adult Social Care, Anytown Council, Civic Centre, Anytown, AN1 1AA` |
| Active | ✓ |

Used for: Alex Morgan, Jordan Blake.

### Funding authority 2 — Private Funder Ltd

| Field | Value |
|-------|-------|
| Code | `PRIV-FUNDER` |
| Name | `Private Funder Ltd` |
| Type | `Private` |
| Billing frequency | `Weekly` |
| Email | `invoices@private-funder.example` |
| Contact name | `Accounts Payable` |
| Phone | `01234 567002` |
| Address | `Finance Department, Private Funder Ltd, 100 Commerce Park, Anytown, AN2 2BB` |
| Active | ✓ |

Used for: Sam Taylor (optional).

---

## 5. Contracts and rates

Create contracts on each client profile → **Funding contracts** tab (or equivalent contract UI).

### Contract 1 — Alex Morgan / Anytown Council

| Field | Value |
|-------|-------|
| Resident | Alex Morgan (`DEMO001`) |
| Funding authority | Anytown Council |
| Invoice category | `General Care` (pre-seeded — do not recreate) |
| Nominal code | `4000` — Care income |
| Invoice template | General Care Template (create in §6 first, then link) |
| Contract start date | `2026-04-01` |
| Contract end date | *(leave blank — open-ended)* |
| Status | `Active` |

**Rate (add on contract):**

| Field | Value |
|-------|-------|
| Effective from | `2026-04-01` |
| Effective to | *(open-ended)* |
| Frequency | `Weekly` |
| Amount | `575.00` |
| Notes | `Council residential rate — demo` |

### Contract 2 — Jordan Blake / Anytown Council

| Field | Value |
|-------|-------|
| Resident | Jordan Blake (`DEMO002`) |
| Funding authority | Anytown Council |
| Invoice category | `General Care` |
| Nominal code | `4000` |
| Invoice template | General Care Template |
| Contract start date | `2026-05-15` |
| Contract end date | *(open-ended)* |
| Status | `Active` |

**Rate:**

| Field | Value |
|-------|-------|
| Effective from | `2026-05-15` |
| Frequency | `Weekly` |
| Amount | `600.00` |
| Notes | `Council nursing rate — demo` |

### Contract 3 — Sam Taylor / Private Funder Ltd (optional)

| Field | Value |
|-------|-------|
| Resident | Sam Taylor (`DEMO003`) |
| Funding authority | Private Funder Ltd |
| Invoice category | `General Care` |
| Nominal code | `4000` |
| Invoice template | General Care Template |
| Contract start date | `2026-06-01` |
| Contract end date | *(open-ended)* |
| Status | `Active` |

**Rate:**

| Field | Value |
|-------|-------|
| Effective from | `2026-06-01` |
| Frequency | `Weekly` |
| Amount | `550.00` |
| Notes | `Private weekly rate — demo` |

### Contract scenario summary

| Contract | Resident | Funder | Rate | Scenario |
|----------|----------|--------|------|------------|
| 1 | Alex Morgan | Council (monthly funder metadata) | £575/week | Paid invoice — council residential |
| 2 | Jordan Blake | Council | £600/week | Outstanding invoice + credit note — council nursing |
| 3 (optional) | Sam Taylor | Private (weekly funder metadata) | £550/week | Second funder type + second care home |

**Important:** Do not create two **Active** contracts on the same resident + authority + **General Care** category with overlapping dates — the application rejects this (`OVERLAPPING_FUNDING_CONTRACT`).

---

## 6. Billing setup (master data)

Enter in this order before generating invoices.

### Company

| Field | Value |
|-------|-------|
| Route | `/companies` |
| Name | `Demo Care Ltd` |

### Care homes

| Code | Name | Beds | Company |
|------|------|------|---------|
| `RIVER01` | River View House | `24` | Demo Care Ltd |
| `MEADOW02` | Meadow Court | `18` | Demo Care Ltd |

Route: `/care-homes`

### Nominal code

| Field | Value |
|-------|-------|
| Route | `/nominal-codes` |
| Code | `4000` |
| Name | `Care income` |

### Invoice categories

Route: `/invoice-categories` — **verify only** (created automatically with the organisation):

| Code | Name |
|------|------|
| `GENERAL_CARE` | General Care |
| `MISC` | Miscellaneous |

Do **not** recreate these.

### Invoice template

| Field | Value |
|-------|-------|
| Route | `/invoice-templates` |
| Name | `General Care Template` |
| Invoice category | General Care |
| Company | Demo Care Ltd *(optional filter)* |
| Care home | *(leave blank for tenant-wide default)* |
| Funding authority | *(leave blank for default)* |
| Bank account name | `Demo Care Group Client Account` |
| Sort code | `00-00-00` |
| Account number | `00000000` |
| Contact name | `Demo Finance Team` |
| Contact job title | `Finance Officer` |
| Contact email | `finance@demo-care-group.example` |
| Contact phone | `01234 567890` |
| Footer text | `Payment due within 30 days. Bank details are fictional for demonstration purposes only.` |
| Active | ✓ |

Link this template on each funding contract (§5).

### Billing period for the demo

| Field | Value |
|-------|-------|
| Period start | `2026-08-01` |
| Period end | `2026-08-31` |
| Invoice date (system) | `2026-08-31` (period end) |
| Due date (system) | `2026-09-30` (invoice date + 30-day terms) |

### Billing formula — **DEMO ONLY — NOT FINANCE APPROVED**

Weekly rates are calculated as:

```
(line amount) = round((weekly rate ÷ 7) × inclusive eligible days, 2 dp)
```

| Resident | Rate | August 2026 days | Expected line amount |
|----------|------|------------------|----------------------|
| Alex Morgan | £575.00/week | 31 | **£2,546.43** |
| Jordan Blake | £600.00/week | 31 | **£2,657.14** |
| Sam Taylor (optional) | £550.00/week | 31 | **£2,435.71** |

Source: `RateCalculator` — see `docs/PRODUCTION_BUSINESS_SIGNOFF.md` (status: **PENDING**).

> A 31-day month bills **more** than four weeks at the headline weekly rate. State this clearly if the client asks.

---

## 7. Invoices

Route: `/billing` → Preview → Generate, then `/invoices`.

### Critical: how to get INV-0001 and INV-0002 as separate invoices

The billing engine **groups** lines by Company + Care Home + Funding Authority + Invoice Category. If Alex and Jordan are both at River View House with Anytown Council and General Care, a **single** generate run produces **one** invoice with **two** lines.

To match the demo script (`INV-0001` = Alex, `INV-0002` = Jordan), use **sequential generation**:

| Step | Who exists | Billing action | Result |
|------|------------|----------------|--------|
| 1 | Alex Morgan only (contract + rate) | Generate August 2026 for Demo Care Ltd / River View House / General Care | **INV-0001** — Alex only |
| 2 | Add Jordan Blake (contract + rate) | Generate **same** August 2026 period again | **INV-0002** — Jordan only (Alex skipped as already billed) |

Jordan must **not** exist when you generate INV-0001.

### INV-0001 — Alex Morgan (Paid)

| Field | Expected value |
|-------|----------------|
| Invoice number | `INV-0001` |
| Resident line | Alex Morgan (`DEMO001` / `RVH-001`) |
| Billing period | `2026-08-01` – `2026-08-31` |
| Line amount | **£2,546.43** |
| Invoice total | **£2,546.43** |
| Status | Finalized (Generated) |
| Payment status | **Paid** *(set after generation)* |
| Funder | Anytown Council |
| Care home | River View House |

### INV-0002 — Jordan Blake (Not paid / Outstanding)

| Field | Expected value |
|-------|----------------|
| Invoice number | `INV-0002` |
| Resident line | Jordan Blake (`DEMO002` / `RVH-002`) |
| Billing period | `2026-08-01` – `2026-08-31` |
| Line amount | **£2,657.14** |
| Invoice total | **£2,657.14** |
| Status | Finalized |
| Payment status | **Not paid** |
| Funder | Anytown Council |
| Care home | River View House |

### Internal consistency check

| Invoice | Amount | Payment recorded | Payment status |
|---------|--------|------------------|----------------|
| INV-0001 | £2,546.43 | £2,546.43 (marked Paid) | **Paid** |
| INV-0002 | £2,657.14 | £0 (no payment) | **Not paid** / Outstanding |

After generation, download each PDF once to confirm `%PDF` opens (see §11).

### Optional: INV-0003 — Sam Taylor

Generate after Sam exists, with billing filtered to **Meadow Court** (or after Alex + Jordan are already billed for August). Expected total: **£2,435.71**.

---

## 8. Payments

Payment status is a **flag** on the invoice (`NotPaid` | `Paid`) — not a separate payment transaction record.

| Invoice | Action | Route |
|---------|--------|-------|
| INV-0001 | Mark **Paid** | Invoice detail or list |
| INV-0002 | Leave **Not paid** | — |

**Demo narrative:** Finance tracks collected vs outstanding without a separate spreadsheet.

**Rehearsal note:** You may toggle INV-0002 to Paid and back during rehearsal to test the control; restore **Not paid** before the client demo.

---

## 9. Credit note

Route: `/credit-notes` → Preview → Generate.

### CN-0001 — partial adjustment against INV-0002

| Field | Value |
|-------|-------|
| Credit note number | `CN-0001` *(first credit in tenant)* |
| Source invoice | **INV-0002** (Jordan Blake) |
| Resident | Jordan Blake |
| Scenario | Jordan was overcharged for the last 7 days of August — partial period adjustment |
| Credit period | `2026-08-25` – `2026-08-31` *(must target **one** invoice only)* |
| Credit amount | **£600.00** *(7 days × £600/week ÷ 7)* |
| Reason | `Partial period adjustment — 7 days not billable (demo)` |

### Financial state after CN-0001

| Item | Amount |
|------|--------|
| INV-0002 original total | £2,657.14 |
| CN-0001 credit | −£600.00 |
| Net invoiced balance for Jordan (August) | **£2,057.14** |
| INV-0002 payment status | Still **Not paid** *(credit note does not auto-mark paid)* |

**Presenter script:** *"Jordan Blake was overcharged for part of August — we issue a credit note to correct the record."*

**Rules enforced by the application:**

- One credit note generate request must target **a single** source invoice.
- Credit cannot exceed the remaining creditable amount on the line.
- The original invoice is **not** rewritten — it remains in the invoice list.

**DEMO ONLY — NOT FINANCE APPROVED:** Credit amount follows the same weekly proration formula as billing.

Optional: Download CN-0001 PDF; optionally **Send** (simulated email) on the credit note.

---

## 10. Reports

Route: `/reports`. Run as **TenantAdmin** after all data is entered.

### Must demonstrate

| Report | Filter / period | Expected output |
|--------|-----------------|-----------------|
| **Payment status / outstanding** | *(no date filter required)* | **INV-0002** listed as Not paid; INV-0001 **not** listed (Paid) |
| **Invoices by client** | From `2026-08-01` To `2026-08-31` | Alex Morgan → INV-0001 line £2,546.43; Jordan Blake → INV-0002 line £2,657.14 |

### Also useful if time permits

| Report | Purpose with this dataset |
|--------|---------------------------|
| **Client census** | Lists Alex, Jordan (+ Sam if created) as Current |
| **Current rates** | Shows £575 / £600 / £550 weekly rates |
| **Invoices by care home** | River View House lines for Alex + Jordan |
| **Income by category** | General Care income for August 2026 |
| **Occupancy / availability** | River View 2/24, Meadow 1/18 (if Sam created) |
| **Funding rate history** | Rate effective dates from contracts |

**Report caveat:** The outstanding report shows the invoice **header total**, not net of credit notes. After CN-0001, INV-0002 may still show **£2,657.14** in outstanding — explain that credits are separate documents.

Optional: Export any report to CSV, Excel, or PDF.

---

## 11. PDF demonstration

### Recommended invoice: **INV-0002** (Jordan Blake)

Use INV-0002 for the live PDF demo because it is the **outstanding** invoice and is also used for the simulated **Email** step in `CLIENT_DEMO_SCRIPT.md`.

| Item | Detail |
|------|--------|
| Route | `/invoices/:id` → **Download PDF** |
| Invoice | INV-0002 |
| Resident | Jordan Blake |
| Period | 1 August 2026 – 31 August 2026 |
| Total | £2,657.14 |

### Expected PDF content — point these out to the client

| Section | What to show |
|---------|--------------|
| Header / branding | Organisation name **Demo Care Group**, company **Demo Care Ltd** |
| Recipient | Anytown Council billing contact / template contact details |
| Invoice metadata | `INV-0002`, invoice date **31 Aug 2026**, due date **30 Sep 2026** |
| Care home | River View House (`RIVER01`) |
| Line detail | Jordan Blake, Sage ID `DEMO002`, service period, weekly rate £600.00, days, amount |
| Total | **£2,657.14** |
| Bank details | Fictional `00-00-00` / `00000000` — state clearly these are demo-only |
| Footer | Payment terms text from template |

**Business point:** This is what the funder receives. Amounts are **snapshots** — later edits to the resident record do not change this PDF.

**Also verify:** INV-0001 PDF downloads correctly (Paid invoice example).

---

## 12. Audit trail

Route: `/audit` (TenantAdmin or Administrator).

After completing this guide, the following actions should appear (most recent first). Exact wording may vary; entity types are stable.

| Order (typical) | Entity | Action | Trigger |
|-----------------|--------|--------|---------|
| 1 | CreditNote | Create | Generate CN-0001 |
| 2 | Invoice | Update | Mark INV-0001 Paid; payment status changes |
| 3 | Invoice | Create | Generate INV-0002 |
| 4 | Invoice | Create | Generate INV-0001 |
| 5 | ClientFundingContract / FundingRate | Create | Contracts and rates |
| 6 | Client | Create | Residents added |
| 7 | InvoiceTemplate | Create | General Care Template |
| 8 | NominalCode / FundingAuthority / CareHome / Company | Create | Master data |
| 9 | ApplicationUser | Create | Demo Viewer (if created) |
| 10 | Tenant | Create | Demo Care Group provisioned |

If you send INV-0002 by email (simulated), expect an additional invoice **Update** / send-related audit entry and invoice status **Sent**.

**Demo line:** *"Every significant action is recorded — who did what and when."*

---

## 13. Exact data-entry order

Follow this sequence in the UI. Dependencies are ordered so billing and invoice numbers work predictably.

### Phase A — Environment and tenancy (PlatformAdmin)

1. Start fresh Docker Compose stack (`docker compose down -v` → `docker compose up -d --build`).
2. Log in as **PlatformAdmin** (`admin@localhost` / `<DEMO_PLATFORM_ADMIN_PASSWORD>`).
3. Create organisation **Demo Care Group** with admin `demo-admin@example.com`.
4. Copy temporary TenantAdmin password to password manager.
5. Edit **Existing Organisation** → **Inactive** → Save.
6. Log out.

### Phase B — TenantAdmin setup

7. Log in as `demo-admin@example.com` / temporary password.
8. Complete **password change** at `/change-password`.
9. Verify **Demo Care Group** in header and `/dashboard` loads.

### Phase C — Master data

10. Create company **Demo Care Ltd** (`/companies`).
11. Create care home **River View House** (`RIVER01`, 24 beds).
12. *(Optional)* Create care home **Meadow Court** (`MEADOW02`, 18 beds).
13. Create funding authority **Anytown Council** (`/funding-authorities`).
14. *(Optional)* Create funding authority **Private Funder Ltd**.
15. Create nominal code **4000** (`/nominal-codes`).
16. Verify invoice categories **General Care** / **MISC** exist (`/invoice-categories`).
17. Create invoice template **General Care Template** (`/invoice-templates`).

### Phase D — First resident and first invoice (Alex → INV-0001)

18. Create resident **Alex Morgan** only (`/clients/new`).
19. On Alex's profile → add **funding contract** (Anytown Council, General Care, 4000, template).
20. Add **rate** £575.00/week from `2026-04-01`.
21. Open **Billing** (`/billing`) → Demo Care Ltd, River View House, General Care, **2026-08-01 – 2026-08-31** → Preview → Generate.
22. Confirm **INV-0001** total **£2,546.43**.
23. Download INV-0001 PDF (verify `%PDF`).
24. Mark INV-0001 **Paid**.

### Phase E — Second resident and outstanding invoice (Jordan → INV-0002)

25. Create resident **Jordan Blake**.
26. Add Jordan's **funding contract** and **rate** £600.00/week from `2026-05-15`.
27. Run **Billing** again for the **same August 2026** period (Jordan only should bill; Alex already billed).
28. Confirm **INV-0002** total **£2,657.14**.
29. Leave INV-0002 **Not paid**.
30. Download INV-0002 PDF.

### Phase F — Credit note

31. Open **Credit notes** (`/credit-notes`).
32. Preview credit for Jordan / INV-0002 period **2026-08-25 – 2026-08-31** (or credit **£600.00** on the line).
33. Reason: `Partial period adjustment — 7 days not billable (demo)` → Generate **CN-0001**.
34. Download CN-0001 PDF (optional).

### Phase G — Optional extras

35. *(Optional)* Create **Sam Taylor** at Meadow Court with Private Funder contract → generate August invoice (**INV-0003**).
36. *(Optional)* Create **Demo Viewer** (ReadOnly) and **Demo Accountant** (Administrator) at `/users`.
37. *(Optional)* Send INV-0002 email (simulated) → status **Sent**.

### Phase H — Verify before client arrives

38. Run reports: **Outstanding** and **Invoices by client**.
39. Open `/audit` — confirm recent entries.
40. Run `DEMO_PRE_FLIGHT_CHECKLIST.md`.

---

## 14. Demo-only warnings

| Topic | What to tell the client |
|-------|-------------------------|
| **Existing Organisation** | Migration placeholder — inactive; not used in the demo |
| **Weekly / monthly proration** | Implemented for demo — **DEMO ONLY — NOT FINANCE APPROVED** (`docs/PRODUCTION_BUSINESS_SIGNOFF.md`) |
| **Inclusive billing days** | Admission/discharge days count inclusively — **PENDING** sign-off |
| **Sage CSV mapping** | Provisional — **PENDING** sign-off |
| **Bank details on template** | Fictional (`00-00-00` / `00000000`) |
| **Email** | **Simulated** in Development — workflow completes; no message leaves the machine |
| **Outstanding report vs credits** | Report shows invoice header total; credits are separate CN documents |
| **Azure / production** | Next phase after this demo |
| **Client document upload** | Not in current MVP — use invoice PDFs and reports |

---

### Minimum data required

- **Demo Care Group** organisation (Existing Organisation **inactive**)
- **TenantAdmin** with password change completed
- Company **Demo Care Ltd**, care home **River View House**
- Funding authority **Anytown Council**, nominal **4000**, invoice template with fictional bank details
- Residents **Alex Morgan** and **Jordan Blake** with active contracts and weekly rates
- **INV-0001** (Paid, £2,546.43) and **INV-0002** (Not paid, £2,657.14) for August 2026 — generated **sequentially**
- **CN-0001** against INV-0002 (£600.00 credit)
- Reports and audit verified once

### Optional data

- Care home **Meadow Court** and resident **Sam Taylor** (private funder scenario)
- Users **Demo Viewer** (ReadOnly) and **Demo Accountant** (Administrator)
- Simulated email send on INV-0002 (Sent status)
- Sage 50 export preview for August 2026
- Misc-charge CSV import (requires `MISC` category)
- LocationManager user scoped to `RIVER01` only

### Data that must NOT be entered

| Do not enter | Reason |
|--------------|--------|
| Anything in **Existing Organisation** (tenant Id=1) | Migration artifact — confuses the demo |
| Real customer names, NHS numbers, real addresses, real funder emails | Privacy / GDPR |
| Historical names **Sovereign Care Homes**, **Care Pro**, old UAT data | Internal migration history |
| Overlapping active contracts on the same client + authority + General Care | Application rejects with `OVERLAPPING_FUNDING_CONTRACT` |
| Second full August invoice for Alex after INV-0001 | Blocked as `ALREADY_FULLY_BILLED` |
| Real SMTP / production credentials | Out of scope; security risk |

### Billing/Sage items requiring client confirmation

All items below are **PENDING** in `docs/PRODUCTION_BUSINESS_SIGNOFF.md`. Present as *implemented for demonstration* — not finance-approved production rules.

| Item | Current behaviour | Client question to ask |
|------|-------------------|------------------------|
| Weekly proration | `(weekly rate ÷ 7) × inclusive days` | Is day-rate conversion from weekly fees acceptable? |
| Monthly proration | Daily rate varies by calendar month length | Acceptable for monthly funders? |
| Inclusive billing days | Admission/discharge days included in day count | Match your occupancy rules? |
| 31-day month vs 4 weeks | August at £575/week → £2,546.43, not £2,300 | Accept over four-week headline rate? |
| Sage CSV columns / mapping | Provisional — see `docs/SAGE50_EXPORT.md` | Confirm AccountRef, NominalCode, Department = care home code |
| Credit note limits | No override above remaining balance | Sufficient for your adjustment workflow? |
| Payment status | Paid / Not paid flag only — no partial payment ledger | Need partial payment tracking? |

---

*This guide reflects repository state as of 15 September 2026. No application source code or configuration was modified.*

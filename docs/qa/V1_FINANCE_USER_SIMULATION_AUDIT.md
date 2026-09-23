# V1 Finance User Simulation Audit

**Date:** 2026-09-23  
**Method:** Read-only review of architecture/phase reports, form requirements, and current Angular + API implementation. Workflow steps are traced against documented behaviour and UI/backend code paths. **No live browser execution** was performed; `docs/qa/PHASE_A3_MANUAL_QA.md` sign-off is still empty.  
**Scope:** V1 navigation only (`COMMERCIAL_REVENUE_ENABLED` / `Features:CommercialRevenueEnabled` = false).  
**Audience question:** Can a UK care-home finance administrator complete the real monthly billing workflow confidently without developer assistance?

**Classification legend (usability only):** BLOCKER · HIGH FRICTION · CONFUSING · ACCEPTABLE · CLEAR

---

## 1. Executive summary

A trained finance administrator **can** set up the operational chain (company → care home → resident → funding contract → rate → billing setup masters), run **August-style** monthly billing via the Billing Workspace (previous calendar month is suggested from the resident profile), generate invoices, download PDFs, mark **legacy** payment flags, issue credit notes, run reports, export to Sage (when line snapshots are valid), and review audit entries—**without editing code or database**.

Confidence is ** uneven** because:

- **Payment** in V1 is a **status flag only** (Payments / AR are hidden). Wording is explicit but easy to misread as full cash recording.
- **Mark unpaid** exists on the **invoice list** (bulk) but **not** on invoice detail after marking paid—a common correction path is non-obvious.
- **Sage export** can mark invoices exported in the database while the CSV is **FileMissing**; recovery via **Retry CSV** exists but the failure mode is financially sensitive if not understood.
- **Overlapping funding contract** billing errors expose **Contract IDs** in the message.
- **Configuration** edit URLs for nominal codes, invoice categories, and templates still use **numeric** route segments.
- **Organisation** for a tenant user means **Organisation Settings** and sidebar tenant name—not the platform **Organisations** screen (platform admin only).

Optional demo companies (`Seed:MasterDataForEmptyTenants`) may exist in some environments; otherwise the administrator must create all structure manually—supported by the UI.

**Bottom line:** The monthly billing **generation and export path is operable** for a careful administrator who understands legacy payment flags and Sage validation. **BLOCKER**-class gaps are few but include **payment reversal UX on invoice detail** and **risk of misinterpreting legacy “mark paid” as bank reconciliation**. Several **HIGH FRICTION** items remain around technical wording, internal IDs in errors/URLs, and Sage page messaging.

---

## 2. Complete workflow trace

Assumptions: tenant user with **Administrator** or **TenantAdmin** (write access), Commercial Revenue off, data created through UI or optional empty-tenant seed (`Demo Care Ltd` / `River View House` when configured).

### Step 0 — Sign in

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Login page | CLEAR |
| 2 | User action | Enter username/password (client-side cipher to API) | ACCEPTABLE |
| 3 | Expected | Dashboard; V1 nav sections visible; no Receivables/Payments | CLEAR |
| 4 | Actual | `app.html` shows Dashboard, Operations, Billing Setup, Billing, Reporting, Administration when logged in with tenant | CLEAR |
| 5 | Context preserved | Tenant name in sidebar brand | CLEAR |
| 6 | Internal ID | None on login | CLEAR |
| 7 | Terminology | “Residents” vs API “clients” not shown to user on login | ACCEPTABLE |
| 8 | Validation | Standard auth errors via API | ACCEPTABLE |
| 9 | Next action | Dashboard or Operations | CLEAR |
| 10 | Dev wording | None on login shell | CLEAR |
| 11 | Financial confusion risk | Low | CLEAR |
| 12 | Without developer | Yes | CLEAR |

---

### Step 1 — Organisation (tenant context)

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Dashboard or **Administration → Organisation Settings** (`/settings/organisation`) | ACCEPTABLE |
| 2 | User action | Review/update organisation name, currency, invoice/credit prefixes, payment terms | CLEAR |
| 3 | Expected | Tenant-wide billing defaults applied to new invoices | CLEAR |
| 4 | Actual | `OrganisationSettingsController` + form with required markers per A2.5 | CLEAR |
| 5 | Context preserved | Page states “this organisation only” | CLEAR |
| 6 | Internal ID | TenantId not shown | CLEAR |
| 7 | Terminology | “Organisation” matches UK operator language | CLEAR |
| 8 | Validation | Inline required on name; save feedback on page | ACCEPTABLE |
| 9 | Next action | Operations → Companies (not spelled out on settings page) | HIGH FRICTION |
| 10 | Dev wording | “SMTP credentials stay in server configuration” | ACCEPTABLE (honest) |
| 11 | Financial confusion | Wrong prefix mid-year affects numbering—user-visible fields | CONFUSING |
| 12 | Without developer | Yes | ACCEPTABLE |

**Note:** Platform **Organisations** (`/platform/tenants`) is **only** for platform admins without a tenant context—not part of a normal finance admin monthly run.

---

### Step 2 — Company

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Operations → Companies** | CLEAR |
| 2 | User action | Add company → name (required) → Save | CLEAR |
| 3 | Expected | Company in list; redirect to list | CLEAR |
| 4 | Actual | Create flow; save disabled when invalid (A3) | CLEAR |
| 5 | Context preserved | Breadcrumbs on detail | CLEAR |
| 6 | Internal ID | Detail URL uses UUID when `publicId` present | ACCEPTABLE |
| 7 | Terminology | “Company” | CLEAR |
| 8 | Validation | Duplicate name per tenant blocked server-side | ACCEPTABLE |
| 9 | Next action | Add care home from company detail | CLEAR |
| 10 | Dev wording | Minimal | CLEAR |
| 11 | Financial confusion | Low | CLEAR |
| 12 | Without developer | Yes | CLEAR |

---

### Step 3 — Care home

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Company detail or **Operations → Care Homes** | CLEAR |
| 2 | User action | Create: company, code, name, bed capacity → Save | CLEAR |
| 3 | Expected | Redirect to **care home dashboard** with UUID URL (A3) | CLEAR |
| 4 | Actual | `care-home-form` post-save navigation per phase report | CLEAR |
| 5 | Context preserved | Company preselected via `company` query UUID from company detail | CLEAR |
| 6 | Internal ID | UUID in path when available | ACCEPTABLE |
| 7 | Terminology | “Care home” | CLEAR |
| 8 | Validation | Required fields marked `*` per form audit | CLEAR |
| 9 | Next action | Residents or dashboard widgets | CLEAR |
| 10 | Dev wording | Minimal | CLEAR |
| 11 | Financial confusion | Low | CLEAR |
| 12 | Without developer | Yes | CLEAR |

---

### Step 4 — Resident

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Operations → Residents** or care home dashboard “View all” | CLEAR |
| 2 | User action | Add resident: care home, names, care type, admission date | CLEAR |
| 3 | Expected | Profile with UUID URL | CLEAR |
| 4 | Actual | Reference/Sage ID optional on create (auto-generated if blank) | ACCEPTABLE |
| 5 | Context preserved | Filter `careHome={uuid}` from dashboard handoff | CLEAR |
| 6 | Internal ID | Not required on create | CLEAR |
| 7 | Terminology | UI “Residents”; routes `/clients` | ACCEPTABLE |
| 8 | Validation | Required fields documented in `FORM_FIELD_REQUIREMENTS_FINAL.md` | CLEAR |
| 9 | Next action | Funding tab / add contract | CLEAR |
| 10 | Dev wording | Minimal | CLEAR |
| 11 | Financial confusion | Missing Sage ID later blocks Sage export, not resident create | CONFUSING |
| 12 | Without developer | Yes | ACCEPTABLE |

---

### Step 5 — Funding contract

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Resident profile → Funding → add contract | CLEAR |
| 2 | User action | Select funding authority, invoice category, nominal code, start date | CLEAR |
| 3 | Expected | Return to funding tab; contract visible | CLEAR |
| 4 | Actual | `FundingContractsController` + overlap rules on create | CLEAR |
| 5 | Context preserved | Breadcrumbs Residents → name | CLEAR |
| 6 | Internal ID | Dropdowns by name; no typed contract id | CLEAR |
| 7 | Terminology | “Funding authority”, “Invoice category”, “Nominal code” | ACCEPTABLE |
| 8 | Validation | Overlap on create returns business message (no overlap at create) | CLEAR |
| 9 | Next action | Add rate | CLEAR |
| 10 | Dev wording | Minimal on form | CLEAR |
| 11 | Financial confusion | Wrong category/nominal affects billing stream grouping | ACCEPTABLE |
| 12 | Without developer | Yes | CLEAR |

---

### Step 6 — Rate

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Resident → add rate (contract context) | CLEAR |
| 2 | User action | Effective from, frequency, amount | CLEAR |
| 3 | Expected | Rate on contract; profile shows “who pays” | CLEAR |
| 4 | Actual | Rate overlap validated in `FundingContractService` | CLEAR |
| 5 | Context preserved | Profile warns if no rate before billing | CLEAR |
| 6 | Internal ID | `contractId` may appear in query for rate form—not user-facing label | ACCEPTABLE |
| 7 | Terminology | “Effective from”, frequency, amount | CLEAR |
| 8 | Validation | Service-level frequency/amount rules | ACCEPTABLE |
| 9 | Next action | **Start billing** / Billing workspace | CLEAR |
| 10 | Dev wording | Minimal | CLEAR |
| 11 | Financial confusion | Gap in rate calendar → MISSING_RATE at billing (no £0 assumption) | CLEAR |
| 12 | Without developer | Yes | CLEAR |

---

### Step 7 — Billing setup (authorities, nominals, categories, templates)

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Billing Setup** section | CLEAR |
| 2 | User action | Ensure authority, nominal, category, template exist; tenant seed provides GENERAL_CARE, MISC, etc. | ACCEPTABLE |
| 3 | Expected | Template resolves for PDF/billing | CLEAR |
| 4 | Actual | Usage columns + deactivate confirmations (Phase A/A2.5); system categories protected | CLEAR |
| 5 | Context preserved | Lists are tenant-scoped | CLEAR |
| 6 | Internal ID | Edit URLs `/nominal-codes/{int}`, categories/templates numeric | HIGH FRICTION |
| 7 | Terminology | “System default” vs “Organisation” badges | CLEAR |
| 8 | Validation | Cannot deactivate/rename core system category codes | CLEAR |
| 9 | Next action | Billing Workspace | ACCEPTABLE |
| 10 | Dev wording | “Configuration source” acceptable | ACCEPTABLE |
| 11 | Financial confusion | Deactivating nominal does not remove from existing contracts; Sage uses snapshots | CONFUSING |
| 12 | Without developer | Yes | ACCEPTABLE |

---

### Step 8 — August billing (scope & preview)

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Billing → Billing Workspace** or resident **Start billing** | CLEAR |
| 2 | User action | Company, care home, optional category, period start/end → **Preview** | CLEAR |
| 3 | Expected | Previous month suggested from profile (e.g. August if run in September) | CLEAR |
| 4 | Actual | `suggestedBillingPeriod()` = prior calendar month; query params `company`, `careHome`, `client` UUIDs | CLEAR |
| 5 | Context preserved | Scope banners; readonly scope after preview | CLEAR |
| 6 | Internal ID | Scope selects use internal ids in component state only | ACCEPTABLE |
| 7 | Terminology | “Billing period”, “Invoice category”, workflow steps | CLEAR |
| 8 | Validation | Empty scope → preview may fail or show no lines | ACCEPTABLE |
| 9 | Next action | Review exceptions → Generate | CLEAR |
| 10 | Dev wording | Exception codes mapped via `billingExceptionLabel` when message present | ACCEPTABLE |
| 11 | Financial confusion | Partial period info vs error distinction shown in review | ACCEPTABLE |
| 12 | Without developer | Yes | CLEAR |

---

### Step 9 — Invoice generation

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Billing workspace Step 4 after successful preview | CLEAR |
| 2 | User action | **Generate invoices** when “Ready to generate” | CLEAR |
| 3 | Expected | Invoices created; duplicate period blocked | CLEAR |
| 4 | Actual | SQL lock + `CanGenerate` requires no Error severity and lines > 0; second generate → 400 “already fully billed” (integration test) | CLEAR |
| 5 | Context preserved | Success banner with count and total | CLEAR |
| 6 | Internal ID | None shown | CLEAR |
| 7 | Terminology | “Generate invoices”, “invoice records” | ACCEPTABLE |
| 8 | Validation | Blocked message explains duplicate/already billed | CLEAR |
| 9 | Next action | Invoices list or re-preview | CLEAR |
| 10 | Dev wording | “Re-run preview after generation to avoid duplicate billing” | CLEAR |
| 11 | Financial confusion | Low if user reads blocked state | ACCEPTABLE |
| 12 | Without developer | Yes | CLEAR |

---

### Step 10 — Invoice & PDF

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Billing → Invoices** or dashboard recent invoices | CLEAR |
| 2 | User action | Open invoice → **Download PDF** | CLEAR |
| 3 | Expected | PDF with template branding; UUID detail URL | CLEAR |
| 4 | Actual | PDF via `/api/invoices/{id}/pdf` using loaded DTO internal id for download call | ACCEPTABLE |
| 5 | Context preserved | Breadcrumbs Billing → Invoices → number; links to resident/home/company | CLEAR |
| 6 | Internal ID | PDF request uses numeric id internally; URL may show UUID | ACCEPTABLE |
| 7 | Terminology | Service period, due date, Sage ID on resident panel | ACCEPTABLE |
| 8 | Validation | N/A | — |
| 9 | Next action | Email, credit note, payment flag | ACCEPTABLE |
| 10 | Dev wording | Dev-mode hint on simulated email send | ACCEPTABLE |
| 11 | Financial confusion | Line period vs invoice period explained on page | CLEAR |
| 12 | Without developer | Yes | CLEAR |

---

### Step 11 — Payment (V1 legacy)

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Invoice detail **Payment status** panel | ACCEPTABLE |
| 2 | User action | More menu → **Legacy: mark as paid (status only)** with confirm dialog | CONFUSING |
| 3 | Expected | Status updates; no payment record when AR off | CLEAR (if dialog read) |
| 4 | Actual | Dialog warns flag-only; Payments module hidden | ACCEPTABLE |
| 5 | Context preserved | Status badge updates | CLEAR |
| 6 | Internal ID | None | CLEAR |
| 7 | Terminology | “Legacy”, “payment flag”, reference to hidden Payments | HIGH FRICTION |
| 8 | Validation | Confirm required | CLEAR |
| 9 | Next action | Reports outstanding / Sage export | ACCEPTABLE |
| 10 | Dev wording | “Legacy” in menu label | HIGH FRICTION |
| 11 | Financial confusion | **High** if user skips dialog—looks like cash recorded | HIGH FRICTION |
| 12 | Without developer | Yes, with training | HIGH FRICTION |

---

### Step 12 — Credit note

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | Invoice detail → credit note icon or **Billing → Credit notes** | CLEAR |
| 2 | User action | Period, reason (required), preview → generate | CLEAR |
| 3 | Expected | Credit limited to remaining per line; form clears on success (Phase A) | CLEAR |
| 4 | Actual | Query may include `invoiceId`, `clientId` numbers; list links use `invoicePublicId` when API provides | HIGH FRICTION |
| 5 | Context preserved | Banner for original invoice number and resident | CLEAR |
| 6 | Internal ID | Query params expose numeric invoice/client ids in browser | HIGH FRICTION |
| 7 | Terminology | “Credit note”, “original invoice” | CLEAR |
| 8 | Validation | Client-side reason/period before preview (A2.5) | CLEAR |
| 9 | Next action | PDF/send from credit note list | ACCEPTABLE |
| 10 | Dev wording | Minimal | CLEAR |
| 11 | Financial confusion | Server enforces remaining credit; user cannot over-credit via UI alone | CLEAR |
| 12 | Without developer | Yes | ACCEPTABLE |

---

### Step 13 — Reports

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Reporting → Reports**; dashboard KPI → `report=outstanding` | CLEAR |
| 2 | User action | Select report, dates, Run / CSV / Excel / PDF | CLEAR |
| 3 | Expected | Friendly column labels via `columnLabel()` | CLEAR |
| 4 | Actual | Includes billing-exceptions, outstanding (legacy payment semantics) | ACCEPTABLE |
| 5 | Context preserved | Report type in URL query | ACCEPTABLE |
| 6 | Internal ID | Grid shows business fields; keys mapped | ACCEPTABLE |
| 7 | Terminology | “Payment status / outstanding” | CLEAR |
| 8 | Validation | Date range user responsibility | ACCEPTABLE |
| 9 | Next action | Sage export or fix exceptions report | CLEAR |
| 10 | Dev wording | Low in mapped columns | ACCEPTABLE |
| 11 | Financial confusion | Outstanding = legacy unpaid flag when AR off | CONFUSING |
| 12 | Without developer | Yes | ACCEPTABLE |

---

### Step 14 — Sage export

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Reporting → Sage Export** | ACCEPTABLE |
| 2 | User action | Date from/to → **Validate** → **Export CSV** | CLEAR |
| 3 | Expected | CSV download; invoices marked exported | CLEAR |
| 4 | Actual | Preview must have zero validation errors (`CanExport`); DB commit before file write; **FileMissing** + **Retry CSV** | ACCEPTABLE |
| 5 | Context preserved | Batch history table | ACCEPTABLE |
| 6 | Internal ID | Batch id in support message if write fails | CONFUSING |
| 7 | Terminology | Subtitle: “Provisional CSV mapping…” | HIGH FRICTION |
| 8 | Validation | Per-line Sage ID / nominal snapshot errors listed | CLEAR |
| 9 | Next action | Download or Retry CSV | ACCEPTABLE |
| 10 | Dev wording | “Provisional”, “stakeholder confirmation” | HIGH FRICTION |
| 11 | Financial confusion | Invoices marked exported while file missing | HIGH FRICTION |
| 12 | Without developer | Yes if trained on FileMissing/retry | HIGH FRICTION |

---

### Step 15 — Audit

| # | Field | Observation | Classification |
|---|--------|-------------|----------------|
| 1 | Starting screen | **Administration → Audit** | CLEAR |
| 2 | User action | Filter entity type/action; follow links | CLEAR |
| 3 | Expected | Human labels; safe UUID links where supported | CLEAR |
| 4 | Actual | Phase A filters + reference column; no before/after JSON in list | ACCEPTABLE |
| 5 | Context preserved | Entity type badges | CLEAR |
| 6 | Internal ID | Some entity types without public routes show text only | ACCEPTABLE |
| 7 | Terminology | “Audit trail”, “Actor”, “Action” | CLEAR |
| 8 | Validation | N/A | — |
| 9 | Next action | Investigate change or return to billing | ACCEPTABLE |
| 10 | Dev wording | Entity type values mapped to labels | ACCEPTABLE |
| 11 | Financial confusion | Billing exceptions logged separately (`BillingExceptionLogs`) | CONFUSING |
| 12 | Without developer | Yes | ACCEPTABLE |

---

## 3. Failure-scenario trace

### Resident has no funding contract

| | |
|---|---|
| **Expected business behaviour** | Resident skipped or error; no invoice for that stream. |
| **Actual behaviour** | Preview adds Error `MISSING_CONTRACT` with resident name; blocks generate if any Error. UI: “No active funding contract…” + **Fix funding** / **View resident**. |
| **User-visible explanation** | Server message + `billingExceptionLabel`. |
| **Admin understands?** | CLEAR |
| **Incorrect financial result?** | No—generation blocked. |

**Classification:** CLEAR

---

### Resident has no applicable rate

| | |
|---|---|
| **Expected** | No £0 billing; error for uncovered dates. |
| **Actual** | `MISSING_RATE` Error with authority name and date range in message. |
| **User-visible** | “No applicable rate…” / Fix funding → add rate. |
| **Admin understands?** | CLEAR |
| **Incorrect result?** | No—blocked. |

**Classification:** CLEAR

---

### Overlapping funding contracts exist

| | |
|---|---|
| **Expected** | Block billing for affected authority/category stream until resolved. |
| **Actual** | Create/update blocked with `OVERLAPPING_FUNDING_CONTRACT` message. Billing preview uses `OVERLAPPING_FUNDING_CONTRACTS` with **Contract IDs** list and ISO dates. |
| **User-visible** | Long message including “Contract IDs: 12, 15”. |
| **Admin understands?** | CONFUSING (IDs) |
| **Incorrect result?** | No—blocked. |

**Classification:** HIGH FRICTION (wording); CLEAR (safety)

---

### Duplicate billing attempted

| | |
|---|---|
| **Expected** | Second run produces no duplicate invoices for same finalized periods. |
| **Actual** | Preview warnings/errors `ALREADY_FULLY_BILLED`; generate returns 400 with “already fully billed”. UI generation blocked message. |
| **User-visible** | Explicit blocked generation + helper text to re-preview. |
| **Admin understands?** | CLEAR |
| **Incorrect result?** | No (integration test). |

**Classification:** CLEAR

---

### Billing period is invalid

| | |
|---|---|
| **Expected** | End before start rejected. |
| **Actual** | `INVALID_PERIOD` Error; generate returns plain message. |
| **User-visible** | “Billing period end cannot be before start.” |
| **Admin understands?** | CLEAR |
| **Incorrect result?** | No |

**Classification:** CLEAR

---

### Rate is missing (empty contract rates)

| | |
|---|---|
| **Expected** | Same as no applicable rate. |
| **Actual** | `MISSING_RATE` “No funding rate exists… Billing will not assume £0.” |
| **User-visible** | Yes |
| **Admin understands?** | CLEAR |
| **Incorrect result?** | No |

**Classification:** CLEAR

---

### Nominal code is inactive

| | |
|---|---|
| **Expected** | Ideally warn at setup; billing uses contract FK. |
| **Actual** | Inactive nominals hidden from new dropdowns; existing contract still loads `NominalCode` navigation. Billing proceeds if code on contract. Sage uses **snapshot** at invoice generation. |
| **User-visible** | Usage on list; deactivate confirm shows counts. |
| **Admin understands?** | CONFUSING for “inactive but still billing” |
| **Incorrect result?** | Unlikely unless snapshot empty at generation. |

**Classification:** CONFUSING

---

### Invoice category is inactive

| | |
|---|---|
| **Expected** | Similar to nominal. |
| **Actual** | Inactive categories filtered from billing workspace dropdown; existing contracts retain category id. System defaults cannot deactivate. |
| **User-visible** | Badge + usage |
| **Admin understands?** | ACCEPTABLE |
| **Incorrect result?** | Low |

**Classification:** ACCEPTABLE

---

### Invoice already exists (same period)

| | |
|---|---|
| **Expected** | Treated as duplicate billing / already billed coverage. |
| **Actual** | Coverage table + ALREADY_FULLY_BILLED; generate blocked. |
| **User-visible** | Yes |
| **Admin understands?** | CLEAR |
| **Incorrect result?** | No |

**Classification:** CLEAR

---

### Invoice is unpaid

| | |
|---|---|
| **Expected** | Shows Not paid; included in outstanding report. |
| **Actual** | `LabeledStatus` Payment; list filter Not paid. |
| **User-visible** | Yes |
| **Admin understands?** | CLEAR |
| **Incorrect result?** | N/A |

**Classification:** CLEAR

---

### Invoice is marked paid

| | |
|---|---|
| **Expected** | Status Paid; outstanding report excludes or reflects flag. |
| **Actual** | Legacy POST `payment-status` Paid; detail menu hides mark paid when paid. |
| **User-visible** | Confirm dialog explains flag-only. |
| **Admin understands?** | CONFUSING if dialog ignored |
| **Incorrect result?** | Status flag wrong vs bank, not double invoice |

**Classification:** HIGH FRICTION

---

### Invoice is marked unpaid

| | |
|---|---|
| **Expected** | Reversible flag. |
| **Actual** | **Invoice list**: bulk **Mark unpaid** when selection paid. **Invoice detail**: no mark-unpaid in More menu (only mark paid when unpaid). API supports NotPaid. |
| **User-visible** | List yes; detail no |
| **Admin understands?** | BLOCKER on detail path |
| **Incorrect result?** | Stuck Paid on detail until list bulk action |

**Classification:** BLOCKER (detail UX gap)

---

### Credit note is generated

| | |
|---|---|
| **Expected** | Reduces remaining credit on lines; audited. |
| **Actual** | `CreditNoteService` enforces per-line remaining; void invoices excluded. |
| **User-visible** | Success toast; list refresh |
| **Admin understands?** | CLEAR |
| **Incorrect result?** | Server prevents over-credit |

**Classification:** CLEAR

---

### Sage export file is missing

| | |
|---|---|
| **Expected** | Operator can recover CSV without double-exporting invoices. |
| **Actual** | Batch `FileMissing`; invoices retain export marks; **Retry CSV** endpoint + UI button. |
| **User-visible** | Status column; download hidden when FileMissing |
| **Admin understands?** | CONFUSING without training |
| **Incorrect result?** | Sage ledger out of sync if user assumes no export happened |

**Classification:** HIGH FRICTION

---

### Sage export retry is required

| | |
|---|---|
| **Expected** | Regenerate file from batch invoices. |
| **Actual** | `POST /api/sage-exports/{id}/retry-file` rebuilds CSV. |
| **User-visible** | Retry CSV button |
| **Admin understands?** | ACCEPTABLE after one failure |
| **Incorrect result?** | No duplicate invoice marks (by design) |

**Classification:** ACCEPTABLE

---

## 4. Context-loss points

| Location | What is lost | Classification |
|----------|----------------|----------------|
| Master data edit from invoice (nominal/category/template) | Numeric edit URLs; no publicId | HIGH FRICTION |
| Credit note handoff | Numeric `invoiceId`/`clientId` in query | HIGH FRICTION |
| Care home list at scale | Unpaged API load for search (audit note) | HIGH FRICTION |
| Billing workspace after “Change scope” | User must re-preview; communicated | ACCEPTABLE |
| Audit vs billing exceptions | Exceptions in Reports, not Audit detail | CONFUSING |
| Organisation settings → first company | No guided wizard | HIGH FRICTION |
| Read-only role | Write actions hidden/403 | CLEAR (by design) |

---

## 5. Internal-ID exposure

| Surface | Exposure | Classification |
|---------|----------|----------------|
| Resident/company/care home/invoice routes | UUID preferred | CLEAR |
| Nominal/category/template edit URLs | Integer id | HIGH FRICTION |
| Overlap billing error | “Contract IDs: …” | HIGH FRICTION |
| Credit note query params | `invoiceId`, `clientId` | HIGH FRICTION |
| Sage export failure message | “batch id” for support | CONFUSING |
| Invoice PDF API call | Uses numeric id from DTO | ACCEPTABLE (not shown) |
| Billing workspace scope | Internal select values | ACCEPTABLE |

---

## 6. Technical wording visible to users

| Text | Where | Classification |
|------|--------|----------------|
| Legacy: mark as paid (status only) | Invoice detail menu | HIGH FRICTION |
| Legacy: payment status flag | Confirm dialog title | HIGH FRICTION |
| Provisional CSV mapping… stakeholder confirmation | Sage export header | HIGH FRICTION |
| Contract IDs: n, m | Billing overlap exception | HIGH FRICTION |
| SMTP credentials stay in server configuration | Organisation settings | ACCEPTABLE |
| Development note: delivery is simulated | Invoice send (dev mode) | ACCEPTABLE |
| Billing will not assume £0 | MISSING_RATE message | CLEAR (good) |
| FileMissing | Sage batch status (English enum) | CONFUSING |

---

## 7. Form ambiguity

| Form | Issue | Classification |
|------|--------|----------------|
| Resident create | Reference/Sage optional but required on update | CONFUSING |
| Credit note resident | Optional with hint “leave blank to match period only” | CONFUSING |
| Billing scope | “All care homes” / “All categories” breadth | ACCEPTABLE |
| Funding authority | Custom billing interval days conditional | ACCEPTABLE |
| Invoice template | Many optional branding fields | ACCEPTABLE |
| Company | Only name required on create—clear | CLEAR |

Reference: `docs/ux/FORM_FIELD_REQUIREMENTS_FINAL.md` largely aligned with UI.

---

## 8. Financial-risk UX

| Risk | Mitigation in product | Residual UX risk | Classification |
|------|----------------------|------------------|----------------|
| Double billing | Period finalize + generate block | User ignores “already billed” warnings | ACCEPTABLE |
| £0 rate assumption | MISSING_RATE errors | Low | CLEAR |
| Mark paid without cash | Dialog warning | User skips dialog | HIGH FRICTION |
| Sage export without file | FileMissing + retry | User thinks export did not occur | HIGH FRICTION |
| Void after Sage export | Allowed (policy not enforced in UI) | Re-export semantics unclear | CONFUSING |
| Partial Sage validation | Export blocked if any line invalid | All-or-nothing may surprise | ACCEPTABLE |
| Wrong template/branding | Resolver precedence opaque | Wrong PDF branding | CONFUSING |

---

## 9. Navigation problems

| Issue | Classification |
|-------|----------------|
| Collapsed sidebar icon-only mode—labels in tooltips only | ACCEPTABLE |
| Residents under `/clients` path | ACCEPTABLE |
| Mark unpaid only on list | BLOCKER |
| Payments module referenced but gated off | HIGH FRICTION |
| Platform Organisations vs tenant Organisation Settings naming collision for docs/training | CONFUSING |
| Breadcrumbs improved in A3 for major flows | CLEAR |

---

## 10. Reporting/export problems

| Issue | Classification |
|-------|----------------|
| Sage page lacks company/care home filters in UI (API supports more than UI exposes) | HIGH FRICTION |
| Sage validate/export date-only panel | ACCEPTABLE |
| Reports export buttons always visible; empty data still possible | ACCEPTABLE |
| Billing exceptions report separate from workspace | ACCEPTABLE |
| Outstanding report legacy semantics vs future AR | CONFUSING |

---

## 11. Accessibility observations

| Observation | Classification |
|-------------|----------------|
| Invoice list select column has `aria-label` | CLEAR |
| Icon actions use `ariaLabel` / tooltips on invoice detail | CLEAR |
| Billing status summary `aria-live="polite"` | CLEAR |
| Required field legend with `aria-hidden` on asterisk | ACCEPTABLE |
| Workflow steps visual only—step state may need screen reader polish | ACCEPTABLE |
| Tables responsive hide columns on small viewports; mobile card lists provided in billing review | ACCEPTABLE |
| Colour-only status in some badges mitigated by text labels | ACCEPTABLE |

---

## 12. Items requiring code changes

(Identified for fix implementation—not in scope of this audit.)

1. Invoice detail: **Mark as not paid** (mirror list bulk / API) when legacy payment flag is Paid.
2. Billing overlap messages: remove or replace **Contract IDs** with human references (authority, category, dates only).
3. Credit note handoff: prefer **public** invoice/client query keys only in URLs.
4. Master data edit routes: **PublicId** for nominal, category, template (deferred in A3—needs domain + migration).
5. Sage export UI: optional filters (company/care home) aligned with API; user-friendly **File missing** status label.
6. Optional: surface **Sage ID missing** warning on resident profile before month-end.

---

## 13. Items requiring product decisions

1. **PaidAt** and payment history on invoice (deferred A3).
2. Whether **legacy mark paid** remains in V1 or is hidden until AR module ships.
3. **Void invoice after Sage export** policy and user messaging.
4. **Credit note preview scoped by invoice id** vs period-only matching (multiple invoices per resident).
5. Sage export subtitle—“provisional mapping” vs production-confident wording.
6. Finance **ReadOnly** role: should they run billing preview only (currently generate blocked)?
7. Guided **month-end checklist** vs free navigation.

---

## 14. Items that should NOT be changed

Per architecture audit and phase scope (financial integrity):

- `BillingService` calculation and rate pro-ration logic.
- `Sage50ColumnMap` / CSV column semantics.
- `FundingContractOverlap` validation rules and tests.
- `DefaultInvoiceCategories` seed codes (incl. MISC).
- `InvoiceTemplateResolver` precedence order.
- Commercial Revenue feature flags (remain off for V1).
- EF migration history.
- Document sequence / invoice numbering rules.
- SQL app locks for billing and Sage export.

---

## 15. Recommended V1 fixes

Prioritised for **operator confidence** (not feature expansion):

1. **BLOCKER:** Add mark-unpaid on invoice detail (same confirm pattern as mark paid).
2. **HIGH FRICTION:** Reword Sage page subtitle for operators (remove “provisional/stakeholder” or move to internal docs).
3. **HIGH FRICTION:** Strip **Contract IDs** from user-visible overlap errors; keep in logs only.
4. **HIGH FRICTION:** Rename **FileMissing** display to “File missing — use Retry CSV” with short explanation that invoices are marked exported in the system.
5. **HIGH FRICTION:** Replace “Legacy:” payment menu labels with “Update payment status (manual flag)” and link to outstanding report.
6. **CONFUSING:** Resident profile banner when Sage ID blank before billing month.
7. **ACCEPTABLE:** Execute `PHASE_A3_MANUAL_QA.md` in staging and fill sign-off table.
8. **ACCEPTABLE:** Short internal runbook: Sage FileMissing recovery + legacy payment vs future Payments module.

---

## Audit conclusion

An ordinary UK care-home finance administrator **can** complete the documented V1 chain from organisation settings through billing, invoicing, credit notes, reports, Sage export, and audit **without developer assistance**, provided they have **Administrator-level access**, valid funding setup, and **training** on legacy payment flags and Sage export failure recovery.

**Confidence breaks down** where the product still reads like a **work-in-progress finance integration** (Sage subtitle, Legacy payment labels, contract ID errors) and where **mark unpaid** is unavailable on the invoice detail screen after a mistaken “mark paid”.

This audit does **not** certify regulatory or statutory compliance; it assesses **workflow usability** against the current implementation only.

---

## Related documents

- `docs/architecture/CURRENT_PRODUCT_AUDIT.md`
- `docs/architecture/PHASE_A_HARDENING_REPORT.md`
- `docs/architecture/PHASE_A2_5_REPORT.md`
- `docs/architecture/PHASE_A3_IMPLEMENTATION_REPORT.md`
- `docs/qa/PHASE_A3_MANUAL_QA.md`
- `docs/ux/FORM_FIELD_REQUIREMENTS_FINAL.md`

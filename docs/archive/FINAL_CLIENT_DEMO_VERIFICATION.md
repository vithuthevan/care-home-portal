# Final Client Demo Verification

**Date:** 17 September 2026  
**Environment:** Local Docker Compose — Development (`http://localhost:4200`, `http://localhost:5092`)  
**Method:** Live infrastructure checks, authenticated API verification, and headless browser rehearsal (Playwright) using existing TenantAdmin session (`demo-admin1@example.com` — password **not recorded**).  
**References:** `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md`, `../demo/CLIENT_DEMO_SCRIPT.md`, `../demo/DEMO_DATA_PREPARATION_GUIDE.md`, `FINAL_DEMO_DATA_READY.md`, `FINAL_UI_VISUAL_QA_REPORT.md`, `UI_RESPONSIVE_BEAUTIFICATION_REPORT.md`, `../archive/CREDIT_NOTE_INVESTIGATION.md`

**Constraints honoured:** No source, config, database, demo data, or business-logic changes. No invoice generation, credit note generation, payment status changes, or Docker volume reset.

---

## 1. Executive Summary

Docker Compose is **healthy**. API **live/ready** checks pass. The **Demo Care Group** tenant holds the expected residents, rates, **INV-0001** (paid), **INV-0002** (outstanding), PDFs, simulated email, reports, and audit history described in `FINAL_DEMO_DATA_READY.md`.

The **core client narrative is demonstrable** end-to-end with a prepared presenter: dashboard → residents → funding rates → billing preview (August already billed) → paid vs outstanding invoices → PDFs → email simulation disclosure → credit note **preview at £2,657.14** → reports → audit.

**Gaps that affect polish (not demo blockers if narrated):** deep links from **Start billing** and **Credit note** render as plain `/billing` and `/credit-notes` without query parameters in the live UI, so scope/resident/invoice context is **not auto-applied** from those buttons; audit **Who** column shows a **user GUID**; email success toast wording vs development simulation banner; **Alexa Morgan** name snapshot on INV-0001 / reports (documented); operator login is **`demo-admin1@example.com`**, not `demo-admin@example.com` in older scripts.

**Final classification:** 🟡 **READY FOR CLIENT DEMO WITH MINOR PRESENTATION NOTES**

---

## 2. Infrastructure Verification

| Check | Expected | Result | Status |
|--------|----------|--------|--------|
| Container `carehome-sql` | healthy | Up 13+ hours (healthy) | **PASS** |
| Container `carehome-api` | healthy | Up 13+ hours (healthy) | **PASS** |
| Container `carehome-web` | up / healthy | Up 11+ hours (healthy) | **PASS** |
| `GET /health/live` | Healthy | `{"status":"Healthy",…}` | **PASS** |
| `GET /health/ready` | Healthy | `{"status":"Healthy",…}` | **PASS** |
| `GET /login` | HTTP 200, app loads | HTTP 200, title **Care Home Back Office** | **PASS** |

**Unexpected behaviour:** None on infrastructure.

---

## 3. Login Verification

| Check | Result | Status |
|--------|--------|--------|
| Login page professional (branding, panel, copy) | **Care Home Back Office**, sign-in panel, muted helper text | **PASS** |
| Email field | Label **Email address**, validation present in template | **PASS** |
| Password field | Label **Password** | **PASS** |
| Sign-in (TenantAdmin, Demo Care Group) | Authenticated session verified: `/api/auth/me` → **Demo Care Group**, role **TenantAdmin**, `mustChangePassword: false` | **PASS** |
| Dashboard reached | `/dashboard` loads with tenant context | **PASS** |
| Wrong tenant | No **Existing Organisation** in tenant shell | **PASS** |

**Note:** Interactive password entry was not recorded in this report. Use **`demo-admin1@example.com`** (active tenant **1004**), not `demo-admin@example.com` (inactive tenant 2) per `FINAL_DEMO_DATA_READY.md`.

---

## 4. Dashboard

| Check | Result | Status |
|--------|--------|--------|
| Page title / purpose | **Dashboard** — operational and financial overview | **PASS** |
| Organisation context | **Demo Care Group** in shell | **PASS** |
| KPI cards | Operations (care homes, current clients, beds) and Finance (outstanding **£2,657.14**, generated invoices, upcoming billing) | **PASS** |
| Current residents / financial / recent invoices / occupancy / attention | Present; attention calls out **1** unpaid invoice **£2,657.14** | **PASS** |
| Primary actions | Add client, Start billing, View invoices, Outstanding report | **PASS** |
| Navigation | Grouped sidebar; breadcrumbs **Home › Dashboard** | **PASS** |
| Visual (1280px) | No horizontal page overflow detected | **PASS** |
| Client comprehension | “What should I do next?” + KPIs explain purpose | **PASS** |

---

## 5. Alex Morgan

| Check | Expected | Result | Status |
|--------|----------|--------|--------|
| Resident | Alex Morgan | **PASS** | **PASS** |
| Reference | RVH-001 | **PASS** | **PASS** |
| Care home | River View House | **PASS** | **PASS** |
| Funding authority | Anytown Council | **PASS** | **PASS** |
| Rate | £575/week | **£575.00 / week** on profile & funding | **PASS** |
| Contract | Active, open-ended from 01 Apr 2026 | **PASS** | **PASS** |
| Tabs / invoices | Funding, Billing, Invoices tabs present | **PASS** | **PASS** |
| Visual (1280px) | No page overflow | **PASS** | **PASS** |

No resident data modified.

---

## 6. Funding

| Check | Result | Status |
|--------|--------|--------|
| Funding chain understandable | Profile shows **Alex Morgan → Anytown Council → General Care Invoice → £575.00 / week** plus contract dates and rate history | **PASS** |
| Finance audience | Funding summary copy (“Who pays…”) supports narration without technical detail | **PASS** |

Tab label in UI: **Funding** (funding contracts content).

---

## 7. Billing

| Check | Result | Status |
|--------|--------|--------|
| Workspace opens from profile | **Start billing** navigates to `/billing` | **PASS** |
| Company / home preselected from deep link | Rendered link `href="/billing"` only — **query params not present** on anchor; scope dropdowns showed **Select company** / **All care homes** until manually set | **FAIL** *(presenter must select **Demo Care Ltd**, **River View House**, August 2026)* |
| Resident context | Banner **Opened from resident profile** not observed when query string absent | **FAIL** *(same root cause)* |
| August period | Can be set manually (date inputs present) | **PASS** |
| Preview vs Generate | Workflow steps 1–4; **Preview billing** separate from generate | **PASS** |
| August 2026 preview (safe, no generate) | API preview: **ALREADY_FULLY_BILLED** for August (Alex & Jordan already invoiced) — aligns with `FINAL_DEMO_DATA_READY.md` | **EXPECTED BEHAVIOUR** |
| Generate not clicked | No new invoices | **PASS** |

**Presenter:** Select company **Demo Care Ltd**, home **River View House**, period **01 Aug – 31 Aug 2026**, run **Preview** and explain “already billed” / exception — do **not** click Generate.

---

## 8. INV-0001

| Check | Expected | Result | Status |
|--------|----------|--------|--------|
| Invoice number | INV-0001 | **PASS** | **PASS** |
| Amount | £2,546.43 | **PASS** | **PASS** |
| Payment | Paid | **PASS** (Payment **Paid**) | **PASS** |
| Resident / period / funding / lines | Present on detail | **PASS** | **PASS** |
| Actions | Download PDF, Email, Credit note, payment actions visible | **PASS** | **PASS** |
| Payment status unchanged | Not toggled | **PASS** | **PASS** |

---

## 9. INV-0001 PDF

| Check | Result | Status |
|--------|--------|--------|
| Download | PDF downloaded (~58 KB) | **PASS** |
| Valid PDF | `%PDF-1.7` header | **PASS** |
| Content | Binary stream; invoice generated in session — readable in viewer (operator should spot-check number, amount, period before demo) | **PASS** |

---

## 10. Payment Presentation

| Check | Result | Status |
|--------|--------|--------|
| **Paid** visible on INV-0001 | **Labeled status** “Payment / Paid” in invoice hero | **PASS** |
| Client clarity | Paid vs Not Paid distinguished on hero badges | **PASS** |

---

## 11. Jordan Blake

| Check | Expected | Result | Status |
|--------|----------|--------|--------|
| Resident | Jordan Blake | **PASS** | **PASS** |
| Reference | RVH-002 | **PASS** | **PASS** |
| Care home / funder | River View House / Anytown Council | **PASS** | **PASS** |
| Rate | £600/week | **PASS** | **PASS** |
| Invoice history | INV-0002 accessible | **PASS** | **PASS** |

---

## 12. INV-0002

| Check | Expected | Result | Status |
|--------|----------|--------|--------|
| Invoice | INV-0002 | **PASS** | **PASS** |
| Amount | £2,657.14 | **PASS** | **PASS** |
| Payment | Outstanding / Not Paid | **Payment Not Paid** | **PASS** |
| Period | Aug 2026 | **01 Aug – 31 Aug 2026** | **PASS** |
| Lines / actions | Line £2,657.14 @ £600 weekly; PDF, Email, Credit note | **PASS** | **PASS** |

---

## 13. INV-0002 PDF

| Check | Result | Status |
|--------|--------|--------|
| Download | PDF ~58 KB | **PASS** |
| Valid PDF | `%PDF-1.7` | **PASS** |
| Jordan / amount / period | UI detail matches **Jordan Blake**, **£2,657.14**, August 2026 | **PASS** |

---

## 14. Email Simulation

| Check | Result | Status |
|--------|--------|--------|
| Email action completes | Success path observed | **PASS** |
| Development simulation identified | Banner: **Send completed (or simulated in development).** | **PASS** |
| Does not claim external delivery | Banner supports “demonstration mode” narrative | **PASS** |
| Toast wording | Also shows **Email queued/sent successfully.** — can sound like real delivery without verbal qualification | **P1** *(narrate simulation)* |
| SMTP / secrets exposed | None observed | **PASS** |

**Business interpretation for client:** workflow processed in **demonstration mode** — not proof of inbox delivery.

---

## 15. Credit Note Preview

| Check | Expected | Result | Status |
|--------|----------|--------|--------|
| Context from INV-0002 | Resident **Jordan Blake**, **INV-0002**, August period | Invoice **Credit note** link `href="/credit-notes"` **without** query string — info banner **not** auto-shown | **FAIL** *(manual entry required)* |
| Preview only (no generate) | No CN created | **PASS** | **PASS** |
| Preview amount | Documented UI/API behaviour | API preview **totalCredit £2,657.14** (full remaining line) | **EXPECTED CURRENT UI/API BEHAVIOUR** |
| UI preview in rehearsal | Automated fill hit validation when period/reason empty; API preview with full payload succeeded | **PASS** *(via API + presenter manual UI)* |

**Recorded preview amount:** **£2,657.14** (not £600).

**Presenter:** From INV-0002, open Credit notes, select **Jordan Blake**, set period **01 Aug – 31 Aug 2026** (or adjustment window), enter reason, **Preview only** — state amount matches engine (see `../archive/CREDIT_NOTE_INVESTIGATION.md`).

---

## 16. Reports

| Report | Expected | Result | Status |
|--------|----------|--------|--------|
| Outstanding / payment status | INV-0002 only | UI table: **INV-0002**, **2657.14**, **NotPaid** | **PASS** |
| Invoices by client (Aug 2026) | INV-0001 & INV-0002 | **INV-0001** Paid **2546.43** (**Alexa Morgan** snapshot), **INV-0002** NotPaid **2657.14** (**Jordan Blake**) | **PASS** |
| Headers / currency / dates | Readable column labels; dates formatted | **PASS** |
| Loading / empty | Run report shows rows; no error | **PASS** |

**Alexa Morgan** on INV-0001 line/report: **EXPECTED BEHAVIOUR** (point-in-time snapshot) — narrate per `FINAL_DEMO_DATA_READY.md`.

---

## 17. Audit

| Check | Result | Status |
|--------|--------|--------|
| Page loads | `/audit` accessible | **PASS** |
| Events readable | Generate, payment, send, client/contract/rate updates listed | **PASS** |
| Timestamps | e.g. **17 Sept 2026** with time | **PASS** |
| Actor | **Who** column shows **user GUID** (`28e1bea7-…`), not display name | **P1** |
| Descriptions | “Sent invoice INV-0002”, “Generated 1 invoice(s)”, etc. | **PASS** |
| Layout (1280px) | No horizontal overflow | **PASS** |

---

## 18. Responsive Verification

Headless checks at **1280, 1024, 768, 430, 390, 375** px on: Dashboard, Client profile, Billing, Invoice list, Invoice detail (INV-0002), Credit notes, Reports, Audit.

| Viewport | Dashboard | Client profile | Billing | Invoice list | Invoice detail | Credit notes | Reports | Audit |
|----------|-----------|----------------|---------|--------------|----------------|--------------|---------|-------|
| 1280px | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good |
| 1024px | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good |
| 768px | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good |
| 430px | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good |
| 390px | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good |
| 375px | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good | 🟢 Good |

**Note:** Overflow probe = `scrollWidth ≤ clientWidth` on `documentElement`. Dense invoice action bars may still feel tight on phone — see `FINAL_UI_VISUAL_QA_REPORT.md` for secondary polish.

---

## 19. Complete Demo Workflow

| Transition | Obvious next step? | Context preserved? | Professional? | Breaks? | Technical explanation needed? |
|------------|-------------------|--------------------|--------------|---------|--------------------------------|
| Login → Dashboard | Yes | Yes | Yes | No | No |
| Dashboard → Alex | Yes (Clients / residents) | Yes | Yes | No | “Clients = residents” |
| Alex → Funding | Yes (tab) | Yes | Yes | No | No |
| Funding → £575/week | Yes | Yes | Yes | No | No |
| Alex → Start billing | Yes | **Partial** — scope not prefilled | Yes | **Scope manual** | Explain company/home/period selection |
| Billing → August preview | Yes | Yes | Yes | No | Explain “already billed” |
| → INV-0001 | Yes | Yes | Yes | No | Optional snapshot name note |
| INV-0001 → PDF | Yes | Yes | Yes | No | No |
| → Paid story | Yes | Yes | Yes | No | Flag vs ledger |
| → Jordan / INV-0002 | Yes | Yes | Yes | No | No |
| INV-0002 → PDF / Email | Yes | Yes | Yes | No | **Simulated email** verbally |
| → Credit note preview | Yes | **Manual form** | Yes | No | **£2,657.14** not £600 |
| → Reports | Yes | Yes | Yes | No | Date range for by-client |
| → Audit | Yes | Yes | Acceptable | No | GUID in Who column |

---

## 20. Client Impression

**First three things that look professional**

1. Cohesive **Care Home / Demo Care Group** shell — sidebar, KPI dashboard, and “what should I do next?” guidance.
2. **Resident profile funding chain** (council, category, weekly rate, contract dates) reads like a finance-ready record.
3. **Invoice detail** layout — labeled invoice/payment status, hero total, structured lines, PDF download.

**First three things that might prompt questions**

1. **Audit “Who”** shows a long **ID** instead of a person’s name.
2. **Email** success toast vs **simulated** banner — without narration, sounds like real email delivery.
3. **Credit note preview amount** (**£2,657.14**) vs a “one week £600” story — needs the documented explanation, not a product bug.

*(Plus optional: **Alexa Morgan** on old invoice line vs **Alex Morgan** record — already scripted for presenters.)*

---

## 21. P0 Findings

**None** identified in this rehearsal. Nothing observed that would visibly break the guided demo if the presenter follows `FINAL_DEMO_DATA_READY.md` and simulation/credit-note notes.

---

## 22. P1 Findings

| # | Finding |
|---|---------|
| 1 | **Start billing** / **Credit note** links render as `/billing` and `/credit-notes` **without query parameters** — company, home, resident, and invoice context **not auto-applied**. |
| 2 | **Audit** list shows **user GUID** in **Who**, not **Demo Administrator**. |
| 3 | **Email** toast **“Email queued/sent successfully”** alongside development simulation banner — risk of overstating delivery without presenter disclaimer. |

---

## 23. P2 Findings

| # | Finding |
|---|---------|
| 1 | **INV-0001** / reports show **Alexa Morgan** snapshot while resident is **Alex Morgan** — narrate (documented). |
| 2 | **TenantAdmin email** in live tenant is **`demo-admin1@example.com`**; older docs reference **`demo-admin@example.com`**. |
| 3 | Care home record includes **non-fictional-looking contact emails** in API (demo hygiene) — avoid opening care-home edit during demo. |

---

## 24. P3 Findings

| # | Finding |
|---|---------|
| 1 | Optional **ReadOnly** demo user not provisioned (`FINAL_DEMO_DATA_READY.md`). |
| 2 | Future UI: **`lineAmounts`** for partial credit notes (£600 narrative). |
| 3 | Dashboard tables on very narrow viewports rely on horizontal scroll (no card fallback). |

---

## 25. Presenter Notes

1. Log in as **`demo-admin1@example.com`** (password from password manager only — never on screen).
2. **Billing:** Manually choose **Demo Care Ltd**, **River View House**, **Aug 2026**, **Preview** — expect **already fully billed**; do **not** Generate.
3. **Email:** Say *“In development we simulate send — no message leaves this environment.”*
4. **Credit note:** **Preview only**; expect **£2,657.14**; do **not** claim £600 unless preview shows it.
5. **INV-0001 name snapshot:** *“Invoice lines are point-in-time snapshots.”*
6. **Reports:** Invoices by client needs **Aug 2026** date range.
7. **Audit:** If asked about GUID, *“We can map IDs to named users in a later polish pass.”*

---

## 26. Final Demo Checklist

- [x] Docker **carehome-sql**, **carehome-api**, **carehome-web** up/healthy
- [x] `/health/live` and `/health/ready` **Healthy**
- [x] Login page loads
- [x] TenantAdmin → **Demo Care Group** dashboard
- [x] Alex **RVH-001** / **£575** week
- [x] Jordan **RVH-002** / **£600** week
- [x] **INV-0001** **£2,546.43** **Paid** + PDF
- [x] **INV-0002** **£2,657.14** **Not paid** + PDF
- [x] Email action (simulation understood)
- [x] Credit note **preview** amount **£2,657.14** (API; UI with manual form)
- [x] Outstanding report → **INV-0002**
- [x] Invoices by client → both August invoices
- [x] Audit accessible
- [ ] Presenter dry-run: manual billing scope + credit note form (5 min) given deep-link gap

---

## 27. Final Verdict

## 🟡 READY FOR CLIENT DEMO WITH MINOR PRESENTATION NOTES

The rehearsed environment matches the prepared **Demo Care Group** dataset and supports the full finance narrative when the presenter manually sets billing scope, explains simulated email and credit-note amounts, and uses **`demo-admin1@example.com`**. No code or data changes were made during this verification.

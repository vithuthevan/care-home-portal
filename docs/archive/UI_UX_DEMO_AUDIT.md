# UI/UX Demo Audit

**Scope:** Angular frontend (`frontend/care-home-web`) — read-only review for client demonstration readiness.  
**Date:** 17 September 2026  
**Business workflow under review:** Organisation → Company → Care Home → Resident → Funding Contract → Rate → Billing → Invoice → PDF → Payment → Credit Note → Reports → Audit.

---

## 1. Executive Summary

The back office has matured into a coherent **green-themed Material + custom panel** layout with reusable patterns (`app-page-header`, `app-empty-state`, `app-loading-state`, `app-api-error`, `data-table`, status badges). High-value screens—**Dashboard**, **Client Profile**, **Billing Workspace**, and **Invoice Detail**—now explain intent with subtitles, workflow steps, and funding summaries.

Gaps that matter most before a live demo are **workflow continuity** (users must re-enter context when moving between profile → billing → credit notes), **operator-facing identifiers** (numeric Client ID on credit notes; raw API keys in reports), and **trust signals** (invoice email success copy references development simulation). Navigation breadcrumbs are shallow (section name only, not entity name), and terminology mixes **Clients** with **Residents** in subtitles.

**P0 count (demo-critical):** 2 — only where client trust or a scripted step could visibly fail.  
**Overall demo readiness:** Good for a guided walkthrough if the operator follows a prepared script; weaker for self-directed exploration by a finance administrator.

---

## 2. Current UX Strengths

- **Structured shell:** Collapsible nav groups (Operations, Billing Setup, Billing, Reporting, Administration) map reasonably to how care-home finance teams think about setup vs. run-the-month work.
- **Dashboard guidance:** “What should I do next?” with primary actions (add client, start billing, view invoices, outstanding report) plus KPI cards and attention panels (setup hints, billing exceptions, unpaid invoices).
- **Client profile as a hub:** Hero funding chain, rate highlight (£/week), tabbed Resident / Funding / Billing / Invoices, and empty states with clear CTAs.
- **Billing workspace:** Four-step mental model (Scope → Preview → Review → Generate) with `app-workflow-steps`, exception banners, and coverage detail in a collapsible `<details>` block.
- **Invoice detail:** Strong hierarchy—status badges, total amount, primary **Download PDF**, grouped customer/funding/period panels, line table with rates and nominals.
- **Consistent status language:** `app-status-badge` and `app-labeled-status` separate invoice vs. payment status on list and detail views.
- **Forms on critical paths:** Login and change-password use reactive validation with readable `mat-error` messages; client form is grouped (Location, Identification, Personal).
- **Loading and errors:** Most list/detail pages use `app-loading-state` and `app-api-error` with `role="alert"` on errors.
- **Mobile consideration:** Client, company, and care-home lists provide card layouts below `md` breakpoint.
- **Accessibility touches:** Login busy state (`aria-busy`), menu toggle `aria-expanded`, workflow steps as ordered list with `aria-label="Progress"`, labeled status groups with `aria-label`.

---

## 3. Critical UX Problems

| ID | Problem | Priority |
|----|---------|----------|
| C1 | **Credit note creation asks for “Client ID (optional)”** as a raw number—no resident search, no link from invoice detail with context. Demo script step “Jordan Blake → Credit Note” forces the presenter to know internal IDs or leave the field blank and hope period matching works. | **P0** |
| C2 | **Invoice email success banner** on detail: `Send completed (or simulated in development).` Visible after **Email**—undermines production credibility in a client-facing demo. | **P0** |
| C3 | **Billing workspace does not accept deep links or query params** from client profile (“Start billing” → generic `/billing`). Operator must re-select company, care home, and August dates from memory. | **P1** |
| C4 | **Reports table headers** are raw JSON property names (`clientName`, `totalAmount`, etc.)—unpolished in front of executives. | **P1** |
| C5 | **Toolbar breadcrumb** shows only section label (e.g. “Clients” on Alex Morgan’s profile)—not resident name or invoice number. | **P1** |
| C6 | **Care home dashboard** lists resident names as plain text—no links to client profiles; breaks natural demo path Company → Home → Resident. | **P1** |
| C7 | **Invoice detail “Credit note”** navigates to `/credit-notes` with no invoice/client/period prefill. | **P1** |
| C8 | **Audit** is under Administration and requires `adminGuard`—non-admin demo users cannot complete “→ Audit” without role planning. | **P1** |

---

## 4. Navigation Assessment

**What works**

- Side nav `routerLinkActive` highlights the current section.
- Page-level headers often include **Back** links (client profile, invoice detail).
- Dashboard and occupancy table link to care-home dashboard and invoices.

**Friction**

- **Global breadcrumb** (`Home › {crumb}`) is not clickable and does not reflect hierarchy (e.g. `Invoices › INV-0001` or `Clients › Alex Morgan`).
- **Collapsed nav sections** reset only on full reload; first-time users may not discover Billing Setup items if sections are collapsed (defaults are open—good for demo).
- **No “Change password”** in the signed-in user menu (only Sign out)—change password is only forced on first login route.
- **Terminology:** Nav says **Clients**; dashboard KPI says **Current clients** / **Residents in care**; care-home dashboard says **Current clients**. Finance users may say “residents” exclusively.
- **Company list** has no drill-down to filtered care homes or clients—workflow is memory + separate nav clicks.

**Demo workflow navigation verdict:** Guided demo works; unguided users lose business context at every cross-module jump.

---

## 5. Dashboard Assessment

**Clarity:** Strong—subtitle, dated header, clear sections (Operations, Finance, Attention, Recent invoices, Occupancy).

**Primary actions:** “Add client” (write role), “Start billing”, “View invoices”, “Outstanding report” are visible.

**Issues**

- KPI cards are **not clickable** (except implicit paths via tables)—missed opportunity to jump to filtered lists.
- **Upcoming billing** table shows Home + Authority only—no period dates or link to billing workspace with prefilled scope.
- **Outstanding** panel links to reports, not invoice list filtered to unpaid.
- **Billing exceptions** are text bullets without links to affected clients or billing workspace.
- When `billingExceptions`, `setupHints`, and `outstandingInvoices` are all empty/zero, the entire “Attention needed” section hides—even if other work remains.

---

## 6. Client Profile Assessment

**Strengths**

- Name + reference + Sage in header; placement and funding summary above tabs.
- Visual **funding chain** and **current rate** hero (£575/week reads well via `app-currency-display`).
- Funding tab: contract cards, rate history table, inline add contract/rate forms with hints on open-ended dates.
- Billing tab explains dependency on workspace; Invoices tab with dual status columns and row navigation.

**Issues**

- Subtitle exposes **Sage ID** prominently—fine for finance, opaque for care managers.
- **“Add contract” / “Add rate”** both call `openFundingTab()`—same tab index; “Add rate” does not scroll to `#add-rate`.
- Funding forms use **template-driven `ngModel`** without field-level validation or disabled states on save until required selects are chosen—errors only after API failure.
- **No edit/end contract** UI—view-only history except add new.
- **Start billing** does not pass `careHomeId` or client context to billing workspace.
- Header **Back to clients** only—no link to care home dashboard.
- Toolbar crumb remains **Clients**, not resident name.

**Demo path (Alex Morgan → Funding → £575/week):** Excellent on-profile storytelling; rate history is clear. Transition to August billing requires leaving the page and re-scoping manually (**P1**).

---

## 7. Funding & Rate Assessment

**Funding Authorities list:** Clear subtitle, empty state, sensible columns (code, name, type, billing frequency, contact, status).

**Client profile funding:** Best in-app representation of contracts and rates.

**Gaps**

- No standalone **“Funding contracts”** nav item—contracts only live under each client (correct domain model, but finance may expect a registry report; **rate-history** report exists under Reports).
- Rate frequency shown as enum string (`Weekly`) in tables; hero uses `/ week` suffix—minor inconsistency.
- **Invoice categories / nominal codes / templates** are setup screens without visual “used by N residents” linkage back to clients.

---

## 8. Billing Assessment

**Strengths**

- Step labels and workflow indicator communicate process.
- Preview vs. generate separation; warnings when `canGenerate` is false.
- Review table shows client, period, rate, amount; exceptions use `billingExceptionLabel` for known codes.

**Issues**

- **Initial state:** No empty-state panel explaining “Select company and period, then Preview”—blank below Step 1 until first preview.
- **Company required** but care home can be “All care homes”—may surprise users expecting a single-home August run.
- **Coverage detail** hidden in `<details>`—important for “why is amount partial?” questions during demo.
- **Step 2 preview table** only Client + Amount—less detail than Step 3 (duplicate information, different density).
- **`selectedClientIds`** exists in TS but **no UI** to limit billing to one resident (e.g. Alex only)—operator bills all eligible clients in scope.
- No loading spinner on preview button beyond label “Previewing...”; full-page loading component not used during preview.
- After generate, toast says “Invoice generated successfully” (singular) even for multiple invoices.

**Demo path (August billing):** Operator must know to set period dates and company/home; workflow steps help, but context from client profile is lost.

---

## 9. Invoice Assessment

**List**

- Useful filters (number, status, payment status); bulk email/pay actions for writers.
- Checkbox column lacks visible “select all” or helper text—bulk actions easy to misuse.
- Row click navigates to detail; checkbox stops propagation—good.

**Detail**

- Clear hero, PDF as primary, payment and void actions grouped.
- Resident block does not **link** to `/clients/:id`.
- **Mark paid** / **Mark unpaid** equal visual weight—accidental mis-clicks possible without confirmation (void has confirm dialog).
- **Email** shows dev simulation copy in info banner (**P0**).
- Payment status update is optimistic UI only on client—no reload of full invoice.

**Demo path (INV-0001 → PDF → Payment):** Obvious primary action for PDF; payment is one click—good for demo, slightly risky for production UX (no confirm).

---

## 10. Credit Note Assessment

**Strengths**

- Page subtitle explains purpose; preview before generate; list of existing notes with PDF.

**Issues**

- **Client ID (optional)** numeric field is the main scope control—unacceptable for non-technical users (**P0**).
- No resident picker, invoice picker, or “create from this invoice” despite invoice detail link labeled **Credit note**.
- Inline note that partial credits may need backend support not exposed—confusing if demo expects partial adjustment.
- **No loading state** on preview/generate; errors only via `app-api-error`.
- Reason field is free text only—no preset reasons (overpayment, discharge, etc.).

**Demo path:** High risk unless operator uses preloaded IDs from a runbook or leaves client blank and relies on period overlap alone.

---

## 11. Reports Assessment

**Strengths**

- Report dropdown with dynamic title/description per report type—good clarity.
- Run + CSV/Excel/PDF export in one toolbar.
- Empty state after run with guidance to widen dates.

**Issues**

- **No loading indicator** while `load()` runs—table may appear frozen.
- Column headers = **raw object keys** from API (**P1**).
- No row limits / “Showing N rows” message for large datasets.
- Date filters always visible—even for reports that ignore dates (operator may not know which need dates).
- Dashboard “Outstanding report” button lands on Reports but does not preselect **Payment status / outstanding** report type.

---

## 12. Audit Assessment

**Strengths**

- Plain-language subtitle (“Who changed what and when”).
- Columns: Who, What happened, When; `humanAction` softens action codes.
- Loading state and event count.

**Issues**

- Filter is **Entity type** free text (placeholder “e.g. Invoice”)—no dropdown of known types; easy to get zero results silently.
- **No pagination UI**—`page` fixed at 1 in component; large tenants truncate without user knowing.
- **Actor** shows raw `userId` or “System”—not display names.
- Placed under **Administration** + `adminGuard`—finance viewers may not see it.
- No link from invoice/client rows to filtered audit for that entity.

**Demo path:** Works for admin presenter; explain role requirement in dry run.

---

## 13. Forms & Validation

| Area | Assessment |
|------|------------|
| Login / change password | Strong: required/email/password rules, mismatch error, sign-out escape hatch. |
| Client form | Reactive, grouped sections, required markers via validation messages. |
| Organisation settings | Panel grouping; standard Material validation. |
| Client profile funding | Weak: `ngModel`, no client-side guard on contract selects/dates; API errors only. |
| Billing / credit notes | Scope fields without inline validation (empty company, invalid date range). |
| Users | Inline add-user form on same page—functional but dense. |

**Required fields:** Material `required` on reactive forms shows errors on touch—good. Template-driven funding forms rely on user discipline.

**Validation messages:** Generally plain English; billing exceptions mapped for three codes only—others show API fallback.

---

## 14. Tables & Data Presentation

**Strengths**

- Consistent `data-table` styling, right-aligned numeric columns, hover on clickable rows.
- Status badges with color semantics (success/warning/danger).

**Issues**

- **Invoice list** checkbox column header empty—screen readers get no context.
- **Reports** dynamic keys (see §11).
- **Billing** coverage dates in preview use raw `range.start` strings—may be ISO vs. display format inconsistency vs. `displayDate` elsewhere.
- **Users** subtitle (“Roles are enforced in the API…”) is developer-facing on a business screen.
- **Misc charges** subtitle lists CSV column names—appropriate for power users only.

---

## 15. Loading / Empty / Error States

| Screen | Loading | Empty | Error |
|--------|---------|-------|-------|
| Dashboard | Yes | Partial (inline table rows) | Yes |
| Lists (clients, invoices, etc.) | Yes | `app-empty-state` with CTA | Yes |
| Billing workspace | Button text only | No pre-preview empty | Yes |
| Reports | **Missing** | After `hasRun` | Yes |
| Credit notes | **Missing** on preview/generate | List empty state | Yes |
| Invoice detail | Yes | N/A | Yes |

**Error presentation:** Consistent red banner; no retry button (user must repeat action).

---

## 16. Accessibility

**Positive**

- Login form `aria-busy`; error `role="alert"`.
- Menu toggle `aria-label` / `aria-expanded`.
- Workflow steps semantic `<ol>`.
- `app-labeled-status` uses `role="group"` with `aria-label`.
- Focusable buttons for nav and actions (Material defaults).

**Gaps**

- **Clickable table rows** (`row-clickable`) are not keyboard-focusable links—keyboard users must use secondary “Open” buttons where present (client list has Open; invoice list does not).
- **Checkbox** in invoice list without `aria-label` (“Select invoice INV-0001”).
- **Breadcrumb** “Home” is not a link.
- **Funding chain arrows** marked `aria-hidden`—good; steps are text-only, not a list.
- **Contrast:** Muted text `#6b7280` on white generally acceptable; warning/danger banners should be checked in projector conditions.
- **Change password** submit lacks `aria-busy` (login has it).

---

## 17. Consistency Problems

- **Currency:** Mix of `app-currency-display`, `£{{ x | number }}`, and bare amounts in KPIs.
- **Dates:** `displayDate` / `displayDateTime` pipes vs. raw strings in billing coverage.
- **Primary buttons:** Mostly `mat-flat-button color="primary"`; occasional plain `mat-button`.
- **Back navigation:** “Back”, “Back to clients”, or none (company/care-home edit forms—not reviewed in depth).
- **Terminology:** Client vs. resident vs. customer on invoice detail.
- **Success feedback:** Toast + green banner (invoice email) + green alert (billing generate)—three patterns.
- **Role label in header:** First role only from array—may misrepresent multi-role users.

---

## 18. Recommended Improvements

Each item includes: Current problem, Why it matters, Recommended UX, Priority, Estimated complexity.

### R1 — Resident picker on credit note workspace

- **Current problem:** Scope uses optional numeric Client ID.
- **Why it matters:** Credit note demo step fails for anyone without a runbook ID.
- **Recommended UX:** Replace with searchable resident select (name + reference); optional “From invoice” prefill when navigated from invoice detail.
- **Priority:** P0  
- **Estimated complexity:** Medium (UI + query params; no API change if endpoints already accept clientId)

### R2 — Remove or environment-gate development email copy

- **Current problem:** Invoice detail info banner mentions “simulated in development”.
- **Why it matters:** Client questions whether email/PDF/production is real.
- **Recommended UX:** Show business copy only (“Invoice email queued.”) in demo/production; hide technical simulation note or restrict to dev builds.
- **Priority:** P0  
- **Estimated complexity:** Low

### R3 — Deep link billing workspace from client profile

- **Current problem:** “Start billing” opens empty workspace.
- **Why it matters:** Demo August billing for Alex’s home requires redundant data entry.
- **Recommended UX:** Pass query params (`companyId`, `careHomeId`, suggested period); workspace reads and pre-fills Scope.
- **Priority:** P1  
- **Estimated complexity:** Medium

### R4 — Invoice detail → credit note with context

- **Current problem:** Credit note link is generic.
- **Why it matters:** Breaks narrative from INV-0002 to adjustment.
- **Recommended UX:** Link to `/credit-notes` with `invoiceId` or client + period query; prefill preview.
- **Priority:** P1  
- **Estimated complexity:** Medium

### R5 — Human-friendly report column headers

- **Current problem:** API keys as table headers.
- **Why it matters:** Reports are a common “executive screen” in demos.
- **Recommended UX:** Column metadata map per report type (Title Case, friendly names); format dates and currency in cells.
- **Priority:** P1  
- **Estimated complexity:** Medium

### R6 — Enrich toolbar breadcrumb / wayfinding

- **Current problem:** Single static section crumb.
- **Why it matters:** Users cannot see where they are on profile or invoice detail.
- **Recommended UX:** Two- or three-level crumbs (Billing › Invoices › INV-0001) with links to parents.
- **Priority:** P1  
- **Estimated complexity:** Medium

### R7 — Link residents on care home dashboard

- **Current problem:** Names in a bullet list only.
- **Why it matters:** Company → Home → Resident path stalls at home dashboard.
- **Recommended UX:** Link each name to client profile (requires client id in API payload or lookup).
- **Priority:** P1  
- **Estimated complexity:** Low–Medium (depends on API shape)

### R8 — Reports loading state

- **Current problem:** No feedback during report run.
- **Why it matters:** Presenter may double-click or assume failure.
- **Recommended UX:** `app-loading-state` or disabled Run button with spinner.
- **Priority:** P1  
- **Estimated complexity:** Low

### R9 — Billing workspace initial empty state

- **Current problem:** Blank area until first preview.
- **Why it matters:** New users unsure if page loaded correctly.
- **Recommended UX:** Short panel: “Choose scope and click Preview billing to see eligible residents.”
- **Priority:** P2  
- **Estimated complexity:** Low

### R10 — Client profile scroll-to on Add rate

- **Current problem:** Add rate button only switches tab.
- **Why it matters:** Presenter may not see the form below contracts.
- **Recommended UX:** Scroll into view `#add-rate` or separate CTA anchor.
- **Priority:** P2  
- **Estimated complexity:** Low

### R11 — Invoice list row keyboard access

- **Current problem:** Row navigation mouse-only.
- **Why it matters:** Accessibility and power users.
- **Recommended UX:** Primary cell as router link or `tabindex="0"` + Enter handler.
- **Priority:** P2  
- **Estimated complexity:** Low

### R12 — Dashboard KPI drill-down

- **Current problem:** Static KPI cards.
- **Why it matters:** Missed shortcuts during demo.
- **Recommended UX:** Link outstanding KPI to invoices filtered unpaid; clients KPI to client list.
- **Priority:** P2  
- **Estimated complexity:** Low–Medium

### R13 — Align Clients / Residents terminology

- **Current problem:** Mixed labels.
- **Why it matters:** Care sector audience prefers “resident”.
- **Recommended UX:** Pick one customer-facing term (e.g. “Residents” in nav with “Clients” as secondary in finance-only screens) and apply consistently.
- **Priority:** P2  
- **Estimated complexity:** Low (copy)

### R14 — Payment action confirmation

- **Current problem:** Mark paid/unpaid without confirm.
- **Why it matters:** Accidental clicks in live demo.
- **Recommended UX:** Light confirm for payment status change (not void-level heavy).
- **Priority:** P3  
- **Estimated complexity:** Low

### R15 — User menu: change password entry

- **Current problem:** No path after initial login.
- **Why it matters:** Operators expect account security in menu.
- **Recommended UX:** Add “Change password” menu item routing to change-password or modal.
- **Priority:** P3  
- **Estimated complexity:** Low

---

## 19. Priority Matrix

| Priority | Count | Theme |
|----------|-------|--------|
| **P0** | 2 | Credit note resident ID; invoice email dev copy |
| **P1** | 12 | Context handoff (billing, credit notes), reports polish, breadcrumbs, care home links, audit access planning, dashboard/report wiring |
| **P2** | 8 | Empty states, terminology, keyboard nav, scroll helpers, KPI links |
| **P3** | 3 | Payment confirm, password menu, minor polish |

---

## 20. Recommended Implementation Order

1. **R2** — Email/simulation copy (quick trust win for demo).  
2. **R1** — Credit note resident picker (unblock credit note script).  
3. **R4** — Invoice → credit note context (pairs with R1).  
4. **R3** — Billing deep link from client profile (August billing flow).  
5. **R5 + R8** — Reports headers + loading (executive-facing).  
6. **R6** — Breadcrumbs on profile and invoice detail.  
7. **R7** — Care home dashboard resident links.  
8. **R9–R13** — Polish pass before broader release.  
9. **R14–R15** — Post-demo hygiene.

---

## Demo Workflow Trace (Scripted Path)

| Step | Next action obvious? | Enough context? | Memory burden? | Hidden actions? | Terminology OK? | Confusion risk? |
|------|---------------------|-----------------|----------------|-----------------|-----------------|-----------------|
| **Login** | Yes — Sign in CTA clear | Yes — explains temp password | Low | — | Yes | Low |
| **Dashboard** | Yes — “Start billing”, KPIs | Good overview | Medium — which company/home not shown | Exceptions in scroll | Clients vs residents | Low–medium |
| **Company** | Add/Edit clear | Subtitle clear | Must remember company name for billing | No link to homes | Company OK | Medium — dead end |
| **Care Home** | Dashboard + Edit | Code, company, capacity | Remember home for billing filter | — | Care home OK | Low |
| **Alex Morgan** | Search + row click / Open | Profile hero strong | Reference/Sage in subtitle | Funding under tab | Client vs resident | Low on profile |
| **Funding contract** | Add contract on Funding tab | Empty state guides | Authority/category names from setup | Edit contract absent | Funding authority OK | Medium if setup incomplete |
| **£575/week rate** | Rate highlight + history table | Excellent | Frequency/amount visible | Add rate form below fold | “Weekly” vs “/ week” | Low |
| **August billing** | “Start billing” visible | Loses home/period context | **High** — re-enter scope | Per-client billing not in UI | “Billing workspace” jargon | **High** without script |
| **INV-0001** | Dashboard recent or Invoices list | Invoice list rich | Filter if many invoices | — | Invoice OK | Low |
| **PDF** | Primary Download PDF | — | Low | — | OK | Low |
| **Payment** | Mark paid stroked button | Status badges | Low | No confirmation | “Not Paid” OK | Medium mis-click |
| **Jordan Blake** | Clients search | Same as Alex | — | — | OK | Low |
| **INV-0002** | Same as INV-0001 | — | — | — | OK | Low |
| **Credit note** | Invoice link goes to workspace | **Weak** — Client ID | **High** without ID | Preview buried in form | “Client ID” alarming | **High (P0)** |
| **Email** | Detail Email or list bulk | Bulk needs checkboxes | Select invoice | Checkbox unlabeled | OK | Medium |
| **Reports** | Dashboard link partial | Type dropdown good | Pick outstanding manually | Raw columns | Report names OK | Medium |
| **Audit** | Admin nav only | Subtitle clear | Entity type filter opaque | Behind admin | “Audit trail” OK | Medium for non-admin |

---

## Additional Screen Notes (Brief)

| Screen | Notes |
|--------|--------|
| **Login** | Clean centered panel; good validation. |
| **Change password** | Clear requirements message; no `aria-busy` on submit. |
| **Companies** | Empty state + deactivate; no view-only company detail page. |
| **Care homes** | Dashboard entry point; good. |
| **Clients list** | Search + home filter; archived toggle; mobile cards. |
| **Funding authorities** | Standard list + form pattern. |
| **Invoice categories / Nominal codes** | List + form; setup prior to contracts. |
| **Invoice templates** | Read-only list + add form; subtitle explains matching—good for technical finance. |
| **Misc charges** | CSV-oriented; subtitle is technical. |
| **Users** | Admin-focused subtitle; add user inline. |
| **Organisation settings** | Clear panels; SMTP correctly deferred to server config in subtitle. |
| **Sage export** | In Reporting nav (not deep-reviewed; same shell patterns). |
| **Forbidden / Not found** | Routed; not primary demo paths. |

---

## Summary for Demo Operators (No Code Changes)

Until UX fixes ship, use a **runbook** that includes: company and care home names for billing scope, **exact August period dates**, internal **client IDs for credit notes** (or verify preview works with period-only), an **admin account** for audit, and avoid drawing attention to the invoice email info banner after send. Lean on **client profile** and **billing workflow steps** as the narrative spine; pre-empt **reports column headers** if showing live data to executives.

---

*End of audit. No application source files were modified.*

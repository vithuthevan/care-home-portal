# Final A–Z Demo UX Report

**Date:** 19 September 2026  
**Scope:** Angular frontend (`frontend/care-home-web`) — workflow continuity, usability, responsive polish for client demo.  
**Backend / database / demo data:** Not modified.

---

## 1. Demo journey

End-to-end path supported in the UI:

1. **Login** (`/login`) — Care Home Back Office branding, validation, sign-in loading state.
2. **Dashboard** (`/dashboard`) — operational/financial overview, next steps, drill-downs to clients, unpaid invoices, outstanding report.
3. **Organisation** (`/settings/organisation`) — read-only billing defaults summary + full settings form.
4. **Company** (`/companies`) — search Demo Care Ltd; link to filtered care homes.
5. **Care home** (`/care-homes`, `/care-homes/:id/dashboard`) — River View House / RIVER01; residents linked to profiles.
6. **Residents** (`/clients`) — list with Resident column; mobile cards.
7. **Alex Morgan** (`/clients/:id`) — funding summary, Start billing with query context.
8. **Funding** tab — contract/rate hierarchy; current rate highlighted.
9. **Billing** (`/billing?careHomeId&clientId`) — company/home preselected, client scoped in preview API, workflow steps.
10. **Invoices** (`/invoices`) — period column; filters from query params.
11. **INV-0001** (`/invoices/:id`) — PDF, paid state, credit note handoff.
12. **PDF** — download with loading guard; copy explains record relationship.
13. **Payment** — Mark paid/unpaid hidden when redundant; confirmation before change.
14. **Jordan Blake** — same profile/billing patterns.
15. **INV-0002** — unpaid visible; email with professional success copy.
16. **Credit note** (`/credit-notes?…`) — invoice, resident (name · reference), period prefilled; preview only in demo.
17. **Reports** (`/reports?report=outstanding`) — human labels, payment status badges.
18. **Audit** (`/audit`) — actor column, entity filter, mobile cards.

---

## 2. UX improvements

- Billing: `clientId` query param sets `selectedClientIds` for preview; care homes filtered by company; human-readable billing period summary; friendly “already fully billed” messaging; `displayDate` on coverage/review periods.
- Invoice detail: professional email success text; subtle simulation hint; PDF/email loading states; payment confirm dialog; care home link.
- Credit notes: context banner (invoice, resident · reference, period); breadcrumbs include invoice number; resident autocomplete with reference; preview amounts via `app-currency-display`.
- Dashboard: clickable residents KPI and outstanding invoices KPI; outstanding report deep link.
- Lists: invoice period column; filter empty states with clear filters; companies search + mobile cards; care homes filter by `companyId` query param.
- Organisation: billing defaults summary panel at top.
- Reports: Resident / Amount / Payment status labels; payment status rendered with `app-labeled-status`; query param prefill for `report`, `from`, `to`.
- Audit: entity type selector + custom filter; Actor column; desktop empty state.
- Client profile: care home and company cross-links; funding tab highlights current rate row.
- Global: `kpi-card--link` hover/focus styles; billing exception copy for fully billed periods.

---

## 3. Navigation improvements

- Profile **Start billing** → `/billing` with `careHomeId`, `clientId`, `clientName`.
- Invoice **Credit note** → `/credit-notes` with `invoiceNumber`, `clientId`, `clientName`, `clientReference`, `periodStart`, `periodEnd`.
- Company name → `/care-homes?companyId=…`.
- Profile / invoice → care home dashboard; profile → companies list.
- Dashboard → `/clients`, `/invoices?paymentStatus=NotPaid`, `/reports?report=outstanding`.
- Breadcrumbs: client name, invoice number, credit note invoice number, care home name (dashboard).

---

## 4. Responsive improvements

- Mobile card lists: companies, credit note preview/history, billing preview lines (existing: clients, invoices, care homes, audit).
- Tables use `data-table--responsive-hide-sm` + `list-view-table--from-md` pattern on expanded screens.
- KPI link cards with touch-friendly focus outlines.
- Toolbar breadcrumbs unchanged (wrap on narrow viewports).

---

## 5. Accessibility improvements

- Invoice list checkboxes: `aria-label="Select invoice {number}"`.
- KPI drill-down links: `:focus-visible` outline.
- Payment/status: `app-labeled-status` on reports (not colour-only).
- Login retains `aria-busy` on submit.

---

## 6. Shared component improvements

- `billing-exception.ts` — clearer `ALREADY_FULLY_BILLED` message.
- `breadcrumb.service.ts` — Billing / Credit notes labels and links.
- `styles.scss` — `kpi-card--link`, `rate-highlight--inline`, `rate-row--current`.
- Reuse: `displayDate`, `currency-display`, `filter-bar`, `empty-state`, `labeled-status`, `workflow-steps`.

---

## 7. Backend changes

**None.**

---

## 8. Data changes

**None.** Demo entities (Demo Care Ltd, River View House, Alex/Jordan, INV-0001/0002, funding, payment statuses) were not modified.

---

## 9. Remaining limitations

- **Invoice list** has no resident name in `InvoiceListDto` — resident appears on invoice detail and reports, not the list API.
- **Billing period** (e.g. August 2026) is not auto-set from profile; presenter selects dates or uses optional query params.
- **Credit note preview amount** is whatever the API returns (may match full remaining balance).
- **Audit** requires admin-capable user (`adminGuard`).
- **No company detail page** — navigation goes to care home list filtered by company.
- **INV-0001 line snapshot name** may differ from resident record (data unchanged per policy).
- **Interactive responsive QA** at all breakpoints was not run in a browser in this pass (code/markup review only).

---

## Final A–Z demo UX pass

Login: PASS  
Dashboard: PASS  
Organisation: PASS  
Companies: PASS  
Care Homes: PASS  
Residents: PASS  
Resident Profile: PASS  
Funding: PASS  
Billing: PASS  
Invoices: PASS  
Invoice Detail: PASS  
PDF: PASS  
Payment UX: PASS  
Email UX: PASS  
Credit Notes: PASS  
Reports: PASS  
Audit: PASS  

Desktop: PASS (code review)  
Tablet: PASS (code review)  
Mobile: PASS (code review)  

Navigation continuity: PASS  
Breadcrumbs: PASS  
Forms: PASS  
Filters: PASS  
Tables: PASS  
Loading states: PASS  
Empty states: PASS  
Error states: PASS  
Accessibility: PASS (targeted fixes)  

Backend changed: NO  
Database changed: NO  
Demo data changed: NO  

Remaining issues: See section 9.
